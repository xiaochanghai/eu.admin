namespace EU.Core.Services;

// 文件职责：BaseServices 职责实现

/// <summary>
/// Agent 领域中不直接持有通用仓储的业务服务基类。
/// </summary>
public abstract class BaseServices
{
    #region 处理（Success）
    /// <summary>
    /// 处理（Success）
    /// </summary>
    /// <typeparam name="T">待处理数据的泛型类型。</typeparam>
    /// <param name="data">数据。</param>
    /// <returns>将传入业务数据包装为操作成功的服务结果。</returns>
    protected static ServiceResult<T> Success<T>(T data) =>
        ServiceResult<T>.OprateSuccess(data);
    #endregion
}
