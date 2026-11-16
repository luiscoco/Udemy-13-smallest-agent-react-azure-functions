using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace SmallestAgent.Functions;

// Transport logic is independently testable; real HTTP triggers delegate here.
public sealed class AgentEndpoints(AgentSettings settings, IAgentRunner runner)
{
    public async Task<bool> CheckOriginAsync(HttpContext context)
    {
        var origin = context.Request.Headers.Origin.ToString();
        if (origin.Length > 0 && !settings.AllowedOrigins.Contains(origin, StringComparer.Ordinal))
        {
            await ErrorAsync(context, 403, "Origin is not allowed.");
            return false;
        }
        if (origin.Length > 0)
        {
            context.Response.Headers.AccessControlAllowOrigin = origin;
            context.Response.Headers.Vary = "Origin";
        }
        if (HttpMethods.IsOptions(context.Request.Method))
        {
            context.Response.Headers.AccessControlAllowMethods = "GET, POST, OPTIONS";
            context.Response.Headers.AccessControlAllowHeaders = "Content-Type, Authorization";
            context.Response.StatusCode = 204;
            return false;
        }
        return true;
    }

    public static async Task ErrorAsync(HttpContext context, int status, string detail)
    {
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/json; charset=utf-8";
        await JsonSerializer.SerializeAsync(context.Response.Body, new { detail }, Contracts.Json, context.RequestAborted);
    }

    public static async Task<string?> ReadPromptAsync(HttpContext context)
    {
        var mediaType = context.Request.ContentType?.Split(';')[0].Trim();
        if (!string.Equals(mediaType, "application/json", StringComparison.OrdinalIgnoreCase))
        {
            await ErrorAsync(context, 415, "Use Content-Type: application/json."); return null;
        }
        using var buffer = new MemoryStream();
        var bytes = new byte[4096];
        int count;
        while ((count = await context.Request.Body.ReadAsync(bytes, context.RequestAborted)) > 0)
        {
            if (buffer.Length + count > 32768)
            {
                await ErrorAsync(context, 413, "Request body is too large."); return null;
            }
            await buffer.WriteAsync(bytes.AsMemory(0, count), context.RequestAborted);
        }
        try
        {
            using var json = JsonDocument.Parse(buffer.ToArray());
            var root = json.RootElement;
            var prompt = root.ValueKind == JsonValueKind.Object && root.TryGetProperty("prompt", out var value) && value.ValueKind == JsonValueKind.String
                ? Contracts.ValidatePrompt(value.GetString()) : null;
            if (prompt is not null) return prompt;
            await ErrorAsync(context, 422, "Prompt must contain 1–4000 characters after trimming.");
        }
        catch (JsonException) { await ErrorAsync(context, 400, "Request must contain valid JSON."); }
        return null;
    }

    public async Task QueryAsync(HttpContext context, CancellationToken invocationToken)
    {
        if (!await CheckOriginAsync(context)) return;
        var prompt = await ReadPromptAsync(context);
        if (prompt is null) return;
        if (!settings.Configured) { await ErrorAsync(context, 503, Contracts.MissingKey); return; }
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(context.RequestAborted, invocationToken);
        deadline.CancelAfter(TimeSpan.FromSeconds(180));
        try
        {
            var result = await runner.QueryAsync(prompt, deadline.Token);
            context.Response.ContentType = "application/json; charset=utf-8";
            await JsonSerializer.SerializeAsync(context.Response.Body, result, Contracts.Json, deadline.Token);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested || invocationToken.IsCancellationRequested) { }
        catch (Exception) when (!context.Response.HasStarted) { await ErrorAsync(context, 502, Contracts.Failure); }
    }

    public async Task StreamAsync(HttpContext context, CancellationToken invocationToken)
    {
        if (!await CheckOriginAsync(context)) return;
        var prompt = await ReadPromptAsync(context);
        if (prompt is null) return;
        context.Response.ContentType = "text/event-stream";
        context.Response.Headers.CacheControl = "no-cache, no-transform";
        context.Response.Headers["X-Accel-Buffering"] = "no";
        context.Features.Get<IHttpResponseBodyFeature>()?.DisableBuffering();
        await context.Response.StartAsync(context.RequestAborted);
        async Task SendEvent(string name, object data) {
            await context.Response.WriteAsync($"event: {name}\ndata: {JsonSerializer.Serialize(data, Contracts.Json)}\n\n", context.RequestAborted);
            await context.Response.Body.FlushAsync(context.RequestAborted);
        }
        if (!settings.Configured) { await SendEvent("error", new { detail = Contracts.MissingKey }); return; }
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(context.RequestAborted, invocationToken);
        deadline.CancelAfter(TimeSpan.FromSeconds(180));
        var enumerator = runner.StreamAsync(prompt, deadline.Token).GetAsyncEnumerator(deadline.Token);
        Task<bool>? pending = null;
        var foundResult = false;
        try
        {
            while (true)
            {
                pending ??= enumerator.MoveNextAsync().AsTask();
                using var heartbeat = CancellationTokenSource.CreateLinkedTokenSource(deadline.Token);
                var pulse = Task.Delay(TimeSpan.FromSeconds(15), heartbeat.Token);
                await Task.WhenAny(pending, pulse);
                deadline.Token.ThrowIfCancellationRequested();
                if (!pending.IsCompleted)
                {
                    await context.Response.WriteAsync(": keep-alive\n\n", context.RequestAborted);
                    await context.Response.Body.FlushAsync(context.RequestAborted);
                    continue;
                }
                await heartbeat.CancelAsync();
                var hasNext = await pending;
                pending = null;
                if (!hasNext) break;
                var message = enumerator.Current;
                foundResult |= message.Type == "ResultMessage" && message.Data is AgentResult;
                await SendEvent("message", message);
            }
            if (!foundResult) throw new InvalidOperationException("Missing result.");
            await SendEvent("done", new { status = "complete" });
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested || invocationToken.IsCancellationRequested) { }
        catch (Exception)
        {
            if (!context.RequestAborted.IsCancellationRequested && !invocationToken.IsCancellationRequested)
                await SendEvent("error", new { detail = deadline.IsCancellationRequested ? "The request timed out. Try a shorter prompt." : Contracts.Failure });
        }
        finally
        {
            await deadline.CancelAsync();
            if (pending is not null) { try { await pending; } catch { /* Observe owned iterator cancellation. */ } }
            await enumerator.DisposeAsync();
        }
    }
}
