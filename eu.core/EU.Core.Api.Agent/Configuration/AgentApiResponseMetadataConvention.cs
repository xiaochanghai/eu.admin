using EU.Core.IServices.Abstractions.Auditing;
using EU.Core.IServices.Agents;
using EU.Core.IServices.Approvals;
using EU.Core.IServices.Evaluation;
using EU.Core.IServices.Knowledge;
using EU.Core.IServices.MainAgent;
using EU.Core.IServices.Mcp;
using EU.Core.IServices.Orchestration;
using EU.Core.IServices.Runtime;
using EU.Core.IServices.Skills;
using EU.Core.IServices.Tasks;
using EU.Core.IServices.UnifiedEntry;
using EU.Core.Api.Agent.Controllers;
using EU.Core.Model;
using EU.Core.Model.Models;
using EU.Core.Model.ViewModels.Extend;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace EU.Core.Api.Agent.Configuration;

/// <summary>
/// 为 Agent API Action 生成统一响应包装及特殊协议的 OpenAPI 元数据。
/// </summary>
public sealed class AgentApiResponseMetadataConvention : IApplicationModelConvention
{
    private static readonly IReadOnlyDictionary<string, SpecialResponse> SpecialResponses =
        new Dictionary<string, SpecialResponse>(StringComparer.Ordinal)
        {
            ["AgentRunsController.Run"] = new("text/event-stream", typeof(void)),
            ["ChatRunsController.Start"] = new("text/event-stream", typeof(void)),
            ["AgentsController.Export"] = new("application/json", typeof(byte[])),
            ["SkillsController.ReadFile"] = new("text/plain", typeof(string)),
            ["MetricsController.Get"] = new("text/plain", typeof(string), IncludeDefaultError: false)
        };

    private static readonly IReadOnlyDictionary<string, ServiceResponse> ServiceResponses =
        new Dictionary<string, ServiceResponse>(StringComparer.Ordinal)
        {
            ["AuditController.List"] = Response<IReadOnlyList<AgentOperationAuditRecord>>(),
            ["BusinessQueryRetentionController.Cleanup"] = Response<BusinessQueryCleanupResult>(),
            ["AgentRunsController.List"] = Response<IReadOnlyList<AgentRunAuditRecord>>(),
            ["AgentTasksController.Create"] = Response<AgentTaskRecord>(),
            ["AgentTasksController.List"] = Response<IReadOnlyList<AgentTaskRecord>>(),
            ["AgentTasksController.Get"] = Response<AgentTaskDetailResponse>(),
            ["AgentTasksController.ClaimNext"] = Response<AgentTaskRecord>(),
            ["AgentTasksController.Checkpoint"] = Response<AgentTaskRecord>(),
            ["AgentTasksController.RenewLease"] = Response<AgentTaskRecord>(),
            ["AgentTasksController.Complete"] = Response<AgentTaskRecord>(),
            ["AgentTasksController.Fail"] = Response<AgentTaskRecord>(),
            ["AgentTasksController.Cancel"] = Response<AgentTaskRecord>(),
            ["AgentTasksController.ResumeWithUserInput"] = Response<AgentTaskRecord>(),
            ["AgentsController.List"] = Response<AgentListItem[]>(),
            ["AgentsController.Get"] = Response<AgAgentDefinitionDetailDto>(),
            ["AgentsController.Create"] = Response<AgAgentDefinitionDetailDto>(StatusCodes.Status201Created),
            ["AgentsController.SaveDraft"] = Response<AgentDefinition>(),
            ["AgentsController.Publish"] = Response<AgentDefinition>(),
            ["AgentsController.SetStatus"] = Response<AgentDefinition>(),
            ["AgentsController.Import"] = Response<AgentDefinition>(StatusCodes.Status201Created),
            ["ChatRunsController.ListConversations"] = Response<IReadOnlyList<ConversationRecord>>(),
            ["ChatRunsController.GetConversation"] = Response<ChatConversationDetailResponse>(),
            ["ChatRunsController.ListRuns"] = Response<IReadOnlyList<UnifiedEntryRunRecord>>(),
            ["ChatRunsController.GetRun"] = Response<UnifiedEntryRunRecord>(),
            ["ChatRunsController.GetDetails"] = Response<UnifiedRunDetails>(),
            ["ChatRunsController.GetEvents"] = Response<IReadOnlyList<UnifiedRunEventRecord>>(),
            ["ChatRunsController.Cancel"] = Response<ChatRunCancelResponse>(StatusCodes.Status202Accepted),
            ["MainAgentController.Get"] = Response<MainAgentAssignment>(),
            ["MainAgentController.Set"] = Response<MainAgentAssignment>(),
            ["McpServersController.List"] = Response<IReadOnlyList<McpServerDefinition>>(),
            ["McpServersController.Get"] = Response<McpServerDefinition>(),
            ["McpServersController.Create"] = Response<McpServerDefinition>(StatusCodes.Status201Created),
            ["McpServersController.Update"] = Response<McpServerDefinition>(),
            ["McpServersController.Sync"] = Response<McpServerDefinition>(),
            ["McpServersController.SetArchived"] = Response<McpServerDefinition>(),
            ["McpServersController.ClassifyTool"] = Response<McpServerDefinition>(),
            ["McpToolVersionsController.List"] = Response<IReadOnlyList<PublishedMcpToolReference>>(),
            ["PlatformController.Service"] = Response<PlatformServiceResponse>(),
            ["PlatformController.Capabilities"] = Response<PlatformCapabilitiesResponse>(),
            ["EvaluationSuitesController.List"] = Response<IReadOnlyList<EvaluationSuiteDefinition>>(),
            ["EvaluationSuitesController.Get"] = Response<EvaluationSuiteDefinition>(),
            ["EvaluationSuitesController.Create"] = Response<EvaluationSuiteDefinition>(StatusCodes.Status201Created),
            ["EvaluationSuitesController.SaveDraft"] = Response<EvaluationSuiteDefinition>(),
            ["EvaluationSuitesController.Publish"] = Response<EvaluationSuiteDefinition>(),
            ["EvaluationSuitesController.SetArchived"] = Response<EvaluationSuiteDefinition>(),
            ["EvaluationBatchesController.Run"] = Response<EvaluationBatchRecord>(),
            ["EvaluationBatchesController.Compare"] = Response<EvaluationBatchComparisonReport>(),
            ["EvaluationBatchesController.List"] = Response<IReadOnlyList<EvaluationBatchRecord>>(),
            ["EvaluationBatchesController.Get"] = Response<EvaluationBatchRecord>(),
            ["EvaluationBatchesController.RunModelJudge"] = Response<ModelJudgeReport>(),
            ["EvaluationBatchesController.ListModelJudgeReports"] = Response<IReadOnlyList<ModelJudgeReport>>(),
            ["EvaluationBatchesController.GetModelJudgeReport"] = Response<ModelJudgeReport>(),
            ["KnowledgeBaseReferencesController.List"] = Response<IReadOnlyList<PublishedKnowledgeReference>>(),
            ["RunEvaluationsController.Evaluate"] = Response<RunEvaluationReport>(),
            ["ToolApprovalsController.List"] = Response<IReadOnlyList<ToolApprovalRequestRecord>>(),
            ["ToolApprovalsController.Get"] = Response<ToolApprovalDetailResponse>(),
            ["ToolApprovalsController.Approve"] = Response<ToolApprovalRequestRecord>(),
            ["ToolApprovalsController.Reject"] = Response<ToolApprovalRequestRecord>(),
            ["ToolApprovalsController.Cancel"] = Response<ToolApprovalRequestRecord>(),
            ["ToolApprovalsController.Resume"] = Response<ToolApprovalConversationResumeResult>(),
            ["OrchestrationsController.List"] = Response<IReadOnlyList<OrchestrationDefinition>>(),
            ["OrchestrationsController.Get"] = Response<OrchestrationDefinition>(),
            ["OrchestrationsController.Create"] = Response<OrchestrationDefinition>(StatusCodes.Status201Created),
            ["OrchestrationsController.SaveDraft"] = Response<OrchestrationDefinition>(),
            ["OrchestrationsController.Publish"] = Response<OrchestrationDefinition>(),
            ["OrchestrationsController.SetArchived"] = Response<OrchestrationDefinition>(),
            ["OrchestrationsController.Start"] = Response<OrchestrationRunRecord>(StatusCodes.Status202Accepted),
            ["OrchestrationsController.Runs"] = Response<IReadOnlyList<OrchestrationRunRecord>>(),
            ["OrchestrationsController.Run"] = Response<OrchestrationRunRecord>(),
            ["OrchestrationsController.Cancel"] = Response<OrchestrationRunCancelResponse>(StatusCodes.Status202Accepted),
            ["OrchestrationsController.Details"] = Response<OrchestrationRunDetails>(),
            ["OrchestrationsController.Output"] = Response<OrchestrationRunOutputResponse>(),
            ["SkillsController.List"] = Response<IReadOnlyList<SkillListItem>>(),
            ["SkillsController.Get"] = Response<SkillDefinitionDetailResponse>(),
            ["SkillsController.Create"] = Response<SkillDefinition>(StatusCodes.Status201Created),
            ["SkillsController.Update"] = Response<SkillDefinition>(),
            ["SkillsController.ListFiles"] = Response<IReadOnlyList<SkillFileEntry>>(),
            ["SkillsController.SaveFile"] = Response<SkillDefinition>(),
            ["SkillsController.DeleteFile"] = Response<SkillDefinition>(),
            ["SkillsController.Publish"] = Response<SkillDefinition>(),
            ["SkillsController.SetArchived"] = Response<SkillDefinition>(),
            ["SkillVersionsController.List"] = Response<IReadOnlyList<PublishedSkillReference>>()
        };

    public void Apply(ApplicationModel application)
    {
        ArgumentNullException.ThrowIfNull(application);
        foreach (ControllerModel controller in application.Controllers.Where(IsAgentController))
        {
            foreach (ActionModel action in controller.Actions)
            {
                string key = $"{controller.ControllerType.Name}.{action.ActionMethod.Name}";
                if (SpecialResponses.TryGetValue(key, out SpecialResponse? special))
                {
                    string[] additionalContentTypes = special.IncludeDefaultError
                        && !string.Equals(
                            special.ContentType,
                            "application/json",
                            StringComparison.Ordinal)
                            ? ["application/json"]
                            : [];
                    action.Filters.Add(new ProducesAttribute(
                        special.ContentType,
                        additionalContentTypes));
                    action.Filters.Add(new ProducesResponseTypeAttribute(
                        special.ResponseType,
                        StatusCodes.Status200OK,
                        special.ContentType));
                    if (special.IncludeDefaultError)
                        AddDefaultError(action);
                    continue;
                }

                ServiceResponses.TryGetValue(key, out ServiceResponse? response);
                response ??= InferServiceResponse(action.ActionMethod.ReturnType);
                if (response is null)
                {
                    throw new InvalidOperationException(
                        $"Agent API Action '{key}' does not declare its ServiceResult response type.");
                }
                action.Filters.Add(new ProducesAttribute("application/json"));
                action.Filters.Add(new ProducesResponseTypeAttribute(
                    typeof(ServiceResult<>).MakeGenericType(response.DataType),
                    response.HttpStatus,
                    "application/json"));
                if (string.Equals(
                        key,
                        "OrchestrationsController.Output",
                        StringComparison.Ordinal))
                {
                    action.Filters.Add(new ProducesResponseTypeAttribute(
                        StatusCodes.Status204NoContent));
                }
                AddDefaultError(action);
            }
        }
    }

    public static IReadOnlyCollection<string> SpecialActionKeys =>
        SpecialResponses.Keys.ToArray();

    public static IReadOnlyDictionary<string, Type> ServiceActionDataTypes =>
        ServiceResponses.ToDictionary(item => item.Key, item => item.Value.DataType);

    private static bool IsAgentController(ControllerModel controller) =>
        string.Equals(
            controller.ControllerType.Namespace,
            "EU.Core.Api.Agent.Controllers",
            StringComparison.Ordinal);

    private static void AddDefaultError(ActionModel action) =>
        action.Filters.Add(new ProducesDefaultResponseTypeAttribute(
            typeof(ServiceResult<AgentApiErrorData>)));

    private static ServiceResponse? InferServiceResponse(Type returnType)
    {
        while (returnType.IsGenericType)
        {
            Type genericType = returnType.GetGenericTypeDefinition();
            if (genericType == typeof(Task<>)
                || genericType == typeof(ValueTask<>)
                || genericType == typeof(ActionResult<>))
            {
                returnType = returnType.GetGenericArguments()[0];
                continue;
            }

            break;
        }

        if (returnType.IsGenericType
            && returnType.GetGenericTypeDefinition() == typeof(ServiceResult<>))
        {
            return new ServiceResponse(
                returnType.GetGenericArguments()[0],
                StatusCodes.Status200OK);
        }

        return null;
    }

    private static ServiceResponse Response<T>(
        int httpStatus = StatusCodes.Status200OK) => new(typeof(T), httpStatus);

    private sealed record ServiceResponse(Type DataType, int HttpStatus);

    private sealed record SpecialResponse(
        string ContentType,
        Type ResponseType,
        bool IncludeDefaultError = true);
}
