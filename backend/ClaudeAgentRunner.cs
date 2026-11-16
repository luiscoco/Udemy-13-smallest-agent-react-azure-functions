using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Anthropic;
using Anthropic.Models.Messages;

namespace SmallestAgent.Functions;



// Official C# Messages SDK: one model turn, no tools and no CLI subprocess.
public sealed class ClaudeAgentRunner(AgentSettings settings, IHttpClientFactory clients) : IAgentRunner
{
    public async Task<AgentResult> QueryAsync(string prompt, CancellationToken cancellationToken)
    {
        AgentResult? result = null;
        await foreach (var message in StreamAsync(prompt, cancellationToken))
            if (message.Data is AgentResult final) result = final;
        return result ?? throw new InvalidOperationException("Claude did not return a final result.");
    }
    public async IAsyncEnumerable<AgentMessage> StreamAsync(string prompt, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (!settings.Configured) throw new InvalidOperationException(Contracts.MissingKey);
        using var client = new AnthropicClient { ApiKey = settings.ApiKey, AuthToken = null, BaseUrl = "https://api.anthropic.com", HttpClient = clients.CreateClient("Claude"), MaxRetries = 0, Timeout = TimeSpan.FromMinutes(3) };
        var parameters = new MessageCreateParams
        {
            Model = settings.Model, MaxTokens = settings.MaxTokens,
            Messages = [new UserMessageParam { Content = prompt }],
            System = settings.SystemPrompt, Tools = []
        };
        var timer = Stopwatch.StartNew();
        var answer = new StringBuilder();
        var pendingText = "";
        long inputTokens = 0, outputTokens = 0;
        var stopReason = "";
        var complete = false;
        yield return new("SystemMessage", "Request initialized", new { model = settings.Model, tools_enabled = false });
        await foreach (var chunk in client.Messages.CreateStreaming(parameters, cancellationToken: cancellationToken))
        {
            // Read the SDK's discriminated JSON union; never forward its raw object.
            var data = JsonSerializer.SerializeToElement(chunk);
            var type = data.GetProperty("type").GetString();
            if (type == "message_start" && data.TryGetProperty("message", out var start) && start.TryGetProperty("usage", out var usage))
                inputTokens = usage.TryGetProperty("input_tokens", out var count) ? count.GetInt64() : 0;
            if (type == "content_block_delta" && data.TryGetProperty("delta", out var delta) && delta.GetProperty("type").GetString() == "text_delta")
            {
                var text = delta.GetProperty("text").GetString() ?? "";
                answer.Append(text);
                var safeText = StreamText(ref pendingText, text);
                if (safeText.Length > 0) yield return new("TextDelta", "Answer text received", new { text = safeText });
            }
            if (type == "message_delta")
            {
                if (data.TryGetProperty("usage", out var usageDelta) && usageDelta.TryGetProperty("output_tokens", out var count)) outputTokens = count.GetInt64();
                if (data.TryGetProperty("delta", out var end) && end.TryGetProperty("stop_reason", out var reason)) stopReason = reason.GetString() ?? "";
            }
            if (type == "message_stop") complete = true;
        }
        if (!complete || string.IsNullOrEmpty(stopReason)) throw new InvalidOperationException("Claude stream ended before completion.");
        if (pendingText.Length > 0) yield return new("TextDelta", "Answer text received", new { text = pendingText });
        var result = new AgentResult(Redact(answer.ToString()), 1, timer.ElapsedMilliseconds, inputTokens, outputTokens,
            stopReason, stopReason is not ("end_turn" or "stop_sequence"));
        yield return new("ResultMessage", result.IsError ? "Response stopped early" : "Response complete", result);
    }
    private string Redact(string text) => text.Replace(settings.ApiKey, "[redacted]", StringComparison.Ordinal);
    private string StreamText(ref string pending, string next)
    {
        pending = Redact(pending + next);
        var retained = Math.Min(settings.ApiKey.Length - 1, pending.Length);
        while (retained > 0 && !pending.EndsWith(settings.ApiKey[..retained], StringComparison.Ordinal)) retained--;
        var safe = pending[..(pending.Length - retained)];
        pending = pending[(pending.Length - retained)..];
        return safe;
    }
}
