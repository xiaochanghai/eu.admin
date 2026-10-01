#nullable enable
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EU.Core.Services;

/// <summary>四类基础数据的 MCP 查询和校验规则；写入由各业务服务复用标准 CRUD。</summary>
internal static class BasicDataMcpRules
{
    #region 解析字段补丁
    /// <summary>按公开模型拒绝未知字段、空补丁、重复键和类型错误。</summary>
    /// <param name="values">待解析业务字段。</param>
    /// <returns>已验证的字段模型。</returns>
    internal static T Parse<T>(JsonElement values) where T : class
    {
        if (values.ValueKind != JsonValueKind.Object || !values.EnumerateObject().Any()
            || values.EnumerateObject().Select(p => p.Name).Distinct().Count() != values.EnumerateObject().Count())
            throw new ArgumentException("BASIC_DATA_VALUES_INVALID");
        try
        {
            var result = values.Deserialize<T>(new JsonSerializerOptions { UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow })
                ?? throw new JsonException();
            Validate(result);
            return result;
        }
        catch (JsonException) { throw new ArgumentException("BASIC_DATA_VALUES_INVALID"); }
    }
    #endregion

    #region 验证字段
    /// <summary>规范化首尾空白，验证模型长度和必填业务名称。</summary>
    /// <param name="value">字段模型。</param>
    /// <param name="required">必须非空的名称字段。</param>
    internal static void Validate(object value, params string[] required)
    {
        ArgumentNullException.ThrowIfNull(value);
        foreach (var property in value.GetType().GetProperties().Where(p => p.PropertyType == typeof(string) && p.CanWrite))
            if (property.GetValue(value) is string text) property.SetValue(value, text.Trim());
        if (!Validator.TryValidateObject(value, new ValidationContext(value), new List<ValidationResult>(), true)
            || required.Any(name => string.IsNullOrWhiteSpace(value.GetType().GetProperty(name)?.GetValue(value) as string)))
            throw new ArgumentException("BASIC_DATA_VALUES_INVALID");
    }
    #endregion

    #region 转换和部分更新
    /// <summary>转换已有 DTO，不自行实现持久化。</summary>
    /// <param name="value">源实体或模型。</param>
    /// <returns>目标模型。</returns>
    internal static T Convert<T>(object value) => JsonSerializer.SerializeToElement(value).Deserialize<T>()!;

    #endregion

    #region 合并字段补丁
    /// <summary>只合并实际传入的字段，保持其他业务和系统字段原值。</summary>
    /// <param name="existing">原实体。</param>
    /// <param name="values">实际提供的修改字段。</param>
    /// <returns>合并后的编辑模型。</returns>
    internal static TEdit Merge<TEdit, TValues>(object existing, JsonElement values) where TValues : class
    {
        var supplied = Parse<TValues>(values);
        var edit = Convert<TEdit>(existing);
        foreach (var field in values.EnumerateObject())
            typeof(TEdit).GetProperty(field.Name)!.SetValue(edit, typeof(TValues).GetProperty(field.Name)!.GetValue(supplied));
        return edit;
    }
    #endregion

    #region 构建名称条件
    /// <summary>字段名只由服务端常量提供，输入值由 ORM 参数化。</summary>
    /// <param name="field">服务端声明字段。</param>
    /// <param name="value">参数值。</param>
    /// <param name="contains">是否按关键字匹配。</param>
    /// <returns>参数化查询表达式。</returns>
    private static Expression<Func<T, bool>> Match<T>(string field, string value, bool contains = false)
    {
        var row = Expression.Parameter(typeof(T), "row");
        var property = Expression.Property(row, field);
        var body = contains
            ? (Expression)Expression.Call(property, nameof(string.Contains), Type.EmptyTypes, Expression.Constant(value))
            : Expression.Equal(Expression.Call(property, nameof(string.Trim), Type.EmptyTypes), Expression.Constant(value));
        return Expression.Lambda<Func<T, bool>>(body, row);
    }
    #endregion

    #region 查询基础数据
    /// <summary>限制分页、状态与输出字段，不将系统属性暴露给工具。</summary>
    /// <param name="db">当前服务的数据库作用域。</param>
    /// <param name="input">分页条件。</param>
    /// <param name="code">编号字段。</param>
    /// <param name="name">名称字段。</param>
    /// <param name="shortName">可选简称字段。</param>
    /// <param name="token">取消令牌。</param>
    /// <param name="extraOutput">额外只读业务字段。</param>
    /// <returns>白名单分页结果。</returns>
    internal static async Task<PageModel<Dictionary<string, object?>>> Query<T, TValues>(ISqlSugarClient db, BasicDataMcpQuery input, string code, string name, string? shortName, CancellationToken token, string? extraOutput = null) where T : BasePoco, new()
    {
        token.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(input);
        if (input.PageIndex < 1 || input.PageSize is < 1 or > 100 || (long)input.PageIndex * input.PageSize > int.MaxValue
            || input.Code?.Length > 64 || input.Keyword?.Length > 100) throw new ArgumentException("BASIC_DATA_QUERY_INVALID");
        var query = db.Queryable<T>().Where(row => !row.IsDeleted && row.IsActive == true);
        if (!string.IsNullOrWhiteSpace(input.Code)) query = query.Where(Match<T>(code, input.Code.Trim()));
        if (!string.IsNullOrWhiteSpace(input.Keyword))
        {
            var predicate = Expressionable.Create<T>().Or(Match<T>(name, input.Keyword.Trim(), true));
            if (shortName != null) predicate.Or(Match<T>(shortName, input.Keyword.Trim(), true));
            query = query.Where(predicate.ToExpression());
        }
        int count = await query.Clone().CountAsync(token);
        var rows = await query.OrderBy(row => row.ID).Skip((input.PageIndex - 1) * input.PageSize).Take(input.PageSize).ToListAsync(token);
        var outputFields = typeof(TValues).GetProperties().Select(p => p.Name).Append("ID");
        if (extraOutput != null) outputFields = outputFields.Append(extraOutput);
        var properties = outputFields.Distinct().Select(field => typeof(T).GetProperty(field)!).ToArray();
        return new PageModel<Dictionary<string, object?>>(input.PageIndex, count, input.PageSize,
            rows.Select(row => properties.ToDictionary(property => property.Name, property => property.GetValue(row))).ToList());
    }
    #endregion

    #region 定位唯一目标
    /// <summary>名称精确匹配，多个条件取交集，拒绝无目标和多目标。</summary>
    /// <param name="db">当前服务的数据库作用域。</param>
    /// <param name="target">原名称目标。</param>
    /// <param name="name">名称字段。</param>
    /// <param name="shortName">可选简称字段。</param>
    /// <param name="maxLength">名称长度上限。</param>
    /// <param name="token">取消令牌。</param>
    /// <returns>唯一有效目标。</returns>
    internal static async Task<T> Resolve<T>(ISqlSugarClient db, BasicDataMcpTarget target, string name, string? shortName, int maxLength, CancellationToken token) where T : BasePoco, new()
    {
        token.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(target);
        string? full = target.Name?.Trim(), shortValue = target.ShortName?.Trim();
        if ((string.IsNullOrEmpty(full) && string.IsNullOrEmpty(shortValue)) || full?.Length > maxLength || shortValue?.Length > 32
            || (shortName == null && target.ShortName != null)) throw new ArgumentException("BASIC_DATA_TARGET_INVALID");
        var query = db.Queryable<T>().Where(row => !row.IsDeleted && row.IsActive == true);
        if (!string.IsNullOrEmpty(full)) query = query.Where(Match<T>(name, full));
        if (!string.IsNullOrEmpty(shortValue)) query = query.Where(Match<T>(shortName!, shortValue));
        var rows = await query.Take(2).ToListAsync(token);
        if (rows.Count == 0) throw new ArgumentException("BASIC_DATA_NOT_FOUND");
        if (rows.Count != 1) throw new ArgumentException("BASIC_DATA_TARGET_AMBIGUOUS");
        return rows[0];
    }
    #endregion

    #region 应用层查重
    /// <summary>分别校验业务编号和名称等非空值，包含停用记录，排除逻辑删除和自身。</summary>
    /// <param name="db">当前服务的数据库作用域。</param>
    /// <param name="values">待保存业务字段。</param>
    /// <param name="exclude">修改时排除的自身 ID。</param>
    /// <param name="token">取消令牌。</param>
    /// <param name="fields">分别检查的编号和名称字段。</param>
    /// <returns>查重完成任务，重复时抛出异常。</returns>
    internal static async Task Unique<T>(ISqlSugarClient db, object values, Guid? exclude, CancellationToken token, params string[] fields) where T : BasePoco, new()
    {
        token.ThrowIfCancellationRequested();
        foreach (string field in fields)
        {
            var text = values.GetType().GetProperty(field)!.GetValue(values) as string;
            if (string.IsNullOrWhiteSpace(text)) continue;
            if (await db.Queryable<T>().Where(row => !row.IsDeleted).WhereIF(exclude.HasValue, row => row.ID != exclude)
                .Where(Match<T>(field, text.Trim())).CountAsync(token) > 0)
                throw new ArgumentException($"BASIC_DATA_DUPLICATE:{field}");
        }
    }
    #endregion

    #region 检查引用
    /// <summary>拒绝被已知未删除业务记录引用的目标，包括停用的引用方。</summary>
    /// <param name="db">当前服务的数据库作用域。</param>
    /// <param name="predicate">已知引用条件。</param>
    /// <param name="token">取消令牌。</param>
    /// <returns>引用检查任务，存在引用时抛出异常。</returns>
    internal static async Task NoReference<T>(ISqlSugarClient db, Expression<Func<T, bool>> predicate, CancellationToken token) where T : BasePoco, new()
    {
        if (await db.Queryable<T>().Where(row => !row.IsDeleted).Where(predicate).CountAsync(token) > 0)
            throw new ArgumentException("BASIC_DATA_IN_USE");
    }
    #endregion
}
