using System.Text.Json;
using ModelContextProtocol.Protocol;

namespace EU.Core.Agent.Infrastructure.Mcp;

public static class McpToolResultFormatter
{
    public static string Format(CallToolResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        if (result.StructuredContent is not null)
        {
            return JsonSerializer.Serialize(result.StructuredContent);
        }

        string[] text = result.Content
            .OfType<TextContentBlock>()
            .Select(block => block.Text)
            .Where(value => !string.IsNullOrEmpty(value))
            .ToArray();
        if (text.Length > 0)
        {
            return string.Join(Environment.NewLine, text);
        }

        return JsonSerializer.Serialize(result.Content);
    }
}
