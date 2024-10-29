using EU.Core.Common.Caches;
using EU.Core.Common.Const;
using EU.Core.Common.LogHelper;
using Microsoft.AspNetCore.SignalR;

namespace EU.Core.Hubs;

public class ChatHub : Hub
{
    private RedisCacheService Redis = new(5);
    private string cacheKey = "SignalRConnection";
    public async Task SendMessage(string user, string message)
    {
        await Clients.All.SendAsync("ReceiveMessage", user, message);
    }

    #region 客户端连接时触发
    /// <summary>
    /// 客户端连接时触发
    /// </summary>
    /// <returns></returns>
    public override async Task OnConnectedAsync()
    {
        var connectionId = Context.ConnectionId;
        var arg = $"{connectionId} joined";
        Logger.WriteLog("SignalR", arg);
        await base.OnConnectedAsync();
        await Clients.Client(connectionId).SendAsync(SignalRConsts.METHOD_ON_CONNECTED, connectionId);
    }
    #endregion

    #region 客户端注册
    /// <summary>
    /// 客户端注册
    /// </summary>
    /// <param name="sessionid"></param>
    /// <returns></returns>
    public Task SendRegister(string userId)
    {
        var connectionId = Context.ConnectionId;
        var arg = $"{connectionId} register ,userId:{userId}";
        Logger.WriteLog("SignalR", arg);
        var connectionIds = Redis.Get<List<string>>(cacheKey + "-" + userId) ?? new List<string>();
        if (!connectionIds.Where(o => o == connectionId).Any())
            connectionIds.Add(connectionId);
        Redis.AddObject(cacheKey + "-" + userId, connectionIds);
        Redis.Add(cacheKey + "-" + connectionId, userId);

        return Clients.Client(connectionId).SendAsync(SignalRConsts.METHOD_ON_CONSOLE, $"{DateTime.Now} register success");
    }
    #endregion

    /// <summary>
    /// 客户端断开时触发
    /// </summary>
    /// <param name="ex"></param>
    /// <returns></returns>
    public override async Task OnDisconnectedAsync(Exception ex)
    {
        var connectionId = Context.ConnectionId;
        var arg = $"{connectionId} left";

        var userId = Redis.Get(cacheKey + "-" + connectionId);
        var connectionIds = Redis.Get<List<string>>(cacheKey + "-" + userId) ?? new List<string>();
        var index = connectionIds.FindIndex(o => o == connectionId);
        if (index > -1)
            connectionIds.RemoveAt(index);
        Redis.AddObject(cacheKey + "-" + userId, connectionIds);
        Redis.Remove(cacheKey + "-" + connectionId);
        await base.OnDisconnectedAsync(ex);
    }
}