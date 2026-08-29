#nullable enable

using System.Reflection;
using EU.Core.IServices.Abstractions.Security;
using EU.Core.IServices.Approvals;
using EU.Core.IServices.Mcp;
using EU.Core.Api.Agent.Controllers;
using EU.Core.IServices;
using EU.Core.Model;
using EU.Core.Model.ViewModels.Extend;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using EU.Core.Services;
using Xunit;

namespace EU.Core.Tests.Service_Test;

public sealed class AgMcpApiResponse_Should
{
    [Fact]
    public async Task Wrap_mcp_queries_and_mutations()
    {
        McpServerDefinition server = CreateServer();
        IAgMcpServerDefinitionServices lifecycle = Proxy<IAgMcpServerDefinitionServices>((method, _) =>
            method.Name switch
            {
                nameof(IAgMcpServerDefinitionServices.ListAsync) =>
                    Task.FromResult<IReadOnlyList<McpServerDefinition>>([server]),
                nameof(IAgMcpServerDefinitionServices.GetAsync) =>
                    Task.FromResult<McpServerDefinition?>(server),
                nameof(IAgMcpServerDefinitionServices.CreateAsync) or
                nameof(IAgMcpServerDefinitionServices.UpdateAsync) or
                nameof(IAgMcpServerDefinitionServices.SyncAsync) or
                nameof(IAgMcpServerDefinitionServices.SetArchivedAsync) or
                nameof(IAgMcpServerDefinitionServices.ClassifyToolAsync) =>
                    Task.FromResult(ServiceResult<McpServerDefinition>.OprateSuccess(server)),
                _ => throw new InvalidOperationException(method.Name)
            });
        var controller = WithHttpContext(new McpServersController(lifecycle));

        AssertServiceSuccess<IReadOnlyList<McpServerDefinition>>(
            await controller.List(null, null, CancellationToken.None),
            StatusCodes.Status200OK);
        AssertServiceSuccess<McpServerDefinition>(
            await controller.Get(server.Id, CancellationToken.None),
            StatusCodes.Status200OK);
        AssertServiceSuccess<McpServerDefinition>(
            await controller.Create(CreateRequest(), CancellationToken.None),
            StatusCodes.Status200OK);
        AssertServiceSuccess<McpServerDefinition>(
            await controller.Update(server.Id, UpdateRequest(), CancellationToken.None),
            StatusCodes.Status200OK);
        AssertServiceSuccess<McpServerDefinition>(
            await controller.Sync(server.Id, new SyncMcpServerRequest(0), CancellationToken.None),
            StatusCodes.Status200OK);
        AssertServiceSuccess<McpServerDefinition>(
            await controller.SetArchived(
                server.Id,
                new SetMcpServerArchiveRequest(0, true),
                CancellationToken.None),
            StatusCodes.Status200OK);
        AssertServiceSuccess<McpServerDefinition>(
            await controller.ClassifyTool(
                server.Id,
                Guid.NewGuid(),
                new ClassifyMcpToolRequest(0, McpToolRisk.ReadOnly),
                CancellationToken.None),
            StatusCodes.Status200OK);
    }

    [Fact]
    public async Task Return_fixed_mcp_errors()
    {
        IAgMcpServerDefinitionServices lifecycle = Proxy<IAgMcpServerDefinitionServices>((method, _) =>
            method.Name switch
            {
                nameof(IAgMcpServerDefinitionServices.GetAsync) =>
                    Task.FromResult<McpServerDefinition?>(null),
                nameof(IAgMcpServerDefinitionServices.SyncAsync) =>
                    Task.FromResult(ServiceResult<McpServerDefinition>.Failure(
                        McpServiceStatusCodes.DiscoveryFailed,
                        "Discovery failed.")),
                nameof(IAgMcpServerDefinitionServices.UpdateAsync) =>
                    Task.FromResult(ServiceResult<McpServerDefinition>.Failure(
                        McpServiceStatusCodes.DisableBlocked,
                        "The MCP Server is still referenced.")),
                _ => throw new InvalidOperationException(method.Name)
            });
        var controller = WithHttpContext(new McpServersController(lifecycle));

        AssertServiceError(
            await controller.Get(Guid.NewGuid(), CancellationToken.None),
            McpServiceStatusCodes.NotFound);
        AssertServiceError(
            await controller.Sync(Guid.NewGuid(), new SyncMcpServerRequest(0), CancellationToken.None),
            McpServiceStatusCodes.DiscoveryFailed);
        AssertServiceError(
            await controller.Update(Guid.NewGuid(), UpdateRequest(enabled: false), CancellationToken.None),
            McpServiceStatusCodes.DisableBlocked);
        AssertServiceError(
            await controller.List(null, "invalid", CancellationToken.None),
            McpServiceStatusCodes.ConfigurationInvalid);
    }

    [Fact]
    public async Task Wrap_published_mcp_tool_versions()
    {
        IReadOnlyList<PublishedMcpToolReference> values =
        [
            new(
                Guid.NewGuid(),
                "server",
                "Server",
                Guid.NewGuid(),
                "query",
                "Query data",
                "{}",
                McpToolRisk.ReadOnly,
                new string('a', 64))
        ];
        IPublishedMcpToolCatalog catalog = Proxy<IPublishedMcpToolCatalog>((method, _) =>
            method.Name == nameof(IPublishedMcpToolCatalog.ListAsync)
                ? Task.FromResult(values)
                : throw new InvalidOperationException(method.Name));
        var controller = WithHttpContext(new McpToolVersionsController(catalog));

        AssertServiceSuccess<IReadOnlyList<PublishedMcpToolReference>>(
            await controller.List(CancellationToken.None),
            StatusCodes.Status200OK);
    }

    [Fact]
    public async Task Wrap_approval_queries_and_decisions()
    {
        ToolApprovalRequestRecord pending = CreatePending();
        var repository = new ApprovalRepository([pending]);
        ToolApprovalsController controller = CreateApprovalController(repository);

        ServiceResult<IReadOnlyList<ToolApprovalRequestRecord>> list =
            AssertServiceSuccess<IReadOnlyList<ToolApprovalRequestRecord>>(
                await controller.List(null, 100, CancellationToken.None));
        Assert.Equal(pending.Id, Assert.Single(list.Data).Id);

        ServiceResult<ToolApprovalDetailResponse> detail =
            AssertServiceSuccess<ToolApprovalDetailResponse>(
                await controller.Get(pending.Id, CancellationToken.None));
        Assert.Equal(pending.Id, detail.Data.Approval.Id);

        ServiceResult<ToolApprovalRequestRecord> approved =
            AssertServiceSuccess<ToolApprovalRequestRecord>(
                await controller.Approve(
                    pending.Id,
                    new ToolApprovalDecisionApiRequest { Reason = "approved" },
                    CancellationToken.None));
        Assert.Equal(ToolApprovalStatus.Approved, approved.Data.Status);
        Assert.Equal(pending.EntryRunId, approved.Data.EntryRunId);

        ToolApprovalRequestRecord rejectPending = CreatePending();
        controller = CreateApprovalController(new ApprovalRepository([rejectPending]));
        Assert.Equal(
            ToolApprovalStatus.Rejected,
            AssertServiceSuccess<ToolApprovalRequestRecord>(
                await controller.Reject(
                    rejectPending.Id,
                    new ToolApprovalDecisionApiRequest { Reason = "rejected" },
                    CancellationToken.None)).Data.Status);

        ToolApprovalRequestRecord cancelPending = CreatePending(requester: "operator");
        controller = CreateApprovalController(new ApprovalRepository([cancelPending]));
        Assert.Equal(
            ToolApprovalStatus.Cancelled,
            AssertServiceSuccess<ToolApprovalRequestRecord>(
                await controller.Cancel(
                    cancelPending.Id,
                    new ToolApprovalDecisionApiRequest { Reason = "cancelled" },
                    CancellationToken.None)).Data.Status);
    }

    [Fact]
    public async Task Return_fixed_approval_errors_for_conflict_and_disabled_resume()
    {
        ToolApprovalRequestRecord pending = CreatePending();
        ToolApprovalRequestRecord approved = ToolApprovalStateMachine.Approve(
            pending,
            "operator",
            string.Empty,
            DateTimeOffset.UtcNow);
        ToolApprovalsController controller = CreateApprovalController(
            new ApprovalRepository([approved]));

        ActionResult<ServiceResult<IReadOnlyList<ToolApprovalRequestRecord>>> invalidList =
            await controller.List(null, 0, CancellationToken.None);
        AssertServiceError(
            Assert.IsType<JsonResult>(invalidList.Result),
            StatusCodes.Status400BadRequest,
            630023,
            ToolApprovalErrorCodes.Invalid);
        ActionResult<ServiceResult<ToolApprovalDetailResponse>> missingApproval =
            await controller.Get(Guid.NewGuid(), CancellationToken.None);
        AssertServiceError(
            Assert.IsType<JsonResult>(missingApproval.Result),
            StatusCodes.Status404NotFound,
            630035,
            "TOOL_APPROVAL_NOT_FOUND");

        ActionResult<ServiceResult<ToolApprovalRequestRecord>> invalidDecision =
            await controller.Approve(
                approved.Id,
                new ToolApprovalDecisionApiRequest(),
                CancellationToken.None);
        AssertServiceError(
            Assert.IsType<JsonResult>(invalidDecision.Result),
            StatusCodes.Status409Conflict,
            630024,
            ToolApprovalErrorCodes.InvalidState);
        ActionResult<ServiceResult<ToolApprovalConversationResumeResult>> disabledResume =
            await controller.Resume(approved.Id, CancellationToken.None);
        AssertServiceError(
            Assert.IsType<JsonResult>(disabledResume.Result),
            StatusCodes.Status503ServiceUnavailable,
            630034,
            "TOOL_APPROVAL_DISABLED");
    }

    private static McpServerDefinition CreateServer() => new(
        Guid.NewGuid(),
        "server",
        "Server",
        string.Empty,
        McpTransportKind.StreamableHttp,
        "http://localhost/mcp",
        string.Empty,
        [],
        string.Empty,
        true,
        0,
        McpServerStatus.NotSynced,
        string.Empty,
        null,
        [],
        []);

    private static CreateMcpServerRequest CreateRequest() => new(
        "server",
        "Server",
        string.Empty,
        McpTransportKind.StreamableHttp,
        "http://localhost/mcp",
        string.Empty,
        [],
        string.Empty,
        true);

    private static UpdateMcpServerRequest UpdateRequest(bool enabled = true) => new(
        0,
        "Server",
        string.Empty,
        McpTransportKind.StreamableHttp,
        "http://localhost/mcp",
        string.Empty,
        [],
        string.Empty,
        enabled);

    private static ToolApprovalRequestRecord CreatePending(string requester = "requester")
    {
        DateTimeOffset requestedAt = DateTimeOffset.UtcNow.AddMinutes(-1);
        return new ToolApprovalRequestRecord(
            Guid.NewGuid(),
            "tenant",
            requester,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "query",
            McpToolRisk.Mutating,
            new string('a', 64),
            new string('b', 64),
            "{}",
            ToolApprovalStatus.Pending,
            0,
            requestedAt,
            requestedAt.AddMinutes(15),
            string.Empty,
            string.Empty,
            null,
            null,
            null,
            string.Empty);
    }

    private static ToolApprovalsController CreateApprovalController(
        ApprovalRepository repository)
    {
        var controller = new ToolApprovalsController(
            new ToolApprovalManagementService(repository),
            new CallerContext(),
            TimeProvider.System);
        return WithHttpContext(controller);
    }

    private static TController WithHttpContext<TController>(TController controller)
        where TController : ControllerBase
    {
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                TraceIdentifier = "trace-mcp-contract",
                RequestServices = new ServiceCollection().BuildServiceProvider()
            }
        };
        return controller;
    }

    private static ServiceResult<T> AssertServiceSuccess<T>(
        ServiceResult<T> body,
        int businessStatus)
    {
        Assert.Equal(businessStatus, body.Status);
        Assert.True(body.Success);
        return body;
    }

    private static ServiceResult<T> AssertServiceSuccess<T>(
        ActionResult<ServiceResult<T>> action)
    {
        Assert.Null(action.Result);
        ServiceResult<T> body = Assert.IsType<ServiceResult<T>>(action.Value);
        Assert.Equal(200, body.Status);
        Assert.True(body.Success);
        return body;
    }

    private static ServiceResult<T> AssertServiceSuccess<T>(
        IActionResult action,
        int httpStatus)
    {
        JsonResult json = AssertJsonSuccess(action, httpStatus);
        ServiceResult<T> body = Assert.IsType<ServiceResult<T>>(json.Value);
        Assert.Equal(200, body.Status);
        Assert.True(body.Success);
        return body;
    }

    private static JsonResult AssertJsonSuccess(IActionResult action, int httpStatus)
    {
        JsonResult json = Assert.IsType<JsonResult>(action);
        Assert.Equal(httpStatus, json.StatusCode);
        Assert.Null(json.SerializerSettings);
        return json;
    }

    private static void AssertServiceError<T>(
        ServiceResult<T> body,
        int businessStatus)
    {
        Assert.False(body.Success);
        Assert.Equal(businessStatus, body.Status);
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
        ServiceResult<AgentApiErrorData> body =
            Assert.IsType<ServiceResult<AgentApiErrorData>>(json.Value);
        Assert.False(body.Success);
        Assert.Equal(businessStatus, body.Status);
        Assert.Equal(errorCode, body.Data.ErrorCode);
        Assert.Equal("trace-mcp-contract", body.Data.TraceId);
    }

    private static T Proxy<T>(Func<MethodInfo, object?[]?, object?> handler)
        where T : class
    {
        T value = DispatchProxy.Create<T, DelegateProxy>();
        ((DelegateProxy)(object)value).Handler = handler;
        return value;
    }

    private sealed class CallerContext : ICallerContext
    {
        public string UserId => "operator";

        public string TenantId => "tenant";

        public IReadOnlySet<string> Permissions { get; } = new HashSet<string>();

        public string CorrelationId => "correlation";
    }

    private sealed class ApprovalRepository(
        IEnumerable<ToolApprovalRequestRecord> values) : IToolApprovalRepository
    {
        private readonly Dictionary<Guid, ToolApprovalRequestRecord> _values =
            values.ToDictionary(value => value.Id);
        private readonly List<ToolApprovalDecisionRecord> _decisions = [];

        public Task<ToolApprovalRequestRecord?> GetAsync(
            Guid id,
            string tenantId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_values.TryGetValue(id, out ToolApprovalRequestRecord? value)
                && value.TenantId == tenantId ? value : null);

        public Task<IReadOnlyList<ToolApprovalRequestRecord>> ListAsync(
            ToolApprovalQuery query,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ToolApprovalRequestRecord>>(
                _values.Values
                    .Where(value => value.TenantId == query.TenantId)
                    .Where(value => query.Status is null || value.Status == query.Status)
                    .Take(query.Take)
                    .ToArray());

        public Task<IReadOnlyList<ToolApprovalDecisionRecord>> ListDecisionsAsync(
            Guid approvalId,
            string tenantId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ToolApprovalDecisionRecord>>(
                _decisions.Where(value => value.ApprovalId == approvalId
                    && value.TenantId == tenantId).ToArray());

        public Task<bool> TryReplaceAsync(
            ToolApprovalRequestRecord replacement,
            long expectedLogicalRevision,
            CancellationToken cancellationToken = default)
        {
            if (!_values.TryGetValue(replacement.Id, out ToolApprovalRequestRecord? current)
                || current.LogicalRevision != expectedLogicalRevision)
            {
                return Task.FromResult(false);
            }

            _values[replacement.Id] = replacement;
            _decisions.Add(new ToolApprovalDecisionRecord(
                Guid.NewGuid(),
                replacement.Id,
                replacement.TenantId,
                current.Status,
                replacement.Status,
                replacement.DecisionUserId,
                replacement.DecisionReason,
                replacement.DecidedAtUtc ?? replacement.FinishedAtUtc ?? DateTimeOffset.UtcNow,
                replacement.LogicalRevision));
            return Task.FromResult(true);
        }

        public Task<bool> TryCreateAsync(
            ToolApprovalRequestRecord request,
            string protectedResumePayload,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<ToolApprovalExecutionClaim?> TryClaimExecutionAsync(
            Guid id,
            string tenantId,
            long expectedLogicalRevision,
            DateTimeOffset claimedAtUtc,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<bool> TryCompleteExecutionAsync(
            ToolApprovalRequestRecord replacement,
            long expectedLogicalRevision,
            ToolApprovalExecutionResultRecord result,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<ToolApprovalExecutionResultRecord?> GetExecutionResultAsync(
            Guid id,
            string tenantId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private class DelegateProxy : DispatchProxy
    {
        public Func<MethodInfo, object?[]?, object?> Handler { get; set; } = null!;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            Handler(targetMethod ?? throw new InvalidOperationException(), args);
    }
}
