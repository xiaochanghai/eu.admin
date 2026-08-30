using EU.Core.IServices.Agents;
using EU.Core.Model.ViewModels.Extend;
using EU.Core.IServices.UnifiedEntry;
using Microsoft.Extensions.Options;

namespace EU.Core.Api.Agent.Configuration;

internal static class AgentApiOptionsServiceCollectionExtensions
{
    public static IServiceCollection AddAgentApiOptions(this IServiceCollection services)
    {
        services.AddOptions<AgentPlatformOptions>()
            .BindConfiguration(AgentPlatformOptions.SectionName)
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<AgentPlatformOptions>, AgentPlatformOptionsValidator>();
        services.AddOptions<AgentControlOptions>()
            .BindConfiguration(AgentControlOptions.SectionName)
            .Validate(
                options => PublicModelProfileCatalog.AreValid(options.ModelProfileIds),
                "AgentControl public model profile identifiers are invalid.")
            .ValidateOnStart();
        services.AddOptions<AgentStorageOptions>()
            .BindConfiguration(AgentStorageOptions.SectionName)
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<AgentStorageOptions>, AgentStorageOptionsValidator>();
        services.AddOptions<AgentEvaluationOptions>()
            .BindConfiguration(AgentEvaluationOptions.SectionName)
            .ValidateOnStart();
        services.AddSingleton<
            IValidateOptions<AgentEvaluationOptions>,
            AgentEvaluationOptionsValidator>();
        services.AddOptions<AgentMcpOptions>()
            .BindConfiguration(AgentMcpOptions.SectionName)
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<AgentMcpOptions>, AgentMcpOptionsValidator>();
        services.AddOptions<ToolApprovalOptions>()
            .BindConfiguration(ToolApprovalOptions.SectionName)
            .ValidateOnStart();
        services.AddSingleton<
            IValidateOptions<ToolApprovalOptions>,
            ToolApprovalOptionsValidator>();
        services.AddOptions<BusinessQueryForwardingOptions>()
            .BindConfiguration(BusinessQueryForwardingOptions.SectionName)
            .ValidateOnStart();
        services.AddOptions<BusinessQueryResultRetentionOptions>()
            .BindConfiguration(BusinessQueryResultRetentionOptions.SectionName)
            .ValidateOnStart();
        services.AddSingleton<
            IValidateOptions<BusinessQueryResultRetentionOptions>,
            BusinessQueryResultRetentionOptionsValidator>();
        services.AddSingleton(serviceProvider =>
        {
            BusinessQueryResultRetentionOptions retention = serviceProvider
                .GetRequiredService<IOptions<BusinessQueryResultRetentionOptions>>()
                .Value;
            return new BusinessQueryResultLimits(
                retention.MaximumResultBytes,
                retention.MaximumConversationBytes);
        });
        services.AddSingleton<
            IValidateOptions<BusinessQueryForwardingOptions>,
            BusinessQueryForwardingOptionsValidator>();
        services.AddSingleton<
            IValidateOptions<BusinessQueryForwardingOptions>,
            BusinessQueryMcpEgressOptionsValidator>();
        services.AddOptions<AgentExecutionOptions>()
            .BindConfiguration(AgentExecutionOptions.SectionName)
            .ValidateOnStart();
        services.AddSingleton<
            IValidateOptions<AgentExecutionOptions>,
            AgentExecutionOptionsValidator>();
        services.AddOptions<UnifiedEntryOptions>()
            .BindConfiguration(UnifiedEntryOptions.SectionName)
            .ValidateOnStart();
        services.AddSingleton<
            IValidateOptions<UnifiedEntryOptions>,
            UnifiedEntryOptionsValidator>();
        services.AddOptions<AgentDeploymentOptions>()
            .BindConfiguration(AgentDeploymentOptions.SectionName)
            .ValidateOnStart();
        services.AddSingleton<
            IValidateOptions<AgentDeploymentOptions>,
            AgentDeploymentOptionsValidator>();
        services.AddOptions<AgentRateLimitOptions>()
            .BindConfiguration(AgentRateLimitOptions.SectionName)
            .ValidateOnStart();
        services.AddSingleton<
            IValidateOptions<AgentRateLimitOptions>,
            AgentRateLimitOptionsValidator>();
        services.AddOptions<AgentCapacityOptions>()
            .BindConfiguration(AgentCapacityOptions.SectionName)
            .ValidateOnStart();
        services.AddSingleton<
            IValidateOptions<AgentCapacityOptions>,
            AgentCapacityOptionsValidator>();
        services.AddOptions<AgentIdempotencyOptions>()
            .BindConfiguration(AgentIdempotencyOptions.SectionName)
            .ValidateOnStart();
        services.AddSingleton<
            IValidateOptions<AgentIdempotencyOptions>,
            AgentIdempotencyOptionsValidator>();
        services.AddOptions<AgentHttpSecurityOptions>()
            .BindConfiguration(AgentHttpSecurityOptions.SectionName)
            .ValidateOnStart();
        services.AddSingleton<
            IValidateOptions<AgentHttpSecurityOptions>,
            AgentHttpSecurityOptionsValidator>();
        services.AddOptions<HostOptions>()
            .Configure<IOptions<AgentDeploymentOptions>>((options, deployment) =>
                options.ShutdownTimeout = TimeSpan.FromSeconds(
                    deployment.Value.ShutdownTimeoutSeconds));

        return services;
    }
}
