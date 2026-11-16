using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace SmallestAgent.Functions;

public sealed class AgentFunctions(AgentEndpoints endpoints, AgentSettings settings)
{
    [Function("Health")]
    public async Task<IActionResult> Health([HttpTrigger(AuthorizationLevel.Anonymous, "get", "options", Route = "health")] HttpRequest request)
    {
        if (await endpoints.CheckOriginAsync(request.HttpContext)) {
            request.HttpContext.Response.ContentType = "application/json";
            await JsonSerializer.SerializeAsync(request.HttpContext.Response.Body, new { status="ok", api_key_configured=settings.Configured }, Contracts.Json, request.HttpContext.RequestAborted);
        }
        return new EmptyResult();
    }

    [Function("AgentQuery")]
    public async Task<IActionResult> Query([HttpTrigger(AuthorizationLevel.Anonymous, "post", "options", Route="agent/query")] HttpRequest request, CancellationToken cancellationToken)
    {
        await endpoints.QueryAsync(request.HttpContext, cancellationToken);
        return new EmptyResult();
    }

    [Function("AgentStream")]
    public async Task<IActionResult> Stream([HttpTrigger(AuthorizationLevel.Anonymous, "post", "options", Route="agent/stream")] HttpRequest request, CancellationToken cancellationToken)
    {
        await endpoints.StreamAsync(request.HttpContext, cancellationToken);
        return new EmptyResult();
    }

    [Function("ApiDocs")]
    public async Task<IActionResult> Docs([HttpTrigger(AuthorizationLevel.Anonymous, "get", "options", Route="docs")] HttpRequest request)
    {
        if(await endpoints.CheckOriginAsync(request.HttpContext)) {
            request.HttpContext.Response.ContentType="text/html; charset=utf-8";
            await request.HttpContext.Response.WriteAsync(ApiDocs.Html, request.HttpContext.RequestAborted);
        }
        return new EmptyResult();
    }

    [Function("OpenApi")]
    public async Task<IActionResult> OpenApi([HttpTrigger(AuthorizationLevel.Anonymous, "get", "options", Route="openapi.json")] HttpRequest request)
    {
        if(await endpoints.CheckOriginAsync(request.HttpContext)) {
            request.HttpContext.Response.ContentType="application/json";
            await request.HttpContext.Response.WriteAsync(ApiDocs.OpenApi, request.HttpContext.RequestAborted);
        }
        return new EmptyResult();
    }
}
