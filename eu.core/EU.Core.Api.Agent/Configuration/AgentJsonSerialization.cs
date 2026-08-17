using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;

namespace EU.Core.Api.Agent.Configuration;

/// <summary>
/// Agent API 统一使用的 JSON 序列化配置。
/// </summary>
public static class AgentJsonSerialization
{
    public static JsonSerializerOptions PascalCase { get; } = CreatePascalCaseOptions();

    public static void ConfigureMvc(JsonOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
        options.JsonSerializerOptions.DictionaryKeyPolicy = null;
        options.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter(namingPolicy: null, allowIntegerValues: false));
    }

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
