using EU.Core.Common;
using EU.Core.Common.LogHelper;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using MQTTnet;
using MQTTnet.AspNetCore;
using MQTTnet.Protocol;
using MQTTnet.Server;
using System.Buffers;
using System.Net;

namespace EU.Core.Extensions;

/// <summary>
/// MQTT 服务扩展
/// </summary>
public static class MqttSetup
{
    /// <summary>
    /// 添加 MQTT 服务
    /// </summary>
    public static IServiceCollection AddMqttSetup(this IServiceCollection services, MqttBrokerSetting mqttOptions)
    {
        if (!mqttOptions.Enabled)
            return services;

        services.AddSingleton(mqttOptions);
        services.AddSingleton<MqttServer>();
        services.AddSingleton<MqttBrokerService>();
        services.AddMqttTcpServerAdapter();
        services.AddHostedMqttServerWithServices(options =>
        {
            options.WithDefaultEndpointPort(mqttOptions.Port).WithDefaultEndpoint();
            options.WithDefaultEndpointBoundIPAddress(IPAddress.Any);
            options.WithDefaultCommunicationTimeout(TimeSpan.FromSeconds(mqttOptions.CommunicationTimeoutSeconds));
            if (mqttOptions.PersistentSessions)
                options.WithPersistentSessions();
            options.Build();
        }).AddMqttConnectionHandler().AddConnections();

        if (mqttOptions.EnableWebSocket)
            services.AddMqttWebSocketServerAdapter();

        return services;
    }

    /// <summary>
    /// 配置 MQTT 事件（适用于非 ASP.NET Core 环境，如控制台应用）
    /// </summary>
    public static IServiceProvider ConfigureMqttEvents(this IServiceProvider serviceProvider)
    {
        var mqttOptions = serviceProvider.GetService<MqttBrokerSetting>();
        if (mqttOptions == null || !mqttOptions.Enabled)
            return serviceProvider;

        var mqttServer = serviceProvider.GetRequiredService<MqttServer>();
        var mqttService = serviceProvider.GetRequiredService<MqttBrokerService>();

        mqttServer.ClientConnectedAsync += mqttService.OnClientConnectedAsync;
        mqttServer.StartedAsync += mqttService.OnServerStartedAsync;
        mqttServer.StoppedAsync += mqttService.OnServerStoppedAsync;
        mqttServer.ClientSubscribedTopicAsync += mqttService.OnClientSubscribedTopicAsync;
        mqttServer.ClientUnsubscribedTopicAsync += mqttService.OnClientUnsubscribedTopicAsync;
        mqttServer.ValidatingConnectionAsync += mqttService.OnValidatingConnectionAsync;
        mqttServer.ClientDisconnectedAsync += mqttService.OnClientDisconnectedAsync;
        mqttServer.InterceptingPublishAsync += mqttService.OnInterceptingPublishAsync;

        return serviceProvider;
    }

    /// <summary>
    /// 使用 MQTT 中间件
    /// </summary>
    public static void UseMqttSetup(this IApplicationBuilder app)
    {
        var mqttOptions = app.ApplicationServices.GetService<MqttBrokerSetting>();
        if (mqttOptions == null || !mqttOptions.Enabled)
            return;

        var mqttService = app.ApplicationServices.GetRequiredService<MqttBrokerService>();

        if (mqttOptions.EnableWebSocket)
            app.UseWebSockets();

        app.UseMqttServer(server =>
        {
            server.ClientConnectedAsync += mqttService.OnClientConnectedAsync;
            server.StartedAsync += mqttService.OnServerStartedAsync;
            server.StoppedAsync += mqttService.OnServerStoppedAsync;
            server.ClientSubscribedTopicAsync += mqttService.OnClientSubscribedTopicAsync;
            server.ClientUnsubscribedTopicAsync += mqttService.OnClientUnsubscribedTopicAsync;
            server.ValidatingConnectionAsync += mqttService.OnValidatingConnectionAsync;
            server.ClientDisconnectedAsync += mqttService.OnClientDisconnectedAsync;
            server.InterceptingPublishAsync += mqttService.OnInterceptingPublishAsync;
        });
    }

    /// <summary>
    /// 映射 MQTT WebSocket 端点
    /// </summary>
    public static IEndpointRouteBuilder MapMqttWebSocketEndpoint(this IEndpointRouteBuilder endpoints)
    {
        var mqttOptions = endpoints.ServiceProvider.GetService<MqttBrokerSetting>();
        if (mqttOptions == null || !mqttOptions.Enabled || !mqttOptions.EnableWebSocket)
            return endpoints;

        endpoints.MapMqtt(NormalizeWebSocketPath(mqttOptions.WebSocketPath));
        return endpoints;
    }

    private static string NormalizeWebSocketPath(string? webSocketPath)
    {
        if (string.IsNullOrWhiteSpace(webSocketPath))
            return "/mqtt";

        return webSocketPath.StartsWith('/') ? webSocketPath : $"/{webSocketPath}";
    }
}

/// <summary>
/// MQTT Broker 服务
/// </summary>
public class MqttBrokerService
{
    private readonly MqttBrokerSetting _settings;

    public MqttBrokerService(MqttBrokerSetting settings)
    {
        _settings = settings;
    }

    public Task OnServerStartedAsync(EventArgs e)
    {
        Logger.WriteLog($"MQTT Server started on port {_settings.Port}");
        return Task.CompletedTask;
    }

    public Task OnServerStoppedAsync(EventArgs e)
    {
        Logger.WriteLog("MQTT Server stopped");
        return Task.CompletedTask;
    }

    public Task OnClientConnectedAsync(ClientConnectedEventArgs e)
    {
        if (_settings.RecordClientConnections)
            Logger.WriteLog($"MQTT Client connected: {e.ClientId}");

        return Task.CompletedTask;
    }

    public Task OnClientDisconnectedAsync(ClientDisconnectedEventArgs e)
    {
        if (_settings.RecordClientConnections)
            Logger.WriteLog($"MQTT Client disconnected: {e.ClientId}");
        return Task.CompletedTask;
    }

    public Task OnClientSubscribedTopicAsync(ClientSubscribedTopicEventArgs e)
    {
        Logger.WriteLog($"Client {e.ClientId} subscribed to topic {e.TopicFilter.Topic}");
        return Task.CompletedTask;
    }

    public Task OnClientUnsubscribedTopicAsync(ClientUnsubscribedTopicEventArgs e)
    {
        Logger.WriteLog($"Client {e.ClientId} unsubscribed from topic {e.TopicFilter}");
        return Task.CompletedTask;
    }

    public Task OnValidatingConnectionAsync(ValidatingConnectionEventArgs e)
    {
        if (_settings.AllowAnonymous)
        {
            e.ReasonCode = MqttConnectReasonCode.Success;
            return Task.CompletedTask;
        }

        if (string.IsNullOrWhiteSpace(_settings.UserName) || string.IsNullOrWhiteSpace(_settings.Password))
        {
            Logger.WriteLog("MQTT authentication is required, but broker credentials are not configured.");
            e.ReasonCode = MqttConnectReasonCode.BadUserNameOrPassword;
            return Task.CompletedTask;
        }

        if (string.Equals(e.UserName, _settings.UserName, StringComparison.Ordinal) &&
            string.Equals(e.Password, _settings.Password, StringComparison.Ordinal))
        {
            e.ReasonCode = MqttConnectReasonCode.Success;
        }
        else
        {
            e.ReasonCode = MqttConnectReasonCode.BadUserNameOrPassword;
        }

        return Task.CompletedTask;
    }

    public Task OnInterceptingPublishAsync(InterceptingPublishEventArgs e)
    {
        var topic = e.ApplicationMessage.Topic;
        var payload = e.ApplicationMessage.ConvertPayloadToString();

        Logger.WriteLog($"Received MQTT message on topic {topic}: {payload}");

        // 在这里处理你的业务逻辑
        // 例如：解析 JSON、处理命令等
        // if (topic == "A/B/C") { ... }

        return Task.CompletedTask;
    }
}

/// <summary>
/// MQTT 扩展方法
/// </summary>
public static class MqttExtensions
{
    /// <summary>
    /// 将消息载荷转换为字符串
    /// </summary>
    public static string ConvertPayloadToString(this MqttApplicationMessage msg)
    {
        if (msg.Payload.IsEmpty)
            return string.Empty;

        if (msg.Payload.IsSingleSegment)
        {
            return System.Text.Encoding.UTF8.GetString(msg.Payload.FirstSpan);
        }

        return System.Text.Encoding.UTF8.GetString(msg.Payload.ToArray());
    }

    private static byte[] ToArray(this ReadOnlySequence<byte> sequence)
    {
        if (sequence.IsSingleSegment)
        {
            return sequence.FirstSpan.ToArray();
        }

        var array = new byte[sequence.Length];
        sequence.CopyTo(array);
        return array;
    }
}
