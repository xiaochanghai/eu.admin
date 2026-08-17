#nullable enable

using System.Text.Json;
using EU.Core.Api.Agent.Configuration;
using EU.Core.Api.Agent.Errors;
using EU.Core.Model;
using EU.Core.Model.ViewModels.Extend;
using Xunit;

namespace EU.Core.Tests.Service_Test;

public sealed class AgAgentApiResponseFoundation_Should
{
    [Fact]
    public void Register_every_fixed_error_code_with_unique_business_status()
    {
        Assert.Equal(188, AgentApiErrorCatalog.All.Count);
        Assert.Equal(
            AgentApiErrorCatalog.All.Count,
            AgentApiErrorCatalog.All.Keys.Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(
            AgentApiErrorCatalog.All.Count,
            AgentApiErrorCatalog.All.Values.Select(value => value.Status).Distinct().Count());
        Assert.All(
            AgentApiErrorCatalog.All.Values,
            value => Assert.InRange(value.Status, 600000, 699999));
    }

    [Theory]
    [InlineData("REQUEST_INVALID", 600001, 400)]
    [InlineData("REQUEST_UNSUPPORTED_MEDIA_TYPE", 600012, 415)]
    [InlineData("AGENT_NOT_FOUND", 610001, 404)]
    [InlineData("SKILL_ARCHIVE_BLOCKED", 620013, 409)]
    [InlineData("MCP_DISABLE_BLOCKED", 630010, 409)]
    [InlineData("KNOWLEDGE_SERVICE_UNAVAILABLE", 640010, 503)]
    [InlineData("ORCHESTRATION_RUN_INPUT_INVALID", 650010, 400)]
    [InlineData("MODEL_INVOCATION_FAILED", 660003, 502)]
    [InlineData("MODEL_JUDGE_EXECUTION_FAILED", 670029, 502)]
    [InlineData("AGENT_AUDIT_UNAVAILABLE", 680001, 503)]
    [InlineData("UNEXPECTED_ERROR", 690001, 500)]
    public void Resolve_fixed_error_code(
        string errorCode,
        int expectedStatus,
        int expectedHttpStatus)
    {
        AgentApiErrorDescriptor descriptor = AgentApiErrorCatalog.Resolve(errorCode);

        Assert.Equal(expectedStatus, descriptor.Status);
        Assert.Equal(expectedHttpStatus, descriptor.HttpStatus);
    }

    [Fact]
    public void Keep_http_status_absent_for_runtime_only_error()
    {
        AgentApiErrorDescriptor descriptor =
            AgentApiErrorCatalog.Resolve("OPERATION_CANCELLED");

        Assert.Equal(600011, descriptor.Status);
        Assert.Null(descriptor.HttpStatus);
    }

    [Fact]
    public void Resolve_unknown_error_to_safe_fallback()
    {
        AgentApiErrorDescriptor descriptor =
            AgentApiErrorCatalog.Resolve("EXTERNAL_UNKNOWN_ERROR");

        Assert.Equal(699999, descriptor.Status);
        Assert.Equal(500, descriptor.HttpStatus);
    }

    [Fact]
    public void Create_failure_with_business_status_and_error_metadata()
    {
        var metadata = new AgentApiErrorData("AGENT_NOT_FOUND", "trace-123");

        ServiceResult<AgentApiErrorData> result =
            ServiceResult<AgentApiErrorData>.Failure(610001, "Agent 不存在。", metadata);

        Assert.Equal(610001, result.Status);
        Assert.False(result.Success);
        Assert.Equal("Agent 不存在。", result.Message);
        Assert.Null(result.MessageDev);
        Assert.Equal(0, result.Count);
        Assert.Same(metadata, result.Data);
    }

    [Fact]
    public void Serialize_wrapper_as_pascal_case_without_renaming_dynamic_data_keys()
    {
        var data = new Dictionary<string, object?>
        {
            ["json_schema_key"] = new Dictionary<string, object?>
            {
                ["required_field"] = true
            }
        };
        ServiceResult<Dictionary<string, object?>> result =
            ServiceResult<Dictionary<string, object?>>.QuerySuccess(data);

        string json = JsonSerializer.Serialize(result, AgentJsonSerialization.PascalCase);
        using JsonDocument document = JsonDocument.Parse(json);
        JsonElement root = document.RootElement;

        Assert.Equal(200, root.GetProperty("Status").GetInt32());
        Assert.True(root.GetProperty("Success").GetBoolean());
        Assert.True(root.TryGetProperty("Message", out _));
        Assert.True(root.TryGetProperty("MessageDev", out _));
        Assert.True(root.TryGetProperty("Count", out _));
        JsonElement dynamicData = root.GetProperty("Data");
        Assert.True(dynamicData.TryGetProperty("json_schema_key", out JsonElement schema));
        Assert.True(schema.TryGetProperty("required_field", out _));
        Assert.False(root.TryGetProperty("status", out _));
    }
}
