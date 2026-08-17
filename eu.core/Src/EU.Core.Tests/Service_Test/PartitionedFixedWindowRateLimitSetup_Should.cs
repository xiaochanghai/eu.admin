#nullable enable

using System.Threading.RateLimiting;
using EU.Core.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace EU.Core.Tests.Service_Test;

public sealed class PartitionedFixedWindowRateLimitSetup_Should
{
    [Fact]
    public async Task Apply_partition_limits_and_preserve_unlimited_requests()
    {
        bool rejected = false;
        var services = new ServiceCollection();
        services.AddPartitionedFixedWindowRateLimit(
            context => context.Request.Path.StartsWithSegments("/api")
                ? new FixedWindowRateLimitPartition("user-1", 1, TimeSpan.FromMinutes(1))
                : null,
            (_, _) =>
            {
                rejected = true;
                return ValueTask.CompletedTask;
            });

        using ServiceProvider provider = services.BuildServiceProvider();
        RateLimiterOptions options = provider
            .GetRequiredService<IOptions<RateLimiterOptions>>()
            .Value;
        Assert.NotNull(options.GlobalLimiter);
        Assert.NotNull(options.OnRejected);

        var limited = new DefaultHttpContext();
        limited.Request.Path = "/api/agents";
        using RateLimitLease first = options.GlobalLimiter.AttemptAcquire(limited);
        using RateLimitLease second = options.GlobalLimiter.AttemptAcquire(limited);
        Assert.True(first.IsAcquired);
        Assert.False(second.IsAcquired);

        await options.OnRejected!(new OnRejectedContext
        {
            HttpContext = limited,
            Lease = second
        }, CancellationToken.None);
        Assert.True(rejected);

        var unlimited = new DefaultHttpContext();
        unlimited.Request.Path = "/health";
        using RateLimitLease unlimitedFirst = options.GlobalLimiter.AttemptAcquire(unlimited);
        using RateLimitLease unlimitedSecond = options.GlobalLimiter.AttemptAcquire(unlimited);
        Assert.True(unlimitedFirst.IsAcquired);
        Assert.True(unlimitedSecond.IsAcquired);
    }

    [Theory]
    [InlineData("", 1, 1)]
    [InlineData("partition", 0, 1)]
    [InlineData("partition", 1, 0)]
    public void Reject_invalid_partition_configuration(
        string partitionKey,
        int permitLimit,
        int windowSeconds)
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            new FixedWindowRateLimitPartition(
                partitionKey,
                permitLimit,
                TimeSpan.FromSeconds(windowSeconds)));
    }
}
