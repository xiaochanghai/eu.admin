using EU.Core.Api.Agent.Configuration;
using EU.Core.IServices;
using EU.Core.IServices.Tasks;
using Microsoft.Extensions.Options;

#nullable enable

namespace EU.Core.Api.Agent.Background;

public sealed class AgentTaskWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<AgentTaskWorkerOptions> options,
    TimeProvider timeProvider,
    ILogger<AgentTaskWorker> logger) : BackgroundService
{
    private readonly AgentTaskWorkerOptions _options = options.Value;
    private readonly string _workerId = string.IsNullOrWhiteSpace(options.Value.WorkerId)
        ? $"{Environment.MachineName}:{Environment.ProcessId}"
        : options.Value.WorkerId.Trim();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled) return;

        TimeSpan pollInterval = TimeSpan.FromSeconds(Math.Clamp(_options.PollIntervalSeconds, 1, 60));
        TimeSpan leaseDuration = TimeSpan.FromSeconds(Math.Clamp(_options.LeaseSeconds, 30, 3600));
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using IServiceScope scope = scopeFactory.CreateScope();
                IAgAgentTaskServices tasks = scope.ServiceProvider.GetRequiredService<IAgAgentTaskServices>();
                IAgentTaskExecutor[] executors = scope.ServiceProvider.GetServices<IAgentTaskExecutor>()
                    .OrderBy(value => value.SourceType, StringComparer.Ordinal)
                    .ToArray();
                bool executed = false;
                foreach (IAgentTaskExecutor executor in executors)
                {
                    AgentTaskRecord? task = await tasks.TryClaimNextAsync(new ClaimAgentTaskCommand(
                        string.Empty, _workerId, leaseDuration, timeProvider.GetUtcNow(), true,
                        executor.SourceType), stoppingToken);
                    if (task is null) continue;

                    executed = true;
                    await executor.ExecuteAsync(
                        new AgentTaskExecutionContext(task, _workerId, leaseDuration), stoppingToken);
                    break;
                }

                if (!executed) await Task.Delay(pollInterval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Agent task worker iteration failed.");
                await Task.Delay(pollInterval, stoppingToken);
            }
        }
    }
}
