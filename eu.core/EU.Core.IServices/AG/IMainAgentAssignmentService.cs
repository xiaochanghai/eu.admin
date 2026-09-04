using EU.Core.IServices.MainAgent;

#nullable enable

namespace EU.Core.IServices;

public interface IMainAgentAssignmentService
{
    Task<ServiceResult<MainAgentAssignment>> GetAsync(
        CancellationToken cancellationToken = default);

    Task<ServiceResult<MainAgentAssignment>> SetAsync(
        SetMainAgentCommand command,
        CancellationToken cancellationToken = default);
}
