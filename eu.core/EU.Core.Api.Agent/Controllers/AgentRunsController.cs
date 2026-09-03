using EU.Core.Api.Agent.Errors;
using EU.Core.Api.Agent.Security;
using EU.Core.IServices;
using EU.Core.IServices.Abstractions.Security;
using EU.Core.IServices.Runtime;
using EU.Core.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EU.Core.Api.Agent.Controllers;

[Route("api/agents/{agentId:guid}")]
[Authorize(Policy = AgentAuthorizationPolicies.Debug)]
public sealed class AgentRunsController(
    IAgentRuntimeService runtime,
    ICallerContext caller) : Base.ControllerBase
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
            await AgentApiErrorResponseWriter.WriteAsync(
                HttpContext,
                error.Code,
                "The Agent run could not be started.",
                cancellationToken: cancellationToken);
            return;
        }

        AgentRunContext context = preparation.Context! with
        {
            ExecutionIdentity = new AgentExecutionIdentity(
                caller.UserId,
                caller.TenantId,
                caller.Permissions,
                caller.CorrelationId)
        };

        Response.StatusCode = StatusCodes.Status200OK;
        Response.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache, no-store";
        Response.Headers.Append("X-Accel-Buffering", "no");
        await Response.StartAsync(cancellationToken);
        await foreach (AgentRunEvent value in runtime
            .StreamAsync(context, cancellationToken)
            .WithCancellation(cancellationToken))
        {
            string eventName = ToEventName(value.Kind);
            string json = JsonSerializer.Serialize(value, SerializerOptions);
            await WriteFrameAsync(eventName, json, cancellationToken);
        }
    }

    [HttpGet("runs")]
    public async Task<ServiceResult<IReadOnlyList<AgentRunAuditRecord>>> List(
        Guid agentId,
        [FromQuery] int take = 20,
        CancellationToken cancellationToken = default) =>
        ServiceResult<IReadOnlyList<AgentRunAuditRecord>>.QuerySuccess(
            await runtime.ListAuditAsync(agentId, take, cancellationToken));

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
            AgentRunEventKind.SkillStarted => "skill-started",
            AgentRunEventKind.KnowledgeRetrieved => "knowledge-retrieved",
            AgentRunEventKind.ToolStarted => "tool-started",
            AgentRunEventKind.Citation => "citation",
            AgentRunEventKind.ToolSucceeded => "tool-succeeded",
            AgentRunEventKind.ToolBlocked => "tool-blocked",
            AgentRunEventKind.ToolFailed => "tool-failed",
            _ => kind.ToString().ToLowerInvariant()
        };
}

public sealed record StartAgentRunRequest(string Input);
