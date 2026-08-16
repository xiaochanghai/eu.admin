using System.Security.Cryptography;
using System.Text;
using EU.Core.Agent.Application.Approvals;
using EU.Core.Agent.Application.Mcp;
using EU.Core.Model.Entity;
using EU.Core.Services;
using Xunit;

#nullable enable

namespace EU.Core.Tests.Service_Test;

public sealed class AgToolApprovalPersistence_Should
{
    [Fact]
    public async Task Persist_full_approval_execution_lifecycle()
    {
        using var fixture = CreateFixture();
        var service = CreateService(fixture);
        DateTimeOffset requestedAt = DateTimeOffset.Parse("2026-08-16T07:00:00Z");
        ToolApprovalRequestRecord pending = CreatePending(requestedAt);
        const string protectedResumePayload = "enc:v1:resume-payload";

        Assert.True(await service.TryCreateAsync(pending, protectedResumePayload));
        Assert.False(await service.TryCreateAsync(pending, protectedResumePayload));
        Assert.Null(await service.GetAsync(pending.Id, "another-tenant"));

        ToolApprovalRequestRecord approved = ToolApprovalStateMachine.Approve(
            pending,
            "approver-user",
            "approved for test",
            requestedAt.AddMinutes(1));
        Assert.True(await service.TryReplaceAsync(approved, expectedLogicalRevision: 0));
        Assert.False(await service.TryReplaceAsync(approved, expectedLogicalRevision: 0));

        IReadOnlyList<ToolApprovalDecisionRecord> decisions =
            await service.ListDecisionsAsync(pending.Id, pending.TenantId);
        ToolApprovalDecisionRecord decision = Assert.Single(decisions);
        Assert.Equal(ToolApprovalStatus.Pending, decision.FromStatus);
        Assert.Equal(ToolApprovalStatus.Approved, decision.ToStatus);
        Assert.Equal(1, decision.ResultingLogicalRevision);

        ToolApprovalExecutionClaim? claim = await service.TryClaimExecutionAsync(
            pending.Id,
            pending.TenantId,
            expectedLogicalRevision: 1,
            requestedAt.AddMinutes(2));
        Assert.NotNull(claim);
        Assert.Equal(ToolApprovalStatus.Consuming, claim.Request.Status);
        Assert.Equal(2, claim.Request.LogicalRevision);
        Assert.Equal(protectedResumePayload, claim.ProtectedResumePayload);
        Assert.Equal(Sha256(protectedResumePayload), claim.ProtectedResumePayloadSha256);
        Assert.Null(await service.TryClaimExecutionAsync(
            pending.Id,
            pending.TenantId,
            expectedLogicalRevision: 1,
            requestedAt.AddMinutes(2)));

        DateTimeOffset finishedAt = requestedAt.AddMinutes(3);
        ToolApprovalRequestRecord completed = ToolApprovalStateMachine.Complete(
            claim.Request,
            succeeded: true,
            errorCode: string.Empty,
            finishedAt);
        const string protectedContent = "enc:v1:tool-result";
        var executionResult = new ToolApprovalExecutionResultRecord(
            pending.Id,
            pending.TenantId,
            Succeeded: true,
            Blocked: false,
            protectedContent,
            Sha256(protectedContent),
            Sha256("plain tool result"),
            string.Empty,
            finishedAt);

        Assert.True(await service.TryCompleteExecutionAsync(
            completed,
            expectedLogicalRevision: 2,
            executionResult));
        Assert.False(await service.TryCompleteExecutionAsync(
            completed,
            expectedLogicalRevision: 2,
            executionResult));

        ToolApprovalRequestRecord? reloaded = await service.GetAsync(
            pending.Id,
            pending.TenantId);
        ToolApprovalExecutionResultRecord? reloadedResult =
            await service.GetExecutionResultAsync(pending.Id, pending.TenantId);
        Assert.NotNull(reloaded);
        Assert.NotNull(reloadedResult);
        Assert.Equal(ToolApprovalStatus.Consumed, reloaded.Status);
        Assert.Equal(3, reloaded.LogicalRevision);
        Assert.Equal(executionResult, reloadedResult);
    }

    [Fact]
    public async Task Recover_claimed_execution_without_a_persisted_result()
    {
        using var fixture = CreateFixture();
        var service = CreateService(fixture);
        DateTimeOffset requestedAt = DateTimeOffset.Parse("2026-08-16T08:00:00Z");
        ToolApprovalRequestRecord pending = CreatePending(requestedAt);

        Assert.True(await service.TryCreateAsync(pending, "enc:v1:resume-payload"));
        ToolApprovalRequestRecord approved = ToolApprovalStateMachine.Approve(
            pending,
            "approver-user",
            string.Empty,
            requestedAt.AddMinutes(1));
        Assert.True(await service.TryReplaceAsync(approved, 0));
        Assert.NotNull(await service.TryClaimExecutionAsync(
            pending.Id,
            pending.TenantId,
            1,
            requestedAt.AddMinutes(2)));

        Assert.Equal(1, await service.RecoverInterruptedExecutionsAsync(
            requestedAt.AddMinutes(3)));
        Assert.Equal(0, await service.RecoverInterruptedExecutionsAsync(
            requestedAt.AddMinutes(4)));

        ToolApprovalRequestRecord? recovered = await service.GetAsync(
            pending.Id,
            pending.TenantId);
        Assert.NotNull(recovered);
        Assert.Equal(ToolApprovalStatus.Failed, recovered.Status);
        Assert.Equal(3, recovered.LogicalRevision);
        Assert.Equal(ToolApprovalErrorCodes.ExecutionOutcomeUnknown, recovered.ErrorCode);
        Assert.Equal(requestedAt.AddMinutes(3), recovered.FinishedAtUtc);
        Assert.Null(await service.GetExecutionResultAsync(pending.Id, pending.TenantId));
    }

    private static AgentPersistenceSqliteFixture CreateFixture() => new(
        typeof(AgToolApprovalRequest),
        typeof(AgToolApprovalPayload),
        typeof(AgToolApprovalDecision),
        typeof(AgToolApprovalExecutionResult));

    private static AgToolApprovalRequestServices CreateService(
        AgentPersistenceSqliteFixture fixture) => new(
        fixture.CreateRepository<AgToolApprovalRequest>());

    internal static ToolApprovalRequestRecord CreatePending(
        DateTimeOffset requestedAt) => new(
        Guid.NewGuid(),
        "tenant-a",
        "requester-user",
        Guid.NewGuid(),
        Guid.NewGuid(),
        Guid.NewGuid(),
        Guid.NewGuid(),
        Guid.NewGuid(),
        Guid.NewGuid(),
        "business-query",
        McpToolRisk.Mutating,
        Sha256("tool-schema"),
        Sha256("arguments"),
        "{}",
        ToolApprovalStatus.Pending,
        0,
        requestedAt,
        requestedAt.AddMinutes(10),
        string.Empty,
        string.Empty,
        null,
        null,
        null,
        string.Empty);

    internal static string Sha256(string value) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
