#nullable enable

using System.Text.Json;

namespace EU.Core.Services;

// 文件职责：InternalToolSchemaBuilder 职责实现

internal static class InternalToolSchemaBuilder
{
    #region 内部工具参数架构

    /// <summary>
    /// 内部工具参数架构
    /// </summary>
    /// <param name="versionPropertyName">保存版本号的属性名称。</param>
    /// <param name="allowedVersionIds">允许访问的版本标识集合。</param>
    /// <param name="valuePropertyName">保存值的属性名称。</param>
    /// <param name="maximumValueCharacters">字段值允许的最大字符数。</param>
    /// <param name="maximumReasonCharacters">原因说明允许的最大字符数。</param>
    /// <returns>限制可选版本标识、输入及原因长度且禁止额外属性的内部工具 JSON Schema。</returns>
    public static string Build(
        string versionPropertyName,
        IReadOnlyList<Guid> allowedVersionIds,
        string valuePropertyName,
        int maximumValueCharacters,
        int maximumReasonCharacters)
    {
        string[] allowed = allowedVersionIds
            .Where(value => value != Guid.Empty)
            .Distinct()
            .OrderBy(value => value)
            .Select(value => value.ToString())
            .ToArray();
        var schema = new Dictionary<string, object?>
        {
            ["type"] = "object",
            ["additionalProperties"] = false,
            ["required"] = new[]
            {
                versionPropertyName,
                valuePropertyName,
                "reason"
            },
            ["properties"] = new Dictionary<string, object?>
            {
                [versionPropertyName] = new Dictionary<string, object?>
                {
                    ["type"] = "string",
                    ["format"] = "uuid",
                    ["enum"] = allowed
                },
                [valuePropertyName] = new Dictionary<string, object?>
                {
                    ["type"] = "string",
                    ["minLength"] = 1,
                    ["maxLength"] = maximumValueCharacters
                },
                ["reason"] = new Dictionary<string, object?>
                {
                    ["type"] = "string",
                    ["minLength"] = 1,
                    ["maxLength"] = maximumReasonCharacters
                }
            }
        };
        return JsonSerializer.Serialize(schema);
    }

    #endregion
}
