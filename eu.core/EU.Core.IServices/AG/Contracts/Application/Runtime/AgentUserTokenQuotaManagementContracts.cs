using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

#nullable enable

namespace EU.Core.IServices.Runtime;

/// <summary>当前集团、公司的额度覆盖设置；不重置已经消费或预占的额度。</summary>
/// <param name="UseDefaults">是否使用宿主默认额度。</param>
/// <param name="DailyTotalTokens">覆盖的日额度，未启用或默认设置时为空。</param>
/// <param name="MonthlyTotalTokens">覆盖的月额度，未启用或默认设置时为空。</param>
/// <param name="RequestReservationTokens">覆盖的每次请求预占量，默认设置时为空。</param>
/// <param name="Revision">并发版本，尚无覆盖记录时为 0。</param>
public sealed record AgentUserTokenQuotaPolicy(
    bool UseDefaults,
    [property: JsonNumberHandling(JsonNumberHandling.WriteAsString | JsonNumberHandling.AllowReadingFromString)] long? DailyTotalTokens,
    [property: JsonNumberHandling(JsonNumberHandling.WriteAsString | JsonNumberHandling.AllowReadingFromString)] long? MonthlyTotalTokens,
    [property: JsonNumberHandling(JsonNumberHandling.WriteAsString | JsonNumberHandling.AllowReadingFromString)] long? RequestReservationTokens,
    [property: JsonNumberHandling(JsonNumberHandling.WriteAsString | JsonNumberHandling.AllowReadingFromString)] long Revision);

/// <summary>额度修改命令；集团、公司和操作者由服务端提供。</summary>
public sealed class AgentUserTokenQuotaPolicyInput
{
    /// <summary>一次操作的幂等标识，相同标识仅可重放相同内容。</summary>
    public Guid OperationId { get; init; }
    /// <summary>读取到的版本，首次创建为 0。</summary>
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public long ExpectedRevision { get; init; }
    /// <summary>恢复宿主默认设置；不是清空用量账本。</summary>
    public bool UseDefaults { get; init; }
    /// <summary>日额度；null 表示不启用该周期。</summary>
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public long? DailyTotalTokens { get; init; }
    /// <summary>月额度；null 表示不启用该周期。</summary>
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public long? MonthlyTotalTokens { get; init; }
    /// <summary>每次模型请求的预占量。</summary>
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public long? RequestReservationTokens { get; init; }
    /// <summary>人工修改原因，保存到不可变审计记录。</summary>
    [Required, StringLength(500)]
    public string Reason { get; init; } = "";
}

/// <summary>人工对账输入，完整实际用量必须有供应商记录等外部依据。</summary>
public sealed class AgentUserTokenQuotaReconcileInput
{
    /// <summary>幂等操作标识。</summary>
    public Guid OperationId { get; init; }
    /// <summary>已核实的完整实际总 Token；真实零用量必须显式填写 0。</summary>
    [JsonRequired]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public required long ActualTokens { get; init; }
    /// <summary>确认请求已终止、用量经过核实；不允许据此自动释放活跃请求。</summary>
    public bool Confirmed { get; init; }
    /// <summary>对账原因。</summary>
    [Required, StringLength(500)]
    public string Reason { get; init; } = "";
    /// <summary>供应商账单或人工核查记录引用；不填写密钥、令牌或账单正文。</summary>
    [Required, StringLength(256)]
    public string EvidenceReference { get; init; } = "";
}

/// <summary>当前集团、公司的待核查预占；调用用户仅用于审计追踪。</summary>
/// <param name="Id">原预占标识。</param>
/// <param name="RunId">所属运行标识，仅用于追踪。</param>
/// <param name="ConsumerUserId">实际发起模型请求的用户，不参与额度归属。</param>
/// <param name="State">0 预占、1 已开始、3 用量未知；不是人工释放授权。</param>
/// <param name="ReservedTokens">仍保留的预占量。</param>
/// <param name="SettledAtUtc">报告未知的 UTC 时间，未终结时为空。</param>
public sealed record AgentUserTokenQuotaPending(Guid Id, Guid RunId, Guid ConsumerUserId, int State,
    [property: JsonNumberHandling(JsonNumberHandling.WriteAsString)] long ReservedTokens, DateTimeOffset? SettledAtUtc);

/// <summary>对账回执；重复提交相同操作返回同一回执。</summary>
/// <param name="OperationId">此次幂等操作标识。</param>
/// <param name="ReservationId">核实的原预占记录。</param>
/// <param name="ActualTokens">依据核实的完整实际用量。</param>
/// <param name="AppliedAtUtc">事务应用时间（UTC）。</param>
public sealed record AgentUserTokenQuotaReconciliation(Guid OperationId, Guid ReservationId,
    [property: JsonNumberHandling(JsonNumberHandling.WriteAsString | JsonNumberHandling.AllowReadingFromString)] long ActualTokens, DateTimeOffset AppliedAtUtc);

/// <summary>管理命令错误，不泄漏数据库异常正文。</summary>
public static class AgentUserTokenQuotaManagementErrors
{
    /// <summary>命令、原因、依据或用量无效。</summary>
    public const string Invalid = "MODEL_TOKEN_QUOTA_INVALID";
    /// <summary>版本、记录状态或幂等标识冲突。</summary>
    public const string Conflict = "MODEL_TOKEN_QUOTA_CONFLICT";
    /// <summary>当前集团、公司下没有目标记录。</summary>
    public const string NotFound = "MODEL_TOKEN_QUOTA_NOT_FOUND";
}
