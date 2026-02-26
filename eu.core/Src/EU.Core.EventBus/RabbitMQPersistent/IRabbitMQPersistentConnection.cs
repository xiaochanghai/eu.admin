using RabbitMQ.Client;

namespace EU.Core.EventBus;

/// <summary>
/// RabbitMQ持久连接
/// 接口
/// </summary>
public interface IRabbitMQPersistentConnection : IDisposable
{
    /// <summary>
    /// 是否已经连接
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// 尝试重连
    /// </summary>
    Task<bool> TryConnectAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 创建 Channel（RabbitMQ.Client v7 由 IModel 升级为 IChannel）
    /// </summary>
    Task<IChannel> CreateChannelAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 发布消息
    /// </summary>
    Task PublishMessageAsync(string message, string exchangeName, string routingKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// 订阅消息
    /// </summary>
    Task StartConsumingAsync(string queueName, CancellationToken cancellationToken = default);
}
