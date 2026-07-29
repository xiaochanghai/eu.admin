using Autofac.Extensions.DependencyInjection;
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
using EU.Core.Agent.Infrastructure.Mcp;
using EU.Core.Agent.Infrastructure.Persistence;
using EU.Core.Agent.Infrastructure.Skills;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Serilog;
using EU.Core.Agent.Runtime;

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
builder.Services.AddOptions<AgentPlatformOptions>()
    .BindConfiguration(AgentPlatformOptions.SectionName)
    .ValidateOnStart();
builder.Services.AddSingleton<IValidateOptions<AgentPlatformOptions>, AgentPlatformOptionsValidator>();
builder.Services.AddOptions<AgentControlOptions>()
    .BindConfiguration(AgentControlOptions.SectionName)
    .Validate(
        options => PublicModelProfileCatalog.AreValid(options.ModelProfileIds),
        "AgentControl public model profile identifiers are invalid.")
    .ValidateOnStart();
builder.Services.AddOptions<AgentStorageOptions>()
    .BindConfiguration(AgentStorageOptions.SectionName)
    .ValidateOnStart();
builder.Services.AddSingleton<IValidateOptions<AgentStorageOptions>, AgentStorageOptionsValidator>();
builder.Services.AddOptions<AgentMcpOptions>()
    .BindConfiguration(AgentMcpOptions.SectionName)
    .ValidateOnStart();
builder.Services.AddSingleton<IValidateOptions<AgentMcpOptions>, AgentMcpOptionsValidator>();
builder.Services.AddOptions<AgentExecutionOptions>()
    .BindConfiguration(AgentExecutionOptions.SectionName)
    .ValidateOnStart();
builder.Services.AddSingleton<
    IValidateOptions<AgentExecutionOptions>,
    AgentExecutionOptionsValidator>();
builder.Services.AddHealthChecks()
    .AddCheck<ReplicaModeHealthCheck>("replica-mode");
builder.Services.AddOpenTelemetry();
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
builder.Services.AddSingleton<IOrchestrationRepository>(services =>
{
    AgentStorageOptions options = services.GetRequiredService<IOptions<AgentStorageOptions>>().Value;
    return string.Equals(options.Provider, "InMemory", StringComparison.OrdinalIgnoreCase)
        ? new InMemoryOrchestrationRepository()
        : new SqliteOrchestrationRepository(options.ResolveDatabasePath(
            services.GetRequiredService<IHostEnvironment>().ContentRootPath));
});
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
        options.AllowedStdioCommands,
        options.EnableStdio,
        TimeSpan.FromSeconds(options.ConnectionTimeoutSeconds),
        TimeSpan.FromSeconds(options.DiscoveryTimeoutSeconds)));
});
builder.Services.AddSingleton<IMcpToolDiscovery>(services =>
    services.GetRequiredService<SdkMcpToolDiscovery>());
builder.Services.AddSingleton<IMcpRuntimeToolInvoker>(services =>
{
    AgentExecutionOptions options =
        services.GetRequiredService<IOptions<AgentExecutionOptions>>().Value;
    return new SdkMcpRuntimeToolInvoker(
        services.GetRequiredService<IMcpServerRepository>(),
        services.GetRequiredService<SdkMcpToolDiscovery>(),
        TimeSpan.FromSeconds(options.ToolCallTimeoutSeconds));
});
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
            TimeSpan.FromSeconds(execution.ToolCallTimeoutSeconds)),
        services.GetRequiredService<IModelCredentialResolver>(),
        services.GetRequiredService<IMcpRuntimeToolInvoker>());
});
builder.Services.AddSingleton<ISkillFileStore>(services =>
{
    AgentStorageOptions options = services
        .GetRequiredService<IOptions<AgentStorageOptions>>()
        .Value;
    return new ControlledSkillFileStore(
        options.ResolveSkillRootPath(
            services.GetRequiredService<IHostEnvironment>().ContentRootPath));
});
builder.Services.AddSingleton<JsonSchemaValidator>();
builder.Services.AddSingleton<AgentLifecycleService>();
builder.Services.AddSingleton<AgentQueryService>();
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

WebApplication app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ProblemDetailsMiddleware>();
app.UseMiddleware<RequestBodyLimitMiddleware>();
app.UseDefaultFiles();
app.UseStaticFiles();

if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("AgentPlatform:ExposeOpenApi"))
{
    app.MapOpenApi();
}

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = ReplicaModeHealthCheck.WriteResponseAsync,
});
app.MapControllers();

app.Run();
