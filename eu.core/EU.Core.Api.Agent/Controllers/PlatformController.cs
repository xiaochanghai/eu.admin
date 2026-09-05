using EU.Core.Api.Agent.Configuration;
using EU.Core.Api.Agent.Health;
using EU.Core.IServices.Agents;
using EU.Core.Model.ViewModels.Extend;
using EU.Core.IServices.MainAgent;
using EU.Core.IServices.Skills;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Authorization;
using EU.Core.Api.Agent.Security;
using EU.Core.Model;
using EU.Core.IServices;

namespace EU.Core.Api.Agent.Controllers;

// 文件职责：PlatformController 接口处理

/// <summary>
/// 提供 Agent 平台能力和部署信息的 HTTP 接口。
/// </summary>
/// <param name="platform">Agent 平台能力配置。</param>
/// <param name="evaluation">Agent 评测功能配置。</param>
/// <param name="modelProfiles">用于查询可公开展示的模型配置的目录。</param>
/// <param name="mainAgentAssignments">用于查询和维护当前主 Agent 分配的服务。</param>
[Route("api/platform")]
[Authorize(Policy = AgentAuthorizationPolicies.AuditRead)]
public sealed class PlatformController(
    IOptions<AgentPlatformOptions> platform,
    IOptions<AgentEvaluationOptions> evaluation,
    IPublicModelProfileCatalog modelProfiles,
    IMainAgentAssignmentService mainAgentAssignments) : Base.ControllerBase
{
    #region 处理（Service）
    /// <summary>
    /// 处理（Service）
    /// </summary>
    /// <returns>服务结果，成功时包含Agent 宿主服务信息，失败时包含错误状态和提示。</returns>
    [HttpGet("service")]
    public ServiceResult<PlatformServiceResponse> Service() =>
        ServiceResult<PlatformServiceResponse>.QuerySuccess(new PlatformServiceResponse(
            platform.Value.ServiceName,
            ReplicaModeHealthCheck.ReplicaMode));
    #endregion

    #region 处理（Capabilities）
    /// <summary>
    /// 处理（Capabilities）
    /// </summary>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>服务结果，成功时包含Agent 宿主能力清单，失败时包含错误状态和提示。</returns>
    [HttpGet("capabilities")]
    public async Task<ServiceResult<PlatformCapabilitiesResponse>> Capabilities(CancellationToken cancellationToken)
    {
        bool mainAgent = (await mainAgentAssignments.GetAsync(cancellationToken)).Success;
        return ServiceResult<PlatformCapabilitiesResponse>.QuerySuccess(
            new PlatformCapabilitiesResponse(
                "sqlsugar",
                false,
                new PlatformDeploymentResponse(
                    AgentDefinition.ServerDeploymentTarget,
                    AgentDefinition.ApiHost),
                modelProfiles.ProfileIds,
                new PlatformFeatureResponse(
                    true, true, true, true, true, true,
                    evaluation.Value.EnableModelJudge,
                    mainAgent,
                    false)));
    }
    #endregion
}

/// <summary>
/// 平台服务信息响应。
/// </summary>
/// <param name="Service">服务名称。</param>
/// <param name="ReplicaMode">服务副本运行模式。</param>
public sealed record PlatformServiceResponse(string Service, string ReplicaMode);

/// <summary>
/// 平台能力响应。
/// </summary>
/// <param name="StorageMode">数据存储模式。</param>
/// <param name="Volatile">数据是否可能随实例重启丢失。</param>
/// <param name="Deployment">平台部署信息。</param>
/// <param name="ModelProfileIds">平台提供的模型配置标识集合。</param>
/// <param name="Features">平台功能能力。</param>
public sealed record PlatformCapabilitiesResponse(
    string StorageMode,
    bool Volatile,
    PlatformDeploymentResponse Deployment,
    IReadOnlyList<string> ModelProfileIds,
    PlatformFeatureResponse Features);

/// <summary>
/// 平台部署信息响应。
/// </summary>
/// <param name="Target">部署目标。</param>
/// <param name="Host">当前宿主名称。</param>
public sealed record PlatformDeploymentResponse(string Target, string Host);

/// <summary>
/// 平台功能开关响应。
/// </summary>
/// <param name="AgentControl">是否支持 Agent 定义管理。</param>
/// <param name="Runtime">是否支持 Agent 运行。</param>
/// <param name="Skills">是否支持技能管理与执行。</param>
/// <param name="Mcp">是否支持 MCP 服务与工具。</param>
/// <param name="Knowledge">是否支持知识库。</param>
/// <param name="Orchestration">是否支持编排。</param>
/// <param name="ModelJudge">是否支持模型裁判评测。</param>
/// <param name="MainAgent">是否支持主 Agent 配置。</param>
/// <param name="Schedules">是否支持计划任务。</param>
public sealed record PlatformFeatureResponse(
    bool AgentControl,
    bool Runtime,
    bool Skills,
    bool Mcp,
    bool Knowledge,
    bool Orchestration,
    bool ModelJudge,
    bool MainAgent,
    bool Schedules);
