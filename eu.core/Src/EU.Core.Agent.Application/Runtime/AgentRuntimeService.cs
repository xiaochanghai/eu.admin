using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Channels;
using EU.Core.Agent.Application.Agents;
using EU.Core.Model.ViewModels.Extend;
using EU.Core.Agent.Application.Mcp;
using EU.Core.Agent.Application.Validation;
using EU.Core.Agent.Application.Knowledge;
using EU.Core.Agent.Application.Skills;

namespace EU.Core.Agent.Application.Runtime;

public sealed class AgentRuntimeService(
    IAgentRepository agents,
    IPublishedMcpToolCatalog toolCatalog,
    IAgentRuntimeEngine engine,
    IAgentRunAuditRepository auditRepository,
    JsonSchemaValidator schemaValidator,
    IPublishedKnowledgeCatalog? knowledgeCatalog = null,
    IKnowledgeRetriever? knowledgeRetriever = null,
    IPublishedSkillVersionCatalog? skillCatalog = null,
    IPublishedSkillContentStore? skillContentStore = null)
{
    public const int MaximumInputCharacters = 32_768;
    public const int MaximumSkillInstructionCharacters = 131_072;

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

        return await PrepareSnapshotAsync(agent, snapshot, normalizedInput, cancellationToken);
    }

    public async Task<AgentRunPreparationResult> PrepareVersionAsync(
        Guid agentId,
        Guid agentVersionId,
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

        AgentVersionSnapshot? snapshot = agent.PublishedVersions
            .FirstOrDefault(version => version.Id == agentVersionId)
            ?.Snapshot;
        if (snapshot is null)
        {
            return AgentRunPreparationResult.Failure(
                AgentRunErrorCodes.VersionMissing,
                "The requested Agent version is not published by this Agent.");
        }

        return await PrepareSnapshotAsync(agent, snapshot, normalizedInput, cancellationToken);
    }

    private async Task<AgentRunPreparationResult> PrepareSnapshotAsync(
        AgentDefinition agent,
        AgentVersionSnapshot snapshot,
        string normalizedInput,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<PublishedSkillContent> selectedSkills =
            await MaterializeSkillsAsync(snapshot, cancellationToken);
        if (selectedSkills.Count != snapshot.Skills.Count)
        {
            return AgentRunPreparationResult.Failure(
                AgentRunErrorCodes.SkillUnavailable,
                $"One or more frozen Skill versions are unavailable or exceed the {MaximumSkillInstructionCharacters}-character instruction limit.");
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
                    AgentRunErrorCodes.KnowledgeServiceUnavailable,
                    "The knowledge retrieval service is temporarily unavailable.");
            }

            IReadOnlyDictionary<Guid, PublishedKnowledgeReference> availableKnowledge =
                (await knowledgeCatalog.ListAsync(cancellationToken))
                .ToDictionary(value => value.KnowledgeBaseId);
            foreach (AgentKnowledgeBindingSnapshot binding in snapshot.KnowledgeBases)
            {
                if (!availableKnowledge.TryGetValue(
                        binding.KnowledgeBaseId,
                        out PublishedKnowledgeReference? value))
                {
                    return AgentRunPreparationResult.Failure(
                        AgentRunErrorCodes.KnowledgeBindingUnavailable,
                        "The Agent's knowledge binding is unavailable. Review the knowledge authorization and publish a new Agent version.");
                }

                if (value.LogicalRevision != binding.LogicalRevision)
                {
                    return AgentRunPreparationResult.Failure(
                        AgentRunErrorCodes.KnowledgeRevisionStale,
                        "The Agent's knowledge revision is outdated. Publish a new Agent version and update the Main Agent assignment.");
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
            Skills = SkillContractCloner.ReadOnly(
                selectedSkills.Select(SkillContractCloner.Clone)),
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

    private async Task<IReadOnlyList<PublishedSkillContent>> MaterializeSkillsAsync(
        AgentVersionSnapshot snapshot,
        CancellationToken cancellationToken)
    {
        if (snapshot.Skills.Count == 0)
        {
            return SkillContractCloner.ReadOnly(
                Array.Empty<PublishedSkillContent>());
        }

        if (skillCatalog is null || skillContentStore is null)
        {
            return SkillContractCloner.ReadOnly(
                Array.Empty<PublishedSkillContent>());
        }

        IReadOnlyList<PublishedSkillReference> catalogValues;
        try
        {
            catalogValues = await skillCatalog.ListAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            return SkillContractCloner.ReadOnly(
                Array.Empty<PublishedSkillContent>());
        }

        var available = new Dictionary<Guid, PublishedSkillReference>();
        foreach (PublishedSkillReference reference in catalogValues)
        {
            if (!available.TryAdd(reference.VersionId, reference))
            {
                return SkillContractCloner.ReadOnly(
                    Array.Empty<PublishedSkillContent>());
            }
        }

        var materialized = new List<PublishedSkillContent>();
        int combinedCharacters = 0;
        foreach (AgentSkillBindingSnapshot binding in snapshot.Skills)
        {
            if (!available.TryGetValue(
                    binding.SkillVersionId,
                    out PublishedSkillReference? reference))
            {
                return SkillContractCloner.ReadOnly(
                    Array.Empty<PublishedSkillContent>());
            }

            PublishedSkillContent? content;
            try
            {
                content = await skillContentStore.ReadAsync(
                    reference,
                    cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch
            {
                return SkillContractCloner.ReadOnly(
                    Array.Empty<PublishedSkillContent>());
            }

            if (content is null ||
                content.Instructions is null ||
                content.SkillVersionId != reference.VersionId ||
                !string.Equals(
                    content.SkillCode,
                    reference.SkillCode,
                    StringComparison.Ordinal) ||
                !string.Equals(
                    content.VersionLabel,
                    reference.VersionLabel,
                    StringComparison.Ordinal) ||
                !string.Equals(
                    content.ManifestSha256,
                    reference.ManifestSha256,
                    StringComparison.Ordinal))
            {
                return SkillContractCloner.ReadOnly(
                    Array.Empty<PublishedSkillContent>());
            }

            if (content.Instructions.Length >
                MaximumSkillInstructionCharacters - combinedCharacters)
            {
                return SkillContractCloner.ReadOnly(
                    Array.Empty<PublishedSkillContent>());
            }

            combinedCharacters += content.Instructions.Length;
            materialized.Add(SkillContractCloner.Clone(content) with
            {
                SkillName = reference.SkillName
            });
        }

        return SkillContractCloner.ReadOnly(materialized);
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
            await foreach (AgentRunEvent value in channel.Reader.ReadAllAsync())
            {
                yield return value;
            }
        }
        finally
        {
            await producer;
        }

        cancellationToken.ThrowIfCancellationRequested();
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
        bool waitingForApproval = false;
        try
        {
            await writer.WriteAsync(new AgentRunEvent(
                context.RunId,
                ++sequence,
                AgentRunEventKind.Started,
                context.StartedAtUtc), cancellationToken);
            foreach (PublishedSkillContent skill in context.Skills)
            {
                await writer.WriteAsync(new AgentRunEvent(
                    context.RunId,
                    ++sequence,
                    AgentRunEventKind.SkillStarted,
                    DateTimeOffset.UtcNow,
                    SkillVersionId: skill.SkillVersionId,
                    SkillName: skill.SkillName), cancellationToken);
            }

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
                if (value.Kind == AgentRunEventKind.ApprovalRequired)
                {
                    waitingForApproval = true;
                }
                CancellationToken eventCancellation =
                    value.Kind is AgentRunEventKind.ToolSucceeded or
                        AgentRunEventKind.ToolBlocked or
                        AgentRunEventKind.ToolFailed
                        ? CancellationToken.None
                        : cancellationToken;
                await writer.WriteAsync(value, eventCancellation);
            }

            cancellationToken.ThrowIfCancellationRequested();

            if (waitingForApproval)
            {
                await auditRepository.SaveAsync(CreateAudit(
                    context,
                    AgentRunStatus.WaitingForApproval,
                    DateTimeOffset.UtcNow,
                    outputCharacters,
                    AgentRunErrorCodes.ToolApprovalRequired,
                    toolCalls.Values), CancellationToken.None);
                return;
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
        }
        finally
        {
            writer.TryComplete();
        }
    }

    public Task<IReadOnlyList<AgentRunAuditRecord>> ListAuditAsync(
        Guid agentId,
        int take,
        CancellationToken cancellationToken = default) =>
        auditRepository.ListAsync(agentId, Math.Clamp(take, 1, 100), cancellationToken);

    public Task TerminatePreparedRunAsync(
        AgentRunContext context,
        AgentRunStatus status,
        string errorCode,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (status is AgentRunStatus.Running)
        {
            throw new ArgumentOutOfRangeException(
                nameof(status),
                "A prepared Agent run can only be explicitly terminated.");
        }

        return auditRepository.SaveAsync(CreateAudit(
            context,
            status,
            DateTimeOffset.UtcNow,
            outputCharacters: 0,
            errorCode ?? string.Empty,
            []), cancellationToken);
    }

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
