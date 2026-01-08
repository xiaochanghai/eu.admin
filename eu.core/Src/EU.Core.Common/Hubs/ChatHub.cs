using EU.Core.Common.Caches;
using EU.Core.Common.Const;
using Microsoft.AspNetCore.SignalR;
using Serilog;

namespace EU.Core.Hubs;

/// <summary>
/// 聊天 SignalR Hub
/// </summary>
public class ChatHub : Hub
{
    private RedisCacheService _redis;
    private RedisCacheService Redis => _redis ??= RedisCacheService.Create(5);
    private const string CacheKeyPrefix = "SignalRConnection";

    /// <summary>
    /// 发送消息给所有客户端
    /// </summary>
    /// <param name="user">发送者</param>
    /// <param name="message">消息内容</param>
    public async Task SendMessage(string user, string message)
    {
        if (string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(message))
        {
            Log.Warning("SendMessage 接收到空的用户或消息");
            return;
        }

        try
        {
            await Clients.All.SendAsync("ReceiveMessage", user, message);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "发送消息失败: User={User}, Message={Message}", user, message);
            throw;
        }
    }

    /// <summary>
    /// 客户端连接时触发
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        var connectionId = Context.ConnectionId;

        try
        {
            Log.Information("SignalR 客户端连接: ConnectionId={ConnectionId}", connectionId);

            await base.OnConnectedAsync();
            await Clients.Client(connectionId).SendAsync(SignalRConsts.METHOD_ON_CONNECTED, connectionId);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "客户端连接处理失败: ConnectionId={ConnectionId}", connectionId);
            throw;
        }
    }

    /// <summary>
    /// 客户端注册
    /// </summary>
    /// <param name="userId">用户ID</param>
    public async Task SendRegister(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            Log.Warning("SendRegister 接收到空的 userId");
            return;
        }

        var connectionId = Context.ConnectionId;

        try
        {
            Log.Information("SignalR 客户端注册: ConnectionId={ConnectionId}, UserId={UserId}", connectionId, userId);

            // 获取用户的连接ID列表
            var userConnectionKey = GetUserConnectionKey(userId);
            var connectionIds = Redis.Get<List<string>>(userConnectionKey) ?? new List<string>();

            // 如果连接ID不存在，则添加
            if (!connectionIds.Contains(connectionId))
            {
                connectionIds.Add(connectionId);
                Redis.AddObject(userConnectionKey, connectionIds);
            }

            // 存储连接ID到用户ID的映射
            var connectionUserKey = GetConnectionUserKey(connectionId);
            Redis.Add(connectionUserKey, userId);

            await Clients.Client(connectionId).SendAsync(
                SignalRConsts.METHOD_ON_CONSOLE,
                $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} 注册成功");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "客户端注册失败: ConnectionId={ConnectionId}, UserId={UserId}", connectionId, userId);
            throw;
        }
    }

    /// <summary>
    /// 客户端断开时触发
    /// </summary>
    /// <param name="ex">断开异常信息</param>
    public override async Task OnDisconnectedAsync(Exception ex)
    {
        var connectionId = Context.ConnectionId;

        try
        {
            Log.Information("SignalR 客户端断开: ConnectionId={ConnectionId}", connectionId);

            // 获取用户ID
            var connectionUserKey = GetConnectionUserKey(connectionId);
            var userId = Redis.Get(connectionUserKey);

            if (!string.IsNullOrWhiteSpace(userId))
            {
                // 从用户的连接列表中移除此连接
                var userConnectionKey = GetUserConnectionKey(userId);
                var connectionIds = Redis.Get<List<string>>(userConnectionKey) ?? new List<string>();

                if (connectionIds.Remove(connectionId))
                {
                    Redis.AddObject(userConnectionKey, connectionIds);
                }

                // 删除连接到用户的映射
                Redis.Remove(connectionUserKey);
            }

            await base.OnDisconnectedAsync(ex);
        }
        catch (Exception error)
        {
            Log.Error(error, "客户端断开处理失败: ConnectionId={ConnectionId}", connectionId);
        }
    }

    /// <summary>
    /// 获取用户连接缓存键
    /// </summary>
    private static string GetUserConnectionKey(string userId) => $"{CacheKeyPrefix}-{userId}";

    /// <summary>
    /// 获取连接用户缓存键
    /// </summary>
    private static string GetConnectionUserKey(string connectionId) => $"{CacheKeyPrefix}-{connectionId}";
}
