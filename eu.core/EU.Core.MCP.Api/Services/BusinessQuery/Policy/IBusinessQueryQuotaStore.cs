namespace EU.Core.Api.MCP.Services.BusinessQuery.Policy;

public sealed record BusinessQueryQuotaRequest(
    string UserId,
    string TenantId,
    string PlanHash,
    int Complexity,
    DateTimeOffset EvaluatedAtUtc);

public sealed record BusinessQueryQuotaReservationResult(
    bool Accepted,
    Guid? ReservationId)
{
    public static BusinessQueryQuotaReservationResult Allow(Guid reservationId) =>
        new(true, reservationId);

    public static BusinessQueryQuotaReservationResult Deny() => new(false, null);
}

public enum BusinessQueryQuotaOutcome
{
    Succeeded,
    Failed,
    Cancelled
}

public interface IBusinessQueryQuotaStore
{
    Task<BusinessQueryQuotaReservationResult> TryReserveAsync(
        BusinessQueryQuotaRequest request,
        CancellationToken cancellationToken);

    Task SettleAsync(
        Guid reservationId,
        BusinessQueryQuotaOutcome outcome,
        CancellationToken cancellationToken);
}
