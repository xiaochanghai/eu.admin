using System.Data;
using System.Security.Cryptography;
using System.Text;
using EU.Core.Agent.Application.Approvals;
using EU.Core.Agent.Application.Mcp;

#nullable enable

namespace EU.Core.Services;

public sealed class AgToolApprovalRequestServices :
    BaseServices<AgToolApprovalRequest>,
    IAgToolApprovalRequestServices,
    IToolApprovalRepository
{
    public AgToolApprovalRequestServices(IBaseRepository<AgToolApprovalRequest> dal)
        : base(dal ?? throw new ArgumentNullException(nameof(dal)))
    {
    }

    public async Task<bool> TryCreateAsync(
        ToolApprovalRequestRecord request,
        string protectedResumePayload,
        CancellationToken cancellationToken = default)
    {
        ToolApprovalStateMachine.ValidateNew(request, protectedResumePayload);
        cancellationToken.ThrowIfCancellationRequested();
        await Db.Ado.BeginTranAsync(IsolationLevel.Serializable);
        try
        {
            if (await Db.Queryable<AgToolApprovalRequest>()
                .Where(value => value.ID == request.Id)
                .AnyAsync())
            {
                await Db.Ado.RollbackTranAsync();
                return false;
            }

            await Db.Insertable(MapRequestEntity(request)).ExecuteCommandAsync();
            await Db.Insertable(new AgToolApprovalPayload
            {
                ID = Guid.NewGuid(),
                ApprovalId = request.Id,
                ProtectedPayload = protectedResumePayload,
                ProtectedPayloadSha256 = Sha256(protectedResumePayload),
                IsDeleted = false,
                IsActive = true
            }).ExecuteCommandAsync();
            await Db.Ado.CommitTranAsync();
            return true;
        }
        catch
        {
            await Db.Ado.RollbackTranAsync();
            throw;
        }
    }

    public async Task<ToolApprovalRequestRecord?> GetAsync(
        Guid id,
        string tenantId,
        CancellationToken cancellationToken = default)
    {
        RequiredTenant(tenantId);
        cancellationToken.ThrowIfCancellationRequested();
        AgToolApprovalRequest? value = await Db.Queryable<AgToolApprovalRequest>()
            .Where(item => item.ID == id && item.TenantId == tenantId && !item.IsDeleted)
            .FirstAsync();
        return value is null ? null : MapRequest(value);
    }

    public async Task<IReadOnlyList<ToolApprovalRequestRecord>> ListAsync(
        ToolApprovalQuery query,
        CancellationToken cancellationToken = default)
    {
        ToolApprovalStateMachine.ValidateQuery(query);
        cancellationToken.ThrowIfCancellationRequested();
        List<AgToolApprovalRequest> values = await Db.Queryable<AgToolApprovalRequest>()
            .Where(value => value.TenantId == query.TenantId && !value.IsDeleted)
            .WhereIF(query.Status is not null, value => value.Status == (int)query.Status!.Value)
            .OrderBy(value => value.RequestedAtUtc, OrderByType.Desc)
            .OrderBy(value => value.ID)
            .Take(query.Take)
            .ToListAsync();
        return values.Select(MapRequest).ToArray();
    }

    public async Task<IReadOnlyList<ToolApprovalDecisionRecord>> ListDecisionsAsync(
        Guid approvalId,
        string tenantId,
        CancellationToken cancellationToken = default)
    {
        RequiredTenant(tenantId);
        cancellationToken.ThrowIfCancellationRequested();
        List<AgToolApprovalDecision> values = await Db.Queryable<AgToolApprovalDecision>()
            .Where(value => value.ApprovalId == approvalId &&
                            value.TenantId == tenantId && !value.IsDeleted)
            .OrderBy(value => value.ResultingLogicalRevision)
            .OrderBy(value => value.ID)
            .ToListAsync();
        return values.Select(MapDecision).ToArray();
    }

    public async Task<bool> TryReplaceAsync(
        ToolApprovalRequestRecord replacement,
        long expectedLogicalRevision,
        CancellationToken cancellationToken = default)
    {
        ToolApprovalRequestRecord? existing = await GetAsync(
            replacement.Id, replacement.TenantId, cancellationToken);
        if (existing is null || existing.LogicalRevision != expectedLogicalRevision)
        {
            return false;
        }

        ToolApprovalStateMachine.ValidateReplacement(existing, replacement);
        await Db.Ado.BeginTranAsync();
        try
        {
            int updated = await UpdateStateAsync(
                replacement,
                expectedLogicalRevision,
                existing.Status);
            if (updated != 1)
            {
                await Db.Ado.RollbackTranAsync();
                return false;
            }

            if (IsHumanDecision(existing.Status, replacement.Status))
            {
                await Db.Insertable(new AgToolApprovalDecision
                {
                    ID = Guid.NewGuid(),
                    ApprovalId = replacement.Id,
                    TenantId = replacement.TenantId,
                    FromStatus = (int)existing.Status,
                    ToStatus = (int)replacement.Status,
                    DecisionUserId = replacement.DecisionUserId,
                    DecisionReason = replacement.DecisionReason,
                    DecidedAtUtc = replacement.DecidedAtUtc!.Value.UtcDateTime,
                    ResultingLogicalRevision = replacement.LogicalRevision,
                    IsDeleted = false,
                    IsActive = true
                }).ExecuteCommandAsync();
            }

            await Db.Ado.CommitTranAsync();
            return true;
        }
        catch
        {
            await Db.Ado.RollbackTranAsync();
            throw;
        }
    }

    public async Task<ToolApprovalExecutionClaim?> TryClaimExecutionAsync(
        Guid id,
        string tenantId,
        long expectedLogicalRevision,
        DateTimeOffset claimedAtUtc,
        CancellationToken cancellationToken = default)
    {
        RequiredTenant(tenantId);
        cancellationToken.ThrowIfCancellationRequested();
        DateTime claimed = claimedAtUtc.UtcDateTime;
        await Db.Ado.BeginTranAsync();
        try
        {
            int updated = await Db.Updateable<AgToolApprovalRequest>()
                .SetColumns(value => new AgToolApprovalRequest
                {
                    Status = (int)ToolApprovalStatus.Consuming,
                    LogicalRevision = value.LogicalRevision + 1,
                    ClaimedAtUtc = claimed
                })
                .Where(value => value.ID == id && value.TenantId == tenantId &&
                                value.LogicalRevision == expectedLogicalRevision &&
                                value.Status == (int)ToolApprovalStatus.Approved &&
                                value.RequestedAtUtc <= claimed && value.ExpiresAtUtc > claimed &&
                                !value.IsDeleted)
                .ExecuteCommandAsync();
            if (updated != 1)
            {
                await Db.Ado.RollbackTranAsync();
                return null;
            }

            AgToolApprovalRequest? request = await Db.Queryable<AgToolApprovalRequest>()
                .Where(value => value.ID == id && value.TenantId == tenantId && !value.IsDeleted)
                .FirstAsync();
            AgToolApprovalPayload? payload = await Db.Queryable<AgToolApprovalPayload>()
                .Where(value => value.ApprovalId == id && !value.IsDeleted)
                .FirstAsync();
            if (request is null || payload is null)
            {
                await Db.Ado.RollbackTranAsync();
                return null;
            }

            string protectedPayload = Required(payload.ProtectedPayload, "ProtectedPayload");
            string payloadSha256 = Required(payload.ProtectedPayloadSha256, "ProtectedPayloadSha256");
            if (!string.Equals(Sha256(protectedPayload), payloadSha256, StringComparison.Ordinal))
            {
                throw new InvalidDataException("The protected tool approval payload failed its integrity check.");
            }

            try
            {
                ToolApprovalStateMachine.ValidateProtectedPayload(protectedPayload);
            }
            catch (ToolApprovalException exception)
            {
                throw new InvalidDataException(
                    "The protected tool approval payload envelope is invalid.", exception);
            }

            ToolApprovalRequestRecord result = MapRequest(request);
            await Db.Ado.CommitTranAsync();
            return new ToolApprovalExecutionClaim(result, protectedPayload, payloadSha256);
        }
        catch
        {
            await Db.Ado.RollbackTranAsync();
            throw;
        }
    }

    public async Task<bool> TryCompleteExecutionAsync(
        ToolApprovalRequestRecord replacement,
        long expectedLogicalRevision,
        ToolApprovalExecutionResultRecord result,
        CancellationToken cancellationToken = default)
    {
        try
        {
            ToolApprovalStateMachine.ValidateExecutionResultEnvelope(result);
        }
        catch (ToolApprovalException)
        {
            return false;
        }

        ToolApprovalRequestRecord? existing = await GetAsync(
            replacement.Id, replacement.TenantId, cancellationToken);
        if (existing is null || existing.LogicalRevision != expectedLogicalRevision ||
            !ValidResult(replacement, result))
        {
            return false;
        }

        ToolApprovalStateMachine.ValidateReplacement(existing, replacement);
        await Db.Ado.BeginTranAsync();
        try
        {
            if (await UpdateStateAsync(replacement, expectedLogicalRevision, existing.Status) != 1)
            {
                await Db.Ado.RollbackTranAsync();
                return false;
            }

            await Db.Insertable(new AgToolApprovalExecutionResult
            {
                ID = Guid.NewGuid(),
                ApprovalId = result.ApprovalId,
                TenantId = result.TenantId,
                Succeeded = result.Succeeded,
                Blocked = result.Blocked,
                ProtectedContent = result.ProtectedContent,
                ProtectedContentSha256 = result.ProtectedContentSha256,
                ContentSha256 = result.ContentSha256,
                ErrorCode = result.ErrorCode,
                FinishedAtUtc = result.FinishedAtUtc.UtcDateTime,
                IsDeleted = false,
                IsActive = true
            }).ExecuteCommandAsync();
            await Db.Ado.CommitTranAsync();
            return true;
        }
        catch
        {
            await Db.Ado.RollbackTranAsync();
            throw;
        }
    }

    public async Task<ToolApprovalExecutionResultRecord?> GetExecutionResultAsync(
        Guid id,
        string tenantId,
        CancellationToken cancellationToken = default)
    {
        RequiredTenant(tenantId);
        cancellationToken.ThrowIfCancellationRequested();
        AgToolApprovalExecutionResult? value = await Db.Queryable<AgToolApprovalExecutionResult>()
            .Where(item => item.ApprovalId == id && item.TenantId == tenantId && !item.IsDeleted)
            .FirstAsync();
        if (value is null)
        {
            return null;
        }

        ToolApprovalExecutionResultRecord result = MapExecutionResult(value);
        try
        {
            ToolApprovalStateMachine.ValidateExecutionResultEnvelope(result);
        }
        catch (ToolApprovalException exception)
        {
            throw new InvalidDataException(
                "The protected tool approval result envelope is invalid.", exception);
        }

        return result;
    }

    public async Task<int> RecoverInterruptedExecutionsAsync(
        DateTimeOffset recoveredAtUtc,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        DateTime recovered = recoveredAtUtc.UtcDateTime;
        return await Db.Updateable<AgToolApprovalRequest>()
            .SetColumns(value => new AgToolApprovalRequest
            {
                Status = (int)ToolApprovalStatus.Failed,
                LogicalRevision = value.LogicalRevision + 1,
                FinishedAtUtc = value.ClaimedAtUtc > recovered ? value.ClaimedAtUtc : recovered,
                ErrorCode = ToolApprovalErrorCodes.ExecutionOutcomeUnknown
            })
            .Where(value => value.Status == (int)ToolApprovalStatus.Consuming &&
                            !SqlFunc.Subqueryable<AgToolApprovalExecutionResult>()
                                .Where(result => result.ApprovalId == value.ID && !result.IsDeleted)
                                .Any() &&
                            !value.IsDeleted)
            .ExecuteCommandAsync();
    }

    private async Task<int> UpdateStateAsync(
        ToolApprovalRequestRecord replacement,
        long expectedLogicalRevision,
        ToolApprovalStatus expectedStatus)
    {
        DateTime? decidedAtUtc = replacement.DecidedAtUtc?.UtcDateTime;
        DateTime? claimedAtUtc = replacement.ClaimedAtUtc?.UtcDateTime;
        DateTime? finishedAtUtc = replacement.FinishedAtUtc?.UtcDateTime;
        return await Db.Updateable<AgToolApprovalRequest>()
            .SetColumns(_ => new AgToolApprovalRequest
            {
                Status = (int)replacement.Status,
                LogicalRevision = replacement.LogicalRevision,
                DecisionUserId = replacement.DecisionUserId,
                DecisionReason = replacement.DecisionReason,
                DecidedAtUtc = decidedAtUtc,
                ClaimedAtUtc = claimedAtUtc,
                FinishedAtUtc = finishedAtUtc,
                ErrorCode = replacement.ErrorCode
            })
            .Where(value => value.ID == replacement.Id &&
                            value.TenantId == replacement.TenantId &&
                            value.LogicalRevision == expectedLogicalRevision &&
                            value.Status == (int)expectedStatus && !value.IsDeleted)
            .ExecuteCommandAsync();
    }

    private static AgToolApprovalRequest MapRequestEntity(ToolApprovalRequestRecord value) => new()
    {
        ID = value.Id,
        TenantId = value.TenantId,
        RequesterUserId = value.RequesterUserId,
        ConversationId = value.ConversationId,
        EntryRunId = value.EntryRunId,
        AgentRunId = value.AgentRunId,
        AgentVersionId = value.AgentVersionId,
        McpServerId = value.McpServerId,
        ToolVersionId = value.ToolVersionId,
        ToolName = value.ToolName,
        Risk = (int)value.Risk,
        ToolSchemaSha256 = value.ToolSchemaSha256,
        ArgumentsSha256 = value.ArgumentsSha256,
        SafeArgumentsSummaryJson = value.SafeArgumentsSummaryJson,
        Status = (int)value.Status,
        LogicalRevision = value.LogicalRevision,
        RequestedAtUtc = value.RequestedAtUtc.UtcDateTime,
        ExpiresAtUtc = value.ExpiresAtUtc.UtcDateTime,
        DecisionUserId = value.DecisionUserId,
        DecisionReason = value.DecisionReason,
        DecidedAtUtc = value.DecidedAtUtc?.UtcDateTime,
        ClaimedAtUtc = value.ClaimedAtUtc?.UtcDateTime,
        FinishedAtUtc = value.FinishedAtUtc?.UtcDateTime,
        ErrorCode = value.ErrorCode,
        IsDeleted = false,
        IsActive = true
    };

    private static ToolApprovalRequestRecord MapRequest(AgToolApprovalRequest value) => new(
        value.ID,
        Required(value.TenantId, "TenantId"),
        Required(value.RequesterUserId, "RequesterUserId"),
        Required(value.ConversationId, "ConversationId"),
        Required(value.EntryRunId, "EntryRunId"),
        Required(value.AgentRunId, "AgentRunId"),
        Required(value.AgentVersionId, "AgentVersionId"),
        Required(value.McpServerId, "McpServerId"),
        Required(value.ToolVersionId, "ToolVersionId"),
        Required(value.ToolName, "ToolName"),
        (McpToolRisk)Required(value.Risk, "Risk"),
        Required(value.ToolSchemaSha256, "ToolSchemaSha256"),
        Required(value.ArgumentsSha256, "ArgumentsSha256"),
        Required(value.SafeArgumentsSummaryJson, "SafeArgumentsSummaryJson"),
        (ToolApprovalStatus)Required(value.Status, "Status"),
        Required(value.LogicalRevision, "LogicalRevision"),
        ToOffset(Required(value.RequestedAtUtc, "RequestedAtUtc")),
        ToOffset(Required(value.ExpiresAtUtc, "ExpiresAtUtc")),
        Required(value.DecisionUserId, "DecisionUserId"),
        Required(value.DecisionReason, "DecisionReason"),
        value.DecidedAtUtc.HasValue ? ToOffset(value.DecidedAtUtc.Value) : null,
        value.ClaimedAtUtc.HasValue ? ToOffset(value.ClaimedAtUtc.Value) : null,
        value.FinishedAtUtc.HasValue ? ToOffset(value.FinishedAtUtc.Value) : null,
        Required(value.ErrorCode, "ErrorCode"));

    private static ToolApprovalDecisionRecord MapDecision(AgToolApprovalDecision value) => new(
        value.ID,
        Required(value.ApprovalId, "Decision.ApprovalId"),
        Required(value.TenantId, "Decision.TenantId"),
        (ToolApprovalStatus)Required(value.FromStatus, "Decision.FromStatus"),
        (ToolApprovalStatus)Required(value.ToStatus, "Decision.ToStatus"),
        Required(value.DecisionUserId, "Decision.DecisionUserId"),
        Required(value.DecisionReason, "Decision.DecisionReason"),
        ToOffset(Required(value.DecidedAtUtc, "Decision.DecidedAtUtc")),
        Required(value.ResultingLogicalRevision, "Decision.ResultingLogicalRevision"));

    private static ToolApprovalExecutionResultRecord MapExecutionResult(
        AgToolApprovalExecutionResult value) => new(
        Required(value.ApprovalId, "ExecutionResult.ApprovalId"),
        Required(value.TenantId, "ExecutionResult.TenantId"),
        Required(value.Succeeded, "ExecutionResult.Succeeded"),
        Required(value.Blocked, "ExecutionResult.Blocked"),
        Required(value.ProtectedContent, "ExecutionResult.ProtectedContent"),
        Required(value.ProtectedContentSha256, "ExecutionResult.ProtectedContentSha256"),
        Required(value.ContentSha256, "ExecutionResult.ContentSha256"),
        Required(value.ErrorCode, "ExecutionResult.ErrorCode"),
        ToOffset(Required(value.FinishedAtUtc, "ExecutionResult.FinishedAtUtc")));

    private static bool ValidResult(
        ToolApprovalRequestRecord replacement,
        ToolApprovalExecutionResultRecord result) =>
        result.ApprovalId == replacement.Id &&
        string.Equals(result.TenantId, replacement.TenantId, StringComparison.Ordinal) &&
        result.FinishedAtUtc == replacement.FinishedAtUtc &&
        result.Succeeded == (replacement.Status == ToolApprovalStatus.Consumed) &&
        string.Equals(result.ErrorCode, replacement.ErrorCode, StringComparison.Ordinal);

    private static bool IsHumanDecision(ToolApprovalStatus from, ToolApprovalStatus to) =>
        from == ToolApprovalStatus.Pending &&
        to is ToolApprovalStatus.Approved or ToolApprovalStatus.Rejected or ToolApprovalStatus.Cancelled;

    private static void RequiredTenant(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ToolApprovalException(
                ToolApprovalErrorCodes.Invalid,
                "The tool approval tenant is required.");
        }
    }

    private static string Sha256(string value) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    private static DateTimeOffset ToOffset(DateTime value) =>
        new(DateTime.SpecifyKind(value, DateTimeKind.Utc));

    private static T Required<T>(T? value, string field) where T : struct =>
        value ?? throw new InvalidDataException($"Tool approval field '{field}' is missing.");

    private static string Required(string? value, string field) =>
        value ?? throw new InvalidDataException($"Tool approval field '{field}' is missing.");
}
