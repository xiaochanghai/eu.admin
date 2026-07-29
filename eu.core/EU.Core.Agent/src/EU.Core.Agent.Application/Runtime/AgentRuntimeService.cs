using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Channels;
using EU.Core.Agent.Application.Agents;
using EU.Core.Agent.Application.Mcp;
using EU.Core.Agent.Application.Validation;
using EU.Core.Agent.Application.Knowledge;

namespace EU.Core.Agent.Application.Runtime;

public sealed class AgentRuntimeService(
    IAgentRepository agents,
    IPublishedMcpToolCatalog toolCatalog,
    IAgentRuntimeEngine engine,
    IAgentRunAuditRepository auditRepository,
    JsonSchemaValidator schemaValidator,
    IPublishedKnowledgeCatalog? knowledgeCatalog = null,
    IKnowledgeRetriever? knowledgeRetriever = null)
{
    public const int MaximumInputCharacters = 32_768;

    public async Task<AgentRunPreparationResult> PrepareAsync(
        Guid agentId,
        string? input,
        CancellationToken cancellationToken = default)
    {
        string normalizedInput = input?.Trim() ?? string.Empty;
        if (normalizedInput.Length is 0 or > MaximumInputCharacters)
        {
            return AgentRunPreparationResult.Failure(
                AgentRunErrorCodes.InputInvalid,
                $"Run input must contain from 1 through {MaximumInputCharacters} characters.");
        }

        AgentDefinition? agent = await agents.GetByIdAsync(agentId, cancellationToken);
        if (agent is null)
        {
            return AgentRunPreparationResult.Failure(
                AgentRunErrorCodes.AgentNotFound,
                "The Agent was not found.");
        }

        if (agent.RuntimeStatus != AgentRuntimeStatus.Enabled)
        {
            return AgentRunPreparationResult.Failure(
                AgentRunErrorCodes.AgentDisabled,
                "The Agent is disabled.");
        }

        AgentVersionSnapshot? snapshot =
            agent.PublishedVersions.LastOrDefault()?.Snapshot;
        if (snapshot is null)
        {
            return AgentRunPreparationResult.Failure(
                AgentRunErrorCodes.VersionMissing,
                "The Agent has no published version.");
        }

        IReadOnlyDictionary<Guid, PublishedMcpToolReference> available =
            (await toolCatalog.ListAsync(cancellationToken))
            .ToDictionary(tool => tool.ToolVersionId);
        var selected = new List<PublishedMcpToolReference>();
        foreach (AgentToolBindingSnapshot binding in snapshot.Tools)
        {
            if (!available.TryGetValue(binding.ToolVersionId, out PublishedMcpToolReference? tool))
            {
                return AgentRunPreparationResult.Failure(
                    AgentRunErrorCodes.ToolUnavailable,
                    $"Authorized MCP tool version '{binding.ToolVersionId}' is no longer available.");
            }

            selected.Add(tool);
        }

        DateTimeOffset startedAt = DateTimeOffset.UtcNow;
        IReadOnlyList<KnowledgeSearchResult> knowledge = Array.Empty<KnowledgeSearchResult>();
        if (snapshot.KnowledgeBases.Count > 0)
        {
            if (knowledgeCatalog is null || knowledgeRetriever is null)
            {
                return AgentRunPreparationResult.Failure(
                    AgentRunErrorCodes.KnowledgeUnavailable,
                    "Knowledge retrieval is not available.");
            }

            IReadOnlyDictionary<Guid, PublishedKnowledgeReference> availableKnowledge =
                (await knowledgeCatalog.ListAsync(cancellationToken))
                .ToDictionary(value => value.KnowledgeBaseId);
            foreach (AgentKnowledgeBindingSnapshot binding in snapshot.KnowledgeBases)
            {
                if (!availableKnowledge.TryGetValue(binding.KnowledgeBaseId, out PublishedKnowledgeReference? value) ||
                    value.LogicalRevision != binding.LogicalRevision)
                {
                    return AgentRunPreparationResult.Failure(
                        AgentRunErrorCodes.KnowledgeUnavailable,
                        $"Authorized knowledge base '{binding.KnowledgeBaseId}' is no longer available.");
                }
            }

            knowledge = await knowledgeRetriever.SearchAsync(
                snapshot.KnowledgeBases.Select(value => value.KnowledgeBaseId).ToArray(),
                normalizedInput,
                6,
                cancellationToken);
        }

        var context = new AgentRunContext(
            Guid.NewGuid(),
            agent.Id,
            AgentContractCloner.Clone(snapshot),
            normalizedInput,
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalizedInput)))
                .ToLowerInvariant(),
            startedAt,
            McpContractCloner.ReadOnly(selected))
        {
            Knowledge = KnowledgeContractCloner.ReadOnly(knowledge)
        };
        await auditRepository.SaveAsync(CreateAudit(
            context,
            AgentRunStatus.Running,
            finishedAt: null,
            outputCharacters: 0,
            errorCode: "",
            []), cancellationToken);
        return AgentRunPreparationResult.Success(context);
    }

    public async IAsyncEnumerable<AgentRunEvent> StreamAsync(
        AgentRunContext context,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var channel = Channel.CreateUnbounded<AgentRunEvent>(
            new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = true
            });
        Task producer = ProduceAsync(context, channel.Writer, cancellationToken);
        try
        {
            await foreach (AgentRunEvent value in channel.Reader
                .ReadAllAsync(cancellationToken))
            {
                yield return value;
            }
        }
        finally
        {
            await producer;
        }
    }

    private async Task ProduceAsync(
        AgentRunContext context,
        ChannelWriter<AgentRunEvent> writer,
        CancellationToken cancellationToken)
    {
        long sequence = 0;
        int outputCharacters = 0;
        var output = new StringBuilder();
        var toolCalls = new Dictionary<Guid, AgentToolCallAuditRecord>();
        await writer.WriteAsync(new AgentRunEvent(
            context.RunId,
            ++sequence,
            AgentRunEventKind.Started,
            context.StartedAtUtc), cancellationToken);
        foreach (KnowledgeSearchResult citation in context.Knowledge)
        {
            await writer.WriteAsync(new AgentRunEvent(
                context.RunId,
                ++sequence,
                AgentRunEventKind.Citation,
                DateTimeOffset.UtcNow,
                $"[kb:{citation.KnowledgeBaseCode}/{citation.FileName}#{citation.ChunkSequence}]"),
                cancellationToken);
        }

        try
        {
            await foreach (AgentRunEvent source in engine
                .StreamAsync(context, cancellationToken)
                .WithCancellation(cancellationToken))
            {
                AgentRunEvent value = source with
                {
                    RunId = context.RunId,
                    Sequence = ++sequence
                };
                if (value.Kind == AgentRunEventKind.Delta)
                {
                    outputCharacters += value.Text.Length;
                    output.Append(value.Text);
                }

                TrackToolCall(value, context, toolCalls);
                await writer.WriteAsync(value, cancellationToken);
            }

            if (context.Snapshot.OutputMode == AgentOutputMode.Structured)
            {
                if (!string.IsNullOrWhiteSpace(context.Snapshot.OutputJsonSchema) &&
                    !schemaValidator.ValidateInstance(
                        context.Snapshot.OutputJsonSchema,
                        output.ToString()).Succeeded)
                {
                    throw new InvalidDataException("The structured Agent output is invalid.");
                }
            }

            DateTimeOffset finishedAt = DateTimeOffset.UtcNow;
            await auditRepository.SaveAsync(CreateAudit(
                context,
                AgentRunStatus.Completed,
                finishedAt,
                outputCharacters,
                "",
                toolCalls.Values), CancellationToken.None);
            await writer.WriteAsync(new AgentRunEvent(
                context.RunId,
                ++sequence,
                AgentRunEventKind.Completed,
                finishedAt), cancellationToken);
            writer.TryComplete();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            DateTimeOffset finishedAt = DateTimeOffset.UtcNow;
            await auditRepository.SaveAsync(CreateAudit(
                context,
                AgentRunStatus.Cancelled,
                finishedAt,
                outputCharacters,
                "",
                toolCalls.Values), CancellationToken.None);
            writer.TryComplete();
        }
        catch (Exception exception)
        {
            string errorCode = exception switch
            {
                InvalidDataException => AgentRunErrorCodes.OutputInvalid,
                AgentRuntimeException runtimeException =>
                    runtimeException.ErrorCode,
                _ => AgentRunErrorCodes.ModelFailed
            };
            DateTimeOffset finishedAt = DateTimeOffset.UtcNow;
            await auditRepository.SaveAsync(CreateAudit(
                context,
                AgentRunStatus.Failed,
                finishedAt,
                outputCharacters,
                errorCode,
                toolCalls.Values), CancellationToken.None);
            await writer.WriteAsync(new AgentRunEvent(
                context.RunId,
                ++sequence,
                AgentRunEventKind.Failed,
                finishedAt,
                ErrorCode: errorCode), CancellationToken.None);
            writer.TryComplete();
        }
    }

    public Task<IReadOnlyList<AgentRunAuditRecord>> ListAuditAsync(
        Guid agentId,
        int take,
        CancellationToken cancellationToken = default) =>
        auditRepository.ListAsync(agentId, Math.Clamp(take, 1, 100), cancellationToken);

    private static AgentRunAuditRecord CreateAudit(
        AgentRunContext context,
        AgentRunStatus status,
        DateTimeOffset? finishedAt,
        int outputCharacters,
        string errorCode,
        IEnumerable<AgentToolCallAuditRecord> calls) =>
        new(
            context.RunId,
            context.AgentId,
            context.Snapshot.VersionId,
            context.Snapshot.AgentCode,
            status,
            context.StartedAtUtc,
            finishedAt,
            context.InputSha256,
            outputCharacters,
            calls.Count(),
            errorCode,
            calls.OrderBy(call => call.StartedAtUtc).ToArray());

    private static void TrackToolCall(
        AgentRunEvent value,
        AgentRunContext context,
        IDictionary<Guid, AgentToolCallAuditRecord> calls)
    {
        if (value.ToolVersionId is not Guid toolVersionId)
        {
            return;
        }
        Guid callId = value.ToolCallId ?? toolVersionId;

        PublishedMcpToolReference? tool =
            context.Tools.FirstOrDefault(candidate => candidate.ToolVersionId == toolVersionId);
        if (tool is null)
        {
            return;
        }

        if (value.Kind == AgentRunEventKind.ToolStarted)
        {
            calls[callId] = new AgentToolCallAuditRecord(
                toolVersionId,
                tool.ToolName,
                tool.Risk,
                AgentRunEventKind.ToolStarted,
                value.OccurredAtUtc,
                value.OccurredAtUtc,
                "");
        }
        else if (value.Kind is AgentRunEventKind.ToolSucceeded or
                 AgentRunEventKind.ToolBlocked or
                 AgentRunEventKind.ToolFailed)
        {
            DateTimeOffset startedAt = calls.TryGetValue(callId, out AgentToolCallAuditRecord? current)
                ? current.StartedAtUtc
                : value.OccurredAtUtc;
            calls[callId] = new AgentToolCallAuditRecord(
                toolVersionId,
                tool.ToolName,
                tool.Risk,
                value.Kind,
                startedAt,
                value.OccurredAtUtc,
                value.ErrorCode);
        }
    }
}
