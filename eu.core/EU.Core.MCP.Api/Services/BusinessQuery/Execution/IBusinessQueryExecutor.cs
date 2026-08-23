using EU.Core.Api.MCP.Services.BusinessQuery.Compilation;
using EU.Core.Api.MCP.Services.BusinessQuery.Contracts;
using SqlSugar;

namespace EU.Core.Api.MCP.Services.BusinessQuery.Execution;

public sealed record BusinessQueryExecutionResult(
    BusinessQueryResult Result,
    TimeSpan Elapsed,
    string TerminalStatus);

public interface IBusinessQueryExecutor
{
    Task<BusinessQueryExecutionResult> ExecuteAsync(
        CompiledBusinessQuery query,
        BusinessDataSourceDescriptor descriptor,
        ISqlSugarClient database,
        BusinessQueryExecutionLimits limits,
        CancellationToken cancellationToken);
}
