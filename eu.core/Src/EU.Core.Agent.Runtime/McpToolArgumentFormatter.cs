using System.Text.Json;
using EU.Core.Agent.Application.Orchestration;

namespace EU.Core.Agent.Runtime;

public static class McpToolArgumentFormatter
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static string Format(IReadOnlyDictionary<string, object?> arguments)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        return ExecutionPayloadRedactor.RedactJson(
            JsonSerializer.Serialize(arguments, SerializerOptions));
    }
}
