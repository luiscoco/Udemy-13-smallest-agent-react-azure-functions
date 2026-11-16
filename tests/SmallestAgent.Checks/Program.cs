using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using SmallestAgent.Functions;

var checks=0;
void Check(bool condition,string name) { if(!condition) throw new Exception($"FAILED: {name}"); checks++; }
DefaultHttpContext Context(string body="{\"prompt\":\" Hi \"}",string media="application/json") {
 var context=new DefaultHttpContext();context.Request.Method="POST";context.Request.ContentType=media;
 context.Request.Body=new MemoryStream(Encoding.UTF8.GetBytes(body));context.Response.Body=new MemoryStream();return context;
}
string Body(DefaultHttpContext context) => Encoding.UTF8.GetString(((MemoryStream)context.Response.Body).ToArray());
var settings=new AgentSettings("sk-ant-test-only-never-sent","fixture-model",1024,"Teaching",["http://127.0.0.1:5400"]);
var runner=new FixtureRunner();var endpoints=new AgentEndpoints(settings,runner);
foreach(var item in new[]{("{",400),("{}",422),("[]",422),("{\"prompt\":123}",422),("{\"prompt\":\"  \"}",422),(JsonSerializer.Serialize(new{prompt=new string('x',4001)}),422),(JsonSerializer.Serialize(new{prompt=new string('x',33000)}),413)}) {
 var context=Context(item.Item1);await endpoints.QueryAsync(context,default);Check(context.Response.StatusCode==item.Item2,"Invalid body status");
}
Check(runner.Calls==0,"Validation precedes inference");
var wrong=Context("{}","application/json-extra");await endpoints.QueryAsync(wrong,default);Check(wrong.Response.StatusCode==415,"Exact media type");
var origin=Context();origin.Request.Headers.Origin="https://foreign.invalid";await endpoints.QueryAsync(origin,default);Check(origin.Response.StatusCode==403&&runner.Calls==0,"Origin rejection precedes SDK");
var preflight=Context();preflight.Request.Method="OPTIONS";preflight.Request.Headers.Origin=settings.AllowedOrigins[0];await endpoints.QueryAsync(preflight,default);Check(preflight.Response.StatusCode==204&&preflight.Response.Headers.AccessControlAllowOrigin==settings.AllowedOrigins[0],"Allowed preflight");
var missing=new AgentEndpoints(settings with{ApiKey="sk-ant-replace-with-your-key"},runner);
var query=Context();await missing.QueryAsync(query,default);Check(query.Response.StatusCode==503&&runner.Calls==0,"Missing key JSON");
var stream=Context();await missing.StreamAsync(stream,default);Check(Body(stream).Contains("event: error")&&!Body(stream).Contains("event: done")&&runner.Calls==0,"Missing key SSE");
query=Context();await endpoints.QueryAsync(query,default);Check(query.Response.StatusCode==200&&runner.Prompt=="Hi"&&Body(query).Contains("input_tokens"),"Trimmed query and real metrics contract");
stream=Context();await endpoints.StreamAsync(stream,default);Check(Body(stream).Contains("ResultMessage")&&Body(stream).Contains("event: done"),"Final plus done");
runner.Failure=true;stream=Context();await endpoints.StreamAsync(stream,default);Check(Body(stream).Contains("event: error")&&!Body(stream).Contains("event: done")&&!Body(stream).Contains("PRIVATE-EXCEPTION"),"Safe provider failure");
query=Context();await endpoints.QueryAsync(query,default);Check(query.Response.StatusCode==502&&!Body(query).Contains("PRIVATE-EXCEPTION"),"Safe query failure");
var config=new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>{{"AGENT_MAX_TOKENS","0"}}).Build();
try {AgentSettings.Load(config);throw new Exception("Accepted invalid token bound");}catch(InvalidOperationException){Check(true,"Token config bounds");}
Check(Contracts.ValidatePrompt("  Hello  ")=="Hello"&&Contracts.ValidatePrompt(" ")==null,"Prompt contract");
var sdkRunner=new ClaudeAgentRunner(settings,new FixtureClients(settings.ApiKey));
var messages=new List<AgentMessage>();await foreach(var message in sdkRunner.StreamAsync("hello",default))messages.Add(message);
var final=(AgentResult)messages.Last().Data;
Check(final.NumTurns==1&&final.InputTokens==7&&final.OutputTokens==9&&final.TotalCostUsd==null,"Official SDK fixture usage");
Check(final.Result=="Hi [redacted]!"&&!JsonSerializer.Serialize(messages,Contracts.Json).Contains(settings.ApiKey),"Redaction across SDK delta boundaries");
Check(messages.Any(m=>m.Type=="TextDelta")&&final.StopReason=="end_turn"&&!final.IsError,"SDK stream completion projection");
var truncatedRunner=new ClaudeAgentRunner(settings,new FixtureClients(settings.ApiKey,"max_tokens"));
Check((await truncatedRunner.QueryAsync("hello",default)).IsError,"Truncated completion remains error");
using var cts=new CancellationTokenSource();cts.Cancel();
try {await sdkRunner.QueryAsync("cancel",cts.Token);throw new Exception("Accepted cancelled request");}catch(OperationCanceledException){Check(true,"SDK receives cancellation");}
using var openapi=JsonDocument.Parse(ApiDocs.OpenApi);Check(openapi.RootElement.GetProperty("paths").TryGetProperty("/api/agent/stream",out _),"OpenAPI parses");
Console.WriteLine($"{checks} C# endpoint and official SDK fixture checks passed; no live model request.");

sealed class FixtureRunner:IAgentRunner {
 public int Calls;public string Prompt="";public bool Failure;
 public Task<AgentResult> QueryAsync(string prompt,CancellationToken token){Calls++;Prompt=prompt;if(Failure)throw new Exception("PRIVATE-EXCEPTION");return Task.FromResult(new AgentResult("Hello",1,4,7,9,"end_turn",false));}
 public async IAsyncEnumerable<AgentMessage> StreamAsync(string prompt,[EnumeratorCancellation]CancellationToken token){var result=await QueryAsync(prompt,token);yield return new("ResultMessage","Complete",result);}
}
sealed class FixtureClients(string key,string reason="end_turn"):IHttpClientFactory {
 public HttpClient CreateClient(string name)=>new(new FixtureHandler(key,reason));
}
sealed class FixtureHandler(string key,string reason):HttpMessageHandler {
 protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token) {
  token.ThrowIfCancellationRequested();
  if(request.RequestUri?.Host!="api.anthropic.com")throw new Exception("Unexpected provider URL");
  var frames=new List<object>{new{type="message_start",message=new{id="fixture",type="message",role="assistant",model="fixture-model",content=Array.Empty<object>(),stop_reason=(string?)null,stop_sequence=(string?)null,usage=new{input_tokens=7,output_tokens=0}}},new{type="content_block_start",index=0,content_block=new{type="text",text=""}}};
  foreach(var text in new[]{"Hi ",key[..8],key[8..],"!"})frames.Add(new{type="content_block_delta",index=0,delta=new{type="text_delta",text}});
  frames.Add(new{type="content_block_stop",index=0});frames.Add(new{type="message_delta",delta=new{stop_reason=reason,stop_sequence=(string?)null},usage=new{output_tokens=9}});frames.Add(new{type="message_stop"});
  var body=string.Join("",frames.Select(frame=>$"event: {JsonSerializer.SerializeToElement(frame).GetProperty("type").GetString()}\ndata: {JsonSerializer.Serialize(frame)}\n\n"));
  return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK){Content=new StringContent(body,Encoding.UTF8,"text/event-stream")});
 }
}
