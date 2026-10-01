using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;
using EU.Core.Api.MCP.Attributes;
using Xunit;

namespace EU.Core.Tests;

public sealed class McpSchemaTests
{
    [Fact]
    public void Detailed_schema_supports_nested_arrays_names_and_required_fields()
    {
        var attribute = new McpToolAttribute("sample", "sample", typeof(Input)) { DetailedSchema = true };
        var schema = JsonSerializer.SerializeToElement(attribute.GetInputSchema());
        Assert.Equal("items", schema.GetProperty("required")[0].GetString());
        var items = schema.GetProperty("properties").GetProperty("items");
        Assert.Equal("array", items.GetProperty("type").GetString());
        Assert.Equal("code", items.GetProperty("items").GetProperty("required")[0].GetString());
        Assert.Equal(12, items.GetProperty("items").GetProperty("properties").GetProperty("code").GetProperty("maxLength").GetInt32());
    }

    [Fact]
    public void Recursive_models_fail_predictably_instead_of_recursing_forever()
    {
        var attribute = new McpToolAttribute("recursive", "recursive", typeof(Recursive)) { DetailedSchema = true };
        Assert.Throws<InvalidOperationException>(() => attribute.GetInputSchema());
    }

    public sealed class Input
    {
        [Required, JsonPropertyName("items")]
        public Item[] Items { get; set; }
    }

    public sealed class Item
    {
        [Required, MaxLength(12), JsonPropertyName("code")]
        public string Code { get; set; }
    }

    public sealed class Recursive
    {
        public Recursive Next { get; set; }
    }
}
