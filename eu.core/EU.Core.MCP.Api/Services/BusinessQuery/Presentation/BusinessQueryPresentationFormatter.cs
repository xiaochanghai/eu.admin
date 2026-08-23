using System.Globalization;
using System.Text;
using EU.Core.Api.MCP.Services.BusinessQuery.Compilation;
using EU.Core.Api.MCP.Services.BusinessQuery.Contracts;

namespace EU.Core.Api.MCP.Services.BusinessQuery.Presentation;

public sealed class BusinessQueryPresentationFormatter
{
    public BusinessQueryPresentation Format(
        CompiledBusinessQuery query,
        BusinessQueryResult result)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(result);
        CultureInfo culture = CultureInfo.GetCultureInfo(query.Culture);
        var columns = result.Columns.Select(value =>
            new BusinessQueryPresentationColumn(
                value.Key,
                value.Key,
                value.Unit,
                value.Currency)).ToArray();
        var rows = result.Rows.Select(row =>
        {
            var values = new Dictionary<string, BusinessQueryPresentationCell>(
                StringComparer.Ordinal);
            foreach (BusinessQueryColumn column in result.Columns)
            {
                BusinessQueryValue value = row.Values[column.Key];
                values.Add(column.Key, new BusinessQueryPresentationCell(
                    FormatValue(value, column, culture),
                    value.UntrustedData));
            }

            return (IReadOnlyDictionary<string, BusinessQueryPresentationCell>)values;
        }).ToArray();
        string title = $"Business query result: {query.Entity}";
        return new BusinessQueryPresentation(
            title,
            columns,
            rows,
            BuildMarkdown(title, columns, rows),
            query.FormatterVersion);
    }

    private static string FormatValue(
        BusinessQueryValue value,
        BusinessQueryColumn column,
        CultureInfo culture)
    {
        if (value.Kind == BusinessQueryValueKind.Null)
        {
            return "—";
        }

        if (value.Kind == BusinessQueryValueKind.Decimal
            && decimal.TryParse(
                value.CanonicalValue,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out decimal number))
        {
            string formatted = number.ToString("N2", culture);
            return string.IsNullOrEmpty(column.Currency)
                ? formatted
                : $"{formatted} {column.Currency}";
        }

        return value.CanonicalValue;
    }

    private static string BuildMarkdown(
        string title,
        IReadOnlyList<BusinessQueryPresentationColumn> columns,
        IReadOnlyList<IReadOnlyDictionary<string, BusinessQueryPresentationCell>> rows)
    {
        var markdown = new StringBuilder();
        markdown.Append("### ").AppendLine(Escape(title));
        markdown.Append("| ")
            .Append(string.Join(" | ", columns.Select(value => Escape(value.Label))))
            .AppendLine(" |");
        markdown.Append("| ")
            .Append(string.Join(" | ", columns.Select(_ => "---")))
            .AppendLine(" |");
        foreach (IReadOnlyDictionary<string, BusinessQueryPresentationCell> row in rows)
        {
            markdown.Append("| ")
                .Append(string.Join(
                    " | ",
                    columns.Select(column => Escape(row[column.Key].DisplayValue))))
                .AppendLine(" |");
        }

        return markdown.ToString()
            .Replace(Environment.NewLine, "\n", StringComparison.Ordinal)
            .TrimEnd();
    }

    private static string Escape(string value) => value
        .Replace("&", "&amp;", StringComparison.Ordinal)
        .Replace("<", "&lt;", StringComparison.Ordinal)
        .Replace(">", "&gt;", StringComparison.Ordinal)
        .Replace("\\", "\\\\", StringComparison.Ordinal)
        .Replace("|", "\\|", StringComparison.Ordinal)
        .Replace("\r", " ", StringComparison.Ordinal)
        .Replace("\n", " ", StringComparison.Ordinal);
}
