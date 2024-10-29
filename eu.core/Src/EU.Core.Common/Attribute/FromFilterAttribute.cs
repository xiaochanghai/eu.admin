using EU.Core.Common.Helper;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace EU.Core.Common;

public class FilterHeaderBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        if (bindingContext == null)
        {
            throw new ArgumentNullException(nameof(bindingContext));
        }

        var name = bindingContext.FieldName;

        var headers = bindingContext.HttpContext.Request.Headers;

        QueryFilter queryFilter;

        if (!headers.ContainsKey(name))
        {
            queryFilter = QueryFilter.Default;
            bindingContext.Result = ModelBindingResult.Success(queryFilter);
            return Task.CompletedTask;
        }

        string filter = headers[name];

        if (bindingContext.ModelType == typeof(string))
        {
            bindingContext.Result = ModelBindingResult.Success(filter);
            return Task.CompletedTask;
        }

        try
        {
            if (string.IsNullOrEmpty(filter) || filter == "%22%22" || filter.ToLower() == "%7b%7d")
            {
                queryFilter = QueryFilter.Default;
            }
            else if (filter.Trim() == "undefined" || filter.Trim() == "null")
            {
                queryFilter = QueryFilter.Default;
                //LoggerHelper.SendLogError($"QueryFilter 反序列化异常: {filter}\r\n请求地址: {bindingContext.HttpContext.Request.GetEncodedUrl()}");
            }
            else
            {
                queryFilter = JsonHelper.JsonToObj<QueryFilter>(System.Web.HttpUtility.UrlDecode(filter));
                SetPredicateValues(queryFilter, bindingContext);
            }

            bindingContext.Result = ModelBindingResult.Success(queryFilter ?? QueryFilter.Default);
        }
        catch
        {
            //LoggerHelper.SendLogError($"QueryFilter 反序列化失败: {filter}\r\n请求地址: {bindingContext.HttpContext.Request.GetEncodedUrl()}");
            bindingContext.Result = ModelBindingResult.Success(QueryFilter.Default);
        }
        return Task.CompletedTask;
    }

    /// <summary>
    /// 设置 PredicateValues 的值类型
    /// </summary>
    /// <param name="queryFilter"></param>
    /// <param name="bindingContext"></param>
    private static void SetPredicateValues(QueryFilter queryFilter, ModelBindingContext bindingContext)
    {
        //if (queryFilter?.PredicateValues == null || queryFilter.PredicateValues.Length == 0)
        //{
        //    return;
        //}

        //for (int i = 0; i < queryFilter.PredicateValues.Length; i++)
        //{
        //    if (queryFilter.PredicateValues[i] is JObject jObj)
        //    {
        //        var prop = jObj.Properties()?.FirstOrDefault();
        //        if (prop == null)
        //            continue;
        //        var type = StringConvertToType(prop.Name);
        //        if (type == null)
        //            continue;
        //        try
        //        {
        //            var v = JsonHelper.JsonToObj(prop.Value?.ToString(), type);
        //            if (v != null)
        //            {
        //                queryFilter.PredicateValues[i] = v;
        //            }
        //        }
        //        catch (Exception)
        //        {
        //            throw;
        //            //LoggerHelper.SendLogError($"QueryFilter.PredicateValues[{i}] [{queryFilter.PredicateValues[i]}] 反序列化失败\r\n" +
        //            //    $"请求地址: {bindingContext.HttpContext.Request.GetEncodedUrl()}\r\n" +
        //            //    $"错误信息: {ex}");
        //        }
        //    }
        //}
    }


    #region 字符串获取类型
    /// <summary>
    /// 根据 <paramref name="name"/> 获取 <see cref="Type"/> 类型
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public static Type StringConvertToType(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return default;
        }

        switch (name.Trim().ToUpper())
        {
            case "INT":
            case "INT32":
                return typeof(int);
            case "INT?":
            case "INT32?":
                return typeof(int?);
            case "INT[]":
            case "INT32[]":
                return typeof(int[]);
            case "INT?[]":
            case "INT32?[]":
                return typeof(int?[]);
            case "LIST<INT>":
            case "LIST<INT32>":
                return typeof(List<int>);
            case "LIST<INT?>":
            case "LIST<INT32?>":
                return typeof(List<int?>);

            case "LONG":
            case "INT64":
                return typeof(long);
            case "LONG?":
            case "INT64?":
                return typeof(long?);
            case "LONG[]":
            case "INT64[]":
                return typeof(long[]);
            case "LONG?[]":
            case "INT64?[]":
                return typeof(long?[]);
            case "LIST<LONG>":
            case "LIST<INT64>":
                return typeof(List<long>);
            case "LIST<LONG?>":
            case "LIST<INT64?>":
                return typeof(List<long?>);

            case "FLOAT":
            case "SINGLE":
                return typeof(float);
            case "FLOAT?":
            case "SINGLE?":
                return typeof(float?);
            case "FLOAT[]":
            case "SINGLE[]":
                return typeof(float[]);
            case "FLOAT?[]":
            case "SINGLE?[]":
                return typeof(float?[]);
            case "LIST<FLOAT>":
            case "LIST<SINGLE>":
                return typeof(List<float>);
            case "LIST<FLOAT?>":
            case "LIST<SINGLE?>":
                return typeof(List<float?>);

            case "DOUBLE":
                return typeof(double);
            case "DOUBLE?":
                return typeof(double?);
            case "DOUBLE[]":
                return typeof(double[]);
            case "DOUBLE?[]":
                return typeof(double?[]);
            case "LIST<DOUBLE>":
                return typeof(List<double>);
            case "LIST<DOUBLE?>":
                return typeof(List<double?>);

            case "DECIMAL":
                return typeof(decimal);
            case "DECIMAL?":
                return typeof(decimal?);
            case "DECIMAL[]":
                return typeof(decimal[]);
            case "DECIMAL?[]":
                return typeof(decimal?[]);
            case "LIST<DECIMAL>":
                return typeof(List<decimal>);
            case "LIST<DECIMAL?>":
                return typeof(List<decimal?>);

            case "DATETIME":
                return typeof(DateTime);
            case "DATETIME?":
                return typeof(DateTime?);
            case "DATETIME[]":
                return typeof(DateTime[]);
            case "DATETIME?[]":
                return typeof(DateTime?[]);
            case "LIST<DATETIME>":
                return typeof(List<DateTime>);
            case "LIST<DATETIME?>":
                return typeof(List<DateTime?>);

            case "GUID":
                return typeof(Guid);
            case "GUID?":
                return typeof(Guid?);
            case "GUID[]":
                return typeof(Guid[]);
            case "GUID?[]":
                return typeof(Guid?[]);
            case "LIST<GUID>":
                return typeof(List<Guid>);
            case "LIST<GUID?>":
                return typeof(List<Guid?>);

            case "BOOL":
                return typeof(bool);
            case "BOOL?":
                return typeof(bool?);
            case "BOOL[]":
                return typeof(bool[]);
            case "BOOL?[]":
                return typeof(bool?[]);
            case "LIST<BOOL>":
                return typeof(List<bool>);
            case "LIST<BOOL?>":
                return typeof(List<bool?>);

            case "STRING":
                return typeof(string);
            case "STRING[]":
                return typeof(string[]);
            case "LIST<STRING>":
                return typeof(List<string>);

            default:
                break;
        }

        return Type.GetType(name);
    }
    #endregion
}

/// <summary>
/// 从 Header 自动反序列化 QueryFilter 并绑定
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = false, Inherited = true)]
public class FromFilterAttribute : Attribute, IBindingSourceMetadata, IModelNameProvider, IBinderTypeProviderMetadata
{
    public FromFilterAttribute()
    {
    }

    public BindingSource BindingSource => BindingSource.Header;

    public string Name { get; set; }

    public Type BinderType => typeof(FilterHeaderBinder);
}
