using EU.Core.Agent.Runtime;
using Xunit;

namespace EU.Core.Agent.Tests.Runtime;

public sealed class McpToolArgumentFormatterTests
{
    [Fact]
    public void Formats_arguments_as_compact_redacted_json()
    {
        IReadOnlyDictionary<string, object?> arguments =
            new Dictionary<string, object?>
            {
                ["supplierId"] = "S1",
                ["options"] = new Dictionary<string, object?>
                {
                    ["authorization"] = "Bearer secret",
                    ["page"] = 2
                }
            };

        string value = McpToolArgumentFormatter.Format(arguments);

        Assert.Equal(
            """{"supplierId":"S1","options":{"authorization":"[REDACTED]","page":2}}""",
            value);
    }
}
