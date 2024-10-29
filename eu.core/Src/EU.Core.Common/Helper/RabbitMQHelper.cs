using System.Collections.Concurrent;
using System.Text;
using Newtonsoft.Json;
using RabbitMQ.Client.Events;
using RabbitMQ.Client;
using EU.Core.Common.Const;
using EU.Core.Common.LogHelper;

namespace EU.Core.Common.Helper;

/// <summary>
/// RabbitMQ连接池
/// </summary>
public class RabbitMQHelper
{
    #region 初始化参数
    //public static Func<string, ConsumeAction> ReceiveMessageCallback { get; set; }
    private readonly static ConcurrentQueue<IConnection> m_FreeConnectionQueue;//空闲连接对象队列
    private readonly static ConcurrentDictionary<IConnection, bool> m_BusyConnectionDic;//使用中（忙）连接对象集合
    private readonly static ConcurrentDictionary<IConnection, int> m_MQConnectionPoolUsingDicNew;//连接池使用率
    private readonly static object m_FreeConnLock = new object();
    private readonly static object m_AddConnLock = new object();
    private readonly static string m_HostName = AppSettings.app(["RabbitMQ", "Connection"]);
     private readonly static int m_Port = AppSettings.app(["RabbitMQ", "Port"]).ObjToInt();
    private readonly static string m_UserName = AppSettings.app(["RabbitMQ", "UserName"]);
    private readonly static string m_Password = AppSettings.app(["RabbitMQ", "Password"]);
    //private readonly static int m_MaxConnectionCount = AppSetting.RabbitMQConfiguration.MaxConnectionCount;
    //private readonly static int m_MaxConnectionUsingCount = AppSetting.RabbitMQConfiguration.MaxConnectionUsingCount;

    private readonly static int m_MaxConnectionCount = 20;//默认最大保持可用连接数
    private readonly static int m_MaxConnectionUsingCount = 1000;//默认最大连接可访问次数
    static RabbitMQHelper()
    {
        m_FreeConnectionQueue = new ConcurrentQueue<IConnection>();
        m_BusyConnectionDic = new ConcurrentDictionary<IConnection, bool>();
        m_MQConnectionPoolUsingDicNew = new ConcurrentDictionary<IConnection, int>();
    }
    #endregion

    #region MQ连接池管理
    internal static IConnection CreateMQConnectionInPoolNew()
    {
        IConnection mqConnection;
        if (m_FreeConnectionQueue.Count + m_BusyConnectionDic.Count < m_MaxConnectionCount)//如果已有连接数小于最大可用连接数，则直接创建新连接
        {
            lock (m_AddConnLock)
            {
                if (m_FreeConnectionQueue.Count + m_BusyConnectionDic.Count < m_MaxConnectionCount)
                {
                    mqConnection = CreateMQConnection();
                    m_BusyConnectionDic[mqConnection] = true;//加入到忙连接集合中
                    m_MQConnectionPoolUsingDicNew[mqConnection] = 1;
                    return mqConnection;
                }
            }
        }
        //如果没有可用空闲连接，则重新进入等待排队
        while (!m_FreeConnectionQueue.TryDequeue(out mqConnection))
        {
            System.Threading.Thread.Sleep(10);
            continue;
        }
        //如果取到空闲连接，判断是否使用次数是否超过最大限制,超过则释放连接并重新创建
        if (m_MQConnectionPoolUsingDicNew[mqConnection] + 1 > m_MaxConnectionUsingCount || !mqConnection.IsOpen)
        {
            mqConnection.Close();
            mqConnection.Dispose();
            mqConnection = CreateMQConnection();
            m_MQConnectionPoolUsingDicNew[mqConnection] = 0;
        }
        m_BusyConnectionDic[mqConnection] = true;//加入到忙连接集合中
        m_MQConnectionPoolUsingDicNew[mqConnection] = m_MQConnectionPoolUsingDicNew[mqConnection] + 1;//使用次数加1
        return mqConnection;
    }
    internal static void ResetMQConnectionToFree(IConnection connection)
    {
        lock (m_FreeConnLock)
        {
            m_BusyConnectionDic.TryRemove(connection, out bool result); //从忙队列中取出
            if (m_FreeConnectionQueue.Count + m_BusyConnectionDic.Count > m_MaxConnectionCount)//如果因为高并发出现极少概率的>MaxConnectionCount，则直接释放该连接
            {
                connection.Close();
                connection.Dispose();
            }
            else
            {
                m_FreeConnectionQueue.Enqueue(connection);//加入到空闲队列，以便持续提供连接服务
            }
        }
    }
    internal static IConnection CreateMQConnection()
    {
        var factory = new ConnectionFactory
        {
            HostName = m_HostName,
            Port = m_Port,
            UserName = m_UserName,
            Password = m_Password,
            AutomaticRecoveryEnabled = true//自动重连
        };
        return factory.CreateConnection();
    }

    #endregion

    #region 发送消息
    /// <summary>
    /// 发送消息
    /// </summary>
    /// <param name="queueName">队列名称</param>
    /// <param name="msg">消息</param>
    /// <returns></returns>
    public static bool SendMsg(string queueName, string msg)
    {
        bool durable = true;
        var connection = CreateMQConnectionInPoolNew();
        try
        {
            using (var channel = connection.CreateModel())//建立通讯信道
            {
                // 参数从前面开始分别意思为：队列名称，是否持久化，独占的队列，不使用时是否自动删除，其他参数
                channel.QueueDeclare(queueName, durable, false, false, null);
                var properties = channel.CreateBasicProperties();
                properties.DeliveryMode = 2;//1表示不持久,2.表示持久化
                if (!durable)
                    properties = null;
                var body = Encoding.UTF8.GetBytes(msg);
                channel.BasicPublish(string.Empty, queueName, properties, body);
            }
            return true;
        }
        catch (Exception)
        {
            //LoggerHelper.SendLog(ex.ToString());
            return false;
        }
        finally
        {
            ResetMQConnectionToFree(connection);
        }
    }
    /// <summary>
    /// 发送消息
    /// </summary>
    /// <typeparam name="T">消息类型</typeparam>
    /// <param name="queueName">队列名称</param>
    /// <param name="msg">消息</param>
    /// <returns></returns>
    public static bool SendMsg<T>(string queueName, T msg) where T : class
    {
        bool durable = true;
        var connection = CreateMQConnectionInPoolNew();
        try
        {
            using (var channel = connection.CreateModel())//建立通讯信道
            {
                // 参数从前面开始分别意思为：队列名称，是否持久化，独占的队列，不使用时是否自动删除，其他参数
                channel.QueueDeclare(queueName, durable, false, false, null);
                var properties = channel.CreateBasicProperties();
                properties.DeliveryMode = 2;//1表示不持久,2.表示持久化
                if (!durable)
                    properties = null;
                var body = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(msg ?? default));
                channel.BasicPublish(string.Empty, queueName, properties, body);
            }
            return true;
        }
        catch (Exception)
        {
            //LoggerHelper.SendLog(ex.ToString());
            throw;
        }
        finally
        {
            ResetMQConnectionToFree(connection);
        }
    }
    #endregion

    #region 消费消息
    /// <summary>
    /// 消费消息
    /// </summary>
    /// <param name="queueName">队列名称</param>
    public static void ConsumeMsg(string queueName, Func<string, ConsumeAction> func)
    {
        var consumer = new RabbitMQConsume();
        consumer.ReceiveMessageCallback += func;
        consumer.ConsumeMsg(queueName);
    }
    /// <summary>
    /// 消费消息
    /// </summary>
    /// <param name="queueName">队列名称</param>
    public static void ConsumeMsg<T>(string queueName, Func<T, ConsumeAction> func) where T : class
    {
        var consumer = new RabbitMQConsume<T>();
        consumer.ReceiveMessageCallback += func;
        consumer.ConsumeMsg(queueName);
    }
    #endregion

    //https://www.cnblogs.com/wei325/p/15174212.html
    //private static ConnectionFactory factory;
    //private static object lockObj = new object();
    ///// <summary>
    ///// 获取单个RabbitMQ连接
    ///// </summary>
    ///// <returns></returns>
    //public static IConnection GetConnection()
    //{
    //    if (factory == null)
    //    {
    //        lock (lockObj)
    //        {
    //            if (factory == null)
    //            {
    //                factory = new ConnectionFactory
    //                {
    //                    HostName = "xxxxx",//ip
    //                    Port = 5672,//端口
    //                    UserName = "hhmed",//账号
    //                    Password = "hhmed",//密码
    //                    VirtualHost = "develop" //虚拟主机
    //                };
    //            }
    //        }
    //    }
    //    return factory.CreateConnection();
    //}
    //public static void SimpleSendMsg()
    //{
    //    string queueName = "simple_order";//队列名
    //    //创建连接
    //    using (var connection = RabbitMQHelper.GetConnection())
    //    {
    //        //创建信道
    //        using (var channel = connection.CreateModel())
    //        {//创建队列
    //            channel.QueueDeclare(queueName, durable: false, exclusive: false, autoDelete: false, arguments: null);
    //            for (var i = 0; i < 10; i++)
    //            {
    //                string message = $"Hello RabbitMQ MessageHello,{i + 1}";
    //                var body = Encoding.UTF8.GetBytes(message);//发送消息
    //                channel.BasicPublish(exchange: "", routingKey: queueName, mandatory: false, basicProperties: null, body);
    //                Console.WriteLine($"发送消息到队列:{queueName},内容:{message}");
    //            }
    //        }
    //    }
    //}
    //public static void SimpleConsumer()
    //{
    //    string queueName = "simple_order";
    //    var connection = RabbitMQHelper.GetConnection();
    //    {
    //        //创建信道
    //        var channel = connection.CreateModel();
    //        {
    //            //创建队列
    //            channel.QueueDeclare(queueName, durable: false, exclusive: false, autoDelete: false, arguments: null);
    //            var consumer = new EventingBasicConsumer(channel);
    //            int i = 0;
    //            consumer.Received += (model, ea) =>
    //            {
    //                //消费者业务处理
    //                var message = Encoding.UTF8.GetString(ea.Body.ToArray());
    //                Console.WriteLine($"{i},队列{queueName}消费消息长度:{message.Length}");
    //                i++;
    //            };
    //            channel.BasicConsume(queueName, true, consumer);
    //        }
    //    }
    //}

}

/// <summary>
/// 消费消息
/// </summary>
internal class RabbitMQConsume
{
    internal Func<string, ConsumeAction> ReceiveMessageCallback { get; set; }

    #region 消费消息
    /// <summary>
    /// 消费消息
    /// </summary>
    /// <param name="queueName">队列名称</param>
    internal void ConsumeMsg(string queueName)
    {
        new System.Threading.Thread(() =>
        {
            while (true)
            {
                try
                {
                    bool durable = true;
                    var connection = RabbitMQHelper.CreateMQConnectionInPoolNew();
                    try
                    {
                        using var channel = connection.CreateModel();
                        channel.QueueDeclare(queueName, durable, false, false, null); //获取队列 
                        channel.BasicQos(0, 1, false); //分发机制为触发式

                        var consumer = new EventingBasicConsumer(channel); //建立消费者
                                                                           //consumer.Received += Consumer_Received;

                        consumer.Received += (model, e) =>
                        {
                            ConsumeAction consumeResult = ConsumeAction.Retry;
                            //处理业务
                            var message = Encoding.UTF8.GetString(e.Body.ToArray());
                            Logger.WriteLog("RabbitMQ", $"队列{queueName}消费消息:{message},不做ack确认");

                            try
                            {
                                consumeResult = ReceiveMessageCallback(message);
                            }
                            catch (Exception)
                            {
                                throw;
                            }
                            if (consumeResult == ConsumeAction.Accept)
                            {
                                channel.BasicAck(e.DeliveryTag, false);  //消息从队列中删除
                            }
                            else if (consumeResult == ConsumeAction.Retry)
                            {
                                channel.BasicNack(e.DeliveryTag, false, true); //消息重回队列
                            }
                            else
                            {
                                channel.BasicNack(e.DeliveryTag, false, false); //消息直接丢弃
                            }
                        };

                        channel.BasicConsume(queueName, false, consumer);// 从左到右参数意思分别是：队列名称、是否读取消息后直接删除消息，消费者
                        while (channel.IsOpen)
                        {
                            System.Threading.Thread.Sleep(2000);
                        }
                    }
                    catch (Exception)
                    {
                        throw;
                    }
                    finally
                    {
                        RabbitMQHelper.ResetMQConnectionToFree(connection);
                    }
                }
                catch (Exception ex)
                {
                    Logger.WriteLog("RabbitMQ", $" Error:{ex}");
                }
                finally
                {
                    //与MQ连接断开或者报错的情况下重连
                    System.Threading.Thread.Sleep(5000);
                }
            }
        })
        .Start();
    }

    /// <summary>
    /// 自定义消息处理
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void Consumer_Received(object sender, BasicDeliverEventArgs e)
    {
        var channel = (IModel)sender;
        ConsumeAction consumeResult = ConsumeAction.Retry;
        try
        {
            consumeResult = ReceiveMessageCallback(e.Body.ToString());
        }
        catch (Exception)
        {
            throw;
        }
        if (consumeResult == ConsumeAction.Accept)
        {
            channel.BasicAck(e.DeliveryTag, false);  //消息从队列中删除
        }
        else if (consumeResult == ConsumeAction.Retry)
        {
            channel.BasicNack(e.DeliveryTag, false, true); //消息重回队列
        }
        else
        {
            channel.BasicNack(e.DeliveryTag, false, false); //消息直接丢弃
        }
    }
    #endregion
}

/// <summary>
/// 消费消息
/// </summary>
internal class RabbitMQConsume<T>
{
    internal Func<T, ConsumeAction> ReceiveMessageCallback { get; set; }

    #region 消费消息
    /// <summary>
    /// 消费消息
    /// </summary>
    /// <param name="queueName">队列名称</param>
    internal void ConsumeMsg(string queueName)
    {
        new System.Threading.Thread(() =>
        {
            while (true)
            {
                try
                {
                    bool durable = true;
                    var connection = RabbitMQHelper.CreateMQConnectionInPoolNew();
                    try
                    {
                        var channel = connection.CreateModel();
                        channel.QueueDeclare(queueName, durable, false, false, null); //获取队列 
                        channel.BasicQos(0, 1, false); //分发机制为触发式
                        var consumer = new EventingBasicConsumer(channel); //建立消费者
                                                                           //consumer.Received += Consumer_Received;

                        consumer.Received += (model, e) =>
                        {
                            ConsumeAction consumeResult = ConsumeAction.Retry;
                            //处理业务
                            var message = Encoding.UTF8.GetString(e.Body.ToArray());
                            Logger.WriteLog("RabbitMQ", $"队列{queueName}消费消息:{message},不做ack确认");

                            try
                            {
                                var inputString = System.Text.UTF8Encoding.UTF8.GetString(e.Body.ToArray());
                                var input = JsonConvert.DeserializeObject<T>(inputString);
                                consumeResult = ReceiveMessageCallback(input);
                            }
                            catch (Exception)
                            {
                                throw;
                            }
                            if (consumeResult == ConsumeAction.Accept)
                            {
                                channel.BasicAck(e.DeliveryTag, false);  //消息从队列中删除
                            }
                            else if (consumeResult == ConsumeAction.Retry)
                            {
                                channel.BasicNack(e.DeliveryTag, false, true); //消息重回队列
                            }
                            else
                            {
                                channel.BasicNack(e.DeliveryTag, false, false); //消息直接丢弃
                            }

                        };

                        channel.BasicConsume(queueName, false, consumer);// 从左到右参数意思分别是：队列名称、是否读取消息后直接删除消息，消费者
                        while (channel.IsOpen)
                        {
                            System.Threading.Thread.Sleep(2000);
                        }
                    }
                    catch (Exception)
                    {
                        throw;
                    }
                    finally
                    {
                        RabbitMQHelper.ResetMQConnectionToFree(connection);
                    }
                }
                catch (Exception ex)
                {
                    Logger.WriteLog("RabbitMQ", $"RabbitMQ Error:{ex.ToString()}");
                }
                finally
                {
                    //与MQ连接断开或者报错的情况下重连
                    System.Threading.Thread.Sleep(5000);
                }
            }
        })
        .Start();
    }
    /// <summary>
    /// 自定义消息处理
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void Consumer_Received(object sender, BasicDeliverEventArgs e)
    {
        var channel = ((EventingBasicConsumer)sender).Model;
        ConsumeAction consumeResult = ConsumeAction.Retry;
        try
        {
            var inputString = System.Text.UTF8Encoding.UTF8.GetString(e.Body.ToArray());
            var model = JsonConvert.DeserializeObject<T>(inputString);
            consumeResult = ReceiveMessageCallback(model);
        }
        catch (Exception)
        {
            throw;
        }
        if (consumeResult == ConsumeAction.Accept)
        {
            channel.BasicAck(e.DeliveryTag, false);  //消息从队列中删除
        }
        else if (consumeResult == ConsumeAction.Retry)
        {
            channel.BasicNack(e.DeliveryTag, false, true); //消息重回队列
        }
        else
        {
            channel.BasicNack(e.DeliveryTag, false, false); //消息直接丢弃
        }
    }
    #endregion
}