using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Text;

namespace EU.Core.EventBus;

/// <summary>
/// RabbitMQ持久连接
/// </summary>
public class RabbitMQPersistentConnection : IRabbitMQPersistentConnection
{
    private static readonly CreateChannelOptions DefaultChannelOptions = new(
        publisherConfirmationsEnabled: false,
        publisherConfirmationTrackingEnabled: false,
        outstandingPublisherConfirmationsRateLimiter: null,
        consumerDispatchConcurrency: 1);

    private readonly IConnectionFactory _connectionFactory;
    private readonly ILogger<RabbitMQPersistentConnection> _logger;
    private readonly int _retryCount;

    private IConnection? _connection;
    private bool _disposed;

    private readonly SemaphoreSlim _syncRoot = new(1, 1);

    // StartConsumingAsync 需要长生命周期 channel，避免 using 立即 dispose
    private readonly ConcurrentDictionary<string, IChannel> _consumerChannels = new();

    public RabbitMQPersistentConnection(IConnectionFactory connectionFactory, ILogger<RabbitMQPersistentConnection> logger, int retryCount = 5)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _retryCount = retryCount;
    }

    /// <summary>
    /// 是否已连接
    /// </summary>
    public bool IsConnected => _connection != null && _connection.IsOpen && !_disposed;

    /// <summary>
    /// 创建 Channel
    /// </summary>
    public async Task<IChannel> CreateChannelAsync(CancellationToken cancellationToken = default)
    {
        if (!IsConnected)
        {
            // 尽量自愈：调用方大多期望“能连上就继续”，否则抛错。
            await TryConnectAsync(cancellationToken).ConfigureAwait(false);
        }

        if (!IsConnected)
        {
            throw new InvalidOperationException("No RabbitMQ connections are available to perform this action");
        }

        return await _connection!.CreateChannelAsync(DefaultChannelOptions, cancellationToken).ConfigureAwait(false);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        foreach (var kv in _consumerChannels)
        {
            try { kv.Value.Dispose(); } catch { /* ignore */ }
        }
        _consumerChannels.Clear();

        try
        {
            _connection?.Dispose();
        }
        catch (IOException ex)
        {
            _logger.LogCritical(ex.ToString());
        }
        catch
        {
            // ignore
        }
    }

    /// <summary>
    /// 连接
    /// </summary>
    public async Task<bool> TryConnectAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("RabbitMQ Client is trying to connect");

        await _syncRoot.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (IsConnected) return true;

            AsyncRetryPolicy policy = Policy
                .Handle<SocketException>()
                .Or<BrokerUnreachableException>()
                .WaitAndRetryAsync(
                    _retryCount,
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    (ex, time) =>
                    {
                        _logger.LogWarning(ex, "RabbitMQ Client could not connect after {TimeOut}s ({ExceptionMessage})", $"{time.TotalSeconds:n1}", ex.Message);
                        return Task.CompletedTask;
                    });

            await policy.ExecuteAsync(async ct =>
            {
                _connection = await _connectionFactory.CreateConnectionAsync(ct).ConfigureAwait(false);
            }, cancellationToken).ConfigureAwait(false);

            if (IsConnected)
            {
                // RabbitMQ.Client v7: 事件接口调整为 Async 版本（如果当前版本存在这些事件）。
                try
                {
                    _connection!.ConnectionShutdownAsync += OnConnectionShutdownAsync;
                    _connection.CallbackExceptionAsync += OnCallbackExceptionAsync;
                    _connection.ConnectionBlockedAsync += OnConnectionBlockedAsync;
                }
                catch
                {
                    // 某些版本/目标框架下事件可能不可用：不阻塞连接成功，交由上层异常/重试兜底。
                }

                _logger.LogInformation(
                    "RabbitMQ Client acquired a persistent connection to '{HostName}'",
                    _connection!.Endpoint.HostName);

                return true;
            }

            _logger.LogCritical("FATAL ERROR: RabbitMQ connections could not be created and opened");
            return false;
        }
        finally
        {
            _syncRoot.Release();
        }
    }

    // ---- v7 Async 事件：用于触发更高层的“断线重连”策略（仅做日志 + 标记，核心重连在调用路径中完成） ----
    private Task OnConnectionBlockedAsync(object? sender, ConnectionBlockedEventArgs e)
    {
        if (_disposed) return Task.CompletedTask;
        _logger.LogWarning("RabbitMQ connection is blocked. Reason: {Reason}", e.Reason);
        return Task.CompletedTask;
    }

    private async Task OnCallbackExceptionAsync(object? sender, CallbackExceptionEventArgs e)
    {
        if (_disposed) return;
        _logger.LogWarning(e.Exception, "RabbitMQ connection threw an exception (callback). Trying to reconnect...");

        // 不在事件线程里做重连风暴：只尝试一次，失败留给下一次业务调用触发重连。
        try { await TryConnectAsync(CancellationToken.None).ConfigureAwait(false); } catch { /* ignore */ }
    }

    private async Task OnConnectionShutdownAsync(object? sender, ShutdownEventArgs e)
    {
        if (_disposed) return;
        _logger.LogWarning("RabbitMQ connection is shutdown. ReplyText: {ReplyText}", e.ReplyText);
        try { await TryConnectAsync(CancellationToken.None).ConfigureAwait(false); } catch { /* ignore */ }
    }

    public async Task PublishMessageAsync(string message, string exchangeName, string routingKey, CancellationToken cancellationToken = default)
    {
        await using var channel = await CreateChannelAsync(cancellationToken).ConfigureAwait(false);

        await channel.ExchangeDeclareAsync(exchange: exchangeName, type: ExchangeType.Direct, durable: true, autoDelete: false,
                arguments: null, passive: false, noWait: false, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        var body = Encoding.UTF8.GetBytes(message);
        // v7: BasicPublishAsync<TProperties> 为泛型；这里使用扩展方法的便捷重载
        await channel.BasicPublishAsync(exchangeName, routingKey, body, cancellationToken).ConfigureAwait(false);
    }

    public async Task StartConsumingAsync(string queueName, CancellationToken cancellationToken = default)
    {
        // 已经在消费就不重复创建
        if (_consumerChannels.ContainsKey(queueName))
            return;

        var channel = await CreateChannelAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            await channel.QueueDeclareAsync(queue: queueName, durable: true, exclusive: false, autoDelete: false,
                    arguments: null, passive: false, noWait: false, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.ReceivedAsync += async (_, b) =>
            {
                var msgBody = b.Body.ToArray();
                var received = Encoding.UTF8.GetString(msgBody);
                Console.WriteLine("Received message: {0}", received);
                await Task.CompletedTask;
            };

            await channel.BasicConsumeAsync(queue: queueName, autoAck: true, consumer: consumer, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            _consumerChannels[queueName] = channel;

            Console.WriteLine("Consuming messages...");
        }
        catch
        {
            try { channel.Dispose(); } catch { /* ignore */ }
            throw;
        }
    }
}
