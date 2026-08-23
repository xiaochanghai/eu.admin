using System.Globalization;
using System.Text;
using System.Text.Json;
using EU.Core.Api.MCP.Services.BusinessQuery.Catalog;
using EU.Core.Api.MCP.Services.BusinessQuery.Contracts;
using EU.Core.Api.MCP.Services.BusinessQuery.Policy;

namespace EU.Core.Api.MCP.Services.BusinessQuery.Compilation;

public sealed class BusinessSqlCompiler
{
    private readonly IReadOnlyDictionary<BusinessCatalogDialect, IBusinessSqlDialect>
        _dialects;

    public BusinessSqlCompiler(IEnumerable<IBusinessSqlDialect>? dialects = null)
    {
        IBusinessSqlDialect[] available = (dialects ??
        [
            new SqlServerBusinessSqlDialect(),
            new SqliteBusinessSqlDialect(),
            new MySqlBusinessSqlDialect()
        ]).ToArray();
        if (available.Length == 0
            || available.Any(value => value is null)
            || available.Select(value => value.Dialect).Distinct().Count()
                != available.Length)
        {
            throw new ArgumentException("SQL Dialect registrations are invalid.", nameof(dialects));
        }

        _dialects = available.ToDictionary(value => value.Dialect);
    }

    public CompiledBusinessQuery Compile(
        BusinessCatalogSnapshot catalog,
        BusinessQueryPlan plan,
        BusinessQueryPolicyDecision policy,
        BusinessQueryEvaluationTime evaluationTime)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(policy);
        ArgumentNullException.ThrowIfNull(evaluationTime);

        string planHash = BusinessQueryPlanFingerprint.Compute(plan);
        if (!policy.Allowed
            || !policy.QuotaReservationId.HasValue
            || policy.QuotaReservationId.Value == Guid.Empty)
        {
            throw Error(BusinessQueryCompilationErrorCodes.PolicyRequired);
        }

        if (policy.CatalogRevision != catalog.Revision
            || !string.Equals(policy.CatalogHash, catalog.Sha256, StringComparison.Ordinal)
            || !string.Equals(policy.PlanHash, planHash, StringComparison.Ordinal)
            || !string.Equals(
                policy.EvaluationTimeHash,
                BusinessQueryEvaluationTimeFingerprint.Compute(evaluationTime),
                StringComparison.Ordinal)
            || plan.Limit > policy.MaximumResultRows
            || policy.Complexity > policy.ComplexityBudget
            || policy.MinimumGroupSize < 2
            || !catalog.IncludeBoundaryTies
            || !string.Equals(evaluationTime.TimeZoneId, catalog.TimeZoneId, StringComparison.Ordinal))
        {
            throw Error(BusinessQueryCompilationErrorCodes.PolicyMismatch);
        }

        if (!_dialects.TryGetValue(catalog.Dialect, out IBusinessSqlDialect? dialect))
        {
            throw Error(BusinessQueryCompilationErrorCodes.DialectUnsupported);
        }

        if (!catalog.Entities.TryGetValue(plan.Entity, out BusinessCatalogEntitySnapshot? root))
        {
            throw Error(BusinessQueryCompilationErrorCodes.FieldInvalid);
        }

        IReadOnlyDictionary<string, FieldBinding> bindings = BuildBindings(
            catalog,
            root,
            RequiredLogicalFields(plan, policy));
        IReadOnlyList<JoinBinding> joins = BuildJoins(catalog, root, bindings.Values);
        var parameters = new ParameterBuilder(dialect);
        var where = new List<string>();

        foreach (BusinessFilter filter in plan.Filters)
        {
            FieldBinding binding = GetBinding(bindings, filter.Field);
            where.Add(CompileFilter(dialect, parameters, binding, filter));
        }

        if (plan.TimeRange is not null)
        {
            if (!evaluationTime.StartUtc.HasValue || !evaluationTime.EndUtc.HasValue)
            {
                throw Error(BusinessQueryCompilationErrorCodes.PolicyMismatch);
            }

            FieldBinding time = GetBinding(bindings, plan.TimeRange.Field);
            string start = parameters.Add(
                BusinessCatalogDataType.DateTime,
                evaluationTime.StartUtc.Value);
            string end = parameters.Add(
                BusinessCatalogDataType.DateTime,
                evaluationTime.EndUtc.Value);
            string expression = Column(dialect, time);
            where.Add($"{expression} >= {start}");
            where.Add($"{expression} < {end}");
        }

        foreach (BusinessScopeConstraint constraint in policy.DataScope.Constraints)
        {
            FieldBinding scope = GetBinding(bindings, constraint.Field);
            if (scope.Field.Kind != BusinessCatalogFieldKind.Scope
                || constraint.Values.Count == 0)
            {
                throw Error(BusinessQueryCompilationErrorCodes.PolicyMismatch);
            }

            string expression = Column(dialect, scope);
            string[] names = constraint.Values
                .Select(value => parameters.Add(scope.Field.DataType, ConvertScopeValue(scope.Field, value)))
                .ToArray();
            where.Add(names.Length == 1
                ? $"{expression} = {names[0]}"
                : $"{expression} IN ({string.Join(", ", names)})");
        }

        var columns = new List<CompiledBusinessQueryColumn>();
        var groupedSelect = new List<string>();
        var groupBy = new List<string>();
        var orderAliases = new Dictionary<string, string>(StringComparer.Ordinal);
        for (int index = 0; index < plan.Dimensions.Count; index++)
        {
            string logicalName = plan.Dimensions[index];
            FieldBinding binding = GetBinding(bindings, logicalName);
            string alias = $"d{index}";
            string expression = Column(dialect, binding);
            groupedSelect.Add($"{expression} AS {dialect.QuoteIdentifier(alias)}");
            groupBy.Add(expression);
            orderAliases.Add(logicalName, alias);
            columns.Add(new CompiledBusinessQueryColumn(
                logicalName,
                logicalName,
                alias,
                binding.Field.DataType,
                binding.Field.Kind,
                binding.Field.Sensitivity,
                binding.Field.Unit,
                binding.Field.Currency,
                binding.Field.Precision,
                binding.Field.Scale));
        }

        for (int index = 0; index < plan.Measures.Count; index++)
        {
            BusinessMeasure measure = plan.Measures[index];
            FieldBinding binding = GetBinding(bindings, measure.Field);
            string alias = $"m{index}";
            groupedSelect.Add(
                $"{CompileMeasure(dialect, binding, measure.Aggregation)} AS {dialect.QuoteIdentifier(alias)}");
            orderAliases.Add(measure.ResultKey, alias);
            columns.Add(new CompiledBusinessQueryColumn(
                measure.ResultKey,
                measure.Field,
                alias,
                MeasureResultType(binding.Field.DataType, measure.Aggregation),
                binding.Field.Kind,
                binding.Field.Sensitivity,
                binding.Field.Unit,
                binding.Field.Currency,
                binding.Field.Precision,
                binding.Field.Scale));
        }

        if (groupedSelect.Count == 0)
        {
            throw Error(BusinessQueryCompilationErrorCodes.FieldInvalid);
        }

        string minimumGroup = parameters.Add(
            BusinessCatalogDataType.Integer,
            policy.MinimumGroupSize);
        string[] minimumGroupChecks =
        [
            $"COUNT(*) >= {minimumGroup}",
            .. plan.Measures
                .Select(value => GetBinding(bindings, value.Field))
                .Where(value =>
                    value.Field.Sensitivity is BusinessCatalogSensitivity.Confidential
                        or BusinessCatalogSensitivity.Restricted
                    && value.Field.NullHandling is BusinessNullHandling.Preserve
                        or BusinessNullHandling.Exclude)
                .DistinctBy(value => value.Field.Name)
                .Select(value => $"COUNT({Column(dialect, value)}) >= {minimumGroup}")
        ];
        string rankLimit = parameters.Add(BusinessCatalogDataType.Integer, plan.Limit);
        string[] requestedOrder = ResolveOrder(plan, orderAliases);
        string[] stableOrder = plan.Dimensions
            .Select((_, index) => $"d{index}")
            .ToArray();
        string groupedColumns = string.Join(
            ", ",
            columns.Select(value =>
                $"{dialect.QuoteIdentifier("g")}.{dialect.QuoteIdentifier(value.SqlAlias)}"));
        string rankedColumns = string.Join(
            ", ",
            columns.Select(value =>
                $"{dialect.QuoteIdentifier("r")}.{dialect.QuoteIdentifier(value.SqlAlias)}"));
        string rankOrder = string.Join(
            ", ",
            requestedOrder.Select(value => QualifyOrder(dialect, "g", value)));
        string finalOrder = string.Join(
            ", ",
            new[]
            {
                $"{dialect.QuoteIdentifier("r")}.{dialect.QuoteIdentifier(CompiledBusinessQuery.BoundaryRankColumnAlias)} ASC"
            }
                .Concat(requestedOrder.Select(value => QualifyOrder(dialect, "r", value)))
                .Concat(stableOrder
                    .Where(alias => !requestedOrder.Any(value =>
                        value.StartsWith(alias + " ", StringComparison.Ordinal)))
                    .Select(alias =>
                        $"{dialect.QuoteIdentifier("r")}.{dialect.QuoteIdentifier(alias)} ASC")));

        var sql = new StringBuilder();
        sql.AppendLine("WITH grouped AS (");
        sql.Append("    SELECT ").AppendLine(string.Join(", ", groupedSelect));
        sql.Append("    FROM ")
            .Append(dialect.QuoteIdentifier(root.PhysicalTable))
            .Append(" AS ")
            .AppendLine(dialect.QuoteIdentifier("e0"));
        foreach (JoinBinding join in joins)
        {
            sql.Append("    LEFT JOIN ")
                .Append(dialect.QuoteIdentifier(join.Target.PhysicalTable))
                .Append(" AS ")
                .Append(dialect.QuoteIdentifier(join.TargetAlias))
                .Append(" ON ")
                .Append(dialect.QuoteIdentifier(join.SourceAlias))
                .Append('.')
                .Append(dialect.QuoteIdentifier(join.Relationship.FromFieldSnapshot.PhysicalColumn))
                .Append(" = ")
                .Append(dialect.QuoteIdentifier(join.TargetAlias))
                .Append('.')
                .AppendLine(dialect.QuoteIdentifier(join.Relationship.ToFieldSnapshot.PhysicalColumn));
        }

        if (where.Count > 0)
        {
            sql.Append("    WHERE ").AppendLine(string.Join(" AND ", where));
        }

        if (groupBy.Count > 0)
        {
            sql.Append("    GROUP BY ").AppendLine(string.Join(", ", groupBy));
        }

        sql.Append("    HAVING ").AppendLine(string.Join(" AND ", minimumGroupChecks));
        sql.AppendLine("),");
        sql.AppendLine("ranked AS (");
        sql.Append("    SELECT ").Append(groupedColumns)
            .Append(", DENSE_RANK() OVER (ORDER BY ")
            .Append(rankOrder)
            .Append(") AS ")
            .AppendLine(dialect.QuoteIdentifier(CompiledBusinessQuery.BoundaryRankColumnAlias));
        sql.Append("    FROM grouped AS ").AppendLine(dialect.QuoteIdentifier("g"));
        sql.AppendLine(")");
        sql.Append("SELECT ").Append(rankedColumns)
            .Append(", ")
            .Append(dialect.QuoteIdentifier("r"))
            .Append('.')
            .AppendLine(dialect.QuoteIdentifier(CompiledBusinessQuery.BoundaryRankColumnAlias));
        sql.Append("FROM ranked AS ").AppendLine(dialect.QuoteIdentifier("r"));
        sql.Append("WHERE ")
            .Append(dialect.QuoteIdentifier("r"))
            .Append('.')
            .Append(dialect.QuoteIdentifier(CompiledBusinessQuery.BoundaryRankColumnAlias))
            .Append(" <= ")
            .AppendLine(rankLimit);
        sql.Append("ORDER BY ").Append(finalOrder);

        return new CompiledBusinessQuery(
            sql.ToString().Replace(Environment.NewLine, "\n", StringComparison.Ordinal),
            parameters.Values,
            columns,
            catalog.Dialect,
            catalog.DataSourceCode,
            plan.Entity,
            catalog.Culture,
            catalog.FormatterVersion,
            catalog.Revision,
            catalog.Sha256,
            planHash,
            policy.DecisionId,
            evaluationTime.EvaluatedAtUtc,
            evaluationTime.TimeZoneId,
            evaluationTime.StartUtc,
            evaluationTime.EndUtc,
            plan.Limit,
            policy.MaximumResultRows,
            catalog.IncludeBoundaryTies);
    }

    private static IReadOnlyDictionary<string, FieldBinding> BuildBindings(
        BusinessCatalogSnapshot catalog,
        BusinessCatalogEntitySnapshot root,
        IEnumerable<string> logicalFields)
    {
        var bindings = new Dictionary<string, FieldBinding>(StringComparer.Ordinal);
        var owners = new HashSet<string>(StringComparer.Ordinal) { root.Name };
        foreach (string logicalName in logicalFields.Distinct(StringComparer.Ordinal))
        {
            BusinessCatalogEntitySnapshot? owner = catalog.Entities.Values.SingleOrDefault(
                value => value.Fields.ContainsKey(logicalName));
            if (owner is null)
            {
                throw Error(BusinessQueryCompilationErrorCodes.FieldInvalid);
            }

            owners.Add(owner.Name);
        }

        IReadOnlyDictionary<string, string> aliases = AssignEntityAliases(catalog, root, owners);
        foreach (string logicalName in logicalFields.Distinct(StringComparer.Ordinal))
        {
            BusinessCatalogEntitySnapshot owner = catalog.Entities.Values.Single(
                value => value.Fields.ContainsKey(logicalName));
            bindings.Add(logicalName, new FieldBinding(
                owner.Fields[logicalName],
                owner,
                aliases[owner.Name]));
        }

        return bindings;
    }

    private static IReadOnlyDictionary<string, string> AssignEntityAliases(
        BusinessCatalogSnapshot catalog,
        BusinessCatalogEntitySnapshot root,
        IReadOnlySet<string> requiredOwners)
    {
        var aliases = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [root.Name] = "e0"
        };
        var requiredRelationships = new List<BusinessCatalogRelationshipSnapshot>();
        foreach (string owner in requiredOwners.Where(value => value != root.Name).Order())
        {
            foreach (BusinessCatalogRelationshipSnapshot relationship in FindPath(catalog, root.Name, owner))
            {
                if (relationship.Cardinality is BusinessRelationshipCardinality.OneToMany
                    or BusinessRelationshipCardinality.ManyToMany)
                {
                    throw Error(BusinessQueryCompilationErrorCodes.JoinUnsafe);
                }

                if (requiredRelationships.All(value => value.Name != relationship.Name))
                {
                    requiredRelationships.Add(relationship);
                }
            }
        }

        int index = 1;
        bool changed;
        do
        {
            changed = false;
            foreach (BusinessCatalogRelationshipSnapshot relationship in requiredRelationships)
            {
                if (aliases.ContainsKey(relationship.FromEntity)
                    && !aliases.ContainsKey(relationship.ToEntity))
                {
                    aliases.Add(relationship.ToEntity, $"e{index++}");
                    changed = true;
                }
            }
        }
        while (changed);

        if (requiredOwners.Any(value => !aliases.ContainsKey(value)))
        {
            throw Error(BusinessQueryCompilationErrorCodes.JoinUnsafe);
        }

        return aliases;
    }

    private static IReadOnlyList<JoinBinding> BuildJoins(
        BusinessCatalogSnapshot catalog,
        BusinessCatalogEntitySnapshot root,
        IEnumerable<FieldBinding> bindings)
    {
        string[] requiredOwners = bindings
            .Select(value => value.Owner.Name)
            .Where(value => value != root.Name)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();
        var relationships = new List<BusinessCatalogRelationshipSnapshot>();
        foreach (string owner in requiredOwners)
        {
            foreach (BusinessCatalogRelationshipSnapshot relationship in FindPath(catalog, root.Name, owner))
            {
                if (relationships.All(value => value.Name != relationship.Name))
                {
                    relationships.Add(relationship);
                }
            }
        }

        var aliases = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [root.Name] = "e0"
        };
        int aliasIndex = 1;
        foreach (BusinessCatalogRelationshipSnapshot relationship in relationships)
        {
            if (!aliases.ContainsKey(relationship.FromEntity))
            {
                throw Error(BusinessQueryCompilationErrorCodes.JoinUnsafe);
            }

            aliases.TryAdd(relationship.ToEntity, $"e{aliasIndex++}");
        }

        var joins = new List<JoinBinding>();
        foreach (BusinessCatalogRelationshipSnapshot relationship in relationships)
        {
            BusinessCatalogEntitySnapshot source = catalog.Entities[relationship.FromEntity];
            BusinessCatalogEntitySnapshot target = catalog.Entities[relationship.ToEntity];
            joins.Add(new JoinBinding(
                new RelationshipBinding(
                    relationship,
                    source.Fields[relationship.FromField],
                    target.Fields[relationship.ToField]),
                target,
                aliases[source.Name],
                aliases[target.Name]));
        }

        return joins;
    }

    private static IReadOnlyList<BusinessCatalogRelationshipSnapshot> FindPath(
        BusinessCatalogSnapshot catalog,
        string start,
        string target)
    {
        var matches = new List<BusinessCatalogRelationshipSnapshot[]>();
        var visiting = new HashSet<string>(StringComparer.Ordinal) { start };

        void Visit(
            string entity,
            BusinessCatalogRelationshipSnapshot[] current)
        {
            foreach (BusinessCatalogRelationshipSnapshot relationship in catalog.Relationships
                         .Where(value => value.FromEntity == entity)
                         .OrderBy(value => value.Name, StringComparer.Ordinal))
            {
                BusinessCatalogRelationshipSnapshot[] path = [.. current, relationship];
                if (relationship.ToEntity == target)
                {
                    matches.Add(path);
                    if (matches.Count > 1)
                    {
                        return;
                    }

                    continue;
                }

                if (visiting.Add(relationship.ToEntity))
                {
                    Visit(relationship.ToEntity, path);
                    visiting.Remove(relationship.ToEntity);
                }
            }
        }

        Visit(start, []);
        return matches.Count == 1
            ? matches[0]
            : throw Error(BusinessQueryCompilationErrorCodes.JoinUnsafe);
    }

    private static IEnumerable<string> RequiredLogicalFields(
        BusinessQueryPlan plan,
        BusinessQueryPolicyDecision policy) =>
        plan.Dimensions
            .Concat(plan.Measures.Select(value => value.Field))
            .Concat(plan.Filters.Select(value => value.Field))
            .Concat(plan.TimeRange is null ? [] : [plan.TimeRange.Field])
            .Concat(policy.DataScope.Constraints.Select(value => value.Field));

    private static string CompileFilter(
        IBusinessSqlDialect dialect,
        ParameterBuilder parameters,
        FieldBinding binding,
        BusinessFilter filter)
    {
        string column = Column(dialect, binding);
        if (filter.Operator == BusinessFilterOperator.In)
        {
            string[] names = filter.Value.EnumerateArray()
                .Select(value => parameters.Add(
                    binding.Field.DataType,
                    ConvertValue(binding.Field, value)))
                .ToArray();
            return $"{column} IN ({string.Join(", ", names)})";
        }

        if (filter.Operator == BusinessFilterOperator.Between)
        {
            JsonElement[] values = filter.Value.EnumerateArray().ToArray();
            if (values.Length != 2)
            {
                throw Error(BusinessQueryCompilationErrorCodes.FieldInvalid);
            }

            string start = parameters.Add(
                binding.Field.DataType,
                ConvertValue(binding.Field, values[0]));
            string end = parameters.Add(
                binding.Field.DataType,
                ConvertValue(binding.Field, values[1]));
            return $"{column} >= {start} AND {column} <= {end}";
        }

        object value = ConvertValue(binding.Field, filter.Value);
        if (filter.Operator == BusinessFilterOperator.Contains)
        {
            value = $"%{dialect.EscapeLikePattern((string)value)}%";
        }

        string parameter = parameters.Add(binding.Field.DataType, value);
        string operation = filter.Operator switch
        {
            BusinessFilterOperator.Equal => "=",
            BusinessFilterOperator.NotEqual => "<>",
            BusinessFilterOperator.GreaterThan => ">",
            BusinessFilterOperator.GreaterThanOrEqual => ">=",
            BusinessFilterOperator.LessThan => "<",
            BusinessFilterOperator.LessThanOrEqual => "<=",
            BusinessFilterOperator.Contains => "LIKE",
            _ => throw Error(BusinessQueryCompilationErrorCodes.FieldInvalid)
        };
        return filter.Operator == BusinessFilterOperator.Contains
            ? $"{column} {operation} {parameter} {dialect.LikeEscapeClause}"
            : $"{column} {operation} {parameter}";
    }

    private static string CompileMeasure(
        IBusinessSqlDialect dialect,
        FieldBinding binding,
        BusinessAggregation aggregation)
    {
        string column = Column(dialect, binding);
        string input = binding.Field.NullHandling == BusinessNullHandling.Zero
            ? $"COALESCE({column}, 0)"
            : column;
        return aggregation switch
        {
            BusinessAggregation.Sum => $"SUM({input})",
            BusinessAggregation.Count => $"COUNT({column})",
            BusinessAggregation.CountDistinct => $"COUNT(DISTINCT {column})",
            BusinessAggregation.Average => $"AVG({input})",
            BusinessAggregation.Minimum => $"MIN({input})",
            BusinessAggregation.Maximum => $"MAX({input})",
            _ => throw Error(BusinessQueryCompilationErrorCodes.FieldInvalid)
        };
    }

    private static BusinessCatalogDataType MeasureResultType(
        BusinessCatalogDataType source,
        BusinessAggregation aggregation) =>
        aggregation is BusinessAggregation.Count or BusinessAggregation.CountDistinct
            ? BusinessCatalogDataType.Integer
            : source;

    private static string[] ResolveOrder(
        BusinessQueryPlan plan,
        IReadOnlyDictionary<string, string> aliases)
    {
        string[] values = plan.OrderBy.Select(value =>
        {
            if (!aliases.TryGetValue(value.Field, out string? alias))
            {
                throw Error(BusinessQueryCompilationErrorCodes.FieldInvalid);
            }

            string direction = value.Direction == BusinessSortDirection.Descending
                ? "DESC"
                : "ASC";
            return $"{alias} {direction}";
        }).ToArray();
        if (values.Length > 0)
        {
            return values;
        }

        if (plan.Dimensions.Count > 0)
        {
            return ["d0 ASC"];
        }

        return ["m0 DESC"];
    }

    private static string QualifyOrder(
        IBusinessSqlDialect dialect,
        string entityAlias,
        string order)
    {
        string[] segments = order.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length != 2 || segments[1] is not ("ASC" or "DESC"))
        {
            throw Error(BusinessQueryCompilationErrorCodes.FieldInvalid);
        }

        return $"{dialect.QuoteIdentifier(entityAlias)}.{dialect.QuoteIdentifier(segments[0])} {segments[1]}";
    }

    private static object ConvertValue(
        BusinessCatalogFieldSnapshot field,
        JsonElement value) =>
        field.DataType switch
        {
            BusinessCatalogDataType.String => value.GetString()!,
            BusinessCatalogDataType.Boolean => value.GetBoolean(),
            BusinessCatalogDataType.Integer => value.GetInt64(),
            BusinessCatalogDataType.Decimal => value.GetDecimal(),
            BusinessCatalogDataType.Date or BusinessCatalogDataType.DateTime =>
                DateTimeOffset.Parse(
                    value.GetString()!,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind).ToUniversalTime(),
            _ => throw Error(BusinessQueryCompilationErrorCodes.FieldInvalid)
        };

    private static object ConvertScopeValue(
        BusinessCatalogFieldSnapshot field,
        string value) =>
        field.DataType switch
        {
            BusinessCatalogDataType.String => value,
            BusinessCatalogDataType.Boolean when bool.TryParse(value, out bool result) => result,
            BusinessCatalogDataType.Integer when long.TryParse(
                value,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out long result) => result,
            BusinessCatalogDataType.Decimal when decimal.TryParse(
                value,
                NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture,
                out decimal result) => result,
            BusinessCatalogDataType.Date or BusinessCatalogDataType.DateTime
                when DateTimeOffset.TryParseExact(
                    value,
                    "O",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind,
                    out DateTimeOffset result) => result.ToUniversalTime(),
            _ => throw Error(BusinessQueryCompilationErrorCodes.PolicyMismatch)
        };

    private static string Column(IBusinessSqlDialect dialect, FieldBinding binding) =>
        $"{dialect.QuoteIdentifier(binding.EntityAlias)}.{dialect.QuoteIdentifier(binding.Field.PhysicalColumn)}";

    private static FieldBinding GetBinding(
        IReadOnlyDictionary<string, FieldBinding> bindings,
        string logicalName) =>
        bindings.TryGetValue(logicalName, out FieldBinding? value)
            ? value
            : throw Error(BusinessQueryCompilationErrorCodes.FieldInvalid);

    private static BusinessQueryCompilationException Error(string code) => new(code);

    private sealed record FieldBinding(
        BusinessCatalogFieldSnapshot Field,
        BusinessCatalogEntitySnapshot Owner,
        string EntityAlias);

    private sealed record RelationshipBinding(
        BusinessCatalogRelationshipSnapshot Metadata,
        BusinessCatalogFieldSnapshot FromFieldSnapshot,
        BusinessCatalogFieldSnapshot ToFieldSnapshot);

    private sealed record JoinBinding(
        RelationshipBinding Relationship,
        BusinessCatalogEntitySnapshot Target,
        string SourceAlias,
        string TargetAlias);

    private sealed class ParameterBuilder(IBusinessSqlDialect dialect)
    {
        private readonly List<BusinessSqlParameter> _values = [];

        public IReadOnlyList<BusinessSqlParameter> Values => _values;

        public string Add(BusinessCatalogDataType type, object value)
        {
            if (_values.Count >= dialect.MaximumParameters)
            {
                throw Error(BusinessQueryCompilationErrorCodes.ParameterLimitExceeded);
            }

            string name = dialect.ParameterName(_values.Count);
            _values.Add(new BusinessSqlParameter(name, type, value));
            return name;
        }
    }
}
