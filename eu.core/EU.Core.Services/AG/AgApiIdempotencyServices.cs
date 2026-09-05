using EU.Core.IServices.Abstractions.Security;

#nullable enable

namespace EU.Core.Services;

// 文件职责：AgApiIdempotencyServices 职责实现

/// <summary>
/// 提供 Agent API 幂等请求记录的持久化服务。
/// </summary>
public sealed class AgApiIdempotencyServices :
    BaseServices<AgApiIdempotency>,
    IAgApiIdempotencyServices,
    IHttpIdempotencyRepository
{
    #region 构造（AgApiIdempotencyServices）
    /// <summary>
    /// 构造（AgApiIdempotencyServices）
    /// </summary>
    /// <param name="dal">当前服务使用的数据访问仓储。</param>
    public AgApiIdempotencyServices(IBaseRepository<AgApiIdempotency> dal)
        : base(dal ?? throw new ArgumentNullException(nameof(dal)))
    {
    }
    #endregion

    #region 处理（BeginAsync）
    /// <summary>
    /// 处理（BeginAsync）
    /// </summary>
    /// <param name="pending">本次请求拟占用作用域的待执行幂等记录，包含请求摘要及有效期。</param>
    /// <param name="nowUtc">当前时间（UTC）。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>包含是否成功占用幂等作用域及对应记录的结果；冲突时携带已有记录。</returns>
    public async Task<HttpIdempotencyBeginResult> BeginAsync(
        HttpIdempotencyRecord pending,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pending);
        cancellationToken.ThrowIfCancellationRequested();

        DateTime now = nowUtc.UtcDateTime;
        await Db.Deleteable<AgApiIdempotency>()
            .Where(value => value.ExpiresAtUtc <= now)
            .ExecuteCommandAsync();

        AgApiIdempotency? existing = await GetByScopeAsync(pending.ScopeSha256);
        if (existing is not null)
        {
            return new HttpIdempotencyBeginResult(false, MapRecord(existing));
        }

        try
        {
            int inserted = await Db.Insertable(MapEntity(pending)).ExecuteCommandAsync();
            if (inserted == 1)
            {
                return new HttpIdempotencyBeginResult(true, Clone(pending));
            }
        }
        catch
        {
            existing = await GetByScopeAsync(pending.ScopeSha256);
            if (existing is null)
            {
                throw;
            }

            return new HttpIdempotencyBeginResult(false, MapRecord(existing));
        }

        existing = await GetByScopeAsync(pending.ScopeSha256)
            ?? throw new InvalidOperationException(
                "The idempotency record disappeared after a conflict.");
        return new HttpIdempotencyBeginResult(false, MapRecord(existing));
    }
    #endregion

    #region 保存 HTTP 响应并完成幂等记录（CompleteAsync）
    /// <summary>
    /// 保存 HTTP 响应并完成幂等记录（CompleteAsync）。
    /// </summary>
    /// <param name="scopeSha256">操作范围的 SHA-256 摘要。</param>
    /// <param name="requestSha256">请求内容的 SHA-256 摘要。</param>
    /// <param name="responseStatusCode">HTTP 响应状态码。</param>
    /// <param name="responseContentType">HTTP 响应的内容类型。</param>
    /// <param name="responseLocation">HTTP 响应的 Location 地址。</param>
    /// <param name="responseBody">HTTP 响应正文。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>异步更新结果：成功将匹配范围和请求摘要的进行中记录更新为已完成时返回 true，否则返回 false。</returns>
    public async Task<bool> CompleteAsync(
        string scopeSha256,
        string requestSha256,
        int responseStatusCode,
        string responseContentType,
        string responseLocation,
        byte[] responseBody,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scopeSha256);
        ArgumentException.ThrowIfNullOrWhiteSpace(requestSha256);
        ArgumentNullException.ThrowIfNull(responseBody);
        cancellationToken.ThrowIfCancellationRequested();

        int updated = await Db.Updateable<AgApiIdempotency>()
            .SetColumns(_ => new AgApiIdempotency
            {
                Status = nameof(HttpIdempotencyStatus.Completed),
                ResponseStatusCode = responseStatusCode,
                ResponseContentType = responseContentType,
                ResponseLocation = responseLocation,
                ResponseBody = responseBody
            })
            .Where(value =>
                value.ScopeSha256 == scopeSha256 &&
                value.RequestSha256 == requestSha256 &&
                value.Status == nameof(HttpIdempotencyStatus.InProgress) &&
                !value.IsDeleted)
            .ExecuteCommandAsync();
        return updated == 1;
    }
    #endregion

    #region 处理（MarkIndeterminateAsync）
    /// <summary>
    /// 处理（MarkIndeterminateAsync）
    /// </summary>
    /// <param name="scopeSha256">操作范围的 SHA-256 摘要。</param>
    /// <param name="requestSha256">请求内容的 SHA-256 摘要。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>表示该异步操作完成的任务。</returns>
    public async Task MarkIndeterminateAsync(string scopeSha256, string requestSha256, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scopeSha256);
        ArgumentException.ThrowIfNullOrWhiteSpace(requestSha256);
        cancellationToken.ThrowIfCancellationRequested();

        await Db.Updateable<AgApiIdempotency>()
            .SetColumns(value => value.Status == nameof(HttpIdempotencyStatus.Indeterminate))
            .Where(value =>
                value.ScopeSha256 == scopeSha256 &&
                value.RequestSha256 == requestSha256 &&
                value.Status == nameof(HttpIdempotencyStatus.InProgress) &&
                !value.IsDeleted)
            .ExecuteCommandAsync();
    }
    #endregion

    #region 处理（AbandonAsync）
    /// <summary>
    /// 处理（AbandonAsync）
    /// </summary>
    /// <param name="scopeSha256">操作范围的 SHA-256 摘要。</param>
    /// <param name="requestSha256">请求内容的 SHA-256 摘要。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>表示该异步操作完成的任务。</returns>
    public async Task AbandonAsync(string scopeSha256, string requestSha256, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scopeSha256);
        ArgumentException.ThrowIfNullOrWhiteSpace(requestSha256);
        cancellationToken.ThrowIfCancellationRequested();

        await Db.Deleteable<AgApiIdempotency>()
            .Where(value =>
                value.ScopeSha256 == scopeSha256 &&
                value.RequestSha256 == requestSha256 &&
                value.Status == nameof(HttpIdempotencyStatus.InProgress))
            .ExecuteCommandAsync();
    }
    #endregion

    #region 获取（GetByScopeAsync）
    /// <summary>
    /// 获取（GetByScopeAsync）
    /// </summary>
    /// <param name="scopeSha256">操作范围的 SHA-256 摘要。</param>
    /// <returns>指定作用域中未删除的幂等实体；不存在时为 null。</returns>
    private async Task<AgApiIdempotency?> GetByScopeAsync(string scopeSha256) =>
        await Db.Queryable<AgApiIdempotency>()
            .Where(value => value.ScopeSha256 == scopeSha256 && !value.IsDeleted)
            .FirstAsync();
    #endregion

    #region 映射（MapEntity）
    /// <summary>
    /// 映射（MapEntity）
    /// </summary>
    /// <param name="value">本次操作使用的HTTP 幂等记录。</param>
    /// <returns>复制响应字节数组并生成新标识的幂等持久化实体。</returns>
    private static AgApiIdempotency MapEntity(HttpIdempotencyRecord value) => new()
    {
        ID = Guid.NewGuid(),
        ScopeSha256 = value.ScopeSha256,
        RequestSha256 = value.RequestSha256,
        Status = value.Status.ToString(),
        ResponseStatusCode = value.ResponseStatusCode,
        ResponseContentType = value.ResponseContentType,
        ResponseLocation = value.ResponseLocation,
        ResponseBody = value.ResponseBody.ToArray(),
        CreatedAtUtc = value.CreatedAtUtc.UtcDateTime,
        ExpiresAtUtc = value.ExpiresAtUtc.UtcDateTime,
        IsDeleted = false,
        IsActive = true
    };
    #endregion

    #region 映射（MapRecord）
    /// <summary>
    /// 映射（MapRecord）
    /// </summary>
    /// <param name="value">本次操作使用的HTTP 幂等实体。</param>
    /// <returns>从持久化字段还原的幂等记录及独立的响应字节数组。</returns>
    private static HttpIdempotencyRecord MapRecord(AgApiIdempotency value) => new(
        Required(value.ScopeSha256, "ScopeSha256"),
        Required(value.RequestSha256, "RequestSha256"),
        Enum.Parse<HttpIdempotencyStatus>(Required(value.Status, "Status"), false),
        Required(value.ResponseStatusCode, "ResponseStatusCode"),
        Required(value.ResponseContentType, "ResponseContentType"),
        Required(value.ResponseLocation, "ResponseLocation"),
        Required(value.ResponseBody, "ResponseBody").ToArray(),
        ToOffset(Required(value.CreatedAtUtc, "CreatedAtUtc")),
        ToOffset(Required(value.ExpiresAtUtc, "ExpiresAtUtc")));
    #endregion

    #region 复制（Clone）
    /// <summary>
    /// 复制（Clone）
    /// </summary>
    /// <param name="value">本次操作使用的HTTP 幂等记录。</param>
    /// <returns>复制响应字节数组后的幂等记录副本。</returns>
    private static HttpIdempotencyRecord Clone(HttpIdempotencyRecord value) =>
        value with { ResponseBody = value.ResponseBody.ToArray() };
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
        value ?? throw new InvalidDataException(
            $"Agent API idempotency field '{field}' is missing.");
    #endregion

    #region 处理（Required）
    /// <summary>
    /// 读取并校验必填字段（Required）。
    /// </summary>
    /// <param name="value">从持久化记录读取的可空字段值。</param>
    /// <param name="field">字段名称，用于校验和错误提示。</param>
    /// <returns>非 null 的必填字段值；缺失时抛出 InvalidDataException。</returns>
    private static string Required(string? value, string field) =>
        value ?? throw new InvalidDataException(
            $"Agent API idempotency field '{field}' is missing.");
    #endregion

    #region 处理（Required）
    /// <summary>
    /// 读取并校验必填字段（Required）。
    /// </summary>
    /// <param name="value">从持久化记录读取的可空字段值。</param>
    /// <param name="field">字段名称，用于校验和错误提示。</param>
    /// <returns>非 null 的必填字段值；缺失时抛出 InvalidDataException。</returns>
    private static byte[] Required(byte[]? value, string field) =>
        value ?? throw new InvalidDataException(
            $"Agent API idempotency field '{field}' is missing.");
    #endregion
}
