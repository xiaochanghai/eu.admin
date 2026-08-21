#nullable enable

using System.Runtime.CompilerServices;
using System.Text.Json;
using EU.Core.Agent.Application.Abstractions.Auditing;
using EU.Core.Agent.Application.Abstractions.Security;
using EU.Core.Agent.Application.Agents;
using EU.Core.Agent.Application.MainAgent;
using EU.Core.Agent.Application.Mcp;
using EU.Core.Agent.Application.Runtime;
using EU.Core.Agent.Application.UnifiedEntry;
using EU.Core.Agent.Application.Validation;
using EU.Core.Api.Agent.Configuration;
using EU.Core.Api.Agent.Controllers;
using EU.Core.Api.Agent.Errors;
using EU.Core.Api.Agent.Security;
using EU.Core.Model;
using EU.Core.Model.ViewModels.Extend;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace EU.Core.Tests.Service_Test;

public sealed class AgRuntimeApiResponse_Should
{
    [Fact]
    public async Task Wrap_chat_json_queries_and_fixed_errors_without_changing_stream_action()
    {
        var repository = new UnifiedRepository();
        var service = (UnifiedEntryService)RuntimeHelpers.GetUninitializedObject(typeof(UnifiedEntryService));
        var controller = new ChatRunsController(service, repository, new CallerContext())
        {
            ControllerContext = Context()
        };

        AssertServiceSuccess(
            await controller.ListConversations(40, CancellationToken.None),
            StatusCodes.Status200OK,
            typeof(IReadOnlyList<ConversationRecord>));
        AssertServiceError(
            await controller.GetConversation("invalid", 40, CancellationToken.None),
            StatusCodes.Status400BadRequest,
            600004,
            ChatApiErrorCodes.InvalidId);
        AssertServiceError(
            await controller.ListRuns("invalid", 20, CancellationToken.None),
            StatusCodes.Status400BadRequest,
            600004,
            ChatApiErrorCodes.InvalidId);
        AssertServiceError(
            await controller.GetRun("invalid", CancellationToken.None),
            StatusCodes.Status400BadRequest,
            600004,
            ChatApiErrorCodes.InvalidId);
        AssertServiceError(
            await controller.GetDetails("invalid", CancellationToken.None),
            StatusCodes.Status400BadRequest,
            600004,
            ChatApiErrorCodes.InvalidId);
        AssertServiceError(
            await controller.GetEvents("invalid", 160, CancellationToken.None),
            StatusCodes.Status400BadRequest,
            600004,
            ChatApiErrorCodes.InvalidId);
        AssertServiceError(
            await controller.Cancel("invalid", CancellationToken.None),
            StatusCodes.Status400BadRequest,
            600004,
            ChatApiErrorCodes.InvalidId);

        Assert.Equal(typeof(Task<IActionResult>), typeof(ChatRunsController)
            .GetMethod(nameof(ChatRunsController.Start))!.ReturnType);
    }

    [Fact]
    public async Task Wrap_agent_history_audit_cleanup_and_platform_json()
    {
        var audit = new AgentRunAuditRepository();
        var runtime = new AgentRuntimeService(
            new EmptyAgentCatalog(), null!, null!, audit, new JsonSchemaValidator());
        var agentRuns = new AgentRunsController(runtime, new CallerContext())
        { ControllerContext = Context() };
        AssertServiceSuccess(
            await agentRuns.List(Guid.NewGuid(), 20, CancellationToken.None),
            StatusCodes.Status200OK,
            typeof(IReadOnlyList<AgentRunAuditRecord>));
        agentRuns.Response.Body = new MemoryStream();
        await agentRuns.Run(
            Guid.NewGuid(), new StartAgentRunRequest("hello"), CancellationToken.None);
        Assert.Equal(StatusCodes.Status404NotFound, agentRuns.Response.StatusCode);
        Assert.StartsWith("application/json", agentRuns.Response.ContentType);
        agentRuns.Response.Body.Position = 0;
        using (JsonDocument error = await JsonDocument.ParseAsync(agentRuns.Response.Body))
        {
            Assert.Equal(610001, error.RootElement.GetProperty("Status").GetInt32());
            Assert.False(error.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal(
                AgentRunErrorCodes.AgentNotFound,
                error.RootElement.GetProperty("Data").GetProperty("ErrorCode").GetString());
        }

        var engine = new CapturingRuntimeEngine();
        Guid agentId = Guid.NewGuid();
        var identityRuntime = new AgentRuntimeService(
            new RunnableAgentCatalog(agentId),
            new EmptyToolCatalog(),
            engine,
            audit,
            new JsonSchemaValidator());
        var identityController = new AgentRunsController(identityRuntime, new CallerContext())
        { ControllerContext = Context() };
        identityController.Response.Body = new MemoryStream();
        await identityController.Run(
            agentId, new StartAgentRunRequest("hello"), CancellationToken.None);
        Assert.NotNull(engine.Context?.ExecutionIdentity);
        Assert.Equal("operator", engine.Context.ExecutionIdentity.UserId);
        Assert.Equal("tenant", engine.Context.ExecutionIdentity.TenantId);
        Assert.Equal("correlation", engine.Context.ExecutionIdentity.CorrelationId);

        var operations = new AuditController(new OperationAuditRepository(), new CallerContext())
        {
            ControllerContext = Context()
        };
        AssertServiceSuccess(
            await operations.List(50, CancellationToken.None),
            StatusCodes.Status200OK,
            typeof(IReadOnlyList<AgentOperationAuditRecord>));

        var unified = new UnifiedRepository();
        var retention = new BusinessQueryRetentionController(
            unified,
            Options.Create(new BusinessQueryResultRetentionOptions { RetentionDays = 30 }),
            TimeProvider.System)
        { ControllerContext = Context() };
        AssertServiceSuccess(
            await retention.Cleanup(CancellationToken.None),
            StatusCodes.Status200OK,
            typeof(BusinessQueryCleanupResult));

        var mainAgent = new MainAgentAssignmentService(new EmptyAgentCatalog(), new MainAgentRepository());
        var platform = new PlatformController(
            Options.Create(new AgentPlatformOptions { ServiceName = "agent-api" }),
            Options.Create(new AgentEvaluationOptions { EnableModelJudge = true }),
            new PublicModelProfileCatalog(["model-profile"]),
            mainAgent)
        { ControllerContext = Context() };
        object serviceData = AssertServiceSuccess(platform.Service(), StatusCodes.Status200OK);
        Assert.Equal("agent-api", serviceData.GetType().GetProperty("Service")?.GetValue(serviceData));
        object capabilityData = AssertServiceSuccess(
            await platform.Capabilities(CancellationToken.None), StatusCodes.Status200OK);
        Assert.Equal("sqlsugar", capabilityData.GetType().GetProperty("StorageMode")?.GetValue(capabilityData));
    }

    private static ControllerContext Context() => new()
    {
        HttpContext = new DefaultHttpContext
        {
            TraceIdentifier = "trace-runtime-contract",
            RequestServices = new ServiceCollection()
                .AddOptions()
                .Configure<JsonOptions>(options =>
                    options.JsonSerializerOptions.PropertyNamingPolicy = null)
                .BuildServiceProvider()
        }
    };

    private static object AssertServiceSuccess(
        IActionResult action,
        int httpStatus,
        Type? expectedDataType = null)
    {
        JsonResult json = Assert.IsType<JsonResult>(action);
        Assert.Equal(httpStatus, json.StatusCode);
        Assert.Null(json.SerializerSettings);
        object body = Assert.IsAssignableFrom<object>(json.Value);
        Assert.Equal(200, body.GetType().GetProperty("Status")?.GetValue(body));
        Assert.Equal(true, body.GetType().GetProperty("Success")?.GetValue(body));
        object data = Assert.IsAssignableFrom<object>(body.GetType().GetProperty("Data")?.GetValue(body));
        if (expectedDataType is not null)
            Assert.True(expectedDataType.IsInstanceOfType(data), data.GetType().FullName);
        return data;
    }

    private static void AssertServiceError(
        IActionResult action,
        int httpStatus,
        int businessStatus,
        string errorCode)
    {
        JsonResult json = Assert.IsType<JsonResult>(action);
        Assert.Equal(httpStatus, json.StatusCode);
        Assert.Null(json.SerializerSettings);
        ServiceResult<AgentApiErrorData> body = Assert.IsType<ServiceResult<AgentApiErrorData>>(json.Value);
        Assert.False(body.Success);
        Assert.Equal(businessStatus, body.Status);
        Assert.Equal(errorCode, body.Data.ErrorCode);
        Assert.Equal("trace-runtime-contract", body.Data.TraceId);
    }

    private sealed class CallerContext : ICallerContext
    {
        public string UserId => "operator";
        public string TenantId => "tenant";
        public IReadOnlySet<string> Permissions { get; } = new HashSet<string>
        {
            AgentAuthorizationPolicies.DebugPermission,
            AgentAuthorizationPolicies.BusinessDataReadPermission
        };
        public string CorrelationId => "correlation";
    }

    private sealed class UnifiedRepository : IUnifiedEntryRepository
    {
        public Task<ConversationRecord?> GetConversationAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<ConversationRecord?>(null);
        public Task<IReadOnlyList<ConversationRecord>> ListConversationsAsync(int take, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ConversationRecord>>([]);
        public Task<IReadOnlyList<ConversationMessageRecord>> ListMessagesAsync(Guid conversationId, int take = 80, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ConversationMessageRecord>>([]);
        public Task<UnifiedEntryRunRecord?> GetRunAsync(Guid runId, CancellationToken cancellationToken = default) =>
            Task.FromResult<UnifiedEntryRunRecord?>(null);
        public Task<IReadOnlyList<UnifiedEntryRunRecord>> ListRunsAsync(Guid conversationId, int take, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<UnifiedEntryRunRecord>>([]);
        public Task<UnifiedRunDetails?> GetDetailsAsync(Guid runId, CancellationToken cancellationToken = default) =>
            Task.FromResult<UnifiedRunDetails?>(null);
        public Task<IReadOnlyList<UnifiedRunEventRecord>> ListEventsAsync(Guid runId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<UnifiedRunEventRecord>>([]);
        public Task<BusinessQueryCleanupResult> RedactExpiredBusinessQueryResultsAsync(DateTimeOffset cutoffUtc, CancellationToken cancellationToken = default) =>
            Task.FromResult(new BusinessQueryCleanupResult(1, 2, 3, cutoffUtc));
        public Task<UnifiedEntryAggregate> SaveAsync(UnifiedEntryAggregate value, CancellationToken cancellationToken = default) =>
            Task.FromResult(value);
    }

    private sealed class AgentRunAuditRepository : IAgentRunAuditRepository
    {
        public Task SaveAsync(AgentRunAuditRecord record, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<IReadOnlyList<AgentRunAuditRecord>> ListAsync(Guid agentId, int take, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<AgentRunAuditRecord>>([]);
    }

    private sealed class OperationAuditRepository : IAgentOperationAuditRepository
    {
        public Task SaveAsync(AgentOperationAuditRecord record, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<IReadOnlyList<AgentOperationAuditRecord>> ListAsync(string tenantId, int take, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<AgentOperationAuditRecord>>([]);
    }

    private sealed class EmptyAgentCatalog : IAgentDefinitionCatalog
    {
        public Task<AgentDefinition?> GetDefinitionAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<AgentDefinition?>(null);
        public Task<IReadOnlyList<AgentDefinition>> ListDefinitionsAsync(AgentDefinitionQuery query, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<AgentDefinition>>([]);
    }

    private sealed class RunnableAgentCatalog(Guid agentId) : IAgentDefinitionCatalog
    {
        private readonly AgentDefinition _definition = Create(agentId);

        public Task<AgentDefinition?> GetDefinitionAsync(
            Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<AgentDefinition?>(id == agentId ? _definition : null);

        public Task<IReadOnlyList<AgentDefinition>> ListDefinitionsAsync(
            AgentDefinitionQuery query, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<AgentDefinition>>([_definition]);

        private static AgentDefinition Create(Guid id)
        {
            Guid versionId = Guid.NewGuid();
            var snapshot = new AgentVersionSnapshot(
                versionId, "agent", "instructions", "model",
                AgentOutputMode.Text, null, [], []);
            var draft = new AgentVersion(
                Guid.NewGuid(), "0.1.0", true, "instructions", "model",
                AgentOutputMode.Text, null, null, null);
            var published = new AgentVersion(
                versionId, "1.0.0", false, "instructions", "model",
                AgentOutputMode.Text, null, null, snapshot);
            return new AgentDefinition(
                id, "agent", "Agent", "", AgentRuntimeStatus.Enabled,
                1, draft, [published]);
        }
    }

    private sealed class EmptyToolCatalog : IPublishedMcpToolCatalog
    {
        public Task<bool> ExistsAsync(Guid toolVersionId, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task<IReadOnlyList<PublishedMcpToolReference>> ListAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<PublishedMcpToolReference>>([]);
    }

    private sealed class CapturingRuntimeEngine : IAgentRuntimeEngine
    {
        public AgentRunContext? Context { get; private set; }

        public async IAsyncEnumerable<AgentRunEvent> StreamAsync(
            AgentRunContext context,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            Context = context;
            await Task.CompletedTask;
            yield return new AgentRunEvent(
                context.RunId,
                1,
                AgentRunEventKind.Completed,
                DateTimeOffset.UtcNow);
        }
    }

    private sealed class MainAgentRepository : IMainAgentAssignmentRepository
    {
        public Task<MainAgentAssignment?> GetAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<MainAgentAssignment?>(null);
        public Task<bool> TryReplaceAsync(MainAgentAssignment value, long? expectedLogicalRevision, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);
    }
}
