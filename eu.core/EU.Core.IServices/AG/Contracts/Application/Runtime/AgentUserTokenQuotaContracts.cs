using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;

#nullable enable

namespace EU.Core.IServices.Runtime;

/// <summary>同一集团、公司共享的自然日/月 Token 配额；与用户及 Agent 标识无关。</summary>
public interface IAgentUserTokenQuota
{
    /// <summary>只读查询当前可信集团、公司的共享余额；关闭时不访问数据库。</summary>
    /// <param name="identity">由认证请求边界提供的身份，不接受客户端所有者参数。</param>
    /// <param name="cancellationToken">查询取消令牌。</param>
    /// <returns>当前集团、公司的配置及周期账本快照。</returns>
    Task<AgentUserTokenQuotaBalance> GetBalanceAsync(AgentExecutionIdentity identity, CancellationToken cancellationToken = default);
    /// <summary>在供应商请求前持久化预占；未启用配额时不访问账本。</summary>
    /// <param name="context">包含可信集团、公司、调用用户及运行标识的执行上下文。</param>
    /// <param name="cancellationToken">预占取消令牌。</param>
    /// <returns>请求专属租约，关闭配额时为空。</returns>
    Task<IAgentUserTokenQuotaLease?> ReserveAsync(AgentRunContext context, CancellationToken cancellationToken = default);
}

/// <summary>一次真实供应商请求的持久化租约，不得跨请求复用。</summary>
public interface IAgentUserTokenQuotaLease : IAsyncDisposable
{
    /// <summary>本次预占总 Token 数，也作为本次请求的事后总用量阈值。</summary>
    long ReservedTokens { get; }
    /// <summary>持久化已开始状态；成功返回后才允许调用供应商。</summary>
    /// <param name="cancellationToken">开始前取消令牌。</param>
    /// <returns>状态保存完成的任务。</returns>
    Task MarkStartedAsync(CancellationToken cancellationToken = default);
    /// <summary>按实际用量结算；null 表示未知，冻结关联周期而不是记零。</summary>
    /// <param name="totalTokens">供应商完整响应报告的非负总用量。</param>
    /// <returns>使用非请求取消令牌完成持久化的任务。</returns>
    Task CompleteAsync(long? totalTokens);
}

/// <summary>配额周期事实，标识由集团、公司、周期种类与本地日期/时区确定。</summary>
/// <param name="Id">稳定周期标识。</param>
/// <param name="Kind">0 为自然日，1 为自然月。</param>
/// <param name="Key">本地日期及 IANA/系统时区标识。</param>
/// <param name="StartUtc">含起点 UTC 时间。</param>
/// <param name="EndUtc">不含终点 UTC 时间。</param>
/// <param name="LimitTokens">当前部署配置的累计上限。</param>
public sealed record AgentUserTokenQuotaWindow(Guid Id, int Kind, string Key, DateTime StartUtc, DateTime EndUtc, long LimitTokens);

/// <summary>内部预占命令，由宿主从可信身份生成，不是 HTTP 输入 DTO。</summary>
/// <param name="Id">单次供应商请求预占标识。</param>
/// <param name="RunId">实际执行请求的 Agent 运行标识，仅用于追踪。</param>
/// <param name="GroupId">可信集团标识。</param>
/// <param name="CompanyId">可信公司标识。</param>
/// <param name="ConsumerUserId">实际发起模型请求的可信用户，仅用于追踪，不参与额度归属。</param>
/// <param name="ReservedTokens">同时从所有配置周期预占的总 Token 数。</param>
/// <param name="Windows">启用的日/月周期，固定按种类排序。</param>
/// <param name="PolicyRevision">启用租户覆盖时读取的设置版本，事务内再次确认；关闭管理时为空。</param>
public sealed record AgentUserTokenQuotaRequest(Guid Id, Guid RunId, Guid GroupId, Guid CompanyId, Guid ConsumerUserId, long ReservedTokens, IReadOnlyList<AgentUserTokenQuotaWindow> Windows, long? PolicyRevision = null);

/// <summary>当前所有者的只读查询命令，不创建运行或预占记录。</summary>
/// <param name="GroupId">可信集团。</param>
/// <param name="CompanyId">可信公司。</param>
/// <param name="RequestReservationTokens">判断下一请求准入所需的配置预占量。</param>
/// <param name="Windows">当前启用的周期。</param>
public sealed record AgentUserTokenQuotaQuery(Guid GroupId, Guid CompanyId, long RequestReservationTokens, IReadOnlyList<AgentUserTokenQuotaWindow> Windows);

/// <summary>租户共享额度快照；Token 数以十进制字符串序列化，避免浏览器长整数精度丢失。</summary>
/// <param name="Enabled">当前是否启用共享额度。</param>
/// <param name="TimeZoneId">周期时区。</param>
/// <param name="EvaluatedAtUtc">计算当前周期的 UTC 时间。</param>
/// <param name="RequestReservationTokens">单次预占量，关闭时为空。</param>
/// <param name="CanReserve">所有周期是否允许下一次预占；关闭时为空，不代表实际请求授权。</param>
/// <param name="Periods">当前启用周期的只读快照，关闭时为空集合。</param>
public sealed record AgentUserTokenQuotaBalance(bool Enabled, string TimeZoneId, DateTimeOffset EvaluatedAtUtc,
    [property: JsonNumberHandling(JsonNumberHandling.WriteAsString)] long? RequestReservationTokens,
    bool? CanReserve, IReadOnlyList<AgentUserTokenQuotaPeriodBalance> Periods);

/// <summary>单周期余额；已结算部分不包含未知消耗，未知时不报告可用余额。</summary>
/// <param name="Kind">Daily 为日额度，Monthly 为月额度。</param>
/// <param name="StartUtc">周期含起点。</param>
/// <param name="EndUtc">周期不含终点。</param>
/// <param name="LimitTokens">当前配置上限。</param>
/// <param name="UsedTokens">已结算的已知用量。</param>
/// <param name="ReservedTokens">尚未释放的预占，包含未知消耗的保留预占。</param>
/// <param name="RemainingTokens">扣除已知消耗与预占后的非负余额；未知用量冻结时为空。</param>
/// <param name="HasUnknownUsage">是否因未知用量冻结。</param>
/// <param name="CanReserve">当前周期是否足以预占下一请求。</param>
public sealed record AgentUserTokenQuotaPeriodBalance(string Kind, DateTimeOffset StartUtc, DateTimeOffset EndUtc,
    [property: JsonNumberHandling(JsonNumberHandling.WriteAsString)] long LimitTokens,
    [property: JsonNumberHandling(JsonNumberHandling.WriteAsString)] long UsedTokens,
    [property: JsonNumberHandling(JsonNumberHandling.WriteAsString)] long ReservedTokens,
    [property: JsonNumberHandling(JsonNumberHandling.WriteAsString)] long? RemainingTokens,
    bool HasUnknownUsage, bool CanReserve);

/// <summary>生成稳定周期和可信配额命令，不估算输入 Token，不读取额外配置源。</summary>
public static class AgentUserTokenQuotaRequests
{
    #region 创建日/月预占命令（Create）
    /// <summary>从执行身份生成租户共享的日/月窗口；调用用户只用于预占追踪。</summary>
    /// <param name="context">执行上下文。</param>
    /// <param name="now">当前 UTC 时间。</param>
    /// <param name="timeZone">自然日/月所属时区。</param>
    /// <param name="dailyTokens">日上限，null 不启用。</param>
    /// <param name="monthlyTokens">月上限，null 不启用。</param>
    /// <param name="reservationTokens">单次模型请求预占量。</param>
    /// <returns>固定归属及原始周期的预占命令。</returns>
    public static AgentUserTokenQuotaRequest Create(AgentRunContext context, DateTimeOffset now, TimeZoneInfo timeZone, long? dailyTokens, long? monthlyTokens, long reservationTokens)
    {
        if (context.RunId == Guid.Empty || context.ExecutionIdentity is null ||
            !Guid.TryParse(context.ExecutionIdentity.UserId, out Guid consumerUserId) || consumerUserId == Guid.Empty)
            throw new AgentRuntimeException(AgentRunErrorCodes.ModelTokenQuotaUnavailable, "A trusted group, company and user identity is required for the Token quota.");
        var query = CreateQuery(context.ExecutionIdentity, now, timeZone, dailyTokens, monthlyTokens, reservationTokens);
        return new(Guid.NewGuid(), context.RunId, query.GroupId, query.CompanyId, consumerUserId, reservationTokens, query.Windows);
    }
    #endregion

    #region 创建当前周期查询（CreateQuery）
    /// <summary>查询与预占使用同一周期算法，但查询不生成请求或运行标识。</summary>
    /// <param name="identity">可信执行身份。</param>
    /// <param name="now">当前 UTC 时间。</param>
    /// <param name="timeZone">周期时区。</param>
    /// <param name="dailyTokens">可选日上限。</param>
    /// <param name="monthlyTokens">可选月上限。</param>
    /// <param name="reservationTokens">配置预占量。</param>
    /// <returns>固定所有者的当前周期查询。</returns>
    public static AgentUserTokenQuotaQuery CreateQuery(AgentExecutionIdentity identity, DateTimeOffset now, TimeZoneInfo timeZone, long? dailyTokens, long? monthlyTokens, long reservationTokens)
    {
        if (identity is null || identity.GroupId is not { } groupId || groupId == Guid.Empty ||
            identity.CompanyId is not { } companyId || companyId == Guid.Empty)
            throw new AgentRuntimeException(AgentRunErrorCodes.ModelTokenQuotaUnavailable, "A trusted group and company identity is required for the Token quota.");
        if (reservationTokens < 1 || dailyTokens is < 1 || monthlyTokens is < 1 ||
            (dailyTokens is null && monthlyTokens is null) || reservationTokens > dailyTokens || reservationTokens > monthlyTokens)
            throw new ArgumentOutOfRangeException(nameof(reservationTokens));
        DateTime local = TimeZoneInfo.ConvertTime(now, timeZone).Date;
        var windows = new List<AgentUserTokenQuotaWindow>(2);
        if (dailyTokens is long daily) AddWindow(0, local, local.AddDays(1), daily);
        if (monthlyTokens is long monthly)
        {
            DateTime month = new(local.Year, local.Month, 1);
            AddWindow(1, month, month.AddMonths(1), monthly);
        }
        return new(groupId, companyId, reservationTokens, windows);

        void AddWindow(int kind, DateTime start, DateTime end, long limit)
        {
            string key = start.ToString(kind == 0 ? "yyyyMMdd" : "yyyyMM", CultureInfo.InvariantCulture) + "@" + timeZone.Id;
            byte[] digest = SHA256.HashData(Encoding.UTF8.GetBytes(FormattableString.Invariant($"{groupId:D}:{companyId:D}:{kind}:{key}")));
            windows.Add(new(new Guid(digest.AsSpan(0, 16)), kind, key,
                TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(start, DateTimeKind.Unspecified), timeZone),
                TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(end, DateTimeKind.Unspecified), timeZone), limit));
        }
    }
    #endregion
}
