using System.Text.Json;
using EU.Core.Agent.Infrastructure.Mcp;
using ModelContextProtocol.Protocol;
using Xunit;

namespace EU.Core.Agent.Tests.Runtime;

public sealed class McpToolResultFormatterTests
{
    [Fact]
    public void Structured_content_is_returned_without_the_protocol_envelope()
    {
        JsonElement structured = JsonSerializer.SerializeToElement(new
        {
            type = "open",
            id = "supplier-7",
            moduleCode = "SUPPLIER"
        });
        var result = new CallToolResult
        {
            Content = [],
            StructuredContent = structured,
            IsError = false
        };

        string content = McpToolResultFormatter.Format(result);
        using JsonDocument document = JsonDocument.Parse(content);

        Assert.Equal("open", document.RootElement.GetProperty("type").GetString());
        Assert.Equal("supplier-7", document.RootElement.GetProperty("id").GetString());
        Assert.Equal("SUPPLIER", document.RootElement.GetProperty("moduleCode").GetString());
        Assert.DoesNotContain("\"content\"", content, StringComparison.Ordinal);
    }

    [Fact]
    public void Text_content_is_returned_as_the_actual_tool_value()
    {
        const string expected =
            """{"type":"open","id":"supplier-7","moduleCode":"SUPPLIER"}""";
        var result = new CallToolResult
        {
            Content = [new TextContentBlock { Text = expected }],
            IsError = false
        };

        Assert.Equal(expected, McpToolResultFormatter.Format(result));
    }
}
