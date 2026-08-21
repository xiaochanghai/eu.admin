using EU.Core.IServices.Mcp;
using EU.Core.IServices.BASE;

#nullable enable

namespace EU.Core.IServices;

public interface IAgMcpServerDefinitionServices : IBaseServices<AgMcpServerDefinition>
{
    Task<ServiceResult<McpServerDefinition>> CreateAsync(CreateMcpServerCommand command, CancellationToken cancellationToken = default);

    Task<McpServerDefinition?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<McpServerDefinition>> ListAsync(McpServerQuery query, CancellationToken cancellationToken = default);

    Task<ServiceResult<McpServerDefinition>> UpdateAsync(UpdateMcpServerCommand command, CancellationToken cancellationToken = default);

    Task<ServiceResult<McpServerDefinition>> SyncAsync(SyncMcpServerCommand command, CancellationToken cancellationToken = default);

    Task<ServiceResult<McpServerDefinition>> ClassifyToolAsync(ClassifyMcpToolCommand command, CancellationToken cancellationToken = default);

    Task<ServiceResult<McpServerDefinition>> SetArchivedAsync(SetMcpServerArchiveCommand command, CancellationToken cancellationToken = default);
}
