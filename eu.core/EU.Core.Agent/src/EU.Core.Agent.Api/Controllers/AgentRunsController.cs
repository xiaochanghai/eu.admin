using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using EU.Core.Agent.Application.Runtime;
using Microsoft.AspNetCore.Mvc;

namespace EU.Core.Agent.Api.Controllers;

[ApiController]
[Route("api/agents/{agentId:guid}")]
public sealed class AgentRunsController(
    AgentRuntimeService runtime) : ControllerBase
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    [HttpPost("runs")]
    public async Task Run(
        Guid agentId,
        [FromBody] StartAgentRunRequest request,
        CancellationToken cancellationToken)
    {
        AgentRunPreparationResult preparation = await runtime.PrepareAsync(
            agentId,
            request.Input,
            cancellationToken);
        if (!preparation.Succeeded)
        {
            AgentRunError error = preparation.Error!;
            int status = error.Code == AgentRunErrorCodes.AgentNotFound
                ? StatusCodes.Status404NotFound
                : error.Code == AgentRunErrorCodes.AgentDisabled
                    ? StatusCodes.Status409Conflict
                    : StatusCodes.Status400BadRequest;
            HttpContext.Response.StatusCode = status;
            HttpContext.Response.ContentType = "application/problem+json";
            await HttpContext.Response.WriteAsJsonAsync(
                new
                {
                    title = "The Agent run could not be started.",
                    status,
                    errorCode = error.Code,
                    detail = error.Message
                },
                cancellationToken);
            return;
        }

        Response.StatusCode = StatusCodes.Status200OK;
        Response.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache, no-store";
        Response.Headers.Append("X-Accel-Buffering", "no");
        await Response.StartAsync(cancellationToken);
        await foreach (AgentRunEvent value in runtime
            .StreamAsync(preparation.Context!, cancellationToken)
            .WithCancellation(cancellationToken))
        {
            string eventName = ToEventName(value.Kind);
            string json = JsonSerializer.Serialize(value, SerializerOptions);
            await WriteFrameAsync(eventName, json, cancellationToken);
        }
    }

    [HttpGet("runs")]
    public async Task<IActionResult> List(
        Guid agentId,
        [FromQuery] int take = 20,
        CancellationToken cancellationToken = default) =>
        Ok(await runtime.ListAuditAsync(agentId, take, cancellationToken));

    private async Task WriteFrameAsync(
        string eventName,
        string json,
        CancellationToken cancellationToken)
    {
        string frame = $"event: {eventName}\ndata: {json}\n\n";
        await Response.Body.WriteAsync(
            Encoding.UTF8.GetBytes(frame),
            cancellationToken);
        await Response.Body.FlushAsync(cancellationToken);
    }

    private static string ToEventName(AgentRunEventKind kind) =>
        kind switch
        {
            AgentRunEventKind.ToolStarted => "tool-started",
            AgentRunEventKind.Citation => "citation",
            AgentRunEventKind.ToolSucceeded => "tool-succeeded",
            AgentRunEventKind.ToolBlocked => "tool-blocked",
            AgentRunEventKind.ToolFailed => "tool-failed",
            _ => kind.ToString().ToLowerInvariant()
        };
}

public sealed record StartAgentRunRequest(string Input);
