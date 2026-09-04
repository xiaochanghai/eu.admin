using Autofac;
using Autofac.Extensions.DependencyInjection;
using EU.Core.Agent.Infrastructure.Mcp;
using EU.Core.Agent.Infrastructure.Skills;
using EU.Core.Agent.Runtime;
using EU.Core.Api.Agent.Configuration;
using EU.Core.Api.Agent.Errors;
using EU.Core.Api.Agent.Health;
using EU.Core.Api.Agent.Observability;
using EU.Core.Api.Agent.Security;
using EU.Core.Common.Core;
using EU.Core.Common.HttpContextUser;
using EU.Core.Extensions;
using EU.Core.Extensions.Filters;
using EU.Core.Extensions.Middlewares;
using EU.Core.IServices;
using EU.Core.IServices.Abstractions.Security;
using EU.Core.IServices.Agents;
using EU.Core.IServices.Approvals;
using EU.Core.IServices.Evaluation;
using EU.Core.IServices.Mcp;
using EU.Core.IServices.Runtime;
using EU.Core.IServices.Skills;
using EU.Core.IServices.UnifiedEntry;
using EU.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Serilog;
using System.Security.Cryptography;
using System.Text.Json.Serialization;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
LocalDotEnvConfiguration.ConfigureWithDotEnvFallback(
    builder.Configuration,
    builder.Environment.ContentRootPath,
    AppContext.BaseDirectory,
    args);

builder.Host
    .UseServiceProviderFactory(new AutofacServiceProviderFactory())
    .ConfigureContainer<ContainerBuilder>(container =>
        container.RegisterModule(new AutofacModuleRegister()))
    .ConfigureAppConfiguration((hostingContext, _) =>
        hostingContext.Configuration.ConfigureApplication());
builder.ConfigureApplication();

builder.Services.AddSingleton(new AppSettings(builder.Configuration));

ServiceExtensions.Init();

builder.Services.AddCacheSetup();
builder.Services.AddSqlsugarSetup();
builder.Services.AddOpenApi();
builder.Services
    .AddControllers(options =>
    {
        options.Filters.Add<GlobalExceptionsFilter>();
        options.Filters.Add<AgentApiValidationResultFilter>();
    })
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
        options.JsonSerializerOptions.DictionaryKeyPolicy = null;
        options.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter(namingPolicy: null, allowIntegerValues: false));
    });
builder.Services.Configure<Microsoft.AspNetCore.Mvc.ApiBehaviorOptions>(options =>
    options.InvalidModelStateResponseFactory = AgentApiValidationResultFilter.InvalidModelState);
builder.Services.AddAgentApiOptions();
builder.Services.AddAgentApiHttpSecurity(builder.Configuration);
builder.Services.AddHttpContextSetup();
builder.Services.AddScoped<ICallerContext, HttpCallerContext>();
builder.Services.AddSingleton<TimeProvider>(TimeProvider.System);
builder.Services.AddSingleton<AgentMetrics>();
builder.Services.AddSingleton<HostDrainState>();
builder.Services.AddSingleton<
    IAuthorizationMiddlewareResultHandler,
    AgentAuthorizationResultHandler>();
builder.Services.AddHealthChecks()
    .AddCheck<ReplicaModeHealthCheck>("process", tags: ["live"])
    .AddCheck<AgentReadinessHealthCheck>("agent-readiness", tags: ["ready"]);
builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics => metrics.AddMeter(AgentMetrics.MeterName));
builder.Services.AddSerilog((_, loggerConfiguration) => loggerConfiguration
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.With<LogRedactionEnricher>()
    .WriteTo.Console());
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
    IHttpContextAccessor httpContextAccessor =
        services.GetRequiredService<IHttpContextAccessor>();
    BusinessQueryToolPolicy? policy = services
        .GetRequiredService<BusinessQueryToolPolicyAccessor>()
        .Policy;
    return new SdkMcpRuntimeToolInvoker(
        services.GetRequiredService<IMcpServerDefinitionCatalog>(),
        services.GetRequiredService<SdkMcpToolDiscovery>(),
        TimeSpan.FromSeconds(options.ToolCallTimeoutSeconds),
        policy,
        policy is null
            ? null
            : services.GetRequiredService<IBusinessQueryContextTokenProvider>(),
        () =>
        {
            HttpContext? context = httpContextAccessor.HttpContext;
            return context?.User.Identity?.IsAuthenticated == true
                ? context.RequestServices.GetService<IUser>()?.GetToken()
                : null;
        });
});
builder.Services.AddSingleton<IMcpRuntimeToolInvoker>(services =>
    services.GetRequiredService<SdkMcpRuntimeToolInvoker>());
builder.Services.AddSingleton<IApprovedMcpRuntimeToolInvoker>(services =>
    services.GetRequiredService<SdkMcpRuntimeToolInvoker>());
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
    builder.Services.AddScoped<ToolApprovalConversationResumeService>();
}
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
        services.GetRequiredService<IMcpRuntimeToolInvoker>(),
        services.GetRequiredService<ILogger<MicrosoftAgentRuntimeEngine>>());
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
builder.Services.AddSingleton<IPublicModelProfileCatalog>(services =>
    new PublicModelProfileCatalog(
        services.GetRequiredService<IOptions<AgentControlOptions>>().Value.ModelProfileIds));
builder.Services.AddSingleton<IModelProfileReferenceCatalog>(services =>
    services.GetRequiredService<IPublicModelProfileCatalog>());
builder.Services.AddSingleton<OrchestrationRuntimeService>();
builder.Services.AddSingleton(services =>
    services.GetRequiredService<IOptions<UnifiedEntryOptions>>()
        .Value
        .ToLimits());
// These runtime coordinators own active executions, cancellation tokens and
// recovery gates that must remain available across HTTP request scopes.
builder.Services.AddSingleton<UnifiedEntryService>();
builder.Services.Configure<AgentTaskWorkerOptions>(
    builder.Configuration.GetSection(AgentTaskWorkerOptions.SectionName));
builder.Services.AddScoped<EU.Core.Api.Agent.Background.IAgentTaskExecutor,
    EU.Core.Api.Agent.Background.ChatAgentTaskExecutor>();
//builder.Services.AddHostedService<EU.Core.Api.Agent.Background.AgentTaskWorker>();
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
builder.Services.ValidateAgentServiceLifetimes();

WebApplication app = builder.Build();
app.ConfigureApplication();
app.UseApplicationSetup();
HostDrainState hostDrainState = app.Services.GetRequiredService<HostDrainState>();
app.Lifetime.ApplicationStopping.Register(hostDrainState.BeginDrain);

await using (AsyncServiceScope startupScope = app.Services.CreateAsyncScope())
{
    IServiceProvider startupServices = startupScope.ServiceProvider;
    await startupServices
        .GetRequiredService<EU.Core.IServices.IAgSkillDefinitionServices>()
        .ReconcileFileAttachmentsAsync(CancellationToken.None);

    if (startupServices.GetRequiredService<IEvaluationBatchRepository>()
        is IEvaluationBatchRecovery evaluationBatchRecovery)
    {
        await evaluationBatchRecovery.RecoverInterruptedAsync(
            TimeProvider.System.GetUtcNow(),
            CancellationToken.None);
    }

    if (startupServices.GetRequiredService<IUnifiedEntryRepository>()
        is IUnifiedEntryRecovery recovery)
    {
        await recovery.RecoverInterruptedAsync(
            TimeProvider.System.GetUtcNow(),
            CancellationToken.None);
    }
    if (toolApproval.Enabled)
    {
        await startupServices.GetRequiredService<IToolApprovalRepository>()
            .RecoverInterruptedExecutionsAsync(
                TimeProvider.System.GetUtcNow(),
                CancellationToken.None);
    }
}

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseResponseBodyRead();
app.UseRequestResponseLogMiddle();
app.UseSerilogRequestLogging();
app.UseMiddleware<SecurityHeadersMiddleware>();
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/skills"))
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        return;
    }

    await next(context);
});
app.UseDefaultFiles();
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = context =>
    {
        context.Context.Response.Headers.CacheControl = "no-store, no-cache, must-revalidate";
        context.Context.Response.Headers.Pragma = "no-cache";
        context.Context.Response.Headers.Expires = "0";
    }
});
app.UseCors(AgentHttpSecurityOptions.CorsPolicyName);
app.UseAuthentication();
app.UseMiddleware<AgentOperationAuditMiddleware>();
app.UseRateLimiter();
app.UseMiddleware<ProblemDetailsMiddleware>();
app.UseMiddleware<RequestBodyLimitMiddleware>();
app.UseAuthorization();
app.UseMiddleware<HttpIdempotencyMiddleware>();
app.UseMiddleware<ExpensiveRequestAdmissionMiddleware>();

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
