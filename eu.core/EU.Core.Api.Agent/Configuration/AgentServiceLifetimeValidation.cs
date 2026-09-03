using EU.Core.IServices.Approvals;
using EU.Core.IServices.Evaluation;
using EU.Core.IServices.Knowledge;
using EU.Core.IServices.MainAgent;
using EU.Core.IServices.Mcp;
using EU.Core.IServices.Orchestration;
using EU.Core.IServices.Runtime;
using EU.Core.IServices.UnifiedEntry;
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
        typeof(IRunEvaluationService),
        typeof(IAgEvaluationSuiteServices),
        typeof(IAgEvaluationBatchExecutionServices),
        typeof(IEvaluationBatchComparisonService),
        typeof(IModelJudgeService)
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
