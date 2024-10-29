using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using EU.Core.Common.Const;
using EU.Core.Model;
using EU.Core.Model.Models.RootTkey;

namespace EU.Core.Common.Extensions;

public static class EntityProperties
{

    /// <summary>
    /// 获取对象里指定成员名称
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <param name="properties"> 格式 Expression<Func<entityt, object>> exp = x => new { x.字段1, x.字段2 };或x=>x.Name</param>
    /// <returns></returns>
    public static string[] GetExpressionProperty<TEntity>(this Expression<Func<TEntity, object>> properties)
    {
        if (properties == null)
            return new string[] { };
        if (properties.Body is NewExpression)
            return ((NewExpression)properties.Body).Members.Select(x => x.Name).ToArray();
        if (properties.Body is MemberExpression)
            return new string[] { ((MemberExpression)properties.Body).Member.Name };
        if (properties.Body is UnaryExpression)
            return new string[] { ((properties.Body as UnaryExpression).Operand as MemberExpression).Member.Name };
        throw new Exception("未实现的表达式");
    }

    public static Dictionary<string, string> GetColumType(this PropertyInfo[] properties, bool containsKey)
    {
        Dictionary<string, string> dictionary = new Dictionary<string, string>();
        foreach (PropertyInfo property in properties)
        {
            //if (!containsKey && property.IsKey())
            //{
            //    continue;
            //}
            var keyVal = GetColumnType(property, true);
            dictionary.Add(keyVal.Key, keyVal.Value);
        }
        return dictionary;
    }

    private static readonly Dictionary<Type, string> entityMapDbColumnType = new Dictionary<Type, string>() {
                {typeof(int),SqlDbTypeName.Int },
                {typeof(int?),SqlDbTypeName.Int },
                {typeof(long),SqlDbTypeName.BigInt },
                {typeof(long?),SqlDbTypeName.BigInt },
                {typeof(decimal),"decimal(18, 5)" },
                {typeof(decimal?),"decimal(18, 5)"  },
                {typeof(double),"decimal(18, 5)" },
                {typeof(double?),"decimal(18, 5)" },
                {typeof(float),"decimal(18, 5)" },
                {typeof(float?),"decimal(18, 5)" },
                {typeof(Guid),"UniqueIdentifier" },
                {typeof(Guid?),"UniqueIdentifier" },
                {typeof(byte),"tinyint" },
                {typeof(byte?),"tinyint" },
                {typeof(string),"nvarchar" }
    };
    /// <summary>
    /// 返回属性的字段及数据库类型
    /// </summary>
    /// <param name="property"></param>
    /// <param name="lenght">是否包括后字段具体长度:nvarchar(100)</param>
    /// <returns></returns>
    public static KeyValuePair<string, string> GetColumnType(this PropertyInfo property, bool lenght = false)
    {
        string colType = "";
        object objAtrr = property.GetTypeCustomAttributes(typeof(ColumnAttribute), out bool asType);
        if (asType)
        {
            colType = ((ColumnAttribute)objAtrr).TypeName.ToLower();
            if (!string.IsNullOrEmpty(colType))
            {
                //不需要具体长度直接返回
                if (!lenght)
                {
                    return new KeyValuePair<string, string>(property.Name, colType);
                }
                if (colType == "decimal" || colType == "double" || colType == "float")
                {
                    objAtrr = property.GetTypeCustomAttributes(typeof(DisplayFormatAttribute), out asType);
                    colType += "(" + (asType ? ((DisplayFormatAttribute)objAtrr).DataFormatString : "18,5") + ")";

                }
                ///如果是string,根据 varchar或nvarchar判断最大长度
                if (property.PropertyType.ToString() == "System.String")
                {
                    colType = colType.Split("(")[0];
                    objAtrr = property.GetTypeCustomAttributes(typeof(MaxLengthAttribute), out asType);
                    if (asType)
                    {
                        int length = ((MaxLengthAttribute)objAtrr).Length;
                        colType += "(" + (length < 1 || length > (colType.StartsWith("n") ? 8000 : 4000) ? "max" : length.ToString()) + ")";
                    }
                    else
                    {
                        colType += "(max)";
                    }
                }
                return new KeyValuePair<string, string>(property.Name, colType);
            }
        }
        if (entityMapDbColumnType.TryGetValue(property.PropertyType, out string value))
        {
            colType = value;
        }
        else
        {
            colType = SqlDbTypeName.NVarChar;
        }
        if (lenght && colType == SqlDbTypeName.NVarChar)
        {
            colType = "nvarchar(max)";
        }
        return new KeyValuePair<string, string>(property.Name, colType);
    }

    /// <summary>
    ///<param name="sql">要执行的sql语句如：通过EntityToSqlTempName.Temp_Insert0.ToString()字符串占位，生成的的sql语句会把EntityToSqlTempName.Temp_Insert0.ToString()替换成生成的sql临时表数据
    ///    string sql = " ;DELETE FROM " + typeEntity.Name + " where " + typeEntity.GetKeyName() +
    ///      " in (select * from " + EntityToSqlTempName.Temp_Insert0.ToString() + ")";
    /// </param>
    /// </summary>
    /// <param name="array"></param>
    /// <param name="fieldType">指定生成的数组值的类型</param>
    /// <param name="sql"></param>
    /// <returns></returns>
    public static string GetArraySql(this object[] array, FieldType fieldType, string sql)
    {
        if (array == null || array.Count() == 0)
        {
            return string.Empty;
        }
        string columnType = string.Empty;
        List<ArrayEntity> arrrayEntityList = array.Select(x => new ArrayEntity { column1 = x.ToString() }).ToList();
        return arrrayEntityList.GetEntitySql(false, sql, null, null, fieldType);
    }
    /// <summary>
    /// 根据实体获取key的类型，用于update或del操作
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static FieldType GetFieldType(this Type typeEntity)
    {
        FieldType fieldType;
        string columnType = typeEntity.GetProperties().Where(x => x.Name == typeEntity.GetKeyName()).ToList()[0].GetColumnType(false).Value;
        switch (columnType)
        {
            case SqlDbTypeName.Int: fieldType = FieldType.Int; break;
            case SqlDbTypeName.BigInt: fieldType = FieldType.BigInt; break;
            case SqlDbTypeName.VarChar: fieldType = FieldType.VarChar; break;
            case SqlDbTypeName.UniqueIdentifier: fieldType = FieldType.UniqueIdentifier; break;
            default: fieldType = FieldType.NvarChar; break;
        }
        return fieldType;
    }
    public static string GetEntitySql<T>(this IEnumerable<T> entityList,
              bool containsKey = false,
              string sql = null,
              Expression<Func<T, object>> ignoreFileds = null,
              Expression<Func<T, object>> fixedColumns = null,
              FieldType? fieldType = null
              )
    {

        if (entityList == null || entityList.Count() == 0) return "";
        PropertyInfo[] propertyInfo = typeof(T).GetProperties().ToArray();
        if (propertyInfo.Count() == 0)
        {
            propertyInfo = entityList.ToArray()[0].GetType().GetGenericProperties().ToArray();
        }
        propertyInfo = propertyInfo.GetGenericProperties().ToArray();

        string[] arr = null;
        if (fixedColumns != null)
        {
            arr = fixedColumns.GetExpressionToArray();
            PropertyInfo keyProperty = typeof(T).GetKeyProperty();
            propertyInfo = propertyInfo.Where(x => (containsKey && x.Name == keyProperty.Name) || arr.Contains(x.Name)).ToArray();
        }
        if (ignoreFileds != null)
        {
            arr = ignoreFileds.GetExpressionToArray();
            propertyInfo = propertyInfo.Where(x => !arr.Contains(x.Name)).ToArray();
        }

        Dictionary<string, string> dictProperties = propertyInfo.GetColumType(containsKey);
        if (fieldType != null)
        {
            string realType = fieldType.ToString();
            if ((int)fieldType == 0 || (int)fieldType == 1)
            {
                realType += "(max)";
            }
            dictProperties = new Dictionary<string, string> { { dictProperties.Select(x => x.Key).ToList()[0], realType } };
        }
        if (dictProperties.Keys.Count * entityList.Count() > 50 * 3000)
        {
            throw new Exception("写入数据太多,请分开写入。");
        }

        string cols = string.Join(",", dictProperties.Select(c => "[" + c.Key + "]" + " " + c.Value));
        StringBuilder declareTable = new StringBuilder();

        string tempTablbe = "#" + EntityToSqlTempName.TempInsert.ToString();

        declareTable.Append("CREATE TABLE " + tempTablbe + " (" + cols + ")");
        declareTable.Append("\r\n");

        //参数总数量
        int parCount = (dictProperties.Count) * (entityList.Count());
        int takeCount = 0;
        int maxParsCount = 2050;
        if (parCount > maxParsCount)
        {
            //如果参数总数量超过2100，设置每次分批循环写入表的大小
            takeCount = maxParsCount / dictProperties.Count;
        }

        int count = 0;
        StringBuilder stringLeft = new StringBuilder();
        StringBuilder stringCenter = new StringBuilder();
        StringBuilder stringRight = new StringBuilder();

        int index = 0;
        foreach (T entity in entityList)
        {
            //每1000行需要分批写入(数据库限制每批至多写入1000行数据)
            if (index == 0 || index >= 1000 || takeCount - index == 0)
            {
                if (stringLeft.Length > 0)
                {
                    declareTable.AppendLine(
                        stringLeft.Remove(stringLeft.Length - 2, 2).Append("',").ToString() +
                        stringCenter.Remove(stringCenter.Length - 1, 1).Append("',").ToString() +
                        stringRight.Remove(stringRight.Length - 1, 1).ToString());

                    stringLeft.Clear(); stringCenter.Clear(); stringRight.Clear();
                }

                stringLeft.AppendLine("exec sp_executesql N'SET NOCOUNT ON;");
                stringCenter.Append("N'");

                index = 0; count = 0;
            }
            stringLeft.Append(index == 0 ? "; INSERT INTO  " + tempTablbe + "  values (" : " ");
            index++;
            foreach (PropertyInfo property in propertyInfo)
            {
                //if (!containsKey && property.IsKey()) { continue; }
                string par = "@v" + count;
                stringLeft.Append(par + ",");
                stringCenter.Append(par + " " + dictProperties[property.Name] + ",");
                object val = property.GetValue(entity);
                if (val == null)
                {
                    stringRight.Append(par + "=NUll,");
                }
                else
                {
                    stringRight.Append(par + "='" + val.ToString().Replace("'", "''''") + "',");
                }
                count++;
            }
            stringLeft.Remove(stringLeft.Length - 1, 1);
            stringLeft.Append("),(");
        }

        if (stringLeft.Length > 0)
        {
            declareTable.AppendLine(
                stringLeft.Remove(stringLeft.Length - 2, 2).Append("',").ToString() +
                stringCenter.Remove(stringCenter.Length - 1, 1).Append("',").ToString() +
                stringRight.Remove(stringRight.Length - 1, 1).ToString());

            stringLeft.Clear(); stringCenter.Clear(); stringRight.Clear();
        }
        if (!string.IsNullOrEmpty(sql))
        {
            sql = sql.Replace(EntityToSqlTempName.TempInsert.ToString(), tempTablbe);
            declareTable.AppendLine(sql);
        }
        else
        {
            declareTable.AppendLine(" SELECT " + (string.Join(",", fixedColumns?.GetExpressionToArray() ?? new string[] { "*" })) + " FROM " + tempTablbe);
        }


        if (tempTablbe.Substring(0, 1) == "#")
        {
            declareTable.AppendLine("; drop table " + tempTablbe);
        }
        return declareTable.ToString();
    }
    public static string GetKeyName(this Type typeinfo)
    {
        return typeinfo.GetProperties().GetKeyName();
    }
    public static string GetKeyName(this PropertyInfo[] properties)
    {
        return properties.GetKeyName(false);
    }
    /// <summary>
    /// 获取key列名
    /// </summary>
    /// <param name="properties"></param>
    /// <param name="keyType">true获取key对应类型,false返回对象Key的名称</param>
    /// <returns></returns>
    public static string GetKeyName(this PropertyInfo[] properties, bool keyType)
    {
        string keyName = string.Empty;
        foreach (PropertyInfo propertyInfo in properties)
        {
            if (!propertyInfo.IsKey())
                continue;
            if (!keyType)
                return propertyInfo.Name;
            var attributes = propertyInfo.GetCustomAttributes(typeof(ColumnAttribute), false);
            //如果没有ColumnAttribute的需要单独再验证，下面只验证有属性的
            if (attributes.Length > 0)
                return ((ColumnAttribute)attributes[0]).TypeName.ToLower();
            else
                return GetColumType(new PropertyInfo[] { propertyInfo }, true)[propertyInfo.Name];
        }
        return keyName;
    }

    /// <summary>
    /// 获取主键字段
    /// </summary>
    /// <param name="propertyInfo"></param>
    /// <returns></returns>
    public static PropertyInfo GetKeyProperty(this Type entity)
    {
        return entity.GetProperties().GetKeyProperty();
    }
    public static PropertyInfo GetKeyProperty(this PropertyInfo[] properties)
    {
        return properties.Where(c => c.IsKey()).FirstOrDefault();
    }
    public static bool IsKey(this PropertyInfo propertyInfo)
    {
        object[] keyAttributes = propertyInfo.GetCustomAttributes(typeof(KeyAttribute), false);
        if (keyAttributes.Length > 0)
            return true;
        return false;
    }

    /// <summary>
    /// 判断是否包含某个属性：
    /// 如 [Editable(true)]
    //  public string MO { get; set; }包含Editable
    /// </summary>
    /// <param name="propertyInfo"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    public static bool ContainsCustomAttributes(this PropertyInfo propertyInfo, Type type)
    {
        propertyInfo.GetTypeCustomAttributes(type, out bool contains);
        return contains;
    }

    /// <summary>
    /// 获取PropertyInfo指定属性
    /// </summary>
    /// <param name="propertyInfo"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    public static object GetTypeCustomAttributes(this PropertyInfo propertyInfo, Type type, out bool asType)
    {
        object[] attributes = propertyInfo.GetCustomAttributes(type, false);
        if (attributes.Length == 0)
        {
            asType = false;
            return new string[0];
        }
        asType = true;
        return attributes[0];
    }

    /// <summary>
    /// 验证数据库字段类型与值是否正确，
    /// </summary>
    /// <param name="propertyInfo">propertyInfo为当字段，当前字段必须有ColumnAttribute属性,
    /// 如字段:标识为数据库int类型[Column(TypeName="int")]  public int Id { get; set; }
    /// 如果是小数float或Decimal必须对propertyInfo字段加DisplayFormatAttribute属性
    /// </param>
    /// <param name="value"></param>
    /// <returns>IEnumerable<(bool, string, object)> bool成否校验成功,string校验失败信息,object,当前校验的值</returns>
    public static IEnumerable<(bool, string, object)> ValidationValueForDbType(this PropertyInfo propertyInfo, params object[] values)
    {
        string dbTypeName = propertyInfo.GetTypeCustomValue<ColumnAttribute>(c => c.TypeName);
        foreach (object value in values)
        {
            yield return dbTypeName.ValidationVal(value, propertyInfo);
        }
    }


    private static readonly Dictionary<Type, string> ProperWithDbType = new Dictionary<Type, string>() {
        {  typeof(string),SqlDbTypeName.NVarChar },
        { typeof(DateTime),SqlDbTypeName.DateTime},
        {typeof(long),SqlDbTypeName.BigInt },
        {typeof(int),SqlDbTypeName.Int},
        { typeof(decimal),SqlDbTypeName.Decimal },
        { typeof(float),SqlDbTypeName.Float },
        { typeof(double),SqlDbTypeName.Double },
        {  typeof(byte),SqlDbTypeName.Int },//类型待完
        { typeof(Guid),SqlDbTypeName.UniqueIdentifier}
    };
    public static string GetProperWithDbType(this PropertyInfo propertyInfo)
    {
        bool result = ProperWithDbType.TryGetValue(propertyInfo.PropertyType, out string value);
        if (result)
        {
            return value;
        }
        return SqlDbTypeName.NVarChar;
    }

    /// <summary>
    /// 验证数据库字段类型与值是否正确，
    /// </summary>
    /// <param name="dbType">数据库字段类型(如varchar,nvarchar,decimal,不要带后面长度如:varchar(50))</param>
    /// <param name="value">值</param>
    /// <param name="propertyInfo">要验证的类的属性，若不为null，则会判断字符串的长度是否正确</param>
    /// <returns>(bool, string, object)bool成否校验成功,string校验失败信息,object,当前校验的值</returns>
    public static (bool, string, object) ValidationVal(this string dbType, object value, PropertyInfo propertyInfo = null)
    {
        if (string.IsNullOrEmpty(dbType))
        {
            dbType = propertyInfo != null ? propertyInfo.GetProperWithDbType() : SqlDbTypeName.NVarChar;
        }
        dbType = dbType.ToLower();
        string val = value?.ToString();
        //验证长度
        string reslutMsg = string.Empty;
        if (dbType == SqlDbTypeName.Int || dbType == SqlDbTypeName.BigInt)
        {
            if (!value.IsInt())
                reslutMsg = "只能为有效整数";
        }
        else if (dbType == SqlDbTypeName.DateTime
            || dbType == SqlDbTypeName.Date
            || dbType == SqlDbTypeName.SmallDateTime
            || dbType == SqlDbTypeName.SmallDate
            )
        {
            if (!value.IsDate())
                reslutMsg = "必须为日期格式";
        }
        else if (dbType == SqlDbTypeName.Float || dbType == SqlDbTypeName.Decimal || dbType == SqlDbTypeName.Double)
        {
            string formatString = string.Empty;
            if (propertyInfo != null)
                formatString = propertyInfo.GetTypeCustomValue<DisplayFormatAttribute>(x => x.DataFormatString);
            //if (string.IsNullOrEmpty(formatString))
            //    throw new Exception("请对字段" + propertyInfo?.Name + "添加DisplayFormat属性标识");

            if (!val.IsNumber(formatString))
            {
                string[] arr = (formatString ?? "10,0").Split(',');
                reslutMsg = $"整数{arr[0]}最多位,小数最多{arr[1]}位";
            }
        }
        else if (dbType == SqlDbTypeName.UniqueIdentifier)
        {
            if (!val.IsGuid())
            {
                reslutMsg = propertyInfo.Name + "Guid不正确";
            }
        }
        else if (propertyInfo != null
            && (dbType == SqlDbTypeName.VarChar
            || dbType == SqlDbTypeName.NVarChar
            || dbType == SqlDbTypeName.NChar
            || dbType == SqlDbTypeName.Char
            || dbType == SqlDbTypeName.Text))
        {

            //默认nvarchar(max) 、text 长度不能超过20000
            if (val.Length > 20000)
            {
                reslutMsg = $"字符长度最多【20000】";
            }
            else
            {
                int length = propertyInfo.GetTypeCustomValue<MaxLengthAttribute>(x => new { x.Length }).GetInt();
                if (length == 0) { return (true, null, null); }
                //判断双字节与单字段
                else if (length < 8000 &&
                    ((dbType.Substring(0, 1) != "n"
                    && Encoding.UTF8.GetBytes(val.ToCharArray()).Length > length)
                     || val.Length > length)
                     )
                {
                    reslutMsg = $"最多只能【{length}】个字符。";
                }
            }
        }
        if (!string.IsNullOrEmpty(reslutMsg) && propertyInfo != null)
        {
            reslutMsg = propertyInfo.GetDisplayName() + reslutMsg;
        }
        return (reslutMsg == "" ? true : false, reslutMsg, value);
    }

    public static string GetDisplayName(this PropertyInfo property)
    {
        string displayName = property.GetTypeCustomValue<DisplayAttribute>(x => new { x.Name });
        if (string.IsNullOrEmpty(displayName))
        {
            return property.Name;
        }
        return displayName;
    }

    public static string GetDescription(this PropertyInfo type)
    {
        Attribute attribute = type.GetCustomAttribute(typeof(DescriptionAttribute));
        if (attribute != null && attribute is DescriptionAttribute)
        {
            return (attribute as DescriptionAttribute).Description ?? type.Name;
        }
        return type.Name;
    }
    public static string GetModuleCode<T>(this T entity)
    {

        string value = string.Empty;
        PropertyInfo[] propertyInfo = typeof(T).GetProperties().ToArray();

        if (typeof(T).GetTypeInfo().IsSubclassOf(typeof(BaseEntity)))
        {
            var ent = entity as BaseEntity;
            value = ent.ModuleCode;
        }
        return value;
    }

    public static string GetProperties<T>(T t)
    {
        string tStr = string.Empty;
        if (t == null)
            return tStr;

        PropertyInfo[] properties = t.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);

        if (properties.Length <= 0)
            return tStr;

        foreach (PropertyInfo item in properties)
        {
            string name = item.Name;
            object value = item.GetValue(t, null);
            if (item.PropertyType.IsValueType || item.PropertyType.Name.StartsWith("String"))
                tStr += string.Format("{0}:{1},", name, value);
            else
                GetProperties(value);
        }
        return tStr;
    }

    public static string GetPropertyValue<T>(this T t, string field)
    {
        string value = string.Empty;
        if (t == null)
            return value;

        PropertyInfo[] properties = t.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);

        if (properties.Length <= 0)
            return value;

        var property = properties.Where(x => x.Name == field).FirstOrDefault();
        value = property.GetValue(t, null).ToString();

        return value;
    }

    public static void SetPropertyValue<T>(this T t, string field, object value)
    {
        PropertyInfo[] properties = t.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);

        if (properties.Length > 0)
        {
            var property = properties.Where(x => x.Name == field).FirstOrDefault();
            property.SetValue(t, value);
        }
    }

    /// <summary>
    /// 获取属性的指定属性
    /// </summary>
    /// <param name="propertyInfo"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    public static object GetTypeCustomAttributes(this MemberInfo member, Type type)
    {
        object[] obj = member.GetCustomAttributes(type, false);
        if (obj.Length == 0) return null;
        return obj[0];
    }

    /// <summary>
    /// 获取类的多个指定属性的值
    /// </summary>
    /// <param name="member">当前类</param>
    /// <param name="type">指定的类</param>
    /// <param name="expression">指定属性的值 格式 Expression<Func<entityt, object>> exp = x => new { x.字段1, x.字段2 };</param>
    /// <returns>返回的是字段+value</returns>
    public static Dictionary<string, string> GetTypeCustomValues<TEntity>(this MemberInfo member, Expression<Func<TEntity, object>> expression)
    {
        var attr = member.GetTypeCustomAttributes(typeof(TEntity));
        if (attr == null)
        {
            return null;
        }

        string[] propertyName = expression.GetExpressionProperty();
        Dictionary<string, string> propertyKeyValues = new Dictionary<string, string>();

        foreach (PropertyInfo property in attr.GetType().GetProperties())
        {
            if (propertyName.Contains(property.Name))
            {
                propertyKeyValues[property.Name] = (property.GetValue(attr) ?? string.Empty).ToString();
            }
        }
        return propertyKeyValues;
    }

    /// <summary>
    /// 获取类的单个指定属性的值(只会返回第一个属性的值)
    /// </summary>
    /// <param name="member">当前类</param>
    /// <param name="type">指定的类</param>
    /// <param name="expression">指定属性的值 格式 Expression<Func<entityt, object>> exp = x => new { x.字段1, x.字段2 };</param>
    /// <returns></returns>
    public static string GetTypeCustomValue<TEntity>(this MemberInfo member, Expression<Func<TEntity, object>> expression)
    {
        var propertyKeyValues = member.GetTypeCustomValues(expression);
        if (propertyKeyValues == null || propertyKeyValues.Count == 0)
        {
            return null;
        }
        return propertyKeyValues.First().Value ?? "";
    }
    /// <summary>
    /// 获取表带有EntityAttribute属性的真实表名
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static string GetEntityTableName(this Type type)
    {
        Attribute attribute = type.GetCustomAttribute(typeof(EntityAttribute));
        if (attribute != null && attribute is EntityAttribute)
        {
            return (attribute as EntityAttribute).TableName ?? type.Name;
        }
        return type.Name;
    }


}

public class ArrayEntity
{
    public string column1 { get; set; }
}

public enum FieldType
{
    VarChar = 0,
    NvarChar,
    Int,
    BigInt,
    UniqueIdentifier
}

public enum EntityToSqlTempName
{
    TempInsert = 0
}
