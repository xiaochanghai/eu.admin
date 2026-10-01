using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace EU.Core.Api.MCP.Attributes;

/// <summary>嵌套对象字段选择和必填约束，不代替工具运行时校验。</summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class McpObjectFieldsAttribute : Attribute
{
    /// <summary>仅发布属性类型自身声明的字段，排除继承的系统字段。</summary>
    public bool DeclaredOnly { get; set; }
    /// <summary>嵌套对象中必须出现的 JSON 字段名。</summary>
    public string[] Required { get; set; } = [];
    /// <summary>嵌套对象至少提供的字段数。</summary>
    public int MinProperties { get; set; }
}

/// <summary>显式启用的模型 Schema 生成器，旧工具继续使用旧生成规则。</summary>
internal static class McpSchemaBuilder
{
    public static object Build(Type type) => BuildType(type, new HashSet<Type>(), false);

    private static Dictionary<string, object> BuildType(Type declaredType, HashSet<Type> path, bool nullable, McpObjectFieldsAttribute? fields = null)
    {
        var type = Nullable.GetUnderlyingType(declaredType) ?? declaredType;
        var schema = new Dictionary<string, object>();
        string kind;
        if (type == typeof(string) || type == typeof(Guid) || type == typeof(DateTime) || type == typeof(DateTimeOffset))
        {
            kind = "string";
            if (type == typeof(Guid)) schema["format"] = "uuid";
            if (type == typeof(DateTime) || type == typeof(DateTimeOffset)) schema["format"] = "date-time";
        }
        else if (type == typeof(bool)) kind = "boolean";
        else if (type == typeof(decimal) || type == typeof(double) || type == typeof(float)) kind = "number";
        else if (type == typeof(byte) || type == typeof(sbyte) || type == typeof(short) || type == typeof(ushort)
            || type == typeof(int) || type == typeof(uint) || type == typeof(long) || type == typeof(ulong)) kind = "integer";
        else
        {
            if (!path.Add(type)) throw new InvalidOperationException($"Recursive MCP schema is unsupported: {type.Name}");
            try
            {
                if (type.IsArray)
                {
                    kind = "array";
                    schema["items"] = BuildType(type.GetElementType()!, path, false);
                }
                else
                {
                    if (type == typeof(object) || type.IsEnum || typeof(System.Collections.IEnumerable).IsAssignableFrom(type))
                        throw new InvalidOperationException($"Unsupported MCP schema type: {type.Name}");
                    kind = "object";
                    var flags = BindingFlags.Public | BindingFlags.Instance;
                    if (fields?.DeclaredOnly == true) flags |= BindingFlags.DeclaredOnly;
                    var properties = new Dictionary<string, object>();
                    var required = new HashSet<string>(fields?.Required ?? []);
                    var nullability = new NullabilityInfoContext();
                    foreach (var property in type.GetProperties(flags).Where(p => p.GetIndexParameters().Length == 0 && p.CanRead))
                    {
                        if (property.GetCustomAttribute<JsonIgnoreAttribute>()?.Condition == JsonIgnoreCondition.Always) continue;
                        string name = property.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ?? property.Name;
                        bool isRequired = property.IsDefined(typeof(RequiredAttribute));
                        if (isRequired) required.Add(name);
                        bool allowsNull = !isRequired && (Nullable.GetUnderlyingType(property.PropertyType) != null
                            || !property.PropertyType.IsValueType && nullability.Create(property).ReadState != NullabilityState.NotNull);
                        var child = BuildType(property.PropertyType, path, allowsNull, property.GetCustomAttribute<McpObjectFieldsAttribute>());
                        if (property.GetCustomAttribute<DescriptionAttribute>() is { } description) child["description"] = description.Description;
                        if (property.GetCustomAttribute<MaxLengthAttribute>() is { } length)
                            child[property.PropertyType == typeof(string) ? "maxLength" : "maxItems"] = length.Length;
                        properties.Add(name, child);
                    }
                    if (required.Any(name => !properties.ContainsKey(name))) throw new InvalidOperationException("MCP required field is not declared.");
                    schema["properties"] = properties;
                    schema["additionalProperties"] = false;
                    schema["required"] = required.ToArray();
                    if (fields?.MinProperties > 0) schema["minProperties"] = fields.MinProperties;
                }
            }
            finally { path.Remove(type); }
        }
        schema["type"] = nullable ? new[] { kind, "null" } : kind;
        return schema;
    }
}
