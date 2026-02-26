using Autofac;
using EU.Core.Common;
using EU.Core.Common.Extensions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Polly;
using Polly.Retry;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using System.Net.Sockets;
using System.Text;

namespace EU.Core.EventBus;

/// <summary>
/// 基于RabbitMQ的事件总线
/// </summary>
public class EventBusRabbitMQ : IEventBus, IDisposable
{
    private const string BROKER_NAME = "Tioboncore_event_bus";

    private static readonly CreateChannelOptions DefaultChannelOptions = new(
        publisherConfirmationsEnabled: false,
        publisherConfirmationTrackingEnabled: false,
        outstandingPublisherConfirmationsRateLimiter: null,
        consumerDispatchConcurrency: 1);

    private readonly IRabbitMQPersistentConnection _persistentConnection;
    private readonly ILogger<EventBusRabbitMQ> _logger;
    private readonly IEventBusSubscriptionsManager _subsManager;
    private readonly ILifetimeScope _autofac;
    private readonly string AUTOFAC_SCOPE_NAME = "Tioboncore_event_bus";
    private readonly int _retryCount;

    private IChannel? _consumerChannel;
    private string _queueName;

    private readonly SemaphoreSlim _consumerChannelLock = new(1, 1);
    private bool _consuming;
    private bool _disposed;

    // consumer channel 重建的退避/降噪参数
    private int _recreateAttempts;
    private DateTimeOffset _nextRecreateAttemptAt = DateTimeOffset.MinValue;
    private DateTimeOffset _lastRecreateLogAt = DateTimeOffset.MinValue;

    public EventBusRabbitMQ(
        IRabbitMQPersistentConnection persistentConnection,
        ILogger<EventBusRabbitMQ> logger,
        ILifetimeScope autofac,
        IEventBusSubscriptionsManager subsManager,
        string? queueName = null,
        int retryCount = 5)
    {
        _persistentConnection = persistentConnection ?? throw new ArgumentNullException(nameof(persistentConnection));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _subsManager = subsManager ?? new InMemoryEventBusSubscriptionsManager();
        _queueName = queueName ?? string.Empty;
        _autofac = autofac;
        _retryCount = retryCount;

        // ctor 里不能 await，启动消费通道改为 lazy
        _subsManager.OnEventRemoved += SubsManager_OnEventRemoved;
    }

    private void SubsManager_OnEventRemoved(object? sender, string eventName)
    {
        _ = SubsManager_OnEventRemovedAsync(eventName);
    }

    private async Task SubsManager_OnEventRemovedAsync(string eventName)
    {
        try
        {
            if (!_persistentConnection.IsConnected)
            {
                await _persistentConnection.TryConnectAsync(CancellationToken.None).ConfigureAwait(false);
            }

            await using var channel = await _persistentConnection.CreateChannelAsync(CancellationToken.None).ConfigureAwait(false);
            await channel.QueueUnbindAsync(queue: _queueName,
                    exchange: BROKER_NAME,
                    routingKey: eventName,
                    arguments: null,
                    cancellationToken: CancellationToken.None)
                .ConfigureAwait(false);

            if (_subsManager.IsEmpty)
            {
                _queueName = string.Empty;
                // 0 不是一个“正常关闭”的 replyCode；这里用 200 表示正常关闭
                if (_consumerChannel != null)
                {
                    try { await _consumerChannel.CloseAsync(200, "OK", false, CancellationToken.None).ConfigureAwait(false); } catch { }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SubsManager_OnEventRemovedAsync failed");
        }
    }

    /// <summary>
    /// 发布（同步包装，兼容 IEventBus）
    /// </summary>
    public void Publish(IntegrationEvent @event)
        => PublishAsync(@event).GetAwaiter().GetResult();

    public async Task PublishAsync(IntegrationEvent @event, CancellationToken cancellationToken = default)
    {
        if (!_persistentConnection.IsConnected)
        {
            await _persistentConnection.TryConnectAsync(cancellationToken).ConfigureAwait(false);
        }

        AsyncRetryPolicy policy = Policy
            .Handle<BrokerUnreachableException>()
            .Or<SocketException>()
            .Or<AlreadyClosedException>()
            .Or<OperationInterruptedException>()
            .WaitAndRetryAsync(
                _retryCount,
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                async (ex, time) =>
                {
                    _logger.LogWarning(ex, "Could not publish event: {EventId} after {Timeout}s ({ExceptionMessage})", @event.Id, $"{time.TotalSeconds:n1}", ex.Message);
                    // 让下一次重试尽量在“已连接”状态下发生
                    if (!_persistentConnection.IsConnected)
                    {
                        try { await _persistentConnection.TryConnectAsync(CancellationToken.None).ConfigureAwait(false); } catch { /* ignore */ }
                    }
                });

        var eventName = @event.GetType().Name;

        _logger.LogTrace("Creating RabbitMQ channel to publish event: {EventId} ({EventName})", @event.Id, eventName);

        await using var channel = await _persistentConnection.CreateChannelAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogTrace("Declaring RabbitMQ exchange to publish event: {EventId}", @event.Id);

        await channel.ExchangeDeclareAsync(exchange: BROKER_NAME, type: "direct", durable: true, autoDelete: false,
                arguments: null, passive: false, noWait: false, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        var message = JsonConvert.SerializeObject(@event);
        var body = Encoding.UTF8.GetBytes(message);

        await policy.ExecuteAsync(async ct =>
        {
            var properties = new BasicProperties { DeliveryMode = DeliveryModes.Persistent };

            _logger.LogTrace("Publishing event to RabbitMQ: {EventId}", @event.Id);

            await channel.BasicPublishAsync(exchange: BROKER_NAME,
                    routingKey: eventName,
                    mandatory: true,
                    basicProperties: properties,
                    body: body,
                    cancellationToken: ct)
                .ConfigureAwait(false);
        }, cancellationToken).ConfigureAwait(false);
    }

    public void SubscribeDynamic<TH>(string eventName) where TH : IDynamicIntegrationEventHandler
        => SubscribeDynamicAsync<TH>(eventName).GetAwaiter().GetResult();

    public async Task SubscribeDynamicAsync<TH>(string eventName, CancellationToken cancellationToken = default) where TH : IDynamicIntegrationEventHandler
    {
        _logger.LogInformation("Subscribing to dynamic event {EventName} with {EventHandler}", eventName, typeof(TH).GetGenericTypeName());

        await DoInternalSubscriptionAsync(eventName, cancellationToken).ConfigureAwait(false);
        _subsManager.AddDynamicSubscription<TH>(eventName);
        await StartBasicConsumeAsync(cancellationToken).ConfigureAwait(false);
    }

    public void Subscribe<T, TH>()
        where T : IntegrationEvent
        where TH : IIntegrationEventHandler<T>
        => SubscribeAsync<T, TH>().GetAwaiter().GetResult();

    public async Task SubscribeAsync<T, TH>(CancellationToken cancellationToken = default)
        where T : IntegrationEvent
        where TH : IIntegrationEventHandler<T>
    {
        var eventName = _subsManager.GetEventKey<T>();
        await DoInternalSubscriptionAsync(eventName, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Subscribing to event {EventName} with {EventHandler}", eventName, typeof(TH).GetGenericTypeName());
        ConsoleHelper.WriteSuccessLine($"Subscribing to event {eventName} with {typeof(TH).GetGenericTypeName()}");

        _subsManager.AddSubscription<T, TH>();
        await StartBasicConsumeAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task DoInternalSubscriptionAsync(string eventName, CancellationToken cancellationToken)
    {
        var containsKey = _subsManager.HasSubscriptionsForEvent(eventName);
        if (containsKey) return;

        if (!_persistentConnection.IsConnected)
        {
            await _persistentConnection.TryConnectAsync(cancellationToken).ConfigureAwait(false);
        }

        await using var channel = await _persistentConnection.CreateChannelAsync(cancellationToken).ConfigureAwait(false);
        await channel.QueueBindAsync(queue: _queueName,
                exchange: BROKER_NAME,
                routingKey: eventName,
                arguments: null,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    public void Unsubscribe<T, TH>()
        where T : IntegrationEvent
        where TH : IIntegrationEventHandler<T>
    {
        var eventName = _subsManager.GetEventKey<T>();

        _logger.LogInformation("Unsubscribing from event {EventName}", eventName);
        _subsManager.RemoveSubscription<T, TH>();
    }

    public void UnsubscribeDynamic<TH>(string eventName) where TH : IDynamicIntegrationEventHandler
    {
        _subsManager.RemoveDynamicSubscription<TH>(eventName);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _consumerChannel?.Dispose();
        _subsManager.Clear();

        _consumerChannelLock.Dispose();
    }

    private async Task StartBasicConsumeAsync(CancellationToken cancellationToken)
    {
        _logger.LogTrace("Starting RabbitMQ basic consume");

        if (_disposed) return;

        if (string.IsNullOrEmpty(_queueName))
        {
            _logger.LogWarning("StartBasicConsume skipped because queue name is empty");
            return;
        }

        // 防止重复启动消费（重连/重复订阅场景）
        if (_consuming) return;

        // consumer channel lazy init
        if (_consumerChannel == null)
        {
            _consumerChannel = await CreateConsumerChannelAsync(cancellationToken).ConfigureAwait(false);
        }

        var consumer = new AsyncEventingBasicConsumer(_consumerChannel);
        consumer.ReceivedAsync += Consumer_Received;

        try
        {
            await _consumerChannel.BasicConsumeAsync(queue: _queueName, autoAck: false, consumer: consumer, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            _consuming = true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "StartBasicConsume failed. Will try to recreate consumer channel.");
            _ = TryRecreateConsumerChannelAsync();
        }
    }

    private async Task Consumer_Received(object sender, BasicDeliverEventArgs eventArgs)
    {
        var eventName = eventArgs.RoutingKey;
        var message = Encoding.UTF8.GetString(eventArgs.Body.Span);

        try
        {
            if (message.ToLowerInvariant().Contains("throw-fake-exception"))
            {
                throw new InvalidOperationException($"Fake exception requested: \"{message}\"");
            }

            await ProcessEvent(eventName, message).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "----- ERROR Processing message \"{Message}\"", message);
        }

        // Even on exception we take the message off the queue.
        try
        {
            if (_consumerChannel != null)
            {
                await _consumerChannel.BasicAckAsync(eventArgs.DeliveryTag, multiple: false, cancellationToken: CancellationToken.None)
                    .ConfigureAwait(false);
            }
        }
        catch (AlreadyClosedException ex)
        {
            _logger.LogWarning(ex, "Ack failed because consumer channel is closed. Will try to recreate channel.");
            _ = TryRecreateConsumerChannelAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Ack failed. Will try to recreate channel.");
            _ = TryRecreateConsumerChannelAsync();
        }
    }

    private async Task<IChannel> CreateConsumerChannelAsync(CancellationToken cancellationToken)
    {
        if (!_persistentConnection.IsConnected)
        {
            await _persistentConnection.TryConnectAsync(cancellationToken).ConfigureAwait(false);
        }

        _logger.LogTrace("Creating RabbitMQ consumer channel");

        var channel = await _persistentConnection.CreateChannelAsync(cancellationToken).ConfigureAwait(false);

        await channel.ExchangeDeclareAsync(exchange: BROKER_NAME, type: "direct", durable: true, autoDelete: false,
                arguments: null, passive: false, noWait: false, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        await channel.QueueDeclareAsync(queue: _queueName, durable: true, exclusive: false, autoDelete: false,
                arguments: null, passive: false, noWait: false, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        // RabbitMQ.Client v7: Channel 事件改为 Async 版本；用于触发“重建 consumer channel”。
        try
        {
            channel.ChannelShutdownAsync += OnConsumerChannelShutdownAsync;
            channel.CallbackExceptionAsync += OnConsumerChannelCallbackExceptionAsync;
        }
        catch
        {
            // 事件不可用时不阻塞创建；用 try/catch + 业务调用路径兜底。
        }

        return channel;
    }

    private Task OnConsumerChannelShutdownAsync(object? sender, ShutdownEventArgs e)
    {
        _logger.LogWarning("RabbitMQ consumer channel shutdown. ReplyText: {ReplyText}", e.ReplyText);
        _ = TryRecreateConsumerChannelAsync();
        return Task.CompletedTask;
    }

    private Task OnConsumerChannelCallbackExceptionAsync(object? sender, CallbackExceptionEventArgs e)
    {
        _logger.LogWarning(e.Exception, "RabbitMQ consumer channel threw an exception (callback). Recreating channel...");
        _ = TryRecreateConsumerChannelAsync();
        return Task.CompletedTask;
    }

    private async Task TryRecreateConsumerChannelAsync()
    {
        if (_disposed) return;

        // 退避：避免 RabbitMQ 不可用时疯狂重建 + 刷屏
        var now = DateTimeOffset.UtcNow;
        if (now < _nextRecreateAttemptAt)
        {
            // 降噪：最多每 30s 打一次“跳过”日志
            if (now - _lastRecreateLogAt > TimeSpan.FromSeconds(30))
            {
                _lastRecreateLogAt = now;
                _logger.LogWarning("Skip recreating consumer channel due to backoff. Next attempt at {NextAttemptAt:O}", _nextRecreateAttemptAt);
            }
            return;
        }

        await _consumerChannelLock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_disposed) return;

            // double-check backoff inside lock
            now = DateTimeOffset.UtcNow;
            if (now < _nextRecreateAttemptAt) return;

            try
            {
                _consuming = false;

                try { _consumerChannel?.Dispose(); } catch { }
                _consumerChannel = null;

                // 确保连接在线（如果断线，先重连）
                if (!_persistentConnection.IsConnected)
                {
                    await _persistentConnection.TryConnectAsync(CancellationToken.None).ConfigureAwait(false);
                }

                _consumerChannel = await CreateConsumerChannelAsync(CancellationToken.None).ConfigureAwait(false);

                // 成功则重置退避计数
                _recreateAttempts = 0;
                _nextRecreateAttemptAt = DateTimeOffset.MinValue;

                // 只有在确实有订阅时才重启消费，避免空订阅时的无意义重建。
                if (!_subsManager.IsEmpty)
                {
                    await StartBasicConsumeAsync(CancellationToken.None).ConfigureAwait(false);
                }

                _logger.LogInformation("RabbitMQ consumer channel recreated successfully");
            }
            catch (Exception ex)
            {
                // 失败：指数退避（封顶 30s）
                _recreateAttempts = Math.Min(_recreateAttempts + 1, 10);
                var delaySeconds = Math.Min(Math.Pow(2, _recreateAttempts), 30);
                _nextRecreateAttemptAt = DateTimeOffset.UtcNow.AddSeconds(delaySeconds);

                // 降噪：失败也别刷屏（同样 30s 一次）
                now = DateTimeOffset.UtcNow;
                if (now - _lastRecreateLogAt > TimeSpan.FromSeconds(30))
                {
                    _lastRecreateLogAt = now;
                    _logger.LogWarning(ex, "Failed to recreate RabbitMQ consumer channel. Backing off for {DelaySeconds}s", delaySeconds);
                }
            }
        }
        finally
        {
            _consumerChannelLock.Release();
        }
    }

    private async Task ProcessEvent(string eventName, string message)
    {
        _logger.LogTrace("Processing RabbitMQ event: {EventName}", eventName);

        if (!_subsManager.HasSubscriptionsForEvent(eventName))
        {
            _logger.LogWarning("No subscription for RabbitMQ event: {EventName}", eventName);
            return;
        }

        using var scope = _autofac.BeginLifetimeScope(AUTOFAC_SCOPE_NAME);
        var subscriptions = _subsManager.GetHandlersForEvent(eventName);

        foreach (var subscription in subscriptions)
        {
            if (subscription.IsDynamic)
            {
                var handler = scope.ResolveOptional(subscription.HandlerType) as IDynamicIntegrationEventHandler;
                if (handler == null) continue;

                dynamic eventData = JObject.Parse(message);
                await Task.Yield();
                await handler.Handle(eventData);
            }
            else
            {
                var handler = scope.ResolveOptional(subscription.HandlerType);
                if (handler == null) continue;

                var eventType = _subsManager.GetEventTypeByName(eventName);
                var integrationEvent = JsonConvert.DeserializeObject(message, eventType);
                var concreteType = typeof(IIntegrationEventHandler<>).MakeGenericType(eventType);

                await Task.Yield();
                await (Task)concreteType.GetMethod("Handle")!.Invoke(handler, new object[] { integrationEvent! })!;
            }
        }
    }
}
