namespace EU.Core.Common;

/// <summary>
/// MQTT Broker 配置
/// </summary>
public class MqttBrokerSetting
{
    public bool Enabled { get; set; } = false;
    public int Port { get; set; } = 1883;
    public string WebSocketPath { get; set; } = "/mqtt";
    public int CommunicationTimeoutSeconds { get; set; } = 5;
    public bool PersistentSessions { get; set; } = true;
    public bool EnableWebSocket { get; set; } = true;
    public bool AllowAnonymous { get; set; } = true;
    public string UserName { get; set; } = "";
    public string Password { get; set; } = "";
    public bool RecordClientConnections { get; set; } = true;
}
