using System.Text.Json;
namespace SmallestAgent.Functions;
public sealed record AgentResult(string Result, int NumTurns, long DurationMs, long InputTokens, long OutputTokens, string StopReason, bool IsError, decimal? TotalCostUsd = null);
public sealed record AgentMessage(string Type, string Summary, object Data);
public interface IAgentRunner {
 Task<AgentResult> QueryAsync(string prompt, CancellationToken cancellationToken);
 IAsyncEnumerable<AgentMessage> StreamAsync(string prompt, CancellationToken cancellationToken);
}
public static class Contracts {
 public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { PropertyNamingPolicy=JsonNamingPolicy.SnakeCaseLower };
 public const string MissingKey="Configure ANTHROPIC_API_KEY in backend/local.settings.json (local) or Azure application settings, then restart.";
 public const string Failure="Claude could not finish. Check your server API key, model access and connection, then try again.";
 public static string? ValidatePrompt(string? value) => value?.Trim() is {Length: >=1 and <=4000} prompt ? prompt : null;
}
