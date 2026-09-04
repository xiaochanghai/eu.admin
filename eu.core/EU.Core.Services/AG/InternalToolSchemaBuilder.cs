#nullable enable

using System.Text.Json;

namespace EU.Core.Services;

#region 文件职责：InternalToolSchemaBuilder 职责实现

internal static class InternalToolSchemaBuilder
{
    #region 内部工具参数架构

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

#endregion
