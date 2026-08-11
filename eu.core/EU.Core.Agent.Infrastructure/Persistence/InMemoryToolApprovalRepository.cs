using System.Collections.ObjectModel;
using System.Security.Cryptography;
using System.Text;
using EU.Core.Agent.Application.Approvals;

namespace EU.Core.Agent.Infrastructure.Persistence;

public sealed class InMemoryToolApprovalRepository : IToolApprovalRepository
{
    private readonly object _gate = new();
    private readonly Dictionary<Guid, ToolApprovalRequestRecord> _requests = [];
    private readonly Dictionary<Guid, (string Payload, string Sha256)> _payloads = [];
    private readonly Dictionary<Guid, ToolApprovalExecutionResultRecord> _results = [];
    private readonly List<ToolApprovalDecisionRecord> _decisions = [];

    public Task<bool> TryCreateAsync(
        ToolApprovalRequestRecord request,
        string protectedResumePayload,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ToolApprovalStateMachine.ValidateNew(request, protectedResumePayload);
        lock (_gate)
        {
            if (_requests.ContainsKey(request.Id))
            {
                return Task.FromResult(false);
            }

            _requests.Add(request.Id, request with { });
            _payloads.Add(
                request.Id,
                (protectedResumePayload, Sha256(protectedResumePayload)));
            return Task.FromResult(true);
        }
    }

    public Task<ToolApprovalRequestRecord?> GetAsync(
        Guid id,
        string tenantId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            ToolApprovalRequestRecord? value =
                _requests.GetValueOrDefault(id);
            return Task.FromResult(
                value is not null
                && string.Equals(value.TenantId, tenantId, StringComparison.Ordinal)
                    ? value with { }
                    : null);
        }
    }

    public Task<IReadOnlyList<ToolApprovalRequestRecord>> ListAsync(
        ToolApprovalQuery query,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ToolApprovalStateMachine.ValidateQuery(query);
        lock (_gate)
        {
            IReadOnlyList<ToolApprovalRequestRecord> values =
                new ReadOnlyCollection<ToolApprovalRequestRecord>(
                    _requests.Values
                        .Where(value =>
                            string.Equals(
                                value.TenantId,
                                query.TenantId,
                                StringComparison.Ordinal)
                            && (query.Status is null
                                || value.Status == query.Status.Value))
                        .OrderByDescending(value => value.RequestedAtUtc)
                        .ThenBy(value => value.Id)
                        .Take(query.Take)
                        .Select(value => value with { })
                        .ToArray());
            return Task.FromResult(values);
        }
    }

    public Task<IReadOnlyList<ToolApprovalDecisionRecord>> ListDecisionsAsync(
        Guid approvalId,
        string tenantId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            IReadOnlyList<ToolApprovalDecisionRecord> values =
                new ReadOnlyCollection<ToolApprovalDecisionRecord>(
                    _decisions
                        .Where(value =>
                            value.ApprovalId == approvalId
                            && string.Equals(
                                value.TenantId,
                                tenantId,
                                StringComparison.Ordinal))
                        .OrderBy(value => value.ResultingLogicalRevision)
                        .Select(value => value with { })
                        .ToArray());
            return Task.FromResult(values);
        }
    }

    public Task<bool> TryReplaceAsync(
        ToolApprovalRequestRecord replacement,
        long expectedLogicalRevision,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            if (!_requests.TryGetValue(
                    replacement.Id,
                    out ToolApprovalRequestRecord? existing)
                || !string.Equals(
                    existing.TenantId,
                    replacement.TenantId,
                    StringComparison.Ordinal)
                || existing.LogicalRevision != expectedLogicalRevision)
            {
                return Task.FromResult(false);
            }

            ToolApprovalStateMachine.ValidateReplacement(existing, replacement);
            _requests[replacement.Id] = replacement with { };
            if (IsHumanDecision(existing, replacement))
            {
                _decisions.Add(CreateDecision(existing, replacement));
            }
            return Task.FromResult(true);
        }
    }

    public Task<ToolApprovalExecutionClaim?> TryClaimExecutionAsync(
        Guid id,
        string tenantId,
        long expectedLogicalRevision,
        DateTimeOffset claimedAtUtc,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            if (!_requests.TryGetValue(id, out ToolApprovalRequestRecord? existing)
                || !string.Equals(existing.TenantId, tenantId, StringComparison.Ordinal)
                || existing.LogicalRevision != expectedLogicalRevision
                || !_payloads.TryGetValue(id, out var payload))
            {
                return Task.FromResult<ToolApprovalExecutionClaim?>(null);
            }

            try
            {
                ToolApprovalStateMachine.ValidateProtectedPayload(payload.Payload);
            }
            catch (ToolApprovalException)
            {
                return Task.FromResult<ToolApprovalExecutionClaim?>(null);
            }

            if (!string.Equals(
                payload.Sha256,
                Sha256(payload.Payload),
                StringComparison.Ordinal))
            {
                return Task.FromResult<ToolApprovalExecutionClaim?>(null);
            }

            ToolApprovalRequestRecord claimed;
            try
            {
                claimed = ToolApprovalStateMachine.Claim(existing, claimedAtUtc);
            }
            catch (ToolApprovalException)
            {
                return Task.FromResult<ToolApprovalExecutionClaim?>(null);
            }

            _requests[id] = claimed;
            return Task.FromResult<ToolApprovalExecutionClaim?>(
                new ToolApprovalExecutionClaim(
                    claimed with { },
                    payload.Payload,
                    payload.Sha256));
        }
    }

    public Task<bool> TryCompleteExecutionAsync(
        ToolApprovalRequestRecord replacement,
        long expectedLogicalRevision,
        ToolApprovalExecutionResultRecord result,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            ToolApprovalStateMachine.ValidateExecutionResultEnvelope(result);
        }
        catch (ToolApprovalException)
        {
            return Task.FromResult(false);
        }

        lock (_gate)
        {
            if (!_requests.TryGetValue(replacement.Id, out ToolApprovalRequestRecord? existing)
                || existing.LogicalRevision != expectedLogicalRevision
                || _results.ContainsKey(replacement.Id)
                || !ValidResult(replacement, result))
            {
                return Task.FromResult(false);
            }

            ToolApprovalStateMachine.ValidateReplacement(existing, replacement);
            _requests[replacement.Id] = replacement with { };
            _results[replacement.Id] = result with { };
            return Task.FromResult(true);
        }
    }

    public Task<ToolApprovalExecutionResultRecord?> GetExecutionResultAsync(
        Guid id,
        string tenantId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            ToolApprovalExecutionResultRecord? value = _results.GetValueOrDefault(id);
            return Task.FromResult(
                value is not null
                && string.Equals(value.TenantId, tenantId, StringComparison.Ordinal)
                    ? value with { }
                    : null);
        }
    }

    public Task<int> RecoverInterruptedExecutionsAsync(
        DateTimeOffset recoveredAtUtc,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            ToolApprovalRequestRecord[] interrupted = _requests.Values
                .Where(value => value.Status == ToolApprovalStatus.Consuming
                    && !_results.ContainsKey(value.Id))
                .ToArray();
            foreach (ToolApprovalRequestRecord value in interrupted)
            {
                _requests[value.Id] = ToolApprovalStateMachine.RecoverUnknownOutcome(
                    value,
                    recoveredAtUtc < value.ClaimedAtUtc!.Value
                        ? value.ClaimedAtUtc.Value
                        : recoveredAtUtc);
            }
            return Task.FromResult(interrupted.Length);
        }
    }

    private static string Sha256(string value) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    private static bool ValidResult(
        ToolApprovalRequestRecord replacement,
        ToolApprovalExecutionResultRecord result) =>
        result.ApprovalId == replacement.Id
        && string.Equals(result.TenantId, replacement.TenantId, StringComparison.Ordinal)
        && result.FinishedAtUtc == replacement.FinishedAtUtc
        && result.Succeeded == (replacement.Status == ToolApprovalStatus.Consumed)
        && string.Equals(result.ErrorCode, replacement.ErrorCode, StringComparison.Ordinal);

    private static bool IsHumanDecision(
        ToolApprovalRequestRecord existing,
        ToolApprovalRequestRecord replacement) =>
        existing.Status == ToolApprovalStatus.Pending
        && replacement.Status is ToolApprovalStatus.Approved
            or ToolApprovalStatus.Rejected
            or ToolApprovalStatus.Cancelled;

    private static ToolApprovalDecisionRecord CreateDecision(
        ToolApprovalRequestRecord existing,
        ToolApprovalRequestRecord replacement) =>
        new(
            Guid.NewGuid(),
            replacement.Id,
            replacement.TenantId,
            existing.Status,
            replacement.Status,
            replacement.DecisionUserId,
            replacement.DecisionReason,
            replacement.DecidedAtUtc!.Value,
            replacement.LogicalRevision);
}
