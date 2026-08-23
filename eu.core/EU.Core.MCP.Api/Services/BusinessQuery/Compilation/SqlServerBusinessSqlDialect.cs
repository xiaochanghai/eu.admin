using System.Text;
using System.Text.RegularExpressions;
using EU.Core.Api.MCP.Services.BusinessQuery.Catalog;

namespace EU.Core.Api.MCP.Services.BusinessQuery.Compilation;

public sealed partial class SqlServerBusinessSqlDialect : IBusinessSqlDialect
{
    public SqlServerBusinessSqlDialect(int maximumParameters = 2_100)
    {
        if (maximumParameters < 1 || maximumParameters > 2_100)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumParameters));
        }

        MaximumParameters = maximumParameters;
    }

    public BusinessCatalogDialect Dialect => BusinessCatalogDialect.SqlServer;

    public int MaximumParameters { get; }

    public string LikeEscapeClause => "ESCAPE '\\'";

    public string QuoteIdentifier(string identifier)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identifier);
        string[] segments = identifier.Split('.');
        if (segments.Any(value => !IdentifierPattern().IsMatch(value)))
        {
            throw new BusinessQueryCompilationException(
                BusinessQueryCompilationErrorCodes.CatalogInvalid);
        }

        return string.Join('.', segments.Select(value => $"[{value}]"));
    }

    public string ParameterName(int index) => $"@p{index}";

    public string EscapeLikePattern(string value) => Escape(value);

    internal static string Escape(string value)
    {
        var result = new StringBuilder(value.Length);
        foreach (char character in value)
        {
            if (character is '\\' or '%' or '_' or '[')
            {
                result.Append('\\');
            }

            result.Append(character);
        }

        return result.ToString();
    }

    [GeneratedRegex("^[A-Za-z_][A-Za-z0-9_]*$", RegexOptions.CultureInvariant)]
    private static partial Regex IdentifierPattern();
}
