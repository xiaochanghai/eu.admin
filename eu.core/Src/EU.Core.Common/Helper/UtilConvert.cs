using Newtonsoft.Json;

namespace EU.Core;

/// <summary>
/// 对象类型转换扩展方法工具类
/// </summary>
public static class UtilConvert
{
    #region 数值类型转换

    /// <summary>
    /// 将对象转换为整型，转换失败返回 0
    /// </summary>
    /// <param name="thisValue">待转换的对象</param>
    /// <returns>转换后的整型值</returns>
    public static int ObjToInt(this object thisValue)
    {
        if (thisValue == null || thisValue == DBNull.Value)
            return 0;

        return int.TryParse(thisValue.ToString(), out int result) ? result : 0;
    }

    /// <summary>
    /// 将对象转换为整型，转换失败返回指定的默认值
    /// </summary>
    /// <param name="thisValue">待转换的对象</param>
    /// <param name="errorValue">转换失败时返回的默认值</param>
    /// <returns>转换后的整型值</returns>
    public static int ObjToInt(this object thisValue, int errorValue)
    {
        if (thisValue == null || thisValue == DBNull.Value)
            return errorValue;

        return int.TryParse(thisValue.ToString(), out int result) ? result : errorValue;
    }

    /// <summary>
    /// 将对象转换为长整型，转换失败返回 0
    /// </summary>
    /// <param name="thisValue">待转换的对象</param>
    /// <returns>转换后的长整型值</returns>
    public static long ObjToLong(this object thisValue)
    {
        if (thisValue == null || thisValue == DBNull.Value)
            return 0;

        return long.TryParse(thisValue.ToString(), out long result) ? result : 0;
    }

    /// <summary>
    /// 将对象转换为 GUID，转换失败返回 null
    /// </summary>
    /// <param name="thisValue">待转换的对象</param>
    /// <returns>转换后的 GUID 值，失败返回 null</returns>
    public static Guid? ObjToGuid(this object thisValue)
    {
        if (thisValue == null || thisValue == DBNull.Value)
            return null;

        return Guid.TryParse(thisValue.ToString(), out Guid result) ? result : null;
    }

    /// <summary>
    /// 将对象转换为浮点数（货币），转换失败返回 0
    /// 注意：建议使用 ObjToDecimal 来处理货币类型
    /// </summary>
    /// <param name="thisValue">待转换的对象</param>
    /// <returns>转换后的浮点数值</returns>
    public static double ObjToMoney(this object thisValue)
    {
        if (thisValue == null || thisValue == DBNull.Value)
            return 0;

        return double.TryParse(thisValue.ToString(), out double result) ? result : 0;
    }

    /// <summary>
    /// 将对象转换为浮点数（货币），转换失败返回指定的默认值
    /// 注意：建议使用 ObjToDecimal 来处理货币类型
    /// </summary>
    /// <param name="thisValue">待转换的对象</param>
    /// <param name="errorValue">转换失败时返回的默认值</param>
    /// <returns>转换后的浮点数值</returns>
    public static double ObjToMoney(this object thisValue, double errorValue)
    {
        if (thisValue == null || thisValue == DBNull.Value)
            return errorValue;

        return double.TryParse(thisValue.ToString(), out double result) ? result : errorValue;
    }

    /// <summary>
    /// 将对象转换为十进制数（推荐用于货币类型），转换失败返回 0
    /// </summary>
    /// <param name="thisValue">待转换的对象</param>
    /// <returns>转换后的十进制数值</returns>
    public static decimal ObjToDecimal(this object thisValue)
    {
        if (thisValue == null || thisValue == DBNull.Value)
            return 0;

        return decimal.TryParse(thisValue.ToString(), out decimal result) ? result : 0;
    }

    /// <summary>
    /// 将对象转换为十进制数（推荐用于货币类型），转换失败返回指定的默认值
    /// </summary>
    /// <param name="thisValue">待转换的对象</param>
    /// <param name="errorValue">转换失败时返回的默认值</param>
    /// <returns>转换后的十进制数值</returns>
    public static decimal ObjToDecimal(this object thisValue, decimal errorValue)
    {
        if (thisValue == null || thisValue == DBNull.Value)
            return errorValue;

        return decimal.TryParse(thisValue.ToString(), out decimal result) ? result : errorValue;
    }

    #endregion

    #region 字符串转换和验证

    /// <summary>
    /// 将对象转换为字符串并去除首尾空白字符，null 返回空字符串
    /// </summary>
    /// <param name="thisValue">待转换的对象</param>
    /// <returns>转换后的字符串</returns>
    public static string ObjToString(this object thisValue)
    {
        if (thisValue == null || thisValue == DBNull.Value)
            return string.Empty;

        return thisValue.ToString().Trim();
    }

    /// <summary>
    /// 将对象转换为字符串并去除首尾空白字符，null 返回指定的默认值
    /// </summary>
    /// <param name="thisValue">待转换的对象</param>
    /// <param name="errorValue">转换失败时返回的默认值</param>
    /// <returns>转换后的字符串</returns>
    public static string ObjToString(this object thisValue, string errorValue)
    {
        if (thisValue == null || thisValue == DBNull.Value)
            return errorValue;

        return thisValue.ToString().Trim();
    }

    /// <summary>
    /// 检查对象是否为 null、空字符串或特殊字符串（"undefined"、"null"）
    /// </summary>
    /// <param name="thisValue">待检查的对象</param>
    /// <returns>如果不为空返回 true，否则返回 false</returns>
    public static bool IsNotEmptyOrNull(this object thisValue)
    {
        var str = ObjToString(thisValue);
        return str != string.Empty && str != "undefined" && str != "null";
    }

    /// <summary>
    /// 检查对象是否为 null、DBNull.Value 或空白字符串
    /// </summary>
    /// <param name="thisValue">待检查的对象</param>
    /// <returns>如果为空返回 true，否则返回 false</returns>
    public static bool IsNullOrEmpty(this object thisValue)
    {
        if (thisValue == null || thisValue == DBNull.Value)
            return true;

        return string.IsNullOrWhiteSpace(thisValue.ToString());
    }

    #endregion

    #region 日期时间转换

    /// <summary>
    /// 将对象转换为日期时间，支持日期字符串和时间戳（秒）转换
    /// </summary>
    /// <param name="thisValue">待转换的对象（日期字符串或时间戳）</param>
    /// <returns>转换后的日期时间，失败返回 DateTime.MinValue</returns>
    public static DateTime ObjToDate(this object thisValue)
    {
        if (thisValue == null || thisValue == DBNull.Value)
            return DateTime.MinValue;

        // 尝试直接解析为日期时间
        if (DateTime.TryParse(thisValue.ToString(), out DateTime result))
            return result;

        // 尝试作为时间戳（秒）解析
        var seconds = ObjToLong(thisValue);
        if (seconds > 0)
        {
            var startTime = TimeZoneInfo.ConvertTime(new DateTime(1970, 1, 1), TimeZoneInfo.Local);
            return startTime.AddSeconds(seconds);
        }

        return DateTime.MinValue;
    }

    /// <summary>
    /// 将对象转换为日期时间，转换失败返回指定的默认值
    /// </summary>
    /// <param name="thisValue">待转换的对象</param>
    /// <param name="errorValue">转换失败时返回的默认值</param>
    /// <returns>转换后的日期时间</returns>
    public static DateTime ObjToDate(this object thisValue, DateTime errorValue)
    {
        if (thisValue == null || thisValue == DBNull.Value)
            return errorValue;

        return DateTime.TryParse(thisValue.ToString(), out DateTime result) ? result : errorValue;
    }

    /// <summary>
    /// 将日期时间转换为时间戳（秒）</summary>
    /// <param name="thisValue">日期时间对象</param>
    /// <returns>时间戳字符串（自 1970-01-01 起的秒数）</returns>
    public static string DateToTimeStamp(this DateTime thisValue)
    {
        TimeSpan ts = thisValue - new DateTime(1970, 1, 1, 0, 0, 0, 0);
        return Convert.ToInt64(ts.TotalSeconds).ToString();
    }

    #endregion

    #region 布尔值转换

    /// <summary>
    /// 将对象转换为布尔值，转换失败返回 false
    /// </summary>
    /// <param name="thisValue">待转换的对象</param>
    /// <returns>转换后的布尔值</returns>
    public static bool ObjToBool(this object thisValue)
    {
        if (thisValue == null || thisValue == DBNull.Value)
            return false;

        return bool.TryParse(thisValue.ToString(), out bool result) && result;
    }

    #endregion

    #region 高级类型转换

    /// <summary>
    /// 将对象动态转换为指定类型
    /// </summary>
    /// <param name="value">待转换的对象</param>
    /// <param name="type">目标类型</param>
    /// <returns>转换后的对象</returns>
    public static object ChangeType(this object value, Type type)
    {
        if (value == null && type.IsGenericType)
            return Activator.CreateInstance(type);

        if (value == null)
            return null;

        if (type == value.GetType())
            return value;

        if (type.IsEnum)
        {
            if (value is string stringValue)
                return Enum.Parse(type, stringValue);
            else
                return Enum.ToObject(type, value);
        }

        if (!type.IsInterface && type.IsGenericType)
        {
            Type innerType = type.GetGenericArguments()[0];
            object innerValue = ChangeType(value, innerType);
            return Activator.CreateInstance(type, new object[] { innerValue });
        }

        if (value is string && type == typeof(Guid))
            return new Guid(value as string);

        if (value is string && type == typeof(Version))
            return new Version(value as string);

        if (!(value is IConvertible))
            return value;

        return Convert.ChangeType(value, type);
    }

    /// <summary>
    /// 将对象转换为指定类型的列表，支持解析括号包裹的逗号分隔值
    /// </summary>
    /// <param name="value">待转换的对象（格式如 "(value1,value2,value3)"）</param>
    /// <param name="type">列表元素的目标类型</param>
    /// <returns>转换后的列表对象</returns>
    public static object ChangeTypeList(this object value, Type type)
    {
        if (value == null)
            return default;

        var gt = typeof(List<>).MakeGenericType(type);
        dynamic lis = Activator.CreateInstance(gt);

        var addMethod = gt.GetMethod("Add");
        string values = value.ToString();

        if (values != null && values.StartsWith("(") && values.EndsWith(")"))
        {
            string[] splits;
            if (values.Contains("\",\""))
            {
                splits = values.Remove(values.Length - 2, 2)
                    .Remove(0, 2)
                    .Split("\",\"");
            }
            else
            {
                splits = values.Remove(0, 1)
                    .Remove(values.Length - 2, 1)
                    .Split(",");
            }

            foreach (var split in splits)
            {
                var str = split;
                if (split.StartsWith("\"") && split.EndsWith("\""))
                {
                    str = split.Remove(0, 1)
                        .Remove(split.Length - 2, 1);
                }

                addMethod.Invoke(lis, new object[] { ChangeType(str, type) });
            }
        }

        return lis;
    }

    #endregion

    #region 序列化和集合操作

    /// <summary>
    /// 将对象序列化为 JSON 字符串
    /// </summary>
    /// <param name="value">待序列化的对象</param>
    /// <returns>JSON 字符串</returns>
    public static string ToJson(this object value)
    {
        return JsonConvert.SerializeObject(value);
    }

    /// <summary>
    /// 安全检查集合是否有元素且所有元素不为 null
    /// </summary>
    /// <typeparam name="T">集合元素类型</typeparam>
    /// <param name="source">待检查的集合</param>
    /// <returns>如果集合不为空且所有元素都不为 null 返回 true，否则返回 false</returns>
    public static bool AnyNoException<T>(this ICollection<T> source)
    {
        if (source == null)
            return false;

        return source.Any() && source.All(s => s != null);
    }

    #endregion
}