using EU.Core.Agent.Application.Mcp;
using EU.Core.IServices.BASE;

#nullable enable

namespace EU.Core.IServices;

public interface IAgMcpServerDefinitionServices : IBaseServices<AgMcpServerDefinition>
{
    Task<McpOperationResult<McpServerDefinition>> CreateAsync(CreateMcpServerCommand command, CancellationToken cancellationToken = default);

    Task<McpServerDefinition?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<McpServerDefinition>> ListAsync(McpServerQuery query, CancellationToken cancellationToken = default);

    Task<McpOperationResult<McpServerDefinition>> UpdateAsync(UpdateMcpServerCommand command, CancellationToken cancellationToken = default);

    Task<McpOperationResult<McpServerDefinition>> SyncAsync(SyncMcpServerCommand command, CancellationToken cancellationToken = default);

    Task<McpOperationResult<McpServerDefinition>> ClassifyToolAsync(ClassifyMcpToolCommand command, CancellationToken cancellationToken = default);

    Task<McpOperationResult<McpServerDefinition>> SetArchivedAsync(SetMcpServerArchiveCommand command, CancellationToken cancellationToken = default);
}
