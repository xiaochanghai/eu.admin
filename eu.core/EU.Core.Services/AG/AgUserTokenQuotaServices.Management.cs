using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using EU.Core.IServices.Runtime;

#nullable enable

namespace EU.Core.Services;

public sealed partial class AgUserTokenQuotaServices
{
    #region 读取额度设置（GetPolicyAsync）
    /// <summary>租户默认设置不写入数据库，损坏或删除的设置不伪装为默认值。</summary>
    /// <param name="groupId">可信集团。</param>
    /// <param name="companyId">可信公司。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>设置快照。</returns>
    public async Task<AgentUserTokenQuotaPolicy> GetPolicyAsync(Guid groupId, Guid companyId, CancellationToken cancellationToken = default)
    {
        ValidateOwner(groupId, companyId);
        cancellationToken.ThrowIfCancellationRequested();
        var result = await ReadPolicyAsync(groupId, companyId);
        cancellationToken.ThrowIfCancellationRequested();
        return result;
    }
    #endregion

    #region 保存额度设置（SavePolicyAsync）
    /// <summary>校验版本及幂等标识，审计和设置一起提交，不重置任何用量。</summary>
    /// <param name="groupId">可信集团。</param>
    /// <param name="companyId">可信公司。</param>
    /// <param name="operatorId">授权操作者。</param>
    /// <param name="input">修改命令。</param>
    /// <param name="cancellationToken">提交前取消令牌。</param>
    /// <returns>提交后的快照或原幂等回执。</returns>
    public async Task<AgentUserTokenQuotaPolicy> SavePolicyAsync(Guid groupId, Guid companyId, Guid operatorId, AgentUserTokenQuotaPolicyInput input, CancellationToken cancellationToken = default)
    {
        ValidateOwner(groupId, companyId);
        if (input is null) throw Invalid();
        ValidateMutation(operatorId, input.OperationId, input.Reason);
        if (input.ExpectedRevision < 0 || !ValidPolicy(input.UseDefaults, input.DailyTotalTokens, input.MonthlyTotalTokens, input.RequestReservationTokens)) throw Invalid();
        Guid ownerId = OwnerId(groupId, companyId);
        string hash = CommandHash(groupId, companyId, operatorId, "Policy", ownerId, input);
        AgentUserTokenQuotaPolicy? result = null;
        await InTransactionAsync(async () =>
        {
            var replay = await ReadAdjustmentAsync(input.OperationId, groupId, companyId, operatorId, hash);
            if (replay is not null) { result = JsonSerializer.Deserialize<AgentUserTokenQuotaPolicy>(replay.ResultJson) ?? throw Unavailable(); return; }
            var before = await ReadPolicyAsync(groupId, companyId);
            if (before.Revision != input.ExpectedRevision || before.Revision == long.MaxValue) throw Conflict();
            var row = new AgUserTokenQuotaPolicy { ID = Guid.NewGuid(), GroupId = groupId, CompanyId = companyId, CreatedBy = operatorId,
                UseDefaults = input.UseDefaults, DailyTotalTokens = input.DailyTotalTokens, MonthlyTotalTokens = input.MonthlyTotalTokens,
                RequestReservationTokens = input.RequestReservationTokens, Revision = before.Revision + 1 };
            if (before.Revision == 0) await Db.Insertable(row).ExecuteCommandAsync();
            else
            {
                int changed = await Db.Updateable<AgUserTokenQuotaPolicy>()
                    .SetColumns(x => new AgUserTokenQuotaPolicy { UseDefaults = row.UseDefaults, DailyTotalTokens = row.DailyTotalTokens,
                        MonthlyTotalTokens = row.MonthlyTotalTokens, RequestReservationTokens = row.RequestReservationTokens, Revision = row.Revision, UpdateBy = operatorId })
                    .Where(x => x.GroupId == groupId && x.CompanyId == companyId && x.Revision == before.Revision && !x.IsDeleted).ExecuteCommandAsync();
                if (changed != 1) throw Conflict();
            }
            result = new(row.UseDefaults, row.DailyTotalTokens, row.MonthlyTotalTokens, row.RequestReservationTokens, row.Revision);
            await WriteAdjustmentAsync(input.OperationId, groupId, companyId, operatorId, "Policy", ownerId, hash, before, result, input.Reason, "");
        }, cancellationToken);
        return result ?? throw Unavailable();
    }
    #endregion

    #region 查询待对账记录（GetPendingAsync）
    /// <summary>包含未知、未开始和未完成记录；列表展示不授权自动退款。</summary>
    /// <param name="groupId">可信集团。</param>
    /// <param name="companyId">可信公司。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>最多 100 条，UTC 时间。</returns>
    public async Task<IReadOnlyList<AgentUserTokenQuotaPending>> GetPendingAsync(Guid groupId, Guid companyId, CancellationToken cancellationToken = default)
    {
        ValidateOwner(groupId, companyId);
        cancellationToken.ThrowIfCancellationRequested();
        var rows = await Db.Queryable<AgUserTokenQuotaReservation>().ClearFilter()
            .Where(x => x.GroupId == groupId && x.CompanyId == companyId && !x.IsDeleted && (x.State == 0 || x.State == 1 || x.State == 3))
            .OrderByDescending(x => x.CreatedTime).OrderBy(x => x.ID).Take(100).ToListAsync();
        cancellationToken.ThrowIfCancellationRequested();
        return rows.Select(x => new AgentUserTokenQuotaPending(x.ID, x.RunId, x.ConsumerUserId, x.State, x.ReservedTokens,
            x.SettledAtUtc is { } time ? new DateTimeOffset(DateTime.SpecifyKind(time, DateTimeKind.Utc)) : null)).ToArray();
    }
    #endregion

    #region 人工核实未知用量（ReconcileAsync）
    /// <summary>在原始日/月账本上结算，只有最后一笔未知用量已核实才解除冻结。</summary>
    /// <param name="groupId">可信集团。</param>
    /// <param name="companyId">可信公司。</param>
    /// <param name="operatorId">授权操作者。</param>
    /// <param name="reservationId">原始预占。</param>
    /// <param name="input">真实用量、原因和核查依据。</param>
    /// <param name="cancellationToken">提交前取消令牌。</param>
    /// <returns>追加式审计回执。</returns>
    public async Task<AgentUserTokenQuotaReconciliation> ReconcileAsync(Guid groupId, Guid companyId, Guid operatorId, Guid reservationId, AgentUserTokenQuotaReconcileInput input, CancellationToken cancellationToken = default)
    {
        ValidateOwner(groupId, companyId);
        if (input is null) throw Invalid();
        ValidateMutation(operatorId, input.OperationId, input.Reason);
        if (reservationId == Guid.Empty || input.ActualTokens < 0 || !input.Confirmed || !ValidText(input.EvidenceReference, 256)) throw Invalid();
        string hash = CommandHash(groupId, companyId, operatorId, "Reconcile", reservationId, input);
        AgentUserTokenQuotaReconciliation? result = null;
        await InTransactionAsync(async () =>
        {
            var replay = await ReadAdjustmentAsync(input.OperationId, groupId, companyId, operatorId, hash);
            if (replay is not null) { result = JsonSerializer.Deserialize<AgentUserTokenQuotaReconciliation>(replay.ResultJson) ?? throw Unavailable(); return; }
            var entry = await Db.Queryable<AgUserTokenQuotaReservation>().ClearFilter()
                .Where(x => x.ID == reservationId && x.GroupId == groupId && x.CompanyId == companyId && !x.IsDeleted).SingleAsync();
            if (entry is null) throw new AgentRuntimeException(AgentUserTokenQuotaManagementErrors.NotFound, "Quota reservation not found.");
            if (entry.State is not (0 or 1 or 3)) throw Conflict();
            if (entry.ReservedTokens < 1 || (entry.DailyPeriodId is null && entry.MonthlyPeriodId is null) || entry.ActualTokens is not null) throw Unavailable();
            if (entry.State != 3)
            {
                // 崩溃遗留预占必须有持久化终态证据。无终态/审批暂停/活跃运行绝不自动释放。
                var audit = await Db.Queryable<AgAgentRunAudit>().Where(x => x.ID == entry.RunId && !x.IsDeleted).SingleAsync();
                if (audit?.FinishedAtUtc is not { } finished || finished > DateTime.UtcNow.AddMinutes(-5) ||
                    audit.Status is not ("Completed" or "Failed" or "Cancelled")) throw Conflict();
                if (entry.State == 0 && input.ActualTokens != 0) throw Invalid();
            }
            var windows = new List<AgentUserTokenQuotaWindow>();
            foreach (var id in new[] { entry.DailyPeriodId, entry.MonthlyPeriodId }.Where(x => x.HasValue).Select(x => x!.Value))
            {
                var period = await Db.Queryable<AgUserTokenQuotaPeriod>().ClearFilter()
                    .Where(x => x.ID == id && x.GroupId == groupId && x.CompanyId == companyId && !x.IsDeleted).SingleAsync() ?? throw Unavailable();
                if (period.UsedTokens < 0 || period.ReservedTokens < entry.ReservedTokens || period.Revision < 0 || period.Revision == long.MaxValue ||
                    period.PeriodKind != (id == entry.DailyPeriodId ? 0 : 1) || period.StartUtc >= period.EndUtc) throw Unavailable();
                windows.Add(new(period.ID, period.PeriodKind, period.PeriodKey, period.StartUtc, period.EndUtc, long.MaxValue));
            }
            var request = new AgentUserTokenQuotaRequest(entry.ID, entry.RunId, groupId, companyId, entry.ConsumerUserId, entry.ReservedTokens, windows);
            foreach (var window in windows.OrderBy(x => x.Kind))
            {
                var period = await LoadPeriodAsync(request, window) ?? throw Unavailable();
                if (input.ActualTokens > long.MaxValue - period.UsedTokens) throw Unavailable();
                bool otherUnknown = await Db.Queryable<AgUserTokenQuotaReservation>().ClearFilter()
                    .Where(x => x.GroupId == groupId && x.CompanyId == companyId && x.ID != reservationId && x.State == 3 &&
                        (x.DailyPeriodId == window.Id || x.MonthlyPeriodId == window.Id)).AnyAsync();
                // 饱和值可能来自此前结算溢出，人工对账不能静默解除此冻结。
                bool frozen = otherUnknown || (period.HasUnknownUsage && period.UsedTokens == long.MaxValue);
                await UpdatePeriodAsync(request, period, period.UsedTokens + input.ActualTokens, period.ReservedTokens - entry.ReservedTokens, frozen);
            }
            DateTime applied = DateTime.UtcNow;
            int changed = await Db.Updateable<AgUserTokenQuotaReservation>()
                .SetColumns(x => new AgUserTokenQuotaReservation { State = 5, ActualTokens = input.ActualTokens, SettledAtUtc = applied })
                .Where(x => x.ID == reservationId && x.GroupId == groupId && x.CompanyId == companyId && x.State == entry.State && !x.IsDeleted).ExecuteCommandAsync();
            if (changed != 1) throw Conflict();
            result = new(input.OperationId, reservationId, input.ActualTokens, new DateTimeOffset(applied, TimeSpan.Zero));
            await WriteAdjustmentAsync(input.OperationId, groupId, companyId, operatorId, "Reconcile", reservationId, hash,
                new { entry.State, entry.ActualTokens, entry.ReservedTokens, entry.DailyPeriodId, entry.MonthlyPeriodId }, result, input.Reason, input.EvidenceReference);
        }, cancellationToken);
        return result ?? throw Unavailable();
    }
    #endregion

    #region 读取有效设置（ReadPolicyAsync）
    /// <summary>含删除行查找，避免错误配置导致额度被意外关闭。</summary>
    /// <param name="groupId">可信集团。</param>
    /// <param name="companyId">可信公司。</param>
    /// <returns>设置快照。</returns>
    private async Task<AgentUserTokenQuotaPolicy> ReadPolicyAsync(Guid groupId, Guid companyId)
    {
        var row = await Db.Queryable<AgUserTokenQuotaPolicy>().ClearFilter().Where(x => x.GroupId == groupId && x.CompanyId == companyId).SingleAsync();
        if (row is null) return new(true, null, null, null, 0);
        if (row.IsDeleted || row.Revision < 1 || !ValidPolicy(row.UseDefaults, row.DailyTotalTokens, row.MonthlyTotalTokens, row.RequestReservationTokens)) throw Unavailable();
        return new(row.UseDefaults, row.DailyTotalTokens, row.MonthlyTotalTokens, row.RequestReservationTokens, row.Revision);
    }
    #endregion

    #region 检查幂等审计（ReadAdjustmentAsync）
    /// <summary>全局操作主键不允许跨集团/公司、跨操作者或换内容重用。</summary>
    /// <param name="id">操作标识。</param>
    /// <param name="groupId">可信集团。</param>
    /// <param name="companyId">可信公司。</param>
    /// <param name="operatorId">操作者。</param>
    /// <param name="hash">完整命令哈希。</param>
    /// <returns>原回执或空值。</returns>
    private async Task<AgUserTokenQuotaAdjustment?> ReadAdjustmentAsync(Guid id, Guid groupId, Guid companyId, Guid operatorId, string hash)
    {
        var row = await Db.Queryable<AgUserTokenQuotaAdjustment>().ClearFilter().Where(x => x.ID == id).SingleAsync();
        if (row is not null && (row.IsDeleted || row.GroupId != groupId || row.CompanyId != companyId || row.OperatorId != operatorId || row.CommandHash != hash)) throw Conflict();
        return row;
    }
    #endregion

    #region 追加调整审计（WriteAdjustmentAsync）
    /// <summary>只能插入，不提供覆盖、删除或单独提交审计的入口。</summary>
    /// <param name="id">操作标识。</param>
    /// <param name="groupId">可信集团。</param>
    /// <param name="companyId">可信公司。</param>
    /// <param name="operatorId">操作者。</param>
    /// <param name="kind">固定操作类型。</param>
    /// <param name="targetId">目标标识。</param>
    /// <param name="hash">命令哈希。</param>
    /// <param name="before">修改前快照。</param>
    /// <param name="result">修改后回执。</param>
    /// <param name="reason">原因。</param>
    /// <param name="evidence">核查依据。</param>
    /// <returns>当前事务内插入任务。</returns>
    private Task<int> WriteAdjustmentAsync(Guid id, Guid groupId, Guid companyId, Guid operatorId, string kind, Guid targetId, string hash, object before, object result, string reason, string evidence) =>
        Db.Insertable(new AgUserTokenQuotaAdjustment { ID = id, GroupId = groupId, CompanyId = companyId, OperatorId = operatorId, CreatedBy = operatorId,
            Kind = kind, TargetId = targetId, CommandHash = hash, BeforeJson = JsonSerializer.Serialize(before), ResultJson = JsonSerializer.Serialize(result),
            Reason = reason.Trim(), EvidenceReference = evidence.Trim(), AppliedAtUtc = DateTime.UtcNow }).ExecuteCommandAsync();
    #endregion

    #region 校验额度（ValidPolicy）
    /// <summary>默认恢复不接受覆盖值；自定义必须启用至少一个有限周期。</summary>
    /// <param name="defaults">是否恢复默认。</param>
    /// <param name="daily">日额度。</param>
    /// <param name="monthly">月额度。</param>
    /// <param name="reservation">请求预占量。</param>
    /// <returns>是否为完整有效设置。</returns>
    private static bool ValidPolicy(bool defaults, long? daily, long? monthly, long? reservation) => defaults
        ? daily is null && monthly is null && reservation is null
        : (daily.HasValue || monthly.HasValue) && daily is not < 1 && monthly is not < 1 && reservation is >= 1 && reservation <= (daily ?? long.MaxValue) && reservation <= (monthly ?? long.MaxValue);
    #endregion

    #region 校验修改命令（ValidateMutation）
    /// <summary>校验可信操作者、幂等标识及有界原因。</summary>
    /// <param name="operatorId">可信操作者。</param>
    /// <param name="operationId">幂等标识。</param>
    /// <param name="reason">原因。</param>
    private static void ValidateMutation(Guid operatorId, Guid operationId, string reason)
    { if (operatorId == Guid.Empty || operationId == Guid.Empty || !ValidText(reason, 500)) throw Invalid(); }
    #endregion

    #region 校验所有者（ValidateOwner）
    /// <summary>校验可信账本所有者。</summary>
    /// <param name="groupId">可信集团。</param>
    /// <param name="companyId">可信公司。</param>
    private static void ValidateOwner(Guid groupId, Guid companyId)
    { if (groupId == Guid.Empty || companyId == Guid.Empty) throw Invalid(); }
    #endregion

    #region 校验文本（ValidText）
    /// <summary>拒绝空白、超长和控制字符，不将核查依据作为执行指令。</summary>
    /// <param name="value">有界输入。</param>
    /// <param name="maximum">长度上限。</param>
    /// <returns>是否为有效文本。</returns>
    private static bool ValidText(string? value, int maximum) => !string.IsNullOrWhiteSpace(value) && value.Length <= maximum && !value.Any(char.IsControl);
    #endregion

    #region 计算命令哈希（CommandHash）
    /// <summary>绑定所有者、操作者、操作类型、目标及完整输入的稳定哈希。</summary>
    /// <param name="groupId">可信集团。</param>
    /// <param name="companyId">可信公司。</param>
    /// <param name="operatorId">操作者。</param>
    /// <param name="kind">操作类型。</param>
    /// <param name="targetId">目标。</param>
    /// <param name="input">完整命令。</param>
    /// <returns>SHA256。</returns>
    private static string CommandHash(Guid groupId, Guid companyId, Guid operatorId, string kind, Guid targetId, object input) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { groupId, companyId, operatorId, kind, targetId, input }))));
    #endregion

    #region 计算租户稳定标识（OwnerId）
    /// <summary>生成仅用于调整审计目标的稳定集团、公司标识。</summary>
    /// <param name="groupId">可信集团。</param>
    /// <param name="companyId">可信公司。</param>
    /// <returns>稳定标识。</returns>
    private static Guid OwnerId(Guid groupId, Guid companyId)
    {
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(FormattableString.Invariant($"{groupId:D}:{companyId:D}")));
        return new Guid(hash.AsSpan(0, 16));
    }
    #endregion

    #region 无效命令（Invalid）
    /// <summary>固定无效输入异常。</summary>
    /// <returns>领域异常。</returns>
    private static AgentRuntimeException Invalid() => new(AgentUserTokenQuotaManagementErrors.Invalid, "Invalid quota management command.");
    #endregion

    #region 版本或状态冲突（Conflict）
    /// <summary>固定版本、状态或幂等冲突异常。</summary>
    /// <returns>领域异常。</returns>
    private static AgentRuntimeException Conflict() => new(AgentUserTokenQuotaManagementErrors.Conflict, "Quota state changed; reload before applying the operation.");
    #endregion
}
