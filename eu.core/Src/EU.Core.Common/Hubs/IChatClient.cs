namespace EU.Core.Hubs;

/// <summary>
/// SignalR 客户端接口
/// </summary>
public interface IChatClient
{
    /// <summary>
    /// 接收消息（对象形式）
    /// </summary>
    /// <param name="message">消息内容</param>
    /// <returns></returns>
    Task ReceiveMessage(object message);

    /// <summary>
    /// 接收消息（用户和文本形式）
    /// </summary>
    /// <param name="user">发送者用户名</param>
    /// <param name="message">消息内容</param>
    /// <returns></returns>
    Task ReceiveMessage(string user, string message);

    /// <summary>
    /// 接收更新通知
    /// </summary>
    /// <param name="message">更新内容</param>
    /// <returns></returns>
    Task ReceiveUpdate(object message);
}
