using EU.Core.Api.MCP.Services.BusinessQuery;
using EU.Core.Api.MCP.Services.BusinessQuery.Contracts;

namespace EU.Core.Api.MCP.Interfaces;

public interface IBusinessQueryService : IBaseService
{
    Task<QueryBusinessDataResponse> QueryAsync(
        string entity,
        IReadOnlyList<string> dimensions,
        IReadOnlyList<BusinessMeasure> measures,
        IReadOnlyList<BusinessFilter> filters,
        BusinessTimeRange? timeRange,
        IReadOnlyList<BusinessOrder> orderBy,
        int limit,
        CancellationToken cancellationToken);
}
