using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using EU.Core.Api.MCP.Services.BusinessQuery.Contracts;

namespace EU.Core.Api.MCP.Services.BusinessQuery.Policy;

public static class BusinessQueryPlanFingerprint
{
    public static string Compute(BusinessQueryPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);

        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteString("entity", plan.Entity);
            WriteStrings(writer, "dimensions", plan.Dimensions);
            writer.WritePropertyName("measures");
            writer.WriteStartArray();
            foreach (BusinessMeasure measure in plan.Measures)
            {
                writer.WriteStartObject();
                writer.WriteString("field", measure.Field);
                writer.WriteString("aggregation", measure.Aggregation.ToString());
                writer.WriteString("resultKey", measure.ResultKey);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
            writer.WritePropertyName("filters");
            writer.WriteStartArray();
            foreach (BusinessFilter filter in plan.Filters)
            {
                writer.WriteStartObject();
                writer.WriteString("field", filter.Field);
                writer.WriteString("operator", filter.Operator.ToString());
                writer.WritePropertyName("value");
                filter.Value.WriteTo(writer);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
            writer.WritePropertyName("timeRange");
            if (plan.TimeRange is null)
            {
                writer.WriteNullValue();
            }
            else
            {
                writer.WriteStartObject();
                writer.WriteString("field", plan.TimeRange.Field);
                if (plan.TimeRange.Preset.HasValue)
                {
                    writer.WriteString("preset", plan.TimeRange.Preset.Value.ToString());
                }
                else
                {
                    writer.WriteString("start", plan.TimeRange.Start!.Value);
                    writer.WriteString("end", plan.TimeRange.End!.Value);
                }

                writer.WriteEndObject();
            }

            writer.WritePropertyName("orderBy");
            writer.WriteStartArray();
            foreach (BusinessOrder order in plan.OrderBy)
            {
                writer.WriteStartObject();
                writer.WriteString("field", order.Field);
                writer.WriteString("direction", order.Direction.ToString());
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
            writer.WriteNumber("limit", plan.Limit);
            writer.WriteEndObject();
        }

        return Convert.ToHexStringLower(SHA256.HashData(stream.ToArray()));
    }

    private static void WriteStrings(
        Utf8JsonWriter writer,
        string propertyName,
        IEnumerable<string> values)
    {
        writer.WritePropertyName(propertyName);
        writer.WriteStartArray();
        foreach (string value in values)
        {
            writer.WriteStringValue(value);
        }

        writer.WriteEndArray();
    }
}
