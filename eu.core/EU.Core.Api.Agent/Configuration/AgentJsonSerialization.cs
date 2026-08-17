using System.Text.Json;
using System.Text.Json.Serialization;

namespace EU.Core.Api.Agent.Configuration;

/// <summary>
/// Agent API 分批迁移期间使用的 JSON 序列化配置。
/// </summary>
public static class AgentJsonSerialization
{
    public static JsonSerializerOptions PascalCase { get; } = CreatePascalCaseOptions();

    private static JsonSerializerOptions CreatePascalCaseOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = null,
            DictionaryKeyPolicy = null
        };
        options.Converters.Add(
            new JsonStringEnumConverter(namingPolicy: null, allowIntegerValues: false));
        return options;
    }
}
