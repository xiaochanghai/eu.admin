namespace EU.Core.Common.LogHelper;

/// <summary>
/// API 周统计数据
/// </summary>
public class ApiWeek
{
    /// <summary>
    /// 周标识
    /// </summary>
    public string week { get; set; } = string.Empty;

    /// <summary>
    /// API 地址
    /// </summary>
    public string url { get; set; } = string.Empty;

    /// <summary>
    /// 访问次数
    /// </summary>
    public int count { get; set; }
}

/// <summary>
/// API 日期统计数据
/// </summary>
public class ApiDate
{
    /// <summary>
    /// 日期
    /// </summary>
    public string date { get; set; } = string.Empty;

    /// <summary>
    /// 访问次数
    /// </summary>
    public int count { get; set; }
}

/// <summary>
/// 活跃用户视图模型
/// </summary>
public class ActiveUserVM
{
    /// <summary>
    /// 用户标识
    /// </summary>
    public string user { get; set; } = string.Empty;

    /// <summary>
    /// 活跃次数
    /// </summary>
    public int count { get; set; }
}

/// <summary>
/// 请求 API 周视图
/// </summary>
public class RequestApiWeekView
{
    /// <summary>
    /// 列名集合
    /// </summary>
    public List<string> columns { get; set; } = new();

    /// <summary>
    /// 行数据（JSON 格式）
    /// </summary>
    public string rows { get; set; } = string.Empty;
}

/// <summary>
/// 访问 API 日期视图
/// </summary>
public class AccessApiDateView
{
    /// <summary>
    /// 列名集合
    /// </summary>
    public string[] columns { get; set; } = Array.Empty<string>();

    /// <summary>
    /// 行数据集合
    /// </summary>
    public List<ApiDate> rows { get; set; } = new();
}

/// <summary>
/// 请求信息
/// </summary>
public class RequestInfo
{
    /// <summary>
    /// IP 地址
    /// </summary>
    public string Ip { get; set; } = string.Empty;

    /// <summary>
    /// 请求 URL
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// 请求时间
    /// </summary>
    public string Datetime { get; set; } = string.Empty;

    /// <summary>
    /// 请求日期
    /// </summary>
    public string Date { get; set; } = string.Empty;

    /// <summary>
    /// 请求周
    /// </summary>
    public string Week { get; set; } = string.Empty;
}

/// <summary>
/// AOP 日志信息
/// </summary>
public class AOPLogInfo
{
    /// <summary>
    /// 请求时间
    /// </summary>
    public string RequestTime { get; set; } = string.Empty;

    /// <summary>
    /// 操作人员
    /// </summary>
    public string OpUserName { get; set; } = string.Empty;

    /// <summary>
    /// 请求方法名
    /// </summary>
    public string RequestMethodName { get; set; } = string.Empty;

    /// <summary>
    /// 请求参数名
    /// </summary>
    public string RequestParamsName { get; set; } = string.Empty;

    /// <summary>
    /// 请求参数数据 JSON
    /// </summary>
    public string RequestParamsData { get; set; } = string.Empty;

    /// <summary>
    /// 请求响应间隔时间
    /// </summary>
    public string ResponseIntervalTime { get; set; } = string.Empty;

    /// <summary>
    /// 响应时间
    /// </summary>
    public string ResponseTime { get; set; } = string.Empty;

    /// <summary>
    /// 响应结果
    /// </summary>
    public string ResponseJsonData { get; set; } = string.Empty;
}

/// <summary>
/// AOP 异常日志信息
/// </summary>
public class AOPLogExInfo
{
    /// <summary>
    /// AOP 日志信息
    /// </summary>
    public AOPLogInfo ApiLogAopInfo { get; set; } = new();

    /// <summary>
    /// 内部异常
    /// </summary>
    public string InnerException { get; set; } = string.Empty;

    /// <summary>
    /// 异常消息
    /// </summary>
    public string ExMessage { get; set; } = string.Empty;
}

/// <summary>
/// 请求日志信息
/// </summary>
public class RequestLogInfo
{
    /// <summary>
    /// 请求地址
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// 请求参数
    /// </summary>
    public string QueryString { get; set; } = string.Empty;

    /// <summary>
    /// Body 参数
    /// </summary>
    public string BodyData { get; set; } = string.Empty;

    /// <summary>
    /// 过滤条件
    /// </summary>
    public string filter { get; set; } = string.Empty;
}
