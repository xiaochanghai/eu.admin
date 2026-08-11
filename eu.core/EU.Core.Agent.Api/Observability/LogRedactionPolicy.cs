using System.Collections;
using System.Globalization;
using System.Reflection;
using Serilog.Core;
using Serilog.Events;

namespace EU.Core.Agent.Api.Observability;

public static class LogRedactionPolicy
{
    public const string RedactedValue = "[REDACTED]";
    private const string CircularValue = "[CIRCULAR]";
    private const string TruncatedValue = "[TRUNCATED]";
    private const string UnavailableValue = "[UNAVAILABLE]";
    private const int MaximumDepth = 8;
    private const int MaximumCollectionItems = 64;

    public static object? Redact(object? value)
    {
        return Redact(value, new HashSet<object>(ReferenceEqualityComparer.Instance), 0);
    }

    internal static LogEventPropertyValue RedactEventProperty(string propertyName, LogEventPropertyValue value, int depth = 0)
    {
        if (IsSensitiveProperty(propertyName) || depth >= MaximumDepth)
        {
            return new ScalarValue(depth >= MaximumDepth ? TruncatedValue : RedactedValue);
        }

        return value switch
        {
            SequenceValue sequence => new SequenceValue(sequence.Elements.Select(element => RedactEventValue(element, depth + 1))),
            StructureValue structure => new StructureValue(
                structure.Properties.Select(property => new LogEventProperty(
                    property.Name,
                    RedactEventProperty(property.Name, property.Value, depth + 1))),
                structure.TypeTag),
            DictionaryValue dictionary => new DictionaryValue(dictionary.Elements.Select(element => new KeyValuePair<ScalarValue, LogEventPropertyValue>(
                element.Key,
                RedactEventProperty(KeyName(element.Key), element.Value, depth + 1)))),
            _ => value,
        };
    }

    internal static bool IsSensitiveProperty(string propertyName)
    {
        string normalized = string.Concat(propertyName.Where(char.IsLetterOrDigit)).ToLowerInvariant();
        return normalized.Contains("apikey", StringComparison.Ordinal) ||
            normalized.Contains("authorization", StringComparison.Ordinal) ||
            normalized.Contains("password", StringComparison.Ordinal) ||
            normalized.Contains("token", StringComparison.Ordinal) ||
            normalized.EndsWith("secret", StringComparison.Ordinal) ||
            normalized.EndsWith("credential", StringComparison.Ordinal) ||
            normalized.Contains("credentialalias", StringComparison.Ordinal) ||
            normalized.Contains("connectionstring", StringComparison.Ordinal);
    }

    private static object? Redact(object? value, ISet<object> ancestors, int depth)
    {
        if (value is null || value is string || value.GetType().IsValueType)
        {
            return value;
        }

        if (depth >= MaximumDepth)
        {
            return TruncatedValue;
        }

        if (!ancestors.Add(value))
        {
            return CircularValue;
        }

        try
        {
            if (value is IDictionary dictionary)
            {
                var redacted = new Dictionary<string, object?>(StringComparer.Ordinal);
                int entryCount = 0;
                foreach (DictionaryEntry entry in dictionary)
                {
                    if (entryCount++ == MaximumCollectionItems)
                    {
                        break;
                    }

                    string key = Convert.ToString(entry.Key, CultureInfo.InvariantCulture) ?? string.Empty;
                    redacted[key] = IsSensitiveProperty(key) ? RedactedValue : Redact(entry.Value, ancestors, depth + 1);
                }

                if (dictionary.Count > MaximumCollectionItems)
                {
                    redacted["[truncated]"] = TruncatedValue;
                }

                return redacted;
            }

            if (value is IEnumerable sequence)
            {
                var redacted = new List<object?>();
                foreach (object? item in sequence)
                {
                    if (redacted.Count == MaximumCollectionItems)
                    {
                        redacted.Add(TruncatedValue);
                        break;
                    }

                    redacted.Add(Redact(item, ancestors, depth + 1));
                }

                return redacted;
            }

            PropertyInfo[] properties = value.GetType()
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(property => property.CanRead && property.GetIndexParameters().Length == 0)
                .ToArray();
            if (properties.Length == 0)
            {
                return UnavailableValue;
            }

            var structured = new Dictionary<string, object?>(StringComparer.Ordinal);
            foreach (PropertyInfo property in properties.Take(MaximumCollectionItems))
            {
                structured[property.Name] = IsSensitiveProperty(property.Name)
                    ? RedactedValue
                    : ReadAndRedact(property, value, ancestors, depth + 1);
            }

            if (properties.Length > MaximumCollectionItems)
            {
                structured["[truncated]"] = TruncatedValue;
            }

            return structured;
        }
        finally
        {
            ancestors.Remove(value);
        }
    }

    private static object? ReadAndRedact(PropertyInfo property, object value, ISet<object> ancestors, int depth)
    {
        try
        {
            return Redact(property.GetValue(value), ancestors, depth);
        }
        catch (TargetInvocationException)
        {
            return UnavailableValue;
        }
    }

    private static LogEventPropertyValue RedactEventValue(LogEventPropertyValue value, int depth)
    {
        if (depth >= MaximumDepth)
        {
            return new ScalarValue(TruncatedValue);
        }

        return value switch
        {
            SequenceValue sequence => new SequenceValue(sequence.Elements.Select(element => RedactEventValue(element, depth + 1))),
            StructureValue structure => new StructureValue(
                structure.Properties.Select(property => new LogEventProperty(
                    property.Name,
                    RedactEventProperty(property.Name, property.Value, depth + 1))),
                structure.TypeTag),
            DictionaryValue dictionary => new DictionaryValue(dictionary.Elements.Select(element => new KeyValuePair<ScalarValue, LogEventPropertyValue>(
                element.Key,
                RedactEventProperty(KeyName(element.Key), element.Value, depth + 1)))),
            _ => value,
        };
    }

    private static string KeyName(ScalarValue key)
    {
        return Convert.ToString(key.Value, CultureInfo.InvariantCulture) ?? string.Empty;
    }
}

public sealed class LogRedactionEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        foreach ((string name, LogEventPropertyValue value) in logEvent.Properties.ToArray())
        {
            logEvent.AddOrUpdateProperty(new LogEventProperty(name, LogRedactionPolicy.RedactEventProperty(name, value)));
        }
    }
}
