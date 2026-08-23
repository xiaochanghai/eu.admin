using SqlSugar;

namespace EU.Core.Api.MCP.Services.BusinessQuery.Execution;

public interface IBusinessDbConnectionFactory
{
    Task<SqlSugarClient> CreateOpenConnectionAsync(
        BusinessDataSourceDescriptor descriptor,
        CancellationToken cancellationToken);
}
