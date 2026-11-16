namespace SmallestAgent.Functions;
public static class ApiDocs
{
 public const string Html = """
 <!doctype html><html lang="en"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
 <title>Smallest Agent API · Azure Functions</title><style>body{font:21px/1.7 system-ui;max-width:960px;margin:3rem auto;padding:1rem;background:#f4f8ff;color:#142547}section{background:#eaf7f0;padding:1.5rem;border-radius:20px;margin:1rem 0}code,pre{background:#eaf0ff;padding:.3rem;white-space:pre-wrap}a{color:#0865cf}</style>
 <h1>The Smallest Agent API</h1><p>React · Azure Functions isolated worker · .NET 10 · Anthropic C# Messages SDK.</p>
 <p>Custom reference page. <a href="openapi.json">OpenAPI JSON</a></p>
 <section><h2>GET /api/health</h2><p>Returns status and api_key_configured. Key presence does not prove authentication, model access or credits.</p></section>
 <section><h2>POST /api/agent/query</h2><pre>Content-Type: application/json
 {"prompt":"Hello world!"}</pre><p>Prompt: 1–4000 characters after trimming; body limit: 32 KiB. Response: result, num_turns=1, duration_ms, input_tokens, output_tokens, stop_reason, is_error, total_cost_usd=null.</p></section>
 <section><h2>POST /api/agent/stream</h2><p>Same JSON input. SSE message events carry safe SystemMessage, TextDelta and ResultMessage application projections. Completion requires final result plus done. SSE error ends without done. These are application events over the C# Messages SDK; no CLI-backed Agent SDK runs here.</p></section>
 <section><h2>Errors and hosting</h2><p>400 invalid JSON; 403 forbidden Origin; 413 body too large; 415 wrong media; 422 invalid prompt; query 503 missing key / 502 provider failure. Stream uses error events after headers. The application deadline is 180 seconds. Credentials stay in server settings. Azure deployment enables Entra authentication separately from these anonymous HTTP trigger declarations.</p></section></html>
 """;
 public const string OpenApi = """
 {"openapi":"3.0.3","info":{"title":"Smallest Agent Azure Functions","version":"1.0.0"},"paths":{
 "/api/health":{"get":{"responses":{"200":{"description":"Connectivity and key presence"}}}},
 "/api/agent/query":{"post":{"requestBody":{"required":true,"content":{"application/json":{"schema":{"$ref":"#/components/schemas/Prompt"}}}},"responses":{"200":{"description":"One-turn result with token usage; cost unavailable"},"400":{"description":"Malformed JSON"},"403":{"description":"Origin forbidden"},"413":{"description":"Body over 32 KiB"},"415":{"description":"Use application/json"},"422":{"description":"Invalid trimmed prompt"},"503":{"description":"Missing key"},"502":{"description":"Provider failure"}}}},
 "/api/agent/stream":{"post":{"requestBody":{"required":true,"content":{"application/json":{"schema":{"$ref":"#/components/schemas/Prompt"}}}},"responses":{"200":{"description":"SSE message, done or error application events","content":{"text/event-stream":{"schema":{"type":"string"}}}},"400":{"description":"Malformed JSON"},"403":{"description":"Origin forbidden"},"413":{"description":"Body too large"},"415":{"description":"Wrong media"},"422":{"description":"Invalid prompt"}}}}},"components":{"schemas":{"Prompt":{"type":"object","required":["prompt"],"properties":{"prompt":{"type":"string","minLength":1,"maxLength":4000,"description":"Trimmed by server before validation"}}}}}}
 """;
}
