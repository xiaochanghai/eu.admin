using Dapper;
using System.Data;

namespace EU.Core.Extensions.Middlewares;

// 自定义 TypeHandler
public class GuidTypeHandler : SqlMapper.TypeHandler<Guid>
{
    public override void SetValue(IDbDataParameter parameter, Guid value)
    {
        parameter.Value = value.ToString();
    }

    public override Guid Parse(object value)
    {
        if (value is Guid guid) return guid;
        if (value is string str && Guid.TryParse(str, out var g)) return g;
        throw new InvalidCastException($"Unable to convert {value?.GetType().Name ?? "null"} to Guid");
    }
}

// 同样处理 Nullable<Guid>
public class NullableGuidTypeHandler : SqlMapper.TypeHandler<Guid?>
{
    public override void SetValue(IDbDataParameter parameter, Guid? value)
    {
        parameter.Value = value?.ToString() ?? (object)DBNull.Value;
    }

    public override Guid? Parse(object value)
    {
        if (value == null || value == DBNull.Value) return null;
        if (value is Guid guid) return guid;
        if (value is string str && Guid.TryParse(str, out var g)) return g;
        return null; // 或抛异常
    }
}
