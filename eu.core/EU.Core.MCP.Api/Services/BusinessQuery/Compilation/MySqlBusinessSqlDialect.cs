using EU.Core.Api.MCP.Services.BusinessQuery.Catalog;

namespace EU.Core.Api.MCP.Services.BusinessQuery.Compilation;

public sealed class MySqlBusinessSqlDialect : IBusinessSqlDialect
{
    public MySqlBusinessSqlDialect(int maximumParameters = 65_535)
    {
        if (maximumParameters < 1 || maximumParameters > 65_535)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumParameters));
        }

        MaximumParameters = maximumParameters;
    }

    public BusinessCatalogDialect Dialect => BusinessCatalogDialect.MySql;

    public int MaximumParameters { get; }

    // '=' avoids dependence on the server's NO_BACKSLASH_ESCAPES SQL mode.
    public string LikeEscapeClause => "ESCAPE '='";

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

        return string.Join('.', segments.Select(value => $"`{value}`"));
    }

    public string ParameterName(int index) => $"@p{index}";

    public string EscapeLikePattern(string value)
    {
        var result = new System.Text.StringBuilder(value.Length);
        foreach (char character in value)
        {
            if (character is '=' or '%' or '_')
            {
                result.Append('=');
            }

            result.Append(character);
        }

        return result.ToString();
    }
}
