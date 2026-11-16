using Microsoft.Extensions.Configuration;
namespace SmallestAgent.Functions;
public sealed record AgentSettings(string ApiKey, string Model, int MaxTokens, string SystemPrompt, string[] AllowedOrigins) {
 public bool Configured => !string.IsNullOrWhiteSpace(ApiKey) && !ApiKey.StartsWith("sk-ant-replace", StringComparison.Ordinal) && !ApiKey.StartsWith('@') && ApiKey!="your_actual_api_key";
 public static AgentSettings Load(IConfiguration configuration, string? contentRoot = null) {
  var file = new Dictionary<string,string>(StringComparer.Ordinal);
  var path = contentRoot is null ? null : Path.Combine(contentRoot,".env");
  if(path is not null && File.Exists(path)) foreach(var raw in File.ReadLines(path)) {
   var line=raw.Trim(); if(line.Length==0||line.StartsWith('#'))continue;
   var index=line.IndexOf('='); if(index>0)file[line[..index].Trim()]=line[(index+1)..].Trim().Trim('"','\'');
  }
  string Read(string name,string fallback="") => configuration[name] ?? file.GetValueOrDefault(name,fallback);
  if(!int.TryParse(Read("AGENT_MAX_TOKENS","1024"),out var tokens)||tokens is <1 or >8192) throw new InvalidOperationException("AGENT_MAX_TOKENS must be an integer from 1 to 8192.");
  return new(Read("ANTHROPIC_API_KEY").Trim(),Read("CLAUDE_MODEL","claude-sonnet-5-5"),tokens,Read("AGENT_SYSTEM_PROMPT","You are a concise teaching assistant."),Read("ALLOWED_ORIGINS").Split(',',StringSplitOptions.TrimEntries|StringSplitOptions.RemoveEmptyEntries));
 }
}
