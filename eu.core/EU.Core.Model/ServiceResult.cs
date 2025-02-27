namespace EU.Core.Model;

/// <summary>
/// 服务层响应实体(泛型)
/// </summary>
public class ServiceResult<T>
{
    /// <summary>
    /// 状态码
    /// </summary>
    public int Status { get; set; } = 200;
    /// <summary>
    /// 操作是否成功
    /// </summary>
    public bool Success { get; set; } = false;
    /// <summary>
    /// 返回信息
    /// </summary>
    public string Message { get; set; } = null;
    /// <summary>
    /// 开发者信息
    /// </summary>
    public string MessageDev { get; set; }
    public int Count { get; set; }
    /// <summary>
    /// 返回数据集合
    /// </summary>
    public T Data { get; set; }

    /// <summary>
    /// 返回成功
    /// </summary>
    /// <param name="message">消息</param>
    /// <returns></returns>
    public static ServiceResult<T> OprateSuccess(string message) => OprateSuccess(true, message, default);


    /// <summary>
    /// 操作成功
    /// </summary>
    /// <param name="data"></param>
    /// <param name="count"></param>
    /// <param name="message"></param>
    /// <param name="success"></param>
    /// <returns></returns>
    public static ServiceResult<T> OprateSuccess(T data, int count, string message = "操作成功", bool success = true) => new ServiceResult<T>
    {
        Message = message,
        Success = success,
        Data = data,
        Count = count
    };

    /// <summary>
    /// 操作成功
    /// </summary>
    /// <param name="data"></param>
    /// <param name="message"></param>
    /// <param name="success"></param>
    /// <returns></returns>
    public static ServiceResult<T> QuerySuccess(T data = default, string message = "查询成功", bool success = true) => new ServiceResult<T>
    {
        Message = message,
        Success = success,
        Data = data,
    };

    /// <summary>
    /// 操作成功
    /// </summary>
    /// <param name="data"></param>
    /// <param name="message"></param>
    /// <param name="success"></param>
    /// <returns></returns>
    public static ServiceResult<T> OprateSuccess(T data = default, string message = "操作成功", bool success = true) => new ServiceResult<T>
    {
        Message = message,
        Success = success,
        Data = data,
    };

    /// <summary>
    /// 返回成功
    /// </summary>
    /// <param name="message">消息</param>
    /// <param name="data">数据</param>
    /// <returns></returns>
    public static ServiceResult<T> OprateSuccess(T data, string message) => OprateSuccess(true, message, data);

    /// <summary>
    /// 返回成功
    /// </summary>
    /// <param name="message">消息</param>
    /// <param name="data">数据</param>
    /// <param name="count">数量</param>
    /// <returns></returns>
    public static ServiceResult<T> OprateSuccess(T data, string message, int count) => OprateSuccess(true, message, data, count);

    /// <summary>
    /// 返回失败
    /// </summary>
    /// <param name="message">消息</param>
    /// <returns></returns>
    public static ServiceResult<T> OprateFailed(string message) => new ServiceResult<T>() { Status = 201, Message = message, Data = default, Success = false, Count = 0 };

    /// <summary>
    /// 返回失败
    /// </summary>
    /// <param name="message">消息</param>
    /// <param name="data">数据</param>
    /// <returns></returns>
    public static ServiceResult<T> OprateFailed(string message, T data) => OprateSuccess(false, message, data);

    /// <summary>
    /// 返回消息
    /// </summary>
    /// <param name="success">失败/成功</param>
    /// <param name="message">消息</param>
    /// <param name="data">数据</param>
    /// <param name="count">数据</param>
    /// <returns></returns>
    public static ServiceResult<T> OprateSuccess(bool success, string message, T data, int count = 0) => new ServiceResult<T>() { Message = message, Data = data, Success = success, Count = count };

}

/// <summary>
/// 服务层响应实体
/// </summary>
public class ServiceResult
{
    /// <summary>
    /// 状态码
    /// </summary>
    public int Status { get; set; } = 200;
    /// <summary>
    /// 操作是否成功
    /// </summary>
    public bool Success { get; set; } = false;
    /// <summary>
    /// 返回信息
    /// </summary>
    public string Message { get; set; } = null;
    /// <summary>
    /// 返回数据集合
    /// </summary>
    public object Data { get; set; }

    /// <summary>
    /// 操作成功
    /// </summary>
    /// <param name="msg"></param>
    /// <returns></returns>
    public static ServiceResult OprateSuccess(string msg = "操作成功")
    {
        return new ServiceResult
        {
            Message = msg,
            Success = true,
            Data = null
        };
    }

    /// <summary>
    /// 操作失败
    /// </summary>
    /// <param name="msg"></param>
    /// <param name="status"></param>
    /// <returns></returns>
    public static ServiceResult OprateFailed(string msg = "操作失败", int status = 500)
    {
        return new ServiceResult
        {
            Message = msg,
            Status = status,
            Data = null
        };
    }
}


/// <summary>
/// 服务层分页响应实体(泛型)
/// </summary>
public class ServicePageResult<T>
{
    /// <summary>
    /// 状态码
    /// </summary>
    public int Status { get; set; } = 200;
    /// <summary>
    /// 操作是否成功
    /// </summary>
    public bool Success { get; set; } = false;
    /// <summary>
    /// 返回信息
    /// </summary>
    public string Message { get; set; } = null;
    /// <summary>
    /// 当前页标
    /// </summary>
    public int Page { get; set; } = 1;
    /// <summary>
    /// 总页数
    /// </summary>
    public int PageCount => (int)Math.Ceiling((decimal)TotalCount / PageSize);
    /// <summary>
    /// 数据总数
    /// </summary>
    public int TotalCount { get; set; } = 0;
    /// <summary>
    /// 每页大小
    /// </summary>
    public int PageSize { set; get; } = 20;
    /// <summary>
    /// 返回数据
    /// </summary>
    public List<T> Data { get; set; }

    public ServicePageResult() { }

    public ServicePageResult(int page, int totalCount, int pageSize, List<T> data)
    {
        Success = true;
        this.Page = page;
        this.TotalCount = totalCount;
        PageSize = pageSize;
        this.Data = data;
    }
}