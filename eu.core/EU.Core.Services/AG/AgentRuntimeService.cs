using EU.Core.IServices.Knowledge;
using EU.Core.IServices.Mcp;
using EU.Core.IServices.Runtime;
using EU.Core.IServices.Skills;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Threading.Channels;

#nullable enable

namespace EU.Core.Services;

// 文件职责：AgentRuntimeService 职责实现

/// <summary>
/// 负责准备并启动 Agent 运行。
/// </summary>
/// <param name="agents">用于查询 Agent 定义及已发布版本的目录。</param>
/// <param name="toolCatalog">用于查询已发布 MCP 工具版本的目录。</param>
/// <param name="engine">执行 Agent 模型推理与工具调用的运行引擎。</param>
/// <param name="auditRepository">用于持久化 Agent 运行审计记录的仓储。</param>
/// <param name="schemaValidator">用于校验 Agent 输入及输出 JSON 结构的校验器。</param>
/// <param name="knowledgeRetriever">可选的知识库检索器。</param>
/// <param name="skillCatalog">用于查询已发布技能版本的目录。</param>
/// <param name="skillContentStore">用于读取已发布技能文件内容的存储。</param>
public sealed class AgentRuntimeService(
    IAgentDefinitionCatalog agents,
    IPublishedMcpToolCatalog toolCatalog,
    IAgentRuntimeEngine engine,
    IAgentRunAuditRepository auditRepository,
    JsonSchemaValidator schemaValidator,
    IKnowledgeRetriever? knowledgeRetriever = null,
    IPublishedSkillVersionCatalog? skillCatalog = null,
    IPublishedSkillContentStore? skillContentStore = null) : IAgentRuntimeService
{
    /// <summary>单次运行输入允许的最大字符数。</summary>
    public const int MaximumInputCharacters = 32_768;
    /// <summary>技能指令允许的最大字符数。</summary>
    public const int MaximumSkillInstructionCharacters = 131_072;

    #region 准备（PrepareAsync）
    /// <summary>
    /// 准备（PrepareAsync）
    /// </summary>
    /// <param name="agentId">Agent 定义标识。</param>
    /// <param name="input">执行输入内容。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>最新发布快照的运行准备结果，成功时包含运行上下文，校验失败时包含错误信息。</returns>
    public async Task<AgentRunPreparationResult> PrepareAsync(Guid agentId, string? input, CancellationToken cancellationToken = default)
    {
        string normalizedInput = input?.Trim() ?? string.Empty;
        if (normalizedInput.Length is 0 or > MaximumInputCharacters)
        {
            return AgentRunPreparationResult.Failure(
                AgentRunErrorCodes.InputInvalid,
                $"Run input must contain from 1 through {MaximumInputCharacters} characters.");
        }

        AgentDefinition? agent = await agents.GetDefinitionAsync(agentId, cancellationToken);
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
    #endregion

    #region 准备（PrepareVersionAsync）
    /// <summary>
    /// 准备（PrepareVersionAsync）
    /// </summary>
    /// <param name="agentId">Agent 定义标识。</param>
    /// <param name="agentVersionId">Agent 版本标识。</param>
    /// <param name="input">执行输入内容。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>指定已发布版本的运行准备结果，成功时包含运行上下文，校验失败时包含错误信息。</returns>
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

        AgentDefinition? agent = await agents.GetDefinitionAsync(agentId, cancellationToken);
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
    #endregion

    #region 准备（PrepareSnapshotAsync）
    /// <summary>
    /// 准备（PrepareSnapshotAsync）
    /// </summary>
    /// <param name="agent">Agent 定义。</param>
    /// <param name="snapshot">版本快照。</param>
    /// <param name="normalizedInput">规范化后的执行输入。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>完成技能、工具及知识库资源校验并记录启动审计后的运行上下文，或对应的准备失败信息。</returns>
    private async Task<AgentRunPreparationResult> PrepareSnapshotAsync(
        AgentDefinition agent,
        AgentVersionSnapshot snapshot,
        string normalizedInput,
        CancellationToken cancellationToken)
    {
        SkillMaterializationResult skillMaterialization =
            await MaterializeSkillsAsync(snapshot, cancellationToken);
        if (!skillMaterialization.Succeeded)
        {
            return AgentRunPreparationResult.Failure(
                AgentRunErrorCodes.SkillUnavailable,
                skillMaterialization.ErrorMessage);
        }

        IReadOnlyList<PublishedSkillContent> selectedSkills =
            skillMaterialization.Skills;

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
            if (knowledgeRetriever is null)
            {
                return AgentRunPreparationResult.Failure(
                    AgentRunErrorCodes.KnowledgeServiceUnavailable,
                    "The knowledge retrieval service is temporarily unavailable.");
            }

            IReadOnlyDictionary<Guid, PublishedKnowledgeReference> availableKnowledge =
                (await knowledgeRetriever.ListPublishedAsync(cancellationToken))
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
            Knowledge = Array.AsReadOnly(knowledge.ToArray())
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
    #endregion

    #region 处理（MaterializeSkillsAsync）
    /// <summary>
    /// 处理（MaterializeSkillsAsync）
    /// </summary>
    /// <param name="snapshot">版本快照。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>快照中已发布技能的实际内容集合，或加载、完整性校验失败的错误说明。</returns>
    private async Task<SkillMaterializationResult> MaterializeSkillsAsync(AgentVersionSnapshot snapshot, CancellationToken cancellationToken)
    {
        if (snapshot.Skills.Count == 0)
        {
            return SkillMaterializationResult.Success(
                Array.Empty<PublishedSkillContent>());
        }

        if (skillCatalog is null || skillContentStore is null)
        {
            return SkillMaterializationResult.Failure(
                "The Skill runtime service is not configured. Remove the Skill bindings or configure Skill storage before running the Agent.");
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
        catch (Exception exception)
        {
            return SkillMaterializationResult.Failure(
                $"The published Skill catalog could not be read ({exception.GetType().Name}). Retry the request or check the Skill storage service.");
        }

        var available = new Dictionary<Guid, PublishedSkillReference>();
        foreach (PublishedSkillReference reference in catalogValues)
        {
            if (!available.TryAdd(reference.VersionId, reference))
            {
                return SkillMaterializationResult.Failure(
                    $"Published Skill version '{reference.VersionId}' occurs more than once in the catalog. Correct the Skill catalog before running the Agent.");
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
                return SkillMaterializationResult.Failure(
                    $"Frozen Skill version '{binding.SkillVersionId}' is not published or is no longer available. Review the Agent's Skill bindings and publish a new Agent version.");
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
            catch (Exception exception)
            {
                return SkillMaterializationResult.Failure(
                    $"The artifact for Skill '{reference.SkillName}' version '{reference.VersionLabel}' could not be read ({exception.GetType().Name}). Restore or republish the Skill, then publish a new Agent version.");
            }

            if (content is null)
            {
                return SkillMaterializationResult.Failure(
                    $"The artifact for Skill '{reference.SkillName}' version '{reference.VersionLabel}' is missing. Restore or republish the Skill, then publish a new Agent version.");
            }

            if (content.Instructions is null ||
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
                return SkillMaterializationResult.Failure(
                    $"The artifact for Skill '{reference.SkillName}' version '{reference.VersionLabel}' does not match its published metadata. Republish the Skill, then publish a new Agent version.");
            }

            if (content.Instructions.Length >
                MaximumSkillInstructionCharacters - combinedCharacters)
            {
                return SkillMaterializationResult.Failure(
                    $"The frozen Skills contain more than {MaximumSkillInstructionCharacters} instruction characters in total. Reduce the bound Skill instructions and publish a new Agent version.");
            }

            combinedCharacters += content.Instructions.Length;
            materialized.Add(SkillContractCloner.Clone(content) with
            {
                SkillName = reference.SkillName
            });
        }

        return SkillMaterializationResult.Success(materialized);
    }
    #endregion

    private sealed record SkillMaterializationResult(
        IReadOnlyList<PublishedSkillContent> Skills,
        string ErrorMessage)
    {
        public bool Succeeded => string.IsNullOrEmpty(ErrorMessage);

        #region 处理（Success）
        /// <summary>
        /// 处理（Success）
        /// </summary>
        /// <param name="skills">技能服务。</param>
        /// <returns>包含只读技能内容集合且无错误消息的技能加载成功结果。</returns>
        public static SkillMaterializationResult Success(IEnumerable<PublishedSkillContent> skills) =>
            new(SkillContractCloner.ReadOnly(skills), string.Empty);
        #endregion

        #region 处理（Failure）
        /// <summary>
        /// 处理（Failure）
        /// </summary>
        /// <param name="errorMessage">失败对应的错误说明。</param>
        /// <returns>包含空技能集合和指定错误消息的技能加载失败结果。</returns>
        public static SkillMaterializationResult Failure(string errorMessage) =>
            new(
                SkillContractCloner.ReadOnly(
                    Array.Empty<PublishedSkillContent>()),
                errorMessage);
        #endregion
    }

    #region 流式输出（StreamAsync）
    /// <summary>
    /// 流式输出（StreamAsync）
    /// </summary>
    /// <param name="context">Agent 运行上下文，包含固定版本快照、输入和工具资源。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>按执行顺序产生的异步事件流。</returns>
    public async IAsyncEnumerable<AgentRunEvent> StreamAsync(AgentRunContext context, [EnumeratorCancellation] CancellationToken cancellationToken = default)
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
    #endregion

    #region 处理（ProduceAsync）
    /// <summary>
    /// 处理（ProduceAsync）
    /// </summary>
    /// <param name="context">Agent 运行上下文，包含固定版本快照、输入和工具资源。</param>
    /// <param name="writer">用于输出 JSON 内容的写入器。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>表示该异步操作完成的任务。</returns>
    private async Task ProduceAsync(AgentRunContext context, ChannelWriter<AgentRunEvent> writer, CancellationToken cancellationToken)
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

            if (context.Snapshot.KnowledgeBases.Count > 0)
            {
                int knowledgeBaseCount = context.Snapshot.KnowledgeBases.Count;
                int knowledgeHitCount = context.Knowledge.Count;
                await writer.WriteAsync(new AgentRunEvent(
                    context.RunId,
                    ++sequence,
                    AgentRunEventKind.KnowledgeRetrieved,
                    DateTimeOffset.UtcNow,
                    $"Knowledge retrieval completed: searched {knowledgeBaseCount} knowledge base(s) and matched {knowledgeHitCount} chunk(s).")
                {
                    KnowledgeBaseCount = knowledgeBaseCount,
                    KnowledgeHitCount = knowledgeHitCount
                }, cancellationToken);
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
    #endregion

    #region 查询列表（ListAuditAsync）
    /// <summary>
    /// 查询列表（ListAuditAsync）
    /// </summary>
    /// <param name="agentId">Agent 定义标识。</param>
    /// <param name="take">最多返回的记录数。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>指定 Agent 最近的运行审计记录，最多 100 条。</returns>
    public Task<IReadOnlyList<AgentRunAuditRecord>> ListAuditAsync(Guid agentId, int take, CancellationToken cancellationToken = default) =>
        auditRepository.ListAsync(agentId, Math.Clamp(take, 1, 100), cancellationToken);
    #endregion

    #region 处理（TerminatePreparedRunAsync）
    /// <summary>
    /// 处理（TerminatePreparedRunAsync）
    /// </summary>
    /// <param name="context">Agent 运行上下文，包含固定版本快照、输入和工具资源。</param>
    /// <param name="status">当前操作使用的状态值。</param>
    /// <param name="errorCode">失败对应的错误码。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>表示该异步操作完成的任务。</returns>
    public Task TerminatePreparedRunAsync(AgentRunContext context, AgentRunStatus status, string errorCode, CancellationToken cancellationToken = default)
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
    #endregion

    #region 创建（CreateAudit）
    /// <summary>
    /// 创建（CreateAudit）
    /// </summary>
    /// <param name="context">Agent 运行上下文，包含固定版本快照、输入和工具资源。</param>
    /// <param name="status">当前操作使用的状态值。</param>
    /// <param name="finishedAt">完成时间（UTC）。</param>
    /// <param name="outputCharacters">输出字符数。</param>
    /// <param name="errorCode">失败对应的错误码。</param>
    /// <param name="calls">调用记录集合。</param>
    /// <returns>包含输入摘要、输出长度和按开始时间排序的工具调用明细的运行审计记录。</returns>
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
    #endregion

    #region 处理（TrackToolCall）
    /// <summary>
    /// 处理（TrackToolCall）
    /// </summary>
    /// <param name="value">本次操作使用的Agent 运行事件。</param>
    /// <param name="context">Agent 运行上下文，包含固定版本快照、输入和工具资源。</param>
    /// <param name="calls">调用记录集合。</param>
    private static void TrackToolCall(AgentRunEvent value, AgentRunContext context, IDictionary<Guid, AgentToolCallAuditRecord> calls)
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
    #endregion
}
