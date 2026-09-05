using System.Data;
using System.Security.Cryptography;
using System.Text;
using EU.Core.IServices.Approvals;
using EU.Core.IServices.Mcp;

#nullable enable

namespace EU.Core.Services;

// 文件职责：AgToolApprovalRequestServices 职责实现

/// <summary>
/// 提供工具调用审批请求的持久化服务。
/// </summary>
public sealed class AgToolApprovalRequestServices :
    BaseServices<AgToolApprovalRequest>,
    IAgToolApprovalRequestServices,
    IToolApprovalRepository
{
    #region 构造（AgToolApprovalRequestServices）
    /// <summary>
    /// 构造（AgToolApprovalRequestServices）
    /// </summary>
    /// <param name="dal">当前服务使用的数据访问仓储。</param>
    public AgToolApprovalRequestServices(IBaseRepository<AgToolApprovalRequest> dal)
        : base(dal ?? throw new ArgumentNullException(nameof(dal)))
    {
    }
    #endregion

    #region 校验并创建工具审批请求及恢复载荷（TryCreateAsync）
    /// <summary>
    /// 校验并创建工具审批请求及恢复载荷（TryCreateAsync）。
    /// </summary>
    /// <param name="request">待创建的审批请求，初始状态必须为 Pending，逻辑修订号必须为零。</param>
    /// <param name="protectedResumePayload">与审批请求一起保存的非空加密恢复载荷。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>审批请求及恢复载荷保存成功时返回 true；审批标识已存在时返回 false。</returns>
    /// <exception cref="ToolApprovalException">审批请求字段、初始状态或恢复载荷不符合状态机约束。</exception>
    public async Task<bool> TryCreateAsync(ToolApprovalRequestRecord request, string protectedResumePayload, CancellationToken cancellationToken = default)
    {
        ToolApprovalStateMachine.ValidateNew(request, protectedResumePayload);
        cancellationToken.ThrowIfCancellationRequested();
        await Db.Ado.BeginTranAsync(IsolationLevel.Serializable);
        try
        {
            if (await Db.Queryable<AgToolApprovalRequest>()
                .Where(value => value.ID == request.Id)
                .AnyAsync())
            {
                await Db.Ado.RollbackTranAsync();
                return false;
            }

            await Db.Insertable(MapRequestEntity(request)).ExecuteCommandAsync();
            await Db.Insertable(new AgToolApprovalPayload
            {
                ID = Guid.NewGuid(),
                ApprovalId = request.Id,
                ProtectedPayload = protectedResumePayload,
                ProtectedPayloadSha256 = Sha256(protectedResumePayload),
                IsDeleted = false,
                IsActive = true
            }).ExecuteCommandAsync();
            await Db.Ado.CommitTranAsync();
            return true;
        }
        catch
        {
            await Db.Ado.RollbackTranAsync();
            throw;
        }
    }
    #endregion

    #region 按租户和审批标识查询请求（GetAsync）
    /// <summary>
    /// 按租户和审批标识查询请求（GetAsync）。
    /// </summary>
    /// <param name="id">工具审批请求标识。</param>
    /// <param name="tenantId">所属租户标识。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>返回指定租户下未删除的审批请求；不存在时返回 null。</returns>
    public async Task<ToolApprovalRequestRecord?> GetAsync(Guid id, string tenantId, CancellationToken cancellationToken = default)
    {
        RequiredTenant(tenantId);
        cancellationToken.ThrowIfCancellationRequested();
        AgToolApprovalRequest? value = await Db.Queryable<AgToolApprovalRequest>()
            .Where(item => item.ID == id && item.TenantId == tenantId && !item.IsDeleted)
            .FirstAsync();
        return value is null ? null : MapRequest(value);
    }
    #endregion

    #region 按租户和可选状态查询审批请求（ListAsync）
    /// <summary>
    /// 按租户和可选状态查询审批请求（ListAsync）。
    /// </summary>
    /// <param name="query">租户、可选审批状态和返回数量限制，Take 须为 1 至 MaximumTake。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>返回符合条件、按请求时间倒序排列且不超过 Take 条的审批记录；没有匹配项时返回空集合。</returns>
    public async Task<IReadOnlyList<ToolApprovalRequestRecord>> ListAsync(ToolApprovalQuery query, CancellationToken cancellationToken = default)
    {
        ToolApprovalStateMachine.ValidateQuery(query);
        cancellationToken.ThrowIfCancellationRequested();
        List<AgToolApprovalRequest> values = await Db.Queryable<AgToolApprovalRequest>()
            .Where(value => value.TenantId == query.TenantId && !value.IsDeleted)
            .WhereIF(query.Status is not null, value => value.Status == (int)query.Status!.Value)
            .OrderBy(value => value.RequestedAtUtc, OrderByType.Desc)
            .OrderBy(value => value.ID)
            .Take(query.Take)
            .ToListAsync();
        return values.Select(MapRequest).ToArray();
    }
    #endregion

    #region 查询指定审批的人工决策历史（ListDecisionsAsync）
    /// <summary>
    /// 查询指定审批的人工决策历史（ListDecisionsAsync）。
    /// </summary>
    /// <param name="approvalId">工具审批请求标识。</param>
    /// <param name="tenantId">所属租户标识。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>返回指定租户和审批下的未删除决策记录，按决策产生的逻辑修订号升序排列；无记录时返回空集合。</returns>
    public async Task<IReadOnlyList<ToolApprovalDecisionRecord>> ListDecisionsAsync(
        Guid approvalId,
        string tenantId,
        CancellationToken cancellationToken = default)
    {
        RequiredTenant(tenantId);
        cancellationToken.ThrowIfCancellationRequested();
        List<AgToolApprovalDecision> values = await Db.Queryable<AgToolApprovalDecision>()
            .Where(value => value.ApprovalId == approvalId &&
                            value.TenantId == tenantId && !value.IsDeleted)
            .OrderBy(value => value.ResultingLogicalRevision)
            .OrderBy(value => value.ID)
            .ToListAsync();
        return values.Select(MapDecision).ToArray();
    }
    #endregion

    #region 按修订号更新审批状态并记录人工决策（TryReplaceAsync）
    /// <summary>
    /// 按修订号更新审批状态并记录人工决策（TryReplaceAsync）。
    /// </summary>
    /// <param name="replacement">保留原审批绑定信息且符合状态机规则的新记录，逻辑修订号须递增一。</param>
    /// <param name="expectedLogicalRevision">现有审批记录应具有的逻辑修订号。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>审批状态及必要的人工决策记录提交成功时返回 true；记录不存在、预期修订号不匹配或并发条件更新未生效时返回 false。</returns>
    /// <exception cref="ToolApprovalException">新记录破坏审批绑定信息，或修订号、状态转换及状态字段不符合状态机规则。</exception>
    public async Task<bool> TryReplaceAsync(ToolApprovalRequestRecord replacement, long expectedLogicalRevision, CancellationToken cancellationToken = default)
    {
        ToolApprovalRequestRecord? existing = await GetAsync(
            replacement.Id, replacement.TenantId, cancellationToken);
        if (existing is null || existing.LogicalRevision != expectedLogicalRevision)
        {
            return false;
        }

        ToolApprovalStateMachine.ValidateReplacement(existing, replacement);
        await Db.Ado.BeginTranAsync();
        try
        {
            int updated = await UpdateStateAsync(
                replacement,
                expectedLogicalRevision,
                existing.Status);
            if (updated != 1)
            {
                await Db.Ado.RollbackTranAsync();
                return false;
            }

            if (IsHumanDecision(existing.Status, replacement.Status))
            {
                await Db.Insertable(new AgToolApprovalDecision
                {
                    ID = Guid.NewGuid(),
                    ApprovalId = replacement.Id,
                    TenantId = replacement.TenantId,
                    FromStatus = (int)existing.Status,
                    ToStatus = (int)replacement.Status,
                    DecisionUserId = replacement.DecisionUserId,
                    DecisionReason = replacement.DecisionReason,
                    DecidedAtUtc = replacement.DecidedAtUtc!.Value.UtcDateTime,
                    ResultingLogicalRevision = replacement.LogicalRevision,
                    IsDeleted = false,
                    IsActive = true
                }).ExecuteCommandAsync();
            }

            await Db.Ado.CommitTranAsync();
            return true;
        }
        catch
        {
            await Db.Ado.RollbackTranAsync();
            throw;
        }
    }
    #endregion

    #region 按修订号认领尚未过期的已批准请求（TryClaimExecutionAsync）
    /// <summary>
    /// 按修订号认领尚未过期的已批准请求（TryClaimExecutionAsync）。
    /// </summary>
    /// <param name="id">工具审批请求标识。</param>
    /// <param name="tenantId">所属租户标识。</param>
    /// <param name="expectedLogicalRevision">认领前审批记录应具有的逻辑修订号。</param>
    /// <param name="claimedAtUtc">执行认领时间，须不早于请求时间且早于过期时间。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>成功转为 Consuming 时返回更新后的审批记录及加密恢复载荷；条件不匹配、记录或载荷不存在时返回 null。</returns>
    public async Task<ToolApprovalExecutionClaim?> TryClaimExecutionAsync(
        Guid id,
        string tenantId,
        long expectedLogicalRevision,
        DateTimeOffset claimedAtUtc,
        CancellationToken cancellationToken = default)
    {
        RequiredTenant(tenantId);
        cancellationToken.ThrowIfCancellationRequested();
        DateTime claimed = claimedAtUtc.UtcDateTime;
        await Db.Ado.BeginTranAsync();
        try
        {
            int updated = await Db.Updateable<AgToolApprovalRequest>()
                .SetColumns(value => new AgToolApprovalRequest
                {
                    Status = (int)ToolApprovalStatus.Consuming,
                    LogicalRevision = value.LogicalRevision + 1,
                    ClaimedAtUtc = claimed
                })
                .Where(value => value.ID == id && value.TenantId == tenantId &&
                                value.LogicalRevision == expectedLogicalRevision &&
                                value.Status == (int)ToolApprovalStatus.Approved &&
                                value.RequestedAtUtc <= claimed && value.ExpiresAtUtc > claimed &&
                                !value.IsDeleted)
                .ExecuteCommandAsync();
            if (updated != 1)
            {
                await Db.Ado.RollbackTranAsync();
                return null;
            }

            AgToolApprovalRequest? request = await Db.Queryable<AgToolApprovalRequest>()
                .Where(value => value.ID == id && value.TenantId == tenantId && !value.IsDeleted)
                .FirstAsync();
            AgToolApprovalPayload? payload = await Db.Queryable<AgToolApprovalPayload>()
                .Where(value => value.ApprovalId == id && !value.IsDeleted)
                .FirstAsync();
            if (request is null || payload is null)
            {
                await Db.Ado.RollbackTranAsync();
                return null;
            }

            string protectedPayload = Required(payload.ProtectedPayload, "ProtectedPayload");
            string payloadSha256 = Required(payload.ProtectedPayloadSha256, "ProtectedPayloadSha256");
            if (!string.Equals(Sha256(protectedPayload), payloadSha256, StringComparison.Ordinal))
            {
                throw new InvalidDataException("The protected tool approval payload failed its integrity check.");
            }

            try
            {
                ToolApprovalStateMachine.ValidateProtectedPayload(protectedPayload);
            }
            catch (ToolApprovalException exception)
            {
                throw new InvalidDataException(
                    "The protected tool approval payload envelope is invalid.", exception);
            }

            ToolApprovalRequestRecord result = MapRequest(request);
            await Db.Ado.CommitTranAsync();
            return new ToolApprovalExecutionClaim(result, protectedPayload, payloadSha256);
        }
        catch
        {
            await Db.Ado.RollbackTranAsync();
            throw;
        }
    }
    #endregion

    #region 原子提交审批终态及工具执行结果（TryCompleteExecutionAsync）
    /// <summary>
    /// 原子提交审批终态及工具执行结果（TryCompleteExecutionAsync）。
    /// </summary>
    /// <param name="replacement">符合状态机规则的审批完成记录，绑定字段须保持不变且修订号递增一。</param>
    /// <param name="expectedLogicalRevision">完成执行前审批记录应具有的逻辑修订号。</param>
    /// <param name="result">与审批标识、租户、完成时间、成功状态及错误码一致的受保护执行结果。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>审批状态与执行结果提交成功时返回 true；结果封装无效、记录不存在、修订号不匹配、结果与审批终态不一致或并发更新未生效时返回 false。</returns>
    /// <exception cref="ToolApprovalException">审批替换记录的绑定信息、修订号或状态转换不符合状态机规则。</exception>
    public async Task<bool> TryCompleteExecutionAsync(
        ToolApprovalRequestRecord replacement,
        long expectedLogicalRevision,
        ToolApprovalExecutionResultRecord result,
        CancellationToken cancellationToken = default)
    {
        try
        {
            ToolApprovalStateMachine.ValidateExecutionResultEnvelope(result);
        }
        catch (ToolApprovalException)
        {
            return false;
        }

        ToolApprovalRequestRecord? existing = await GetAsync(
            replacement.Id, replacement.TenantId, cancellationToken);
        if (existing is null || existing.LogicalRevision != expectedLogicalRevision ||
            !ValidResult(replacement, result))
        {
            return false;
        }

        ToolApprovalStateMachine.ValidateReplacement(existing, replacement);
        await Db.Ado.BeginTranAsync();
        try
        {
            if (await UpdateStateAsync(replacement, expectedLogicalRevision, existing.Status) != 1)
            {
                await Db.Ado.RollbackTranAsync();
                return false;
            }

            await Db.Insertable(new AgToolApprovalExecutionResult
            {
                ID = Guid.NewGuid(),
                ApprovalId = result.ApprovalId,
                TenantId = result.TenantId,
                Succeeded = result.Succeeded,
                Blocked = result.Blocked,
                ProtectedContent = result.ProtectedContent,
                ProtectedContentSha256 = result.ProtectedContentSha256,
                ContentSha256 = result.ContentSha256,
                ErrorCode = result.ErrorCode,
                FinishedAtUtc = result.FinishedAtUtc.UtcDateTime,
                IsDeleted = false,
                IsActive = true
            }).ExecuteCommandAsync();
            await Db.Ado.CommitTranAsync();
            return true;
        }
        catch
        {
            await Db.Ado.RollbackTranAsync();
            throw;
        }
    }
    #endregion

    #region 按租户和审批标识读取执行结果（GetExecutionResultAsync）
    /// <summary>
    /// 按租户和审批标识读取执行结果（GetExecutionResultAsync）。
    /// </summary>
    /// <param name="id">工具审批请求标识，而非执行结果记录标识。</param>
    /// <param name="tenantId">所属租户标识。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>返回通过封装校验的受保护执行结果；不存在时返回 null；已保存结果损坏时通过异常报告。</returns>
    public async Task<ToolApprovalExecutionResultRecord?> GetExecutionResultAsync(Guid id, string tenantId, CancellationToken cancellationToken = default)
    {
        RequiredTenant(tenantId);
        cancellationToken.ThrowIfCancellationRequested();
        AgToolApprovalExecutionResult? value = await Db.Queryable<AgToolApprovalExecutionResult>()
            .Where(item => item.ApprovalId == id && item.TenantId == tenantId && !item.IsDeleted)
            .FirstAsync();
        if (value is null)
        {
            return null;
        }

        ToolApprovalExecutionResultRecord result = MapExecutionResult(value);
        try
        {
            ToolApprovalStateMachine.ValidateExecutionResultEnvelope(result);
        }
        catch (ToolApprovalException exception)
        {
            throw new InvalidDataException(
                "The protected tool approval result envelope is invalid.", exception);
        }

        return result;
    }
    #endregion

    #region 将无结果的中断审批执行标记为失败（RecoverInterruptedExecutionsAsync）
    /// <summary>
    /// 将无结果的中断审批执行标记为失败（RecoverInterruptedExecutionsAsync）。
    /// </summary>
    /// <param name="recoveredAtUtc">恢复时间（UTC）。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>返回从 Consuming 更新为 Failed 且错误码设为 ExecutionOutcomeUnknown 的记录数量；无匹配记录时为零，不重新调用工具。</returns>
    public async Task<int> RecoverInterruptedExecutionsAsync(DateTimeOffset recoveredAtUtc, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        DateTime recovered = recoveredAtUtc.UtcDateTime;
        return await Db.Updateable<AgToolApprovalRequest>()
            .SetColumns(value => new AgToolApprovalRequest
            {
                Status = (int)ToolApprovalStatus.Failed,
                LogicalRevision = value.LogicalRevision + 1,
                FinishedAtUtc = value.ClaimedAtUtc > recovered ? value.ClaimedAtUtc : recovered,
                ErrorCode = ToolApprovalErrorCodes.ExecutionOutcomeUnknown
            })
            .Where(value => value.Status == (int)ToolApprovalStatus.Consuming &&
                            !SqlFunc.Subqueryable<AgToolApprovalExecutionResult>()
                                .Where(result => result.ApprovalId == value.ID && !result.IsDeleted)
                                .Any() &&
                            !value.IsDeleted)
            .ExecuteCommandAsync();
    }
    #endregion

    #region 更新（UpdateStateAsync）
    /// <summary>
    /// 更新（UpdateStateAsync）
    /// </summary>
    /// <param name="replacement">用于替换的新数据。</param>
    /// <param name="expectedLogicalRevision">并发更新要求匹配的逻辑修订号。</param>
    /// <param name="expectedStatus">更新前要求匹配的状态。</param>
    /// <returns>按租户、预期状态及逻辑版本更新审批状态所影响的行数。</returns>
    private async Task<int> UpdateStateAsync(ToolApprovalRequestRecord replacement, long expectedLogicalRevision, ToolApprovalStatus expectedStatus)
    {
        DateTime? decidedAtUtc = replacement.DecidedAtUtc?.UtcDateTime;
        DateTime? claimedAtUtc = replacement.ClaimedAtUtc?.UtcDateTime;
        DateTime? finishedAtUtc = replacement.FinishedAtUtc?.UtcDateTime;
        return await Db.Updateable<AgToolApprovalRequest>()
            .SetColumns(_ => new AgToolApprovalRequest
            {
                Status = (int)replacement.Status,
                LogicalRevision = replacement.LogicalRevision,
                DecisionUserId = replacement.DecisionUserId,
                DecisionReason = replacement.DecisionReason,
                DecidedAtUtc = decidedAtUtc,
                ClaimedAtUtc = claimedAtUtc,
                FinishedAtUtc = finishedAtUtc,
                ErrorCode = replacement.ErrorCode
            })
            .Where(value => value.ID == replacement.Id &&
                            value.TenantId == replacement.TenantId &&
                            value.LogicalRevision == expectedLogicalRevision &&
                            value.Status == (int)expectedStatus && !value.IsDeleted)
            .ExecuteCommandAsync();
    }
    #endregion

    #region 映射（MapRequestEntity）
    /// <summary>
    /// 映射（MapRequestEntity）
    /// </summary>
    /// <param name="value">本次操作使用的工具审批请求记录。</param>
    /// <returns>由审批请求记录构造的主表持久化实体。</returns>
    private static AgToolApprovalRequest MapRequestEntity(ToolApprovalRequestRecord value) => new()
    {
        ID = value.Id,
        TenantId = value.TenantId,
        RequesterUserId = value.RequesterUserId,
        ConversationId = value.ConversationId,
        EntryRunId = value.EntryRunId,
        AgentRunId = value.AgentRunId,
        AgentVersionId = value.AgentVersionId,
        McpServerId = value.McpServerId,
        ToolVersionId = value.ToolVersionId,
        ToolName = value.ToolName,
        Risk = (int)value.Risk,
        ToolSchemaSha256 = value.ToolSchemaSha256,
        ArgumentsSha256 = value.ArgumentsSha256,
        SafeArgumentsSummaryJson = value.SafeArgumentsSummaryJson,
        Status = (int)value.Status,
        LogicalRevision = value.LogicalRevision,
        RequestedAtUtc = value.RequestedAtUtc.UtcDateTime,
        ExpiresAtUtc = value.ExpiresAtUtc.UtcDateTime,
        DecisionUserId = value.DecisionUserId,
        DecisionReason = value.DecisionReason,
        DecidedAtUtc = value.DecidedAtUtc?.UtcDateTime,
        ClaimedAtUtc = value.ClaimedAtUtc?.UtcDateTime,
        FinishedAtUtc = value.FinishedAtUtc?.UtcDateTime,
        ErrorCode = value.ErrorCode,
        IsDeleted = false,
        IsActive = true
    };
    #endregion

    #region 映射（MapRequest）
    /// <summary>
    /// 映射（MapRequest）
    /// </summary>
    /// <param name="value">本次操作使用的工具审批请求实体。</param>
    /// <returns>包含请求身份、工具信息及各阶段时间的审批请求记录。</returns>
    private static ToolApprovalRequestRecord MapRequest(AgToolApprovalRequest value) => new(
        value.ID,
        Required(value.TenantId, "TenantId"),
        Required(value.RequesterUserId, "RequesterUserId"),
        Required(value.ConversationId, "ConversationId"),
        Required(value.EntryRunId, "EntryRunId"),
        Required(value.AgentRunId, "AgentRunId"),
        Required(value.AgentVersionId, "AgentVersionId"),
        Required(value.McpServerId, "McpServerId"),
        Required(value.ToolVersionId, "ToolVersionId"),
        Required(value.ToolName, "ToolName"),
        (McpToolRisk)Required(value.Risk, "Risk"),
        Required(value.ToolSchemaSha256, "ToolSchemaSha256"),
        Required(value.ArgumentsSha256, "ArgumentsSha256"),
        Required(value.SafeArgumentsSummaryJson, "SafeArgumentsSummaryJson"),
        (ToolApprovalStatus)Required(value.Status, "Status"),
        Required(value.LogicalRevision, "LogicalRevision"),
        ToOffset(Required(value.RequestedAtUtc, "RequestedAtUtc")),
        ToOffset(Required(value.ExpiresAtUtc, "ExpiresAtUtc")),
        Required(value.DecisionUserId, "DecisionUserId"),
        Required(value.DecisionReason, "DecisionReason"),
        value.DecidedAtUtc.HasValue ? ToOffset(value.DecidedAtUtc.Value) : null,
        value.ClaimedAtUtc.HasValue ? ToOffset(value.ClaimedAtUtc.Value) : null,
        value.FinishedAtUtc.HasValue ? ToOffset(value.FinishedAtUtc.Value) : null,
        Required(value.ErrorCode, "ErrorCode"));
    #endregion

    #region 映射（MapDecision）
    /// <summary>
    /// 映射（MapDecision）
    /// </summary>
    /// <param name="value">本次操作使用的工具审批决策实体。</param>
    /// <returns>包含审批状态迁移、决策人、原因及结果版本的审批决策记录。</returns>
    private static ToolApprovalDecisionRecord MapDecision(AgToolApprovalDecision value) => new(
        value.ID,
        Required(value.ApprovalId, "Decision.ApprovalId"),
        Required(value.TenantId, "Decision.TenantId"),
        (ToolApprovalStatus)Required(value.FromStatus, "Decision.FromStatus"),
        (ToolApprovalStatus)Required(value.ToStatus, "Decision.ToStatus"),
        Required(value.DecisionUserId, "Decision.DecisionUserId"),
        Required(value.DecisionReason, "Decision.DecisionReason"),
        ToOffset(Required(value.DecidedAtUtc, "Decision.DecidedAtUtc")),
        Required(value.ResultingLogicalRevision, "Decision.ResultingLogicalRevision"));
    #endregion

    #region 映射（MapExecutionResult）
    /// <summary>
    /// 映射（MapExecutionResult）
    /// </summary>
    /// <param name="value">本次操作使用的工具审批执行结果实体。</param>
    /// <returns>包含受保护内容、摘要及完成状态的审批执行结果记录。</returns>
    private static ToolApprovalExecutionResultRecord MapExecutionResult(AgToolApprovalExecutionResult value) => new(
        Required(value.ApprovalId, "ExecutionResult.ApprovalId"),
        Required(value.TenantId, "ExecutionResult.TenantId"),
        Required(value.Succeeded, "ExecutionResult.Succeeded"),
        Required(value.Blocked, "ExecutionResult.Blocked"),
        Required(value.ProtectedContent, "ExecutionResult.ProtectedContent"),
        Required(value.ProtectedContentSha256, "ExecutionResult.ProtectedContentSha256"),
        Required(value.ContentSha256, "ExecutionResult.ContentSha256"),
        Required(value.ErrorCode, "ExecutionResult.ErrorCode"),
        ToOffset(Required(value.FinishedAtUtc, "ExecutionResult.FinishedAtUtc")));
    #endregion

    #region 核对执行结果与审批完成记录（ValidResult）
    /// <summary>
    /// 核对执行结果与审批完成记录（ValidResult）。
    /// </summary>
    /// <param name="replacement">待提交的审批完成记录。</param>
    /// <param name="result">待核对的工具执行结果；本方法不代替结果封装校验。</param>
    /// <returns>审批标识、租户、完成时间及错误码一致，且结果成功标记与审批是否为 Consumed 一致时返回 true，否则返回 false。</returns>
    private static bool ValidResult(ToolApprovalRequestRecord replacement, ToolApprovalExecutionResultRecord result) =>
        result.ApprovalId == replacement.Id &&
        string.Equals(result.TenantId, replacement.TenantId, StringComparison.Ordinal) &&
        result.FinishedAtUtc == replacement.FinishedAtUtc &&
        result.Succeeded == (replacement.Status == ToolApprovalStatus.Consumed) &&
        string.Equals(result.ErrorCode, replacement.ErrorCode, StringComparison.Ordinal);
    #endregion

    #region 判断状态转换是否属于人工审批决策（IsHumanDecision）
    /// <summary>
    /// 判断状态转换是否属于人工审批决策（IsHumanDecision）。
    /// </summary>
    /// <param name="from">转换前的审批状态。</param>
    /// <param name="to">转换后的审批状态。</param>
    /// <returns>从 Pending 转为 Approved、Rejected 或 Cancelled 时返回 true，其余转换返回 false。</returns>
    private static bool IsHumanDecision(ToolApprovalStatus from, ToolApprovalStatus to) =>
        from == ToolApprovalStatus.Pending &&
        to is ToolApprovalStatus.Approved or ToolApprovalStatus.Rejected or ToolApprovalStatus.Cancelled;
    #endregion

    #region 处理（RequiredTenant）
    /// <summary>
    /// 处理（RequiredTenant）
    /// </summary>
    /// <param name="value">必须为非空文本的租户标识。</param>
    private static void RequiredTenant(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ToolApprovalException(
                ToolApprovalErrorCodes.Invalid,
                "The tool approval tenant is required.");
        }
    }
    #endregion

    #region 处理（Sha256）
    /// <summary>
    /// 处理（Sha256）
    /// </summary>
    /// <param name="value">用于计算 SHA-256 摘要的原始文本。</param>
    /// <returns>输入文本 UTF-8 字节的 SHA-256 小写十六进制摘要。</returns>
    private static string Sha256(string value) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    #endregion

    #region 转换（ToOffset）
    /// <summary>
    /// 将数据库时间还原为 UTC 时间（ToOffset）。
    /// </summary>
    /// <param name="value">按 UTC 语义存储的数据库时间。</param>
    /// <returns>将输入时间视为 UTC 后构造的零偏移时间。</returns>
    private static DateTimeOffset ToOffset(DateTime value) =>
        new(DateTime.SpecifyKind(value, DateTimeKind.Utc));
    #endregion

    #region 处理（Required）
    /// <summary>
    /// 读取并校验必填字段（Required）。
    /// </summary>
    /// <typeparam name="T">必填字段的值类型。</typeparam>
    /// <param name="value">从持久化记录读取的可空字段值。</param>
    /// <param name="field">字段名称，用于校验和错误提示。</param>
    /// <returns>非 null 的必填字段值；缺失时抛出 InvalidDataException。</returns>
    private static T Required<T>(T? value, string field) where T : struct =>
        value ?? throw new InvalidDataException($"Tool approval field '{field}' is missing.");
    #endregion

    #region 处理（Required）
    /// <summary>
    /// 读取并校验必填字段（Required）。
    /// </summary>
    /// <param name="value">从持久化记录读取的可空字段值。</param>
    /// <param name="field">字段名称，用于校验和错误提示。</param>
    /// <returns>非 null 的必填字段值；缺失时抛出 InvalidDataException。</returns>
    private static string Required(string? value, string field) =>
        value ?? throw new InvalidDataException($"Tool approval field '{field}' is missing.");
    #endregion
}
