#nullable enable

using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;

namespace EU.Core.Extensions;

/// <summary>
/// 单个固定窗口限流分区的配置。
/// </summary>
public sealed record FixedWindowRateLimitPartition
{
    public FixedWindowRateLimitPartition(
        string partitionKey,
        int permitLimit,
        TimeSpan window)
    {
        if (string.IsNullOrWhiteSpace(partitionKey))
            throw new ArgumentException("The partition key is required.", nameof(partitionKey));
        if (permitLimit < 1)
            throw new ArgumentOutOfRangeException(nameof(permitLimit), permitLimit,
                "The permit limit must be greater than zero.");
        if (window <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(window), window,
                "The rate limit window must be greater than zero.");

        PartitionKey = partitionKey;
        PermitLimit = permitLimit;
        Window = window;
    }

    public string PartitionKey { get; }

    public int PermitLimit { get; }

    public TimeSpan Window { get; }
}

/// <summary>
/// 基于请求动态分区的固定窗口限流注册扩展。
/// </summary>
public static class PartitionedFixedWindowRateLimitSetup
{
    private const string UnlimitedPartitionKey = "rate-limit-disabled";

    public static IServiceCollection AddPartitionedFixedWindowRateLimit(
        this IServiceCollection services,
        Func<HttpContext, FixedWindowRateLimitPartition?> resolvePartition,
        Func<OnRejectedContext, CancellationToken, ValueTask>? onRejected = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(resolvePartition);

        services.AddRateLimiter(options =>
        {
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            {
                FixedWindowRateLimitPartition? partition = resolvePartition(context);
                if (partition is null)
                    return RateLimitPartition.GetNoLimiter(UnlimitedPartitionKey);

                return RateLimitPartition.GetFixedWindowLimiter(
                    partition.PartitionKey,
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = partition.PermitLimit,
                        Window = partition.Window,
                        QueueLimit = 0,
                        AutoReplenishment = true
                    });
            });

            if (onRejected is not null)
                options.OnRejected = onRejected;
        });

        return services;
    }
}
