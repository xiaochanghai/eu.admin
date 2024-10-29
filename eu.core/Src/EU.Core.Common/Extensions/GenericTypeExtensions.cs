using Newtonsoft.Json.Linq;
using System.Data;
using System.Linq.Expressions;
using System.Reflection;

namespace EU.Core.Common.Extensions;

public static class GenericTypeExtensions
{
    public static string GetGenericTypeName(this Type type)
    {
        var typeName = string.Empty;

        if (type.IsGenericType)
        {
            var genericTypes = string.Join(",", type.GetGenericArguments().Select(t => t.Name).ToArray());
            typeName = $"{type.Name.Remove(type.Name.IndexOf('`'))}<{genericTypes}>";
        }
        else
        {
            typeName = type.Name;
        }

        return typeName;
    }

    public static string GetGenericTypeName(this object @object)
    {
        return @object.GetType().GetGenericTypeName();
    }

    /// <summary>
    /// 判断类型是否实现某个泛型
    /// </summary>
    /// <param name="type">类型</param>
    /// <param name="generic">泛型类型</param>
    /// <returns>bool</returns>
    public static bool HasImplementedRawGeneric(this Type type, Type generic)
    {
        // 检查接口类型
        var isTheRawGenericType = type.GetInterfaces().Any(IsTheRawGenericType);
        if (isTheRawGenericType) return true;

        // 检查类型
        while (type != null && type != typeof(object))
        {
            isTheRawGenericType = IsTheRawGenericType(type);
            if (isTheRawGenericType) return true;
            type = type.BaseType;
        }

        return false;

        // 判断逻辑
        bool IsTheRawGenericType(Type t) => generic == (t.IsGenericType ? t.GetGenericTypeDefinition() : t);
    }

    /// <summary>
    /// 复制 <paramref name="source"/> 的副本
    /// </summary>
    /// <typeparam name="TTarget"></typeparam>
    /// <typeparam name="TSource"></typeparam>
    /// <param name="target"></param>
    /// <param name="source"></param>
    /// <returns></returns>
    public static T Clone<T>(T source)
        where T : class
    {
        var text = System.Text.Json.JsonSerializer.Serialize(source, typeof(T));
        return System.Text.Json.JsonSerializer.Deserialize<T>(text);
    }

    public static TTarget CloneTo<TTarget>(this object source, params object[] args)
        where TTarget : class
    {
        var target = (TTarget)Activator.CreateInstance(typeof(TTarget), args);

        return target.CopyFrom(source);
    }

    /// <summary>
    /// 将 <paramref name="source"/> 的值复制到 <paramref name="target"/>,
    /// 仅复制同名的属性或字段
    /// </summary>
    /// <typeparam name="TTarget"></typeparam>
    /// <typeparam name="TSource"></typeparam>
    /// <param name="target"></param>
    /// <param name="source"></param>
    /// <returns></returns>
    public static TTarget CopyFrom<TTarget, TSource>(this TTarget target, TSource source)
        where TTarget : class
        where TSource : class
    {
        foreach (var member in source.GetType().GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
        {
            switch (member)
            {
                case PropertyInfo property:
                    target.PrivateSet(member.Name, property.GetValue(source));
                    break;
                case FieldInfo field:
                    target.PrivateSet(member.Name, field.GetValue(source));
                    break;
                default:
                    break;
            }
        }
        return target;
    }

    /// <summary>
    /// 为对象的指定属性或字段赋值
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="source">对象</param>
    /// <param name="name">属性或字段名称</param>
    /// <param name="value">值</param>
    /// <returns>当前对象</returns>
    public static T PrivateSet<T>(this T source, string name, object value) where T : class
    {
        if (source != null && !string.IsNullOrEmpty(name))
        {
            Type t = typeof(T);
            var members = t.GetMember(name, MemberTypes.Property | MemberTypes.Field, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (var member in members)
            {
                switch (member)
                {
                    case PropertyInfo property:
                        property.SetValue(source, value);
                        break;
                    case FieldInfo field:
                        field.SetValue(source, value);
                        break;
                    default:
                        break;
                }
            }
        }
        return source;
    }

    /// <summary>
    /// 为对象的指定属性或字段赋值
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="TKey"></typeparam>
    /// <param name="source">对象</param>
    /// <param name="expression">选择了某个属性或字段的表达式</param>
    /// <param name="value">值</param>
    /// <returns>当前对象</returns>
    public static T PrivateSet<T, TKey>(this T source, Expression<Func<T, TKey>> expression, TKey value) where T : class
    {
        if (source != null && expression != null)
        {
            if (expression.Body is MemberExpression m && m.Member != null)
            {
                switch (m.Member)
                {
                    case PropertyInfo property:
                        property.SetValue(source, value);
                        break;
                    case FieldInfo field:
                        field.SetValue(source, value);
                        break;
                    default:
                        break;
                }
            }
        }
        return source;
    }

    /// <summary>
    /// 是否为NULL
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="entity"></param>
    /// <returns></returns>
    public static bool IsNull<T>(this T entity) where T : class
    {
        return entity == null;
    }

    /// <summary>
    /// 获取 <typeparamref name="TKey"/> 类型的值
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <param name="entity"></param>
    /// <param name="field"></param>
    /// <returns></returns>
    public static TKey GetValueFromField<T, TKey>(this T entity, string field) where T : class
    {
        if (!entity.HasField(field))
            return default;
        return (TKey)entity.GetType().GetProperty(field).GetValue(entity);
    }

    /// <summary>
    /// 获取object类型值
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="entity"></param>
    /// <param name="field"></param>
    /// <returns></returns>
    public static object GetValueFromField<T>(this T entity, string field) where T : class
    {
        if (!entity.HasField(field))
            return null;
        return typeof(T).GetProperties().FirstOrDefault(p => p.Name == field)?.GetValue(entity, null);
    }

    /// <summary>
    /// 获取string类型值
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="entity"></param>
    /// <param name="field"></param>
    /// <returns></returns>
    public static string GetStringValueFromField<T>(this T entity, string field) where T : class
    {
        if (!entity.HasField(field))
            return null;
        var value = entity.GetValueFromField(field);
        return value == null ? string.Empty : value.ToString();
    }

    /// <summary>
    /// 获取JObject类型值
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="entity"></param>
    /// <param name="field"></param>
    /// <returns></returns>
    public static JObject GetJsonValueFromField<T>(this T entity, string field) where T : class
    {
        if (!entity.HasField(field))
            return null;
        var value = entity.GetValueFromField(field);
        return value == null ? null : Newtonsoft.Json.JsonConvert.DeserializeObject<JObject>(value.ToString());
    }

    /// <summary>
    /// 获取int类型值
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="entity"></param>
    /// <param name="field"></param>
    /// <returns></returns>
    public static int GetIntValueFromField<T>(this T entity, string field) where T : class
    {
        if (!entity.HasField(field))
            return 0;
        var value = entity.GetValueFromField(field);
        return value == null ? 0 : int.Parse(value.ToString());
    }

    /// <summary>
    /// 获取double类型值
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="entity"></param>
    /// <param name="field"></param>
    /// <returns></returns>
    public static double GetDoubleValueFromField<T>(this T entity, string field) where T : class
    {
        if (!entity.HasField(field))
            return 0;
        var value = entity.GetValueFromField(field);
        return value == null ? 0 : double.Parse(value.ToString());
    }

    /// <summary>
    /// 获取DateTime类型值
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="entity"></param>
    /// <param name="field"></param>
    /// <returns></returns>
    public static DateTime GetDateTimeValueFromFieldNotNull<T>(this T entity, string field) where T : class
    {
        if (!entity.HasField(field))
            return DateTime.MaxValue;
        var value = entity.GetValueFromField(field);
        if (value == null || string.IsNullOrEmpty(value.ToString()))
            return DateTime.MaxValue;
        return DateTime.Parse(value.ToString());
    }

    /// <summary>
    /// 获取DateTime?类型值
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="entity"></param>
    /// <param name="field"></param>
    /// <returns></returns>
    public static DateTime? GetDateTimeValueFromField<T>(this T entity, string field) where T : class
    {
        if (!entity.HasField(field))
        {
            return null;
        }
        var value = entity.GetValueFromField(field);
        if (value == null || string.IsNullOrEmpty(value.ToString()))
            return null;
        return DateTime.Parse(value.ToString());
    }

    /// <summary>
    /// 判断是否为DateTime类型
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="entity"></param>
    /// <param name="field"></param>
    /// <returns></returns>
    public static bool FieldTypeIsDateTime<T>(this T entity, string field) where T : class
    {
        if (!entity.HasField(field))
        {
            return false;
        }
        var t = typeof(T).GetProperties().FirstOrDefault(p => p.Name == field).PropertyType;
        return t == typeof(DateTime) || t == typeof(DateTime?);
    }

    /// <summary>
    /// 判断是否为数值类型
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="entity"></param>
    /// <param name="field"></param>
    /// <returns></returns>
    public static bool FieldTypeIsNumber<T>(this T entity, string field) where T : class
    {
        if (!entity.HasField(field))
        {
            return false;
        }
        var t = typeof(T).GetProperties().FirstOrDefault(p => p.Name == field).PropertyType;
        return t == typeof(int) || t == typeof(int?)
            || t == typeof(double) || t == typeof(double?)
            || t == typeof(decimal) || t == typeof(decimal?)
            || t == typeof(long) || t == typeof(long?)
            || t == typeof(float) || t == typeof(float?);
    }

    /// <summary>
    /// 赋值
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="entity"></param>
    /// <param name="field"></param>
    /// <param name="value"></param>
    public static void SetValueForField<T>(this T entity, string field, object value) where T : class
    {
        if (!entity.HasField(field))
        {
            return;
        }
        typeof(T).GetProperties().FirstOrDefault(p => p.Name == field).SetValue(entity, value);
    }

public static bool Equal<T>(this T x, T y)
    {
        return ((IComparable)(x)).CompareTo(y) == 0;
    }

    #region ToDictionary
    /// <summary>
    /// 将实体指定的字段写入字典
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="t"></param>
    /// <param name="expression"></param>
    /// <returns></returns>

    public static Dictionary<string, object> ToDictionary<T>(this T t, Expression<Func<T, object>> expression) where T : class
    {
        Dictionary<string, object> dic = new Dictionary<string, object>();
        string[] fields = expression.GetExpressionToArray();
        PropertyInfo[] properties = expression == null ? t.GetType().GetProperties() : t.GetType().GetProperties().Where(x => fields.Contains(x.Name)).ToArray();

        foreach (var property in properties)
        {
            var value = property.GetValue(t, null);
            dic.Add(property.Name, value != null ? value.ToString() : "");
        }
        return dic;
    }

    public static Dictionary<string, string> ToDictionary<TInterface, T>(this TInterface t, Dictionary<string, string> dic = null) where T : class, TInterface
    {
        if (dic == null)
            dic = new Dictionary<string, string>();
        var properties = typeof(T).GetProperties();
        foreach (var property in properties)
        {
            var value = property.GetValue(t, null);
            if (value == null) continue;
            dic.Add(property.Name, value != null ? value.ToString() : "");
        }
        return dic;
    }

    #endregion


    public static DataTable ToDataTable<T>(this IEnumerable<T> source, Expression<Func<T, object>> columns = null, bool contianKey = true)
    {
        DataTable dtReturn = new DataTable();
        if (source == null) return dtReturn;

        PropertyInfo[] oProps = typeof(T).GetProperties()
            .Where(x => x.PropertyType.Name != "List`1").ToArray();
        if (columns != null)
        {
            string[] columnArray = columns.GetExpressionToArray();
            oProps = oProps.Where(x => columnArray.Contains(x.Name)).ToArray();
        }
        //移除自增主键
        PropertyInfo keyType = oProps.GetKeyProperty();// oProps.GetKeyProperty()?.PropertyType;
        if (!contianKey && keyType != null && (keyType.PropertyType == typeof(int) || keyType.PropertyType == typeof(long)))
        {
            oProps = oProps.Where(x => x.Name != keyType.Name).ToArray();
        }

        foreach (var pi in oProps)
        {
            var colType = pi.PropertyType;

            if ((colType.IsGenericType) && (colType.GetGenericTypeDefinition() == typeof(Nullable<>)))
            {
                colType = colType.GetGenericArguments()[0];
            }

            dtReturn.Columns.Add(new DataColumn(pi.Name, colType));
        }
        foreach (var rec in source)
        {
            var dr = dtReturn.NewRow();
            foreach (var pi in oProps)
            {
                dr[pi.Name] = pi.GetValue(rec, null) == null
                    ? DBNull.Value
                    : pi.GetValue
                        (rec, null);
            }
            dtReturn.Rows.Add(dr);
        }
        return dtReturn;
    }

    /// <summary>
    /// 是否包含该字段
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="entity"></param>
    /// <param name="field"></param>
    /// <returns></returns>
    public static bool HasField<T>(this T entity, string field) where T : class
    {
        return typeof(T).GetProperties().Any(p => p.Name == field);
    }
}