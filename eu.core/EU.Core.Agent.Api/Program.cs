using Autofac.Extensions.DependencyInjection;
using System.Security.Cryptography;
using System.Text.Json.Serialization;
using EU.Core.Agent.Api.Configuration;
using EU.Core.Agent.Api.Controllers;
using EU.Core.Agent.Api.Errors;
using EU.Core.Agent.Api.Health;
using EU.Core.Agent.Api.Observability;
using EU.Core.Agent.Application.Agents;
using EU.Core.Agent.Application.Validation;
using EU.Core.Agent.Application.Skills;
using EU.Core.Agent.Application.Mcp;
using EU.Core.Agent.Application.Runtime;
using EU.Core.Agent.Application.Knowledge;
using EU.Core.Agent.Application.Orchestration;
using EU.Core.Agent.Application.MainAgent;
using EU.Core.Agent.Application.UnifiedEntry;
using EU.Core.Agent.Infrastructure.Mcp;
using EU.Core.Agent.Infrastructure.Knowledge;
using EU.Core.Agent.Infrastructure.Persistence;
using EU.Core.Agent.Infrastructure.Skills;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Serilog;
using EU.Core.Agent.Runtime;
using EU.Core.Agent.Api.Security;
using EU.Core.Agent.Application.Abstractions.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using EU.Core.Agent.Application.Abstractions.Auditing;
using EU.Core.Agent.Application.Approvals;
using EU.Core.Agent.Application.Evaluation;
using EU.Core.Agent.Infrastructure.Security;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
LocalDotEnvConfiguration.Apply(
    builder.Configuration,
    builder.Environment.ContentRootPath,
    AppContext.BaseDirectory);

builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());
builder.Services.AddOpenApi();
builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter(namingPolicy: null, allowIntegerValues: false)));
builder.Services.Configure<Microsoft.AspNetCore.Mvc.ApiBehaviorOptions>(options =>
    options.InvalidModelStateResponseFactory = ApiProblemResults.InvalidModelState);
builder.Services.AddAgentApiOptions();
builder.Services.AddAgentApiHttpSecurity(builder.Configuration, builder.Environment);
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICallerContext, HttpCallerContext>();
builder.Services.AddSingleton<TimeProvider>(TimeProvider.System);
builder.Services.AddSingleton<AgentMetrics>();
builder.Services.AddSingleton<HostDrainState>();
builder.Services.AddSingleton<IAgentOperationAuditRepository>(services =>
{
    AgentStorageOptions options = services
        .GetRequiredService<IOptions<AgentStorageOptions>>()
        .Value;
    return string.Equals(options.Provider, "InMemory", StringComparison.OrdinalIgnoreCase)
        ? new InMemoryAgentOperationAuditRepository()
        : new SqliteAgentOperationAuditRepository(
            options.ResolveDatabasePath(
                services.GetRequiredService<IHostEnvironment>().ContentRootPath));
});
builder.Services.AddSingleton<IHttpIdempotencyRepository>(services =>
{
    AgentStorageOptions options = services
        .GetRequiredService<IOptions<AgentStorageOptions>>()
        .Value;
    return string.Equals(options.Provider, "InMemory", StringComparison.OrdinalIgnoreCase)
        ? new InMemoryHttpIdempotencyRepository()
        : new SqliteHttpIdempotencyRepository(
            options.ResolveDatabasePath(
                services.GetRequiredService<IHostEnvironment>().ContentRootPath));
});
builder.Services.AddSingleton<
    IAuthorizationMiddlewareResultHandler,
    AgentAuthorizationResultHandler>();
builder.Services.AddHealthChecks()
    .AddCheck<ReplicaModeHealthCheck>("process", tags: ["live"])
    .AddCheck<AgentReadinessHealthCheck>("agent-readiness", tags: ["ready"]);
builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics => metrics.AddMeter(AgentMetrics.MeterName));
builder.Services.AddSerilog((_, loggerConfiguration) => loggerConfiguration
    .Enrich.With<LogRedactionEnricher>());
builder.Services.AddSingleton<IAgentRepository>(services =>
{
    AgentStorageOptions options = services
        .GetRequiredService<IOptions<AgentStorageOptions>>()
        .Value;
    return string.Equals(options.Provider, "InMemory", StringComparison.OrdinalIgnoreCase)
        ? new InMemoryAgentRepository()
        : new SqliteAgentRepository(
            options.ResolveDatabasePath(
                services.GetRequiredService<IHostEnvironment>().ContentRootPath));
});
builder.Services.AddSingleton<ISkillRepository>(services =>
{
    AgentStorageOptions options = services
        .GetRequiredService<IOptions<AgentStorageOptions>>()
        .Value;
    return string.Equals(options.Provider, "InMemory", StringComparison.OrdinalIgnoreCase)
        ? new InMemorySkillRepository()
        : new SqliteSkillRepository(
            options.ResolveDatabasePath(
                services.GetRequiredService<IHostEnvironment>().ContentRootPath));
});
builder.Services.AddSingleton<IPublishedSkillVersionCatalog>(services =>
    (IPublishedSkillVersionCatalog)services.GetRequiredService<ISkillRepository>());
builder.Services.AddSingleton<IMcpServerRepository>(services =>
{
    AgentStorageOptions options = services
        .GetRequiredService<IOptions<AgentStorageOptions>>()
        .Value;
    return string.Equals(options.Provider, "InMemory", StringComparison.OrdinalIgnoreCase)
        ? new InMemoryMcpServerRepository()
        : new SqliteMcpServerRepository(
            options.ResolveDatabasePath(
                services.GetRequiredService<IHostEnvironment>().ContentRootPath));
});
builder.Services.AddSingleton<IPublishedMcpToolCatalog>(services =>
    (IPublishedMcpToolCatalog)services.GetRequiredService<IMcpServerRepository>());
builder.Services.AddSingleton<IKnowledgeBaseRepository>(services =>
{
    AgentStorageOptions options = services.GetRequiredService<IOptions<AgentStorageOptions>>().Value;
    return string.Equals(options.Provider, "InMemory", StringComparison.OrdinalIgnoreCase)
        ? new InMemoryKnowledgeBaseRepository()
        : new SqliteKnowledgeBaseRepository(options.ResolveDatabasePath(
            services.GetRequiredService<IHostEnvironment>().ContentRootPath));
});
builder.Services.AddSingleton<IPublishedKnowledgeCatalog>(services =>
    (IPublishedKnowledgeCatalog)services.GetRequiredService<IKnowledgeBaseRepository>());
builder.Services.AddSingleton<IKnowledgeRetriever>(services =>
    (IKnowledgeRetriever)services.GetRequiredService<IKnowledgeBaseRepository>());
builder.Services.AddSingleton<IKnowledgePdfTextExtractor, PdfPigKnowledgePdfTextExtractor>();
builder.Services.AddSingleton<IOrchestrationRepository>(services =>
{
    AgentStorageOptions options = services.GetRequiredService<IOptions<AgentStorageOptions>>().Value;
    return string.Equals(options.Provider, "InMemory", StringComparison.OrdinalIgnoreCase)
        ? new InMemoryOrchestrationRepository()
        : new SqliteOrchestrationRepository(options.ResolveDatabasePath(
            services.GetRequiredService<IHostEnvironment>().ContentRootPath));
});
builder.Services.AddSingleton<IMainAgentAssignmentRepository>(services =>
{
    AgentStorageOptions options = services
        .GetRequiredService<IOptions<AgentStorageOptions>>()
        .Value;
    return string.Equals(options.Provider, "InMemory", StringComparison.OrdinalIgnoreCase)
        ? new InMemoryMainAgentAssignmentRepository()
        : new SqliteMainAgentAssignmentRepository(
            options.ResolveDatabasePath(
                services.GetRequiredService<IHostEnvironment>().ContentRootPath));
});
builder.Services.AddSingleton<IUnifiedEntryRepository>(services =>
{
    AgentStorageOptions options = services
        .GetRequiredService<IOptions<AgentStorageOptions>>()
        .Value;
    return string.Equals(options.Provider, "InMemory", StringComparison.OrdinalIgnoreCase)
        ? new InMemoryUnifiedEntryRepository()
        : new SqliteUnifiedEntryRepository(
            options.ResolveDatabasePath(
                services.GetRequiredService<IHostEnvironment>().ContentRootPath));
});
builder.Services.AddSingleton<IPublishedOrchestrationCatalog>(services =>
    (IPublishedOrchestrationCatalog)services.GetRequiredService<IOrchestrationRepository>());
builder.Services.AddSingleton<IOrchestrationRunRepository>(services =>
{
    AgentStorageOptions options = services.GetRequiredService<IOptions<AgentStorageOptions>>().Value;
    return string.Equals(options.Provider, "InMemory", StringComparison.OrdinalIgnoreCase)
        ? new InMemoryOrchestrationRunRepository()
        : new SqliteOrchestrationRunRepository(options.ResolveDatabasePath(
            services.GetRequiredService<IHostEnvironment>().ContentRootPath));
});
builder.Services.AddSingleton<SdkMcpToolDiscovery>(services =>
{
    AgentMcpOptions options =
        services.GetRequiredService<IOptions<AgentMcpOptions>>().Value;
    return new SdkMcpToolDiscovery(new McpDiscoverySettings(
        options.AllowedHosts,
        options.AllowedPorts,
        options.StdioProfiles.Select(profile => new McpStdioInvocation(
            profile.Command,
            profile.Arguments.ToArray(),
            profile.ExecutableSha256)).ToArray(),
        options.EnableStdio,
        TimeSpan.FromSeconds(options.ConnectionTimeoutSeconds),
        TimeSpan.FromSeconds(options.DiscoveryTimeoutSeconds),
        options.AllowDevelopmentHttp),
        services.GetRequiredService<IMcpCredentialResolver>());
});
builder.Services.AddSingleton<IEvaluationSuiteRepository>(services =>
{
    AgentStorageOptions options = services
        .GetRequiredService<IOptions<AgentStorageOptions>>()
        .Value;
    return string.Equals(options.Provider, "InMemory", StringComparison.OrdinalIgnoreCase)
        ? new InMemoryEvaluationSuiteRepository()
        : new SqliteEvaluationSuiteRepository(
            options.ResolveDatabasePath(
                services.GetRequiredService<IHostEnvironment>().ContentRootPath));
});
builder.Services.AddSingleton<IEvaluationBatchRepository>(services =>
{
    AgentStorageOptions options = services
        .GetRequiredService<IOptions<AgentStorageOptions>>()
        .Value;
    return string.Equals(options.Provider, "InMemory", StringComparison.OrdinalIgnoreCase)
        ? new InMemoryEvaluationBatchRepository()
        : new SqliteEvaluationBatchRepository(
            options.ResolveDatabasePath(
                services.GetRequiredService<IHostEnvironment>().ContentRootPath));
});
builder.Services.AddSingleton<IModelJudgeReportRepository>(services =>
{
    AgentStorageOptions options = services
        .GetRequiredService<IOptions<AgentStorageOptions>>()
        .Value;
    return string.Equals(options.Provider, "InMemory", StringComparison.OrdinalIgnoreCase)
        ? new InMemoryModelJudgeReportRepository()
        : new SqliteModelJudgeReportRepository(
            options.ResolveDatabasePath(
                services.GetRequiredService<IHostEnvironment>().ContentRootPath));
});
builder.Services.AddSingleton<IMcpCredentialResolver,
    DevelopmentMcpCredentialResolver>();
builder.Services.AddSingleton<IMcpToolDiscovery>(services =>
    services.GetRequiredService<SdkMcpToolDiscovery>());
builder.Services.AddSingleton<IBusinessQuerySigningKeyResolver,
    DevelopmentBusinessQuerySigningKeyResolver>();
builder.Services.AddSingleton<IBusinessQueryContextTokenProvider,
    BusinessQueryContextTokenProvider>();
builder.Services.AddSingleton(services =>
{
    BusinessQueryForwardingOptions forwarding = services
        .GetRequiredService<IOptions<BusinessQueryForwardingOptions>>()
        .Value;
    BusinessQueryToolPolicy? policy = forwarding.Enabled
        ? new BusinessQueryToolPolicy(
            forwarding.ServerCode,
            forwarding.ToolName,
            new Uri(forwarding.Origin, UriKind.Absolute),
            forwarding.Issuer,
            forwarding.Audience,
            forwarding.SigningKeyAlias,
            forwarding.CatalogRevision,
            forwarding.CatalogHash,
            forwarding.ToolSchemaHash,
            TimeSpan.FromSeconds(forwarding.TokenLifetimeSeconds),
            forwarding.AllowDevelopmentHttp)
        : null;
    return new BusinessQueryToolPolicyAccessor(policy);
});
builder.Services.AddSingleton<SdkMcpRuntimeToolInvoker>(services =>
{
    AgentExecutionOptions options =
        services.GetRequiredService<IOptions<AgentExecutionOptions>>().Value;
    BusinessQueryToolPolicy? policy = services
        .GetRequiredService<BusinessQueryToolPolicyAccessor>()
        .Policy;
    return new SdkMcpRuntimeToolInvoker(
        services.GetRequiredService<IMcpServerRepository>(),
        services.GetRequiredService<SdkMcpToolDiscovery>(),
        TimeSpan.FromSeconds(options.ToolCallTimeoutSeconds),
        policy,
        policy is null
            ? null
            : services.GetRequiredService<IBusinessQueryContextTokenProvider>());
});
builder.Services.AddSingleton<IMcpRuntimeToolInvoker>(services =>
    services.GetRequiredService<SdkMcpRuntimeToolInvoker>());
builder.Services.AddSingleton<IApprovedMcpRuntimeToolInvoker>(services =>
    services.GetRequiredService<SdkMcpRuntimeToolInvoker>());
builder.Services.AddSingleton<IToolApprovalRepository>(services =>
{
    AgentStorageOptions options = services
        .GetRequiredService<IOptions<AgentStorageOptions>>()
        .Value;
    return string.Equals(
        options.Provider,
        "InMemory",
        StringComparison.OrdinalIgnoreCase)
        ? new InMemoryToolApprovalRepository()
        : new SqliteToolApprovalRepository(options.ResolveDatabasePath(
            services.GetRequiredService<IHostEnvironment>().ContentRootPath));
});
builder.Services.AddSingleton<ToolApprovalManagementService>();
ToolApprovalOptions toolApproval = builder.Configuration
    .GetSection(ToolApprovalOptions.SectionName)
    .Get<ToolApprovalOptions>() ?? new ToolApprovalOptions();
if (toolApproval.Enabled)
{
    builder.Services.AddSingleton<IToolApprovalPayloadProtector>(services =>
    {
        ToolApprovalOptions options = services
            .GetRequiredService<IOptions<ToolApprovalOptions>>()
            .Value;
        IHostEnvironment environment = services.GetRequiredService<IHostEnvironment>();
        string encoded = environment.IsDevelopment()
            ? options.DevelopmentPayloadKey
            : ToolApprovalPayloadKeyResolver.ResolveEncoded(
                environment.ContentRootPath,
                builder.Configuration.GetValue<bool>("AgentPlatform:LoadDotEnv"));
        byte[] key = Convert.FromBase64String(encoded);
        try
        {
            return new AesGcmToolApprovalPayloadProtector(key);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
        }
    });
    builder.Services.AddSingleton<IToolApprovalExecutionPolicy,
        DefaultToolApprovalExecutionPolicy>();
    builder.Services.AddSingleton(services =>
    {
        ToolApprovalOptions options = services
            .GetRequiredService<IOptions<ToolApprovalOptions>>()
            .Value;
        AgentExecutionOptions execution = services
            .GetRequiredService<IOptions<AgentExecutionOptions>>()
            .Value;
        return new ToolApprovalRuntimeService(
            services.GetRequiredService<IToolApprovalRepository>(),
            services.GetRequiredService<IToolApprovalPayloadProtector>(),
            services.GetRequiredService<IPublishedMcpToolCatalog>(),
            services.GetRequiredService<IToolApprovalExecutionPolicy>(),
            services.GetRequiredService<IApprovedMcpRuntimeToolInvoker>(),
            TimeSpan.FromMinutes(options.LifetimeMinutes),
            TimeSpan.FromSeconds(execution.ToolCallTimeoutSeconds + 5),
            execution.MaximumApprovedToolResultBytes);
    });
    builder.Services.AddSingleton<IAgentToolApprovalHandler>(services =>
        services.GetRequiredService<ToolApprovalRuntimeService>());
    builder.Services.AddSingleton<ToolApprovalConversationResumeService>();
}
builder.Services.AddSingleton<IAgentRunAuditRepository>(services =>
{
    AgentStorageOptions options = services
        .GetRequiredService<IOptions<AgentStorageOptions>>()
        .Value;
    return string.Equals(options.Provider, "InMemory", StringComparison.OrdinalIgnoreCase)
        ? new InMemoryAgentRunAuditRepository()
        : new SqliteAgentRunAuditRepository(
            options.ResolveDatabasePath(
                services.GetRequiredService<IHostEnvironment>().ContentRootPath));
});
builder.Services.AddSingleton<IModelCredentialResolver>(services =>
    new EnvironmentAndDotEnvModelCredentialResolver(
        services.GetRequiredService<IHostEnvironment>().ContentRootPath,
        builder.Configuration.GetValue<bool>("AgentPlatform:LoadDotEnv")));
builder.Services.AddSingleton<IAgentRuntimeEngine>(services =>
{
    AgentPlatformOptions platform =
        services.GetRequiredService<IOptions<AgentPlatformOptions>>().Value;
    AgentExecutionOptions execution =
        services.GetRequiredService<IOptions<AgentExecutionOptions>>().Value;
    return new MicrosoftAgentRuntimeEngine(
        new AgentRuntimeOptions(
            new Uri(platform.ModelEndpoint, UriKind.Absolute),
            platform.ModelCredentialAlias,
            TimeSpan.FromSeconds(execution.ModelTimeoutSeconds),
            TimeSpan.FromSeconds(execution.ToolCallTimeoutSeconds),
            execution.MaximumToolResultBytes,
            execution.MaximumModelOutputBytes,
            execution.MaximumModelOutputEvents,
            execution.MaximumModelInputBytes,
            execution.MaximumToolArgumentBytes,
            execution.MaximumInternalToolResultBytes,
            execution.MaximumInternalToolCalls,
            execution.MaximumMcpToolCalls),
        services.GetRequiredService<IModelCredentialResolver>(),
        services.GetRequiredService<IMcpRuntimeToolInvoker>());
});
builder.Services.AddSingleton<IModelJudgeEngine>(services =>
{
    AgentPlatformOptions platform =
        services.GetRequiredService<IOptions<AgentPlatformOptions>>().Value;
    AgentExecutionOptions execution =
        services.GetRequiredService<IOptions<AgentExecutionOptions>>().Value;
    return new MicrosoftExtensionsModelJudgeEngine(
        new AgentRuntimeOptions(
            new Uri(platform.ModelEndpoint, UriKind.Absolute),
            platform.ModelCredentialAlias,
            TimeSpan.FromSeconds(execution.ModelTimeoutSeconds),
            TimeSpan.FromSeconds(execution.ToolCallTimeoutSeconds),
            execution.MaximumToolResultBytes,
            execution.MaximumModelOutputBytes,
            execution.MaximumModelOutputEvents,
            execution.MaximumModelInputBytes,
            execution.MaximumToolArgumentBytes,
            execution.MaximumInternalToolResultBytes,
            execution.MaximumInternalToolCalls,
            execution.MaximumMcpToolCalls),
        services.GetRequiredService<IModelCredentialResolver>());
});
builder.Services.AddSingleton<ControlledSkillFileStore>(services =>
{
    AgentStorageOptions options = services
        .GetRequiredService<IOptions<AgentStorageOptions>>()
        .Value;
    return new ControlledSkillFileStore(
        options.ResolveSkillRootPath(
            services.GetRequiredService<IHostEnvironment>().ContentRootPath));
});
builder.Services.AddSingleton<ISkillFileStore>(services =>
    services.GetRequiredService<ControlledSkillFileStore>());
builder.Services.AddSingleton<IPublishedSkillContentStore>(services =>
    services.GetRequiredService<ControlledSkillFileStore>());
builder.Services.AddSingleton<JsonSchemaValidator>();
builder.Services.AddSingleton<AgentLifecycleService>();
builder.Services.AddSingleton<AgentQueryService>();
builder.Services.AddSingleton<MainAgentAssignmentService>();
builder.Services.AddSingleton<SkillLifecycleService>();
builder.Services.AddSingleton<McpLifecycleService>();
builder.Services.AddSingleton<KnowledgeLifecycleService>();
builder.Services.AddSingleton<OrchestrationLifecycleService>();
builder.Services.AddSingleton<IPublicModelProfileCatalog>(services =>
    new PublicModelProfileCatalog(
        services.GetRequiredService<IOptions<AgentControlOptions>>().Value.ModelProfileIds));
builder.Services.AddSingleton<IModelProfileReferenceCatalog>(services =>
    services.GetRequiredService<IPublicModelProfileCatalog>());
builder.Services.AddSingleton<AgentPackageService>();
builder.Services.AddSingleton<AgentRuntimeService>();
builder.Services.AddSingleton<OrchestrationRuntimeService>();
builder.Services.AddSingleton(services =>
    services.GetRequiredService<IOptions<UnifiedEntryOptions>>()
        .Value
        .ToLimits());
builder.Services.AddSingleton<UnifiedEntryService>();
builder.Services.AddSingleton<RunEvaluationService>();
builder.Services.AddSingleton<IEvaluationTargetCatalog,
    PublishedAgentEvaluationTargetCatalog>();
builder.Services.AddSingleton<EvaluationSuiteLifecycleService>();
builder.Services.AddSingleton<EvaluationBatchService>();
builder.Services.AddSingleton<EvaluationBatchComparisonService>();
builder.Services.AddSingleton(services =>
{
    AgentEvaluationOptions options = services
        .GetRequiredService<IOptions<AgentEvaluationOptions>>()
        .Value;
    return new ModelJudgePolicy(
        options.EnableModelJudge,
        options.ModelJudgeMaximumCases,
        TimeSpan.FromSeconds(options.ModelJudgeTimeoutSeconds));
});
builder.Services.AddSingleton<ModelJudgeService>();

WebApplication app = builder.Build();
HostDrainState hostDrainState = app.Services.GetRequiredService<HostDrainState>();
app.Lifetime.ApplicationStopping.Register(hostDrainState.BeginDrain);

if (app.Services.GetRequiredService<IEvaluationBatchRepository>()
    is IEvaluationBatchRecovery evaluationBatchRecovery)
{
    await evaluationBatchRecovery.RecoverInterruptedAsync(
        TimeProvider.System.GetUtcNow(),
        CancellationToken.None);
}

if (app.Services.GetRequiredService<IUnifiedEntryRepository>()
    is IUnifiedEntryRecovery recovery)
{
    await recovery.RecoverInterruptedAsync(
        TimeProvider.System.GetUtcNow(),
        CancellationToken.None);
}
if (toolApproval.Enabled)
{
    await app.Services.GetRequiredService<IToolApprovalRepository>()
        .RecoverInterruptedExecutionsAsync(
            TimeProvider.System.GetUtcNow(),
            CancellationToken.None);
}

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseCors(AgentHttpSecurityOptions.CorsPolicyName);
app.UseAuthentication();
app.UseMiddleware<AgentOperationAuditMiddleware>();
app.UseRateLimiter();
app.UseMiddleware<ProblemDetailsMiddleware>();
app.UseMiddleware<RequestBodyLimitMiddleware>();
app.UseAuthorization();
app.UseMiddleware<HttpIdempotencyMiddleware>();
app.UseMiddleware<ExpensiveRequestAdmissionMiddleware>();
app.UseDefaultFiles();
app.UseStaticFiles();

if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("AgentPlatform:ExposeOpenApi"))
{
    app.MapOpenApi();
}

app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("live"),
    ResponseWriter = ReplicaModeHealthCheck.WriteResponseAsync,
});
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("live"),
    ResponseWriter = ReplicaModeHealthCheck.WriteResponseAsync,
}).AllowAnonymous();
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("ready"),
    ResponseWriter = AgentReadinessHealthCheck.WriteResponseAsync,
}).AllowAnonymous();
app.MapControllers();

app.Run();
