using System.Text.Json;
using EU.Core.Agent.Application.Mcp;
using EU.Core.Agent.Application.Runtime;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace EU.Core.Agent.Infrastructure.Mcp;

public sealed class SdkMcpRuntimeToolInvoker(
    IMcpServerRepository repository,
    SdkMcpToolDiscovery connections,
    TimeSpan callTimeout) : IMcpRuntimeToolInvoker
{
    private const int MaximumResultCharacters = 1_048_576;

    public async Task<McpRuntimeToolResult> InvokeAsync(
        Guid toolVersionId,
        McpToolRisk expectedRisk,
        IReadOnlyDictionary<string, object?> arguments,
        CancellationToken cancellationToken = default)
    {
        if (expectedRisk != McpToolRisk.ReadOnly)
        {
            return new McpRuntimeToolResult(
                false,
                true,
                "",
                AgentRunErrorCodes.ToolBlocked);
        }

        IReadOnlyList<McpServerDefinition> servers =
            await repository.ListAsync(new McpServerQuery(), cancellationToken);
        McpServerDefinition? server = servers.FirstOrDefault(candidate =>
            candidate.CurrentToolVersionIds.Contains(toolVersionId));
        McpToolVersion? version = server?.ToolVersions.FirstOrDefault(candidate =>
            candidate.Id == toolVersionId);
        if (server is null ||
            version is null ||
            version.Risk != expectedRisk ||
            !server.Enabled ||
            server.Status != McpServerStatus.Healthy)
        {
            return new McpRuntimeToolResult(
                false,
                true,
                "",
                AgentRunErrorCodes.ToolUnavailable);
        }

        string argumentsJson;
        try
        {
            argumentsJson = JsonSerializer.Serialize(arguments);
            if (argumentsJson.Length > 65_536)
            {
                return new McpRuntimeToolResult(
                    false,
                    true,
                    "",
                    AgentRunErrorCodes.InputInvalid);
            }

            using JsonDocument document = JsonDocument.Parse(argumentsJson);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return new McpRuntimeToolResult(
                    false,
                    true,
                    "",
                    AgentRunErrorCodes.InputInvalid);
            }
        }
        catch (Exception exception) when (exception is JsonException or NotSupportedException)
        {
            return new McpRuntimeToolResult(
                false,
                true,
                "",
                AgentRunErrorCodes.InputInvalid);
        }

        using var timeout =
            CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(callTimeout);
        try
        {
            await using McpClient client =
                await connections.ConnectAsync(server, timeout.Token);
            IList<McpClientTool> discovered =
                await client.ListToolsAsync(cancellationToken: timeout.Token);
            McpClientTool? tool = discovered.SingleOrDefault(candidate =>
                string.Equals(
                    candidate.ProtocolTool.Name,
                    version.Name,
                    StringComparison.Ordinal));
            if (tool is null ||
                !SchemasEqual(
                    version.InputSchemaJson,
                    tool.ProtocolTool.InputSchema.GetRawText()))
            {
                return new McpRuntimeToolResult(
                    false,
                    true,
                    "",
                    AgentRunErrorCodes.ToolUnavailable);
            }

            CallToolResult result = await tool.CallAsync(
                arguments,
                cancellationToken: timeout.Token);
            string content = McpToolResultFormatter.Format(result);
            if (content.Length > MaximumResultCharacters)
            {
                return new McpRuntimeToolResult(
                    false,
                    false,
                    "",
                    AgentRunErrorCodes.ToolFailed);
            }

            return result.IsError == true
                ? new McpRuntimeToolResult(
                    false,
                    false,
                    content,
                    AgentRunErrorCodes.ToolFailed)
                : new McpRuntimeToolResult(
                    true,
                    false,
                    content,
                    "");
        }
        catch (OperationCanceledException)
            when (!cancellationToken.IsCancellationRequested &&
                  timeout.IsCancellationRequested)
        {
            return new McpRuntimeToolResult(
                false,
                false,
                "",
                AgentRunErrorCodes.ToolTimedOut);
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            return new McpRuntimeToolResult(
                false,
                false,
                "",
                AgentRunErrorCodes.ToolFailed);
        }
    }

    private static bool SchemasEqual(string expected, string actual)
    {
        try
        {
            using JsonDocument expectedDocument = JsonDocument.Parse(expected);
            using JsonDocument actualDocument = JsonDocument.Parse(actual);
            return JsonElement.DeepEquals(
                expectedDocument.RootElement,
                actualDocument.RootElement);
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
