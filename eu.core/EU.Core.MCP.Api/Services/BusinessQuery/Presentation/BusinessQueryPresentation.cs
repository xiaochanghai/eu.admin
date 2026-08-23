using System.Collections.ObjectModel;

namespace EU.Core.Api.MCP.Services.BusinessQuery.Presentation;

public sealed record BusinessQueryPresentationColumn(
    string Key,
    string Label,
    string Unit,
    string Currency);

public sealed record BusinessQueryPresentationCell(
    string DisplayValue,
    bool UntrustedData);

public sealed class BusinessQueryPresentation
{
    public BusinessQueryPresentation(
        string title,
        IEnumerable<BusinessQueryPresentationColumn> columns,
        IEnumerable<IReadOnlyDictionary<string, BusinessQueryPresentationCell>> rows,
        string markdown,
        string formatterVersion)
    {
        Title = title;
        Columns = new ReadOnlyCollection<BusinessQueryPresentationColumn>(
            columns.Select(value => value with { }).ToArray());
        Rows = new ReadOnlyCollection<IReadOnlyDictionary<string, BusinessQueryPresentationCell>>(
            rows.Select(row => (IReadOnlyDictionary<string, BusinessQueryPresentationCell>)
                new ReadOnlyDictionary<string, BusinessQueryPresentationCell>(
                    row.ToDictionary(
                        value => value.Key,
                        value => value.Value with { },
                        StringComparer.Ordinal))).ToArray());
        Markdown = markdown;
        FormatterVersion = formatterVersion;
    }

    public string Title { get; }

    public IReadOnlyList<BusinessQueryPresentationColumn> Columns { get; }

    public IReadOnlyList<IReadOnlyDictionary<string, BusinessQueryPresentationCell>> Rows { get; }

    public string Markdown { get; }

    public string FormatterVersion { get; }
}
