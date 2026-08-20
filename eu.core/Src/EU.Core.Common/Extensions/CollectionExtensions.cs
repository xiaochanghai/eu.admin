namespace EU.Core.Common.Extensions;

public static class CollectionExtensions
{
    public static IReadOnlyList<T> ToReadOnlyList<T>(this IEnumerable<T> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        return Array.AsReadOnly(values.ToArray());
    }
}
