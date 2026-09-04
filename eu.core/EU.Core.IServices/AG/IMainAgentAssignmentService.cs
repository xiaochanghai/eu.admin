using EU.Core.IServices.MainAgent;

#nullable enable

namespace EU.Core.IServices;

#region 文件职责：IMainAgentAssignmentService 服务契约

public interface IMainAgentAssignmentService
{
    Task<ServiceResult<MainAgentAssignment>> GetAsync(CancellationToken cancellationToken = default);

    Task<ServiceResult<MainAgentAssignment>> SetAsync(SetMainAgentCommand command, CancellationToken cancellationToken = default);
}

#endregion
