using System.Security.Cryptography;
using System.Text;
using EU.Core.IServices.Abstractions.Security;
using EU.Core.Model.Entity;
using EU.Core.Services;
using Xunit;

#nullable enable

namespace EU.Core.Tests.Service_Test;

public sealed class AgApiIdempotencyPersistence_Should
{
    [Fact]
    public async Task Persist_completed_response_and_replay_it_for_duplicate_scope()
    {
        using var fixture = new AgentPersistenceSqliteFixture(typeof(AgApiIdempotency));
        var service = new AgApiIdempotencyServices(
            fixture.CreateRepository<AgApiIdempotency>());
        DateTimeOffset now = DateTimeOffset.Parse("2026-08-16T05:00:00Z");
        HttpIdempotencyRecord pending = CreatePending("scope-a", "request-a", now);

        HttpIdempotencyBeginResult acquired = await service.BeginAsync(pending, now);
        HttpIdempotencyBeginResult duplicate = await service.BeginAsync(pending, now);

        Assert.True(acquired.Acquired);
        Assert.False(duplicate.Acquired);
        Assert.Equal(HttpIdempotencyStatus.InProgress, duplicate.Record.Status);

        byte[] body = Encoding.UTF8.GetBytes("{\"id\":\"resource-1\"}");
        Assert.True(await service.CompleteAsync(
            pending.ScopeSha256,
            pending.RequestSha256,
            201,
            "application/json",
            "/api/resources/resource-1",
            body));

        HttpIdempotencyBeginResult replay = await service.BeginAsync(
            pending,
            now.AddSeconds(1));

        Assert.False(replay.Acquired);
        Assert.Equal(HttpIdempotencyStatus.Completed, replay.Record.Status);
        Assert.Equal(201, replay.Record.ResponseStatusCode);
        Assert.Equal("application/json", replay.Record.ResponseContentType);
        Assert.Equal("/api/resources/resource-1", replay.Record.ResponseLocation);
        Assert.Equal(body, replay.Record.ResponseBody);
        Assert.False(await service.CompleteAsync(
            pending.ScopeSha256,
            pending.RequestSha256,
            200,
            "application/json",
            string.Empty,
            []));
    }

    [Fact]
    public async Task Support_indeterminate_abandon_and_expired_record_cleanup()
    {
        using var fixture = new AgentPersistenceSqliteFixture(typeof(AgApiIdempotency));
        var service = new AgApiIdempotencyServices(
            fixture.CreateRepository<AgApiIdempotency>());
        DateTimeOffset now = DateTimeOffset.Parse("2026-08-16T06:00:00Z");
        HttpIdempotencyRecord uncertain = CreatePending("scope-b", "request-b", now);

        Assert.True((await service.BeginAsync(uncertain, now)).Acquired);
        await service.MarkIndeterminateAsync(
            uncertain.ScopeSha256,
            uncertain.RequestSha256);
        HttpIdempotencyBeginResult marked = await service.BeginAsync(
            uncertain,
            now.AddSeconds(1));
        Assert.False(marked.Acquired);
        Assert.Equal(HttpIdempotencyStatus.Indeterminate, marked.Record.Status);

        HttpIdempotencyRecord abandoned = CreatePending("scope-c", "request-c", now);
        Assert.True((await service.BeginAsync(abandoned, now)).Acquired);
        await service.AbandonAsync(abandoned.ScopeSha256, abandoned.RequestSha256);
        Assert.True((await service.BeginAsync(abandoned, now.AddSeconds(1))).Acquired);

        HttpIdempotencyRecord expiring = CreatePending(
            "scope-expired",
            "request-expired",
            now,
            now.AddSeconds(2));
        Assert.True((await service.BeginAsync(expiring, now)).Acquired);
        HttpIdempotencyRecord trigger = CreatePending("scope-trigger", "request-trigger", now);
        Assert.True((await service.BeginAsync(trigger, now.AddSeconds(3))).Acquired);

        bool expiredStillExists = await fixture.Db.Queryable<AgApiIdempotency>()
            .Where(value => value.ScopeSha256 == expiring.ScopeSha256)
            .AnyAsync();
        Assert.False(expiredStillExists);
    }

    internal static HttpIdempotencyRecord CreatePending(
        string scope,
        string request,
        DateTimeOffset createdAtUtc,
        DateTimeOffset? expiresAtUtc = null) => new(
        Sha256(scope),
        Sha256(request),
        HttpIdempotencyStatus.InProgress,
        0,
        string.Empty,
        string.Empty,
        [],
        createdAtUtc,
        expiresAtUtc ?? createdAtUtc.AddMinutes(5));

    private static string Sha256(string value) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
