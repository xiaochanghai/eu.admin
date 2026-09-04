namespace EU.Core.Services;

#region 文件职责：BaseServices 职责实现

/// <summary>
/// Agent 领域中不直接持有通用仓储的业务服务基类。
/// </summary>
public abstract class BaseServices
{
    protected static ServiceResult<T> Success<T>(T data) =>
        ServiceResult<T>.OprateSuccess(data);
}

#endregion
