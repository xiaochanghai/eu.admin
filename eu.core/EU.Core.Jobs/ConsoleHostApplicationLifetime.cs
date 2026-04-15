using Microsoft.Extensions.Hosting;

namespace EU.Core.Jobs;

internal sealed class ConsoleHostApplicationLifetime : IHostApplicationLifetime
{
    private readonly CancellationTokenSource _started = new();
    private readonly CancellationTokenSource _stopping = new();
    private readonly CancellationTokenSource _stopped = new();

    public ConsoleHostApplicationLifetime()
    {
        _started.Cancel();
    }

    public CancellationToken ApplicationStarted => _started.Token;

    public CancellationToken ApplicationStopping => _stopping.Token;

    public CancellationToken ApplicationStopped => _stopped.Token;

    public void StopApplication()
    {
        if (!_stopping.IsCancellationRequested)
        {
            _stopping.Cancel();
        }

        if (!_stopped.IsCancellationRequested)
        {
            _stopped.Cancel();
        }
    }

    public void RegisterConsoleCancel()
    {
        Console.CancelKeyPress += (s, e) =>
        {
            e.Cancel = true;
            StopApplication();
        };
    }
}
