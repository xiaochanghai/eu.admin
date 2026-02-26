using System.Collections.Concurrent;
using System.Text;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using EU.Core.Common.Const;
using EU.Core.Common.LogHelper;

namespace EU.Core.Common.Helper;

/// <summary>
/// RabbitMQ连接池
/// </summary>
public class RabbitMQHelper
{
    #region 初始化参数

    private static readonly string m_HostName = AppSettings.app(["RabbitMQ", "Connection"]);
    private static readonly int m_Port = AppSettings.app(["RabbitMQ", "Port"]).ObjToInt();
    private static readonly string m_UserName = AppSettings.app(["RabbitMQ", "UserName"]);
    private static readonly string m_Password = AppSettings.app(["RabbitMQ", "Password"]);

    // 单连接：连接是重资源；Channel 轻量，按需创建即可。
    private static IConnection? s_connection;
    private static readonly SemaphoreSlim s_connectionLock = new(1, 1);

    internal static readonly CreateChannelOptions DefaultChannelOptions = new(
        publisherConfirmationsEnabled: false,
        publisherConfirmationTrackingEnabled: false,
        outstandingPublisherConfirmationsRateLimiter: null,
        consumerDispatchConcurrency: 1);

    #endregion

    // ---- 消费线程防重复启动标记（同一队列/同一泛型消费者仅允许启动一次） ----
    internal static readonly ConcurrentDictionary<string, byte> ConsumeStarted = new();

    // ---- 消费线程可停机：每个消费者一个 CTS（StopConsume 时 Cancel） ----
    internal static readonly ConcurrentDictionary<string, CancellationTokenSource> ConsumeCts = new();

    internal static bool TryRegisterConsumer(string key)
    {
        if (!ConsumeStarted.TryAdd(key, 1)) return false;

        // 仅在成功“抢到启动权”时创建 CTS
        var cts = new CancellationTokenSource();
        ConsumeCts[key] = cts;
        return true;
    }

    internal static CancellationToken GetConsumerToken(string key)
    {
        return ConsumeCts.TryGetValue(key, out var cts) ? cts.Token : CancellationToken.None;
    }

    internal static void UnregisterConsumer(string key)
    {
        ConsumeStarted.TryRemove(key, out _);
        if (ConsumeCts.TryRemove(key, out var cts))
        {
            try { cts.Dispose(); } catch { /* ignore */ }
        }
    }

    private static ConnectionFactory CreateFactory()
    {
        return new ConnectionFactory
        {
            HostName = m_HostName,
            Port = m_Port,
            UserName = m_UserName,
            Password = m_Password,
            AutomaticRecoveryEnabled = true // 自动重连
        };
    }

    internal static Task<IConnection> GetConnectionAsync(CancellationToken cancellationToken)
        => GetOrCreateConnectionAsync(cancellationToken);

    private static async Task<IConnection> GetOrCreateConnectionAsync(CancellationToken cancellationToken)
    {
        if (s_connection is { IsOpen: true })
            return s_connection;

        await s_connectionLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (s_connection is { IsOpen: true })
                return s_connection;

            // 如果旧连接已关闭，尽量释放
            if (s_connection != null)
            {
                try { s_connection.Dispose(); } catch { /* ignore */ }
                s_connection = null;
            }

            var factory = CreateFactory();
            s_connection = await factory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
            return s_connection;
        }
        finally
        {
            s_connectionLock.Release();
        }
    }

    /// <summary>
    /// 停止 RabbitMQ 消费线程（string 消费者）
    /// </summary>
    public static void StopConsume(string queueName)
    {
        StopConsume(queueName, "string");
    }

    /// <summary>
    /// 停止 RabbitMQ 消费线程（泛型消费者）
    /// </summary>
    public static void StopConsume<T>(string queueName) where T : class
    {
        StopConsume(queueName, typeof(T).FullName ?? typeof(T).Name);
    }

    private static void StopConsume(string queueName, string typeKey)
    {
        var key = $"{queueName}|{typeKey}";
        if (ConsumeCts.TryGetValue(key, out var cts))
        {
            try { cts.Cancel(); } catch { /* ignore */ }
        }
        else
        {
            // 没启动过就别吵
        }
    }

    #region 发送消息

    /// <summary>
    /// 发送消息（同步包装，兼容旧调用）
    /// </summary>
    /// <param name="queueName">队列名称</param>
    /// <param name="msg">消息</param>
    /// <returns></returns>
    public static bool SendMsg(string queueName, string msg)
        => SendMsgAsync(queueName, msg).GetAwaiter().GetResult();

    /// <summary>
    /// 发送消息
    /// </summary>
    /// <param name="queueName">队列名称</param>
    /// <param name="msg">消息</param>
    /// <returns></returns>
    public static async Task<bool> SendMsgAsync(string queueName, string msg, CancellationToken cancellationToken = default)
    {
        var durable = true;
        try
        {
            var connection = await GetConnectionAsync(cancellationToken).ConfigureAwait(false);

            await using var channel = await connection.CreateChannelAsync(DefaultChannelOptions, cancellationToken).ConfigureAwait(false);

            await channel.QueueDeclareAsync(queueName, durable, exclusive: false, autoDelete: false,
                arguments: null, passive: false, noWait: false, cancellationToken: cancellationToken).ConfigureAwait(false);

            BasicProperties? properties = new() { DeliveryMode = DeliveryModes.Persistent };
            if (!durable)
                properties = null;

            var body = Encoding.UTF8.GetBytes(msg);
            await channel.BasicPublishAsync(string.Empty, queueName, mandatory: false, basicProperties: properties, body: body, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 发送消息（同步包装，兼容旧调用）
    /// </summary>
    /// <typeparam name="T">消息类型</typeparam>
    /// <param name="queueName">队列名称</param>
    /// <param name="msg">消息</param>
    /// <returns></returns>
    public static bool SendMsg<T>(string queueName, T msg) where T : class
        => SendMsgAsync(queueName, msg).GetAwaiter().GetResult();

    /// <summary>
    /// 发送消息
    /// </summary>
    /// <typeparam name="T">消息类型</typeparam>
    /// <param name="queueName">队列名称</param>
    /// <param name="msg">消息</param>
    /// <returns></returns>
    public static async Task<bool> SendMsgAsync<T>(string queueName, T msg, CancellationToken cancellationToken = default) where T : class
    {
        var durable = true;
        var connection = await GetConnectionAsync(cancellationToken).ConfigureAwait(false);

        await using var channel = await connection.CreateChannelAsync(DefaultChannelOptions, cancellationToken).ConfigureAwait(false);

        await channel.QueueDeclareAsync(queueName, durable, exclusive: false, autoDelete: false,
            arguments: null, passive: false, noWait: false, cancellationToken: cancellationToken).ConfigureAwait(false);

        BasicProperties? properties = new() { DeliveryMode = DeliveryModes.Persistent };
        if (!durable)
            properties = null;

        var body = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(msg ?? default));
        await channel.BasicPublishAsync(string.Empty, queueName, mandatory: false, basicProperties: properties, body: body, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        return true;
    }

    #endregion

    #region 消费消息

    /// <summary>
    /// 消费消息（同步包装，兼容旧调用）
    /// </summary>
    /// <param name="queueName">队列名称</param>
    public static void ConsumeMsg(string queueName, Func<string, ConsumeAction> func)
        => ConsumeMsgAsync(queueName, func).GetAwaiter().GetResult();

    /// <summary>
    /// 消费消息
    /// </summary>
    /// <param name="queueName">队列名称</param>
    public static Task ConsumeMsgAsync(string queueName, Func<string, ConsumeAction> func, CancellationToken cancellationToken = default)
    {
        var consumer = new RabbitMQConsume();
        consumer.ReceiveMessageCallback += func;
        return consumer.ConsumeMsgAsync(queueName, cancellationToken);
    }

    /// <summary>
    /// 消费消息（同步包装，兼容旧调用）
    /// </summary>
    /// <param name="queueName">队列名称</param>
    public static void ConsumeMsg<T>(string queueName, Func<T, ConsumeAction> func) where T : class
        => ConsumeMsgAsync(queueName, func).GetAwaiter().GetResult();

    /// <summary>
    /// 消费消息
    /// </summary>
    /// <param name="queueName">队列名称</param>
    public static Task ConsumeMsgAsync<T>(string queueName, Func<T, ConsumeAction> func, CancellationToken cancellationToken = default) where T : class
    {
        var consumer = new RabbitMQConsume<T>();
        consumer.ReceiveMessageCallback += func;
        return consumer.ConsumeMsgAsync(queueName, cancellationToken);
    }

    #endregion
}

/// <summary>
/// 消费消息
/// </summary>
internal class RabbitMQConsume
{
    internal Func<string, ConsumeAction> ReceiveMessageCallback { get; set; } = _ => ConsumeAction.Retry;

    /// <summary>
    /// 消费消息
    /// </summary>
    /// <param name="queueName">队列名称</param>
    internal Task ConsumeMsgAsync(string queueName, CancellationToken cancellationToken = default)
    {
        var key = $"{queueName}|string";
        if (!RabbitMQHelper.TryRegisterConsumer(key))
        {
            Logger.WriteLog("RabbitMQ", $"队列{queueName}消费者已启动，跳过重复启动");
            return Task.CompletedTask;
        }

        var token = RabbitMQHelper.GetConsumerToken(key);

        // 用 Task.Run 替代 Thread，确保不会阻塞调用线程
        _ = Task.Run(async () =>
        {
            try
            {
                while (!token.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
                {
                    IChannel? channel = null;
                    try
                    {
                        var durable = true;
                        var connection = await RabbitMQHelper.GetConnectionAsync(token).ConfigureAwait(false);
                        channel = await connection.CreateChannelAsync(RabbitMQHelper.DefaultChannelOptions, token).ConfigureAwait(false);

                        await channel.QueueDeclareAsync(queueName, durable, exclusive: false, autoDelete: false,
                            arguments: null, passive: false, noWait: false, cancellationToken: token).ConfigureAwait(false);

                        await channel.BasicQosAsync(0, 1, false, token).ConfigureAwait(false);

                        var consumer = new AsyncEventingBasicConsumer(channel);
                        consumer.ReceivedAsync += async (_, e) =>
                        {
                            var consumeResult = ConsumeAction.Retry;
                            var message = Encoding.UTF8.GetString(e.Body.ToArray());
                            Logger.WriteLog("RabbitMQ", $"队列{queueName}消费消息:{message},不做ack确认");

                            try
                            {
                                consumeResult = ReceiveMessageCallback(message);
                            }
                            catch
                            {
                                consumeResult = ConsumeAction.Retry;
                            }

                            if (consumeResult == ConsumeAction.Accept)
                            {
                                await channel.BasicAckAsync(e.DeliveryTag, false, token).ConfigureAwait(false);
                            }
                            else if (consumeResult == ConsumeAction.Retry)
                            {
                                await channel.BasicNackAsync(e.DeliveryTag, false, true, token).ConfigureAwait(false);
                            }
                            else
                            {
                                await channel.BasicNackAsync(e.DeliveryTag, false, false, token).ConfigureAwait(false);
                            }
                        };

                        await channel.BasicConsumeAsync(queueName, autoAck: false, consumer: consumer, cancellationToken: token).ConfigureAwait(false);

                        // StopConsume 时能立刻退出
                        while (channel.IsOpen && !token.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
                        {
                            await Task.Delay(2000, token).ConfigureAwait(false);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        // 正常取消
                    }
                    catch (Exception ex)
                    {
                        Logger.WriteLog("RabbitMQ", $" Error:{ex}");
                    }
                    finally
                    {
                        if (channel != null)
                        {
                            try { await channel.CloseAsync(200, "OK", false, CancellationToken.None).ConfigureAwait(false); } catch { }
                            try { await channel.DisposeAsync().ConfigureAwait(false); } catch { }
                        }

                        // 与MQ连接断开或者报错的情况下重连（可被 StopConsume 立即打断）
                        try
                        {
                            await Task.Delay(5000, token).ConfigureAwait(false);
                        }
                        catch { /* ignore */ }
                    }
                }
            }
            finally
            {
                RabbitMQHelper.UnregisterConsumer(key);
            }
        }, CancellationToken.None);

        return Task.CompletedTask;
    }
}

/// <summary>
/// 消费消息
/// </summary>
internal class RabbitMQConsume<T> where T : class
{
    internal Func<T, ConsumeAction> ReceiveMessageCallback { get; set; } = _ => ConsumeAction.Retry;

    /// <summary>
    /// 消费消息
    /// </summary>
    /// <param name="queueName">队列名称</param>
    internal Task ConsumeMsgAsync(string queueName, CancellationToken cancellationToken = default)
    {
        var key = $"{queueName}|{typeof(T).FullName}";
        if (!RabbitMQHelper.TryRegisterConsumer(key))
        {
            Logger.WriteLog("RabbitMQ", $"队列{queueName}消费者<{typeof(T).Name}>已启动，跳过重复启动");
            return Task.CompletedTask;
        }

        var token = RabbitMQHelper.GetConsumerToken(key);

        _ = Task.Run(async () =>
        {
            try
            {
                while (!token.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
                {
                    IChannel? channel = null;
                    try
                    {
                        var durable = true;
                        var connection = await RabbitMQHelper.GetConnectionAsync(token).ConfigureAwait(false);
                        channel = await connection.CreateChannelAsync(RabbitMQHelper.DefaultChannelOptions, token).ConfigureAwait(false);

                        await channel.QueueDeclareAsync(queueName, durable, exclusive: false, autoDelete: false,
                            arguments: null, passive: false, noWait: false, cancellationToken: token).ConfigureAwait(false);

                        await channel.BasicQosAsync(0, 1, false, token).ConfigureAwait(false);

                        var consumer = new AsyncEventingBasicConsumer(channel);
                        consumer.ReceivedAsync += async (_, e) =>
                        {
                            var consumeResult = ConsumeAction.Retry;
                            var inputString = Encoding.UTF8.GetString(e.Body.ToArray());
                            Logger.WriteLog("RabbitMQ", $"队列{queueName}消费消息:{inputString},不做ack确认");

                            try
                            {
                                var input = JsonConvert.DeserializeObject<T>(inputString);
                                if (input != null)
                                {
                                    consumeResult = ReceiveMessageCallback(input);
                                }
                            }
                            catch
                            {
                                consumeResult = ConsumeAction.Retry;
                            }

                            if (consumeResult == ConsumeAction.Accept)
                            {
                                await channel.BasicAckAsync(e.DeliveryTag, false, token).ConfigureAwait(false);
                            }
                            else if (consumeResult == ConsumeAction.Retry)
                            {
                                await channel.BasicNackAsync(e.DeliveryTag, false, true, token).ConfigureAwait(false);
                            }
                            else
                            {
                                await channel.BasicNackAsync(e.DeliveryTag, false, false, token).ConfigureAwait(false);
                            }
                        };

                        await channel.BasicConsumeAsync(queueName, autoAck: false, consumer: consumer, cancellationToken: token).ConfigureAwait(false);

                        while (channel.IsOpen && !token.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
                        {
                            await Task.Delay(2000, token).ConfigureAwait(false);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        // 正常取消
                    }
                    catch (Exception ex)
                    {
                        Logger.WriteLog("RabbitMQ", $"RabbitMQ Error:{ex}");
                    }
                    finally
                    {
                        if (channel != null)
                        {
                            try { await channel.CloseAsync(200, "OK", false, CancellationToken.None).ConfigureAwait(false); } catch { }
                            try { await channel.DisposeAsync().ConfigureAwait(false); } catch { }
                        }

                        try
                        {
                            await Task.Delay(5000, token).ConfigureAwait(false);
                        }
                        catch { /* ignore */ }
                    }
                }
            }
            finally
            {
                RabbitMQHelper.UnregisterConsumer(key);
            }
        }, CancellationToken.None);

        return Task.CompletedTask;
    }
}
