using EU.Core.Agent.Application.Approvals;
using EU.Core.Agent.Application.Evaluation;
using EU.Core.Agent.Application.Knowledge;
using EU.Core.Agent.Application.MainAgent;
using EU.Core.Agent.Application.Mcp;
using EU.Core.Agent.Application.Orchestration;
using EU.Core.Agent.Application.Runtime;
using EU.Core.Agent.Application.UnifiedEntry;
using EU.Core.Agent.Infrastructure.Mcp;
using EU.Core.Services;
using EU.Core.IServices;

namespace EU.Core.Api.Agent.Configuration;

public static class AgentServiceLifetimeValidation
{
    private static readonly HashSet<Type> ScopedServiceTypes =
    [
        typeof(ToolApprovalManagementService),
        typeof(ToolApprovalConversationResumeService),
        typeof(OrchestrationLifecycleService),
        typeof(RunEvaluationService),
        typeof(IEvaluationTargetCatalog),
        typeof(EvaluationSuiteLifecycleService),
        typeof(IAgEvaluationBatchExecutionServices),
        typeof(EvaluationBatchComparisonService),
        typeof(ModelJudgeService)
    ];

    private static readonly HashSet<Type> SingletonRuntimeServiceTypes =
    [
        typeof(SdkMcpRuntimeToolInvoker),
        typeof(IMcpRuntimeToolInvoker),
        typeof(IApprovedMcpRuntimeToolInvoker),
        typeof(ToolApprovalRuntimeService),
        typeof(IAgentToolApprovalHandler),
        typeof(IAgentRuntimeEngine),
        typeof(MainAgentAssignmentService),
        typeof(AgentRuntimeService),
        typeof(OrchestrationRuntimeService),
        typeof(UnifiedEntryService)
    ];

    public static IServiceCollection ValidateAgentServiceLifetimes(
        this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        string[] singletonServices = services
            .Where(descriptor =>
                descriptor.Lifetime == ServiceLifetime.Singleton &&
                ScopedServiceTypes.Contains(descriptor.ServiceType))
            .Select(descriptor => descriptor.ServiceType.FullName ?? descriptor.ServiceType.Name)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
        if (singletonServices.Length > 0)
        {
            throw new InvalidOperationException(
                "Database-backed Agent services must use a scoped lifetime: " +
                string.Join(", ", singletonServices));
        }

        string[] scopedRuntimeServices = services
            .Where(descriptor =>
                descriptor.Lifetime != ServiceLifetime.Singleton &&
                SingletonRuntimeServiceTypes.Contains(descriptor.ServiceType))
            .Select(descriptor => descriptor.ServiceType.FullName ?? descriptor.ServiceType.Name)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
        if (scopedRuntimeServices.Length > 0)
        {
            throw new InvalidOperationException(
                "Stateful Agent runtime services must use a singleton lifetime: " +
                string.Join(", ", scopedRuntimeServices));
        }

        return services;
    }
}
