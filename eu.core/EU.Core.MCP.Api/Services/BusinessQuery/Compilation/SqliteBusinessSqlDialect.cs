using EU.Core.Api.MCP.Services.BusinessQuery.Catalog;

namespace EU.Core.Api.MCP.Services.BusinessQuery.Compilation;

public sealed class SqliteBusinessSqlDialect : IBusinessSqlDialect
{
    public SqliteBusinessSqlDialect(int maximumParameters = 999)
    {
        if (maximumParameters < 1 || maximumParameters > 32_766)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumParameters));
        }

        MaximumParameters = maximumParameters;
    }

    public BusinessCatalogDialect Dialect => BusinessCatalogDialect.Sqlite;

    public int MaximumParameters { get; }

    public string LikeEscapeClause => "ESCAPE '\\'";

    public string QuoteIdentifier(string identifier)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identifier);
        string[] segments = identifier.Split('.');
        if (segments.Any(value => value.Length == 0
                || !(char.IsAsciiLetter(value[0]) || value[0] == '_')
                || value.Any(character =>
                    !(char.IsAsciiLetterOrDigit(character) || character == '_'))))
        {
            throw new BusinessQueryCompilationException(
                BusinessQueryCompilationErrorCodes.CatalogInvalid);
        }

        return string.Join('.', segments.Select(value => $"\"{value}\""));
    }

    public string ParameterName(int index) => $"@p{index}";

    public string EscapeLikePattern(string value) =>
        SqlServerBusinessSqlDialect.Escape(value);
}
