using System.Collections.ObjectModel;

namespace EU.Core.Api.MCP.Services.BusinessQuery.Policy;

public sealed record BusinessScopeConstraint(
    string Field,
    IReadOnlyList<string> Values);

public sealed class BusinessDataScope
{
    public static BusinessDataScope Empty { get; } = new([]);

    public BusinessDataScope(IEnumerable<BusinessScopeConstraint> constraints)
    {
        Constraints = new ReadOnlyCollection<BusinessScopeConstraint>(
            (constraints ?? []).Select(value => new BusinessScopeConstraint(
                value.Field,
                new ReadOnlyCollection<string>(value.Values.ToArray())))
            .ToArray());
    }

    public IReadOnlyList<BusinessScopeConstraint> Constraints { get; }
}
