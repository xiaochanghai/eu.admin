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


/// <summary>
/// 提供 Agent 运行相关的 HTTP 接口。
/// </summary>
/// <param name="runtime">用于准备和启动 Agent 运行的服务。</param>
/// <param name="caller">提供当前调用方身份、租户及权限的上下文。</param>
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

    #region 运行（Run）
    /// <summary>
    /// 运行（Run）
    /// </summary>
    /// <param name="agentId">Agent 定义标识。</param>
    /// <param name="request">运行Agent 运行所需的请求参数。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>表示 SSE 事件流写入完成的异步任务，事件直接写入 HTTP 响应。</returns>
    [HttpPost("runs")]
    public async Task Run(Guid agentId, [FromBody] StartAgentRunRequest request, CancellationToken cancellationToken)
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
    #endregion

    #region 查询列表（List）
    /// <summary>
    /// 查询列表（List）
    /// </summary>
    /// <param name="agentId">Agent 定义标识。</param>
    /// <param name="take">最多返回的记录数。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>服务结果，成功时包含Agent 运行审计记录集合，失败时包含错误状态和提示。</returns>
    [HttpGet("runs")]
    public async Task<ServiceResult<IReadOnlyList<AgentRunAuditRecord>>> List(
        Guid agentId,
        [FromQuery] int take = 20,
        CancellationToken cancellationToken = default) =>
        ServiceResult<IReadOnlyList<AgentRunAuditRecord>>.QuerySuccess(
            await runtime.ListAuditAsync(agentId, take, cancellationToken));
    #endregion

    #region 写入（WriteFrameAsync）
    /// <summary>
    /// 写入（WriteFrameAsync）
    /// </summary>
    /// <param name="eventName">SSE 事件名称。</param>
    /// <param name="json">写入 SSE data 字段的已序列化 JSON 载荷。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>表示操作完成的异步任务。</returns>
    private async Task WriteFrameAsync(string eventName, string json, CancellationToken cancellationToken)
    {
        string frame = $"event: {eventName}\ndata: {json}\n\n";
        await Response.Body.WriteAsync(
            Encoding.UTF8.GetBytes(frame),
            cancellationToken);
        await Response.Body.FlushAsync(cancellationToken);
    }
    #endregion

    #region 转换（ToEventName）
    /// <summary>
    /// 转换（ToEventName）
    /// </summary>
    /// <param name="kind">记录或事件类型。</param>
    /// <returns>Agent 运行事件对应的 SSE 事件名；未单独映射的类型使用枚举名称的小写形式。</returns>
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
    #endregion
}

/// <summary>
/// 启动 Agent 运行的请求。
/// </summary>
/// <param name="Input">运行或评测使用的输入内容。</param>
public sealed record StartAgentRunRequest(string Input);
