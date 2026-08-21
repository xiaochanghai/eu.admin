using EU.Core.IServices.Abstractions.Security;

#nullable enable

namespace EU.Core.Services;

public sealed class AgApiIdempotencyServices :
    BaseServices<AgApiIdempotency>,
    IAgApiIdempotencyServices,
    IHttpIdempotencyRepository
{
    public AgApiIdempotencyServices(IBaseRepository<AgApiIdempotency> dal)
        : base(dal ?? throw new ArgumentNullException(nameof(dal)))
    {
    }

    public async Task<HttpIdempotencyBeginResult> BeginAsync(
        HttpIdempotencyRecord pending,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pending);
        cancellationToken.ThrowIfCancellationRequested();

        DateTime now = nowUtc.UtcDateTime;
        await Db.Deleteable<AgApiIdempotency>()
            .Where(value => value.ExpiresAtUtc <= now)
            .ExecuteCommandAsync();

        AgApiIdempotency? existing = await GetByScopeAsync(pending.ScopeSha256);
        if (existing is not null)
        {
            return new HttpIdempotencyBeginResult(false, MapRecord(existing));
        }

        try
        {
            int inserted = await Db.Insertable(MapEntity(pending)).ExecuteCommandAsync();
            if (inserted == 1)
            {
                return new HttpIdempotencyBeginResult(true, Clone(pending));
            }
        }
        catch
        {
            existing = await GetByScopeAsync(pending.ScopeSha256);
            if (existing is null)
            {
                throw;
            }

            return new HttpIdempotencyBeginResult(false, MapRecord(existing));
        }

        existing = await GetByScopeAsync(pending.ScopeSha256)
            ?? throw new InvalidOperationException(
                "The idempotency record disappeared after a conflict.");
        return new HttpIdempotencyBeginResult(false, MapRecord(existing));
    }

    public async Task<bool> CompleteAsync(
        string scopeSha256,
        string requestSha256,
        int responseStatusCode,
        string responseContentType,
        string responseLocation,
        byte[] responseBody,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scopeSha256);
        ArgumentException.ThrowIfNullOrWhiteSpace(requestSha256);
        ArgumentNullException.ThrowIfNull(responseBody);
        cancellationToken.ThrowIfCancellationRequested();

        int updated = await Db.Updateable<AgApiIdempotency>()
            .SetColumns(_ => new AgApiIdempotency
            {
                Status = nameof(HttpIdempotencyStatus.Completed),
                ResponseStatusCode = responseStatusCode,
                ResponseContentType = responseContentType,
                ResponseLocation = responseLocation,
                ResponseBody = responseBody
            })
            .Where(value =>
                value.ScopeSha256 == scopeSha256 &&
                value.RequestSha256 == requestSha256 &&
                value.Status == nameof(HttpIdempotencyStatus.InProgress) &&
                !value.IsDeleted)
            .ExecuteCommandAsync();
        return updated == 1;
    }

    public async Task MarkIndeterminateAsync(
        string scopeSha256,
        string requestSha256,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scopeSha256);
        ArgumentException.ThrowIfNullOrWhiteSpace(requestSha256);
        cancellationToken.ThrowIfCancellationRequested();

        await Db.Updateable<AgApiIdempotency>()
            .SetColumns(value => value.Status == nameof(HttpIdempotencyStatus.Indeterminate))
            .Where(value =>
                value.ScopeSha256 == scopeSha256 &&
                value.RequestSha256 == requestSha256 &&
                value.Status == nameof(HttpIdempotencyStatus.InProgress) &&
                !value.IsDeleted)
            .ExecuteCommandAsync();
    }

    public async Task AbandonAsync(
        string scopeSha256,
        string requestSha256,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scopeSha256);
        ArgumentException.ThrowIfNullOrWhiteSpace(requestSha256);
        cancellationToken.ThrowIfCancellationRequested();

        await Db.Deleteable<AgApiIdempotency>()
            .Where(value =>
                value.ScopeSha256 == scopeSha256 &&
                value.RequestSha256 == requestSha256 &&
                value.Status == nameof(HttpIdempotencyStatus.InProgress))
            .ExecuteCommandAsync();
    }

    private async Task<AgApiIdempotency?> GetByScopeAsync(string scopeSha256) =>
        await Db.Queryable<AgApiIdempotency>()
            .Where(value => value.ScopeSha256 == scopeSha256 && !value.IsDeleted)
            .FirstAsync();

    private static AgApiIdempotency MapEntity(HttpIdempotencyRecord value) => new()
    {
        ID = Guid.NewGuid(),
        ScopeSha256 = value.ScopeSha256,
        RequestSha256 = value.RequestSha256,
        Status = value.Status.ToString(),
        ResponseStatusCode = value.ResponseStatusCode,
        ResponseContentType = value.ResponseContentType,
        ResponseLocation = value.ResponseLocation,
        ResponseBody = value.ResponseBody.ToArray(),
        CreatedAtUtc = value.CreatedAtUtc.UtcDateTime,
        ExpiresAtUtc = value.ExpiresAtUtc.UtcDateTime,
        IsDeleted = false,
        IsActive = true
    };

    private static HttpIdempotencyRecord MapRecord(AgApiIdempotency value) => new(
        Required(value.ScopeSha256, "ScopeSha256"),
        Required(value.RequestSha256, "RequestSha256"),
        Enum.Parse<HttpIdempotencyStatus>(Required(value.Status, "Status"), false),
        Required(value.ResponseStatusCode, "ResponseStatusCode"),
        Required(value.ResponseContentType, "ResponseContentType"),
        Required(value.ResponseLocation, "ResponseLocation"),
        Required(value.ResponseBody, "ResponseBody").ToArray(),
        ToOffset(Required(value.CreatedAtUtc, "CreatedAtUtc")),
        ToOffset(Required(value.ExpiresAtUtc, "ExpiresAtUtc")));

    private static HttpIdempotencyRecord Clone(HttpIdempotencyRecord value) =>
        value with { ResponseBody = value.ResponseBody.ToArray() };

    private static DateTimeOffset ToOffset(DateTime value) =>
        new(DateTime.SpecifyKind(value, DateTimeKind.Utc));

    private static T Required<T>(T? value, string field) where T : struct =>
        value ?? throw new InvalidDataException(
            $"Agent API idempotency field '{field}' is missing.");

    private static string Required(string? value, string field) =>
        value ?? throw new InvalidDataException(
            $"Agent API idempotency field '{field}' is missing.");

    private static byte[] Required(byte[]? value, string field) =>
        value ?? throw new InvalidDataException(
            $"Agent API idempotency field '{field}' is missing.");
}
