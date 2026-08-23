using EU.Core.Api.MCP.Services.BusinessQuery.Catalog;

namespace EU.Core.Api.MCP.Services.BusinessQuery.Compilation;

public interface IBusinessSqlDialect
{
    BusinessCatalogDialect Dialect { get; }

    int MaximumParameters { get; }

    string QuoteIdentifier(string identifier);

    string ParameterName(int index);

    string EscapeLikePattern(string value);

    string LikeEscapeClause { get; }
}
