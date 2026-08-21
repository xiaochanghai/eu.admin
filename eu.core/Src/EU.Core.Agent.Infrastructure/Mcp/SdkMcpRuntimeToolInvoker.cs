using System.Text.Json;
using System.Text.Json.Nodes;
using System.Security.Cryptography;
using System.Text;
using EU.Core.IServices.Approvals;
using EU.Core.IServices.Mcp;
using EU.Core.IServices.Runtime;
using EU.Core.IServices.UnifiedEntry;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace EU.Core.Agent.Infrastructure.Mcp;

public sealed class SdkMcpRuntimeToolInvoker(
    IMcpServerDefinitionCatalog serverCatalog,
    SdkMcpToolDiscovery connections,
    TimeSpan callTimeout,
    BusinessQueryToolPolicy? businessQueryPolicy = null,
    IBusinessQueryContextTokenProvider? businessQueryTokens = null) :
    IMcpRuntimeToolInvoker,
    IApprovedMcpRuntimeToolInvoker
{
    private const int MaximumResultCharacters = 1_048_576;

    public async Task<McpRuntimeToolResult> InvokeAsync(
        Guid toolVersionId,
        McpToolRisk expectedRisk,
        IReadOnlyDictionary<string, object?> arguments,
        CancellationToken cancellationToken = default) =>
        await InvokeAsync(
            toolVersionId,
            expectedRisk,
            arguments,
            null,
            cancellationToken);

    public async Task<McpRuntimeToolResult> InvokeAsync(
        Guid toolVersionId,
        McpToolRisk expectedRisk,
        IReadOnlyDictionary<string, object?> arguments,
        McpInvocationContext? invocationContext,
        CancellationToken cancellationToken = default)
        => await InvokeCoreAsync(
            toolVersionId,
            expectedRisk,
            arguments,
            invocationContext,
            approvedClaim: null,
            cancellationToken);

    public async Task<McpRuntimeToolResult> InvokeApprovedAsync(
        ToolApprovalExecutionClaim claim,
        PublishedMcpToolReference tool,
        IReadOnlyDictionary<string, object?> arguments,
        McpInvocationContext invocationContext,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(claim);
        ArgumentNullException.ThrowIfNull(tool);
        ArgumentNullException.ThrowIfNull(invocationContext);
        ToolApprovalRequestRecord approval = claim.Request;
        if (approval.Status != ToolApprovalStatus.Consuming
            || approval.ToolVersionId != tool.ToolVersionId
            || approval.McpServerId != tool.ServerId
            || approval.Risk != tool.Risk
            || tool.Risk is not (McpToolRisk.Mutating or McpToolRisk.HighRisk)
            || approval.AgentRunId != invocationContext.AgentRunId
            || !string.Equals(
                approval.TenantId,
                invocationContext.Identity.TenantId,
                StringComparison.Ordinal)
            || !string.Equals(
                approval.RequesterUserId,
                invocationContext.Identity.UserId,
                StringComparison.Ordinal))
        {
            return BlockedApproval();
        }

        string argumentsJson;
        try
        {
            argumentsJson = JsonSerializer.Serialize(arguments);
        }
        catch (Exception exception) when (exception is JsonException or NotSupportedException)
        {
            return BlockedApproval();
        }

        if (!string.Equals(
            Sha256(argumentsJson),
            approval.ArgumentsSha256,
            StringComparison.Ordinal))
        {
            return BlockedApproval();
        }

        return await InvokeCoreAsync(
            tool.ToolVersionId,
            tool.Risk,
            arguments,
            invocationContext,
            claim,
            cancellationToken);
    }

    private async Task<McpRuntimeToolResult> InvokeCoreAsync(
        Guid toolVersionId,
        McpToolRisk expectedRisk,
        IReadOnlyDictionary<string, object?> arguments,
        McpInvocationContext? invocationContext,
        ToolApprovalExecutionClaim? approvedClaim,
        CancellationToken cancellationToken)
    {
        bool riskAllowed = approvedClaim is null
            ? expectedRisk == McpToolRisk.ReadOnly
            : expectedRisk is McpToolRisk.Mutating or McpToolRisk.HighRisk;
        if (!riskAllowed)
        {
            return new McpRuntimeToolResult(
                false,
                true,
                "",
                AgentRunErrorCodes.ToolBlocked);
        }

        IReadOnlyList<McpServerDefinition> servers =
            await serverCatalog.ListAsync(new McpServerQuery(), cancellationToken);
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

        string? executionToken = null;
        if (businessQueryPolicy is not null)
        {
            bool reservedServer = string.Equals(
                server.Code,
                businessQueryPolicy.ServerCode,
                StringComparison.Ordinal);
            bool reservedTool = string.Equals(
                version.Name,
                businessQueryPolicy.ToolName,
                StringComparison.Ordinal);
            bool exactTarget = businessQueryPolicy.Matches(
                server.Code,
                version.Name,
                server.Endpoint);
            if ((reservedServer || reservedTool) && !exactTarget)
            {
                return BlockedConfiguration();
            }

            if (exactTarget)
            {
                if (server.Transport != McpTransportKind.StreamableHttp
                    || invocationContext is null
                    || businessQueryTokens is null)
                {
                    return BlockedConfiguration();
                }

                try
                {
                    executionToken = await businessQueryTokens.CreateAsync(
                        invocationContext,
                        businessQueryPolicy,
                        new PublishedMcpToolReference(
                            server.Id,
                            server.Code,
                            server.Name,
                            version.Id,
                            version.Name,
                            version.Description,
                            version.InputSchemaJson,
                            version.Risk,
                            version.Sha256),
                        cancellationToken);
                }
                catch (Exception) when (!cancellationToken.IsCancellationRequested)
                {
                    return BlockedConfiguration();
                }
            }
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

            if (executionToken is not null)
            {
                tool = tool.WithMeta(new JsonObject
                {
                    [BusinessQueryContextTokenProvider.MetadataKey] = executionToken
                });
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

    private static McpRuntimeToolResult BlockedApproval() => new(
        false,
        true,
        string.Empty,
        ToolApprovalErrorCodes.InvalidState);

    private static string Sha256(string value) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    private static McpRuntimeToolResult BlockedConfiguration() => new(
        false,
        true,
        string.Empty,
        AgentRunErrorCodes.ToolConfigurationInvalid);

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
