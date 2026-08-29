namespace EU.Core.Services;

/// <summary>
/// Agent 领域中不直接持有通用仓储的业务服务基类。
/// </summary>
public abstract class BaseServices
{
    protected static ServiceResult<T> Success<T>(T data) =>
        ServiceResult<T>.OprateSuccess(data);
}
