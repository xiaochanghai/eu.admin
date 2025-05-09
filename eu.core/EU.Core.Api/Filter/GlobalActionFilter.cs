using EU.Core.Common.Const;
using Microsoft.AspNetCore.Mvc.Filters;

namespace EU.Core.Api.Filter;

/// <summary>
/// 全局请求验证
/// </summary>
public class GlobalActionFilter : ActionFilterAttribute
{
    /// <summary>
    /// 
    /// </summary>
    public string Message { get; set; }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="filterContext"></param>
    public override void OnActionExecuting(ActionExecutingContext filterContext)
    {
        //检查实体合法性
        if (!filterContext.ModelState.IsValid)
        {
            StringBuilder msg = new StringBuilder();
            foreach (var value in filterContext.ModelState.Values)
            {
                if (value.Errors.Count > 0)
                {
                    foreach (var error in value.Errors)
                    {
                        msg.AppendLine(error.ErrorMessage);
                    }
                }
            }
            filterContext.Result = new JsonResult(ServiceResult.OprateFailed($"参数验证失败:{msg}"));
            return;
        }

        ////检查用户信息
        //var tenantId = filterContext.HttpContext.User?.Claims?.Where(o => o.Type == CustomClaimTypes.TenantId).FirstOrDefault()?.Value;
        //var issuer = filterContext.HttpContext.User?.Claims?.Where(o => o.Type == CustomClaimTypes.Issuer).FirstOrDefault()?.Value;
        //var userId = filterContext.HttpContext.User?.Claims?.Where(o => o.Type == CustomClaimTypes.UserId).FirstOrDefault()?.Value;
        //var userName = filterContext.HttpContext.User?.Claims?.Where(o => o.Type == CustomClaimTypes.UserName).FirstOrDefault()?.Value;
        //var sessionId = filterContext.HttpContext.User?.Claims?.Where(o => o.Type == CustomClaimTypes.SessionId).FirstOrDefault()?.Value;
        //var sids = RedisHelper.Get<List<Guid>>(string.Format(RedisConsts.SYSTEM_LOGIN_USERID, issuer, userId)) ?? new List<Guid>();
        //if (string.IsNullOrEmpty(sessionId) || !sids.Any(o => o.ToString() == sessionId))
        //{
        //    filterContext.Result = new JsonResult(MessageModel.OprateFailed(ResponseText.LOGIN_USER_QUIT, MessageModelCode.LoginFailed));
        //    return;
        //}

        ////记录ip
        //var ip = filterContext.HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        //if (string.IsNullOrEmpty(ip))
        //{
        //    ip = filterContext.HttpContext.Request.HttpContext.Connection.RemoteIpAddress.ToString();
        //}
        //var claims = new Claim[] {
        //                new Claim(CustomClaimTypes.RequestIp, ip),//添加IP
        //            };
        //filterContext.HttpContext.User.AddIdentity(new ClaimsIdentity(claims));

        ////记录日志(GET请求不记录)
        //if (filterContext.HttpContext.Request.Method != "GET")
        //{
        //    LoggerOperator.PushLogger(new Domain.Model.System.SystemLogger(Guid.NewGuid())
        //    {
        //        Module = GetModel(filterContext.HttpContext.Request.Path),
        //        Type = GetType(filterContext.HttpContext.Request.Method, filterContext.HttpContext.Request.Path),
        //        Level = $"INFO",
        //        Content = $"{filterContext.HttpContext.User.Claims.FirstOrDefault(o => o.Type == CustomClaimTypes.Issuer)?.Value} {filterContext.HttpContext.Request.Path} {Dic2String(filterContext.ActionArguments)}",
        //        Creator = string.IsNullOrEmpty(userName) ? "sys" : userName,
        //        CreatorId = string.IsNullOrEmpty(userId) ? new Guid?() : Guid.Parse(userId),
        //        CreationTime = DateTimeHelper.Now(),
        //        TenantId = string.IsNullOrEmpty(tenantId) ? new Guid?() : Guid.Parse(tenantId)
        //    });
        //    LoggerOperator.PushModelLogger(new Domain.Model.System.SystemModelLogger(Guid.NewGuid())
        //    {
        //        Module = GetModel(filterContext.HttpContext.Request.Path),
        //        Oprate = filterContext.HttpContext.Request.Method,
        //        ApiUrl = GetPath(filterContext.HttpContext.Request.Path),
        //        Creator = string.IsNullOrEmpty(userName) ? "sys" : userName,
        //        CreatorId = string.IsNullOrEmpty(userId) ? new Guid?() : Guid.Parse(userId),
        //        CreationTime = DateTimeHelper.Now(),
        //        TenantId = string.IsNullOrEmpty(tenantId) ? new Guid?() : Guid.Parse(tenantId)
        //    });
        //}

        //LoggerOperator.PushTerminalLogger(filterContext.HttpContext.User.Claims.FirstOrDefault(o => o.Type == CustomClaimTypes.TerminalId)?.Value);
        base.OnActionExecuting(filterContext);
    }
    private static string GetPath(string rpath)
    {
        string res = rpath;
        try
        {
            StringBuilder sb = new StringBuilder();
            string[] empties = res.Split(new string[] { "/" }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < empties.Length; ++i)
            {
                //36位长度，并且全为小写是ID
                if (empties[i].Length == 36 && empties[i] == empties[i].ToLower())
                {
                    continue;
                }
                sb.Append(empties[i]);
                if (i != empties.Length - 1)
                    sb.Append("/");
                if (empties[i].ToLower().StartsWith("by"))
                {
                    i++;
                }
            }
            if (sb.Length > 0)
                res = sb.ToString();
        }
        catch { }
        return res;
    }

    private static string Dic2String(IDictionary<string, object> dic)
    {
        if (dic == null || dic.Keys.Count <= 0)
            return string.Empty;
        StringBuilder sb = new StringBuilder();
        try
        {
            foreach (KeyValuePair<string, object> item in dic)
            {
                if (item.Value == null)
                    continue;
                sb.Append($"{item.Key}:{(item.Value.ToString().StartsWith("{") ? item.Value.ToString() : Newtonsoft.Json.JsonConvert.SerializeObject(item.Value))};");
            }
        }
        catch { }
        return sb.ToString();
    }

    //private static string GetModel(string path)
    //{
    //    string model = SystemSetting.SYS_LOG_MODEL_SYS;
    //    if (path.StartsWith("/api/Dialysis"))
    //    {
    //        model = SystemSetting.SYS_LOG_MODEL_DIALYSIS;
    //    }
    //    else if (path.StartsWith("/api/Emr"))
    //    {
    //        model = SystemSetting.SYS_LOG_MODEL_PATIENT;
    //    }
    //    else if (path.StartsWith("/api/Dept"))
    //    {
    //        model = SystemSetting.SYS_LOG_MODEL_DEPT;
    //    }
    //    else if (path.StartsWith("/api/System"))
    //    {
    //        model = SystemSetting.SYS_LOG_MODEL_SYS;
    //    }
    //    else if (path.StartsWith("/api/Authorize"))
    //    {
    //        model = SystemSetting.SYS_LOG_MODEL_LOGIN;
    //    }
    //    else if (path.StartsWith("/api/Tmpl"))
    //    {
    //        model = SystemSetting.SYS_LOG_MODEL_TMPL;
    //    }
    //    else if (path.StartsWith("/api/Schedule"))
    //    {
    //        model = SystemSetting.SYS_LOG_MODEL_SCHEDULE;
    //    }

    //    return model;
    //}
    //private static string GetType(string method, string path)
    //{
    //    string type = SystemSetting.SYS_LOG_TYPE_OTHER;
    //    switch (method)
    //    {
    //        case "GET":
    //            type = SystemSetting.SYS_LOG_TYPE_QUERY;
    //            break;
    //        case "PUT":
    //            type = SystemSetting.SYS_LOG_TYPE_EDIT;
    //            break;
    //        case "POST":
    //            type = SystemSetting.SYS_LOG_TYPE_INSERTY;
    //            break;
    //        case "DELETE":
    //            type = SystemSetting.SYS_LOG_TYPE_DELETE;
    //            break;
    //        default:
    //            break;
    //    }
    //    if (path.StartsWith("/api/Token"))
    //    {
    //        type = SystemSetting.SYS_LOG_TYPE_LOGIN;
    //    }
    //    return type;
    //}
    /// <summary>
    /// 
    /// </summary>
    /// <param name="filterContext"></param>
    public override void OnActionExecuted(ActionExecutedContext filterContext)
    {
        base.OnActionExecuted(filterContext);
    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="filterContext"></param>
    public override void OnResultExecuting(ResultExecutingContext filterContext)
    {
        //400错误，统一处理
        if (filterContext.Result is BadRequestObjectResult badResult && badResult.StatusCode == 400)
        {
            var error = $"{filterContext.HttpContext.Request.Method} {filterContext.HttpContext.Request.Path} 请求的数据类型不正确";
            if (badResult.Value is Microsoft.AspNetCore.Mvc.ValidationProblemDetails values)
            {
                StringBuilder sb = new StringBuilder();
                foreach (var value in values.Errors)
                {
                    if (value.Value is string[] empties && empties.Length > 0)
                    {
                        if (empties[0].Contains(". Path"))
                        {
                            sb.AppendLine($"{value.Key.ToString()}:{empties[0].Split(". Path")[0]}");
                        }
                        else
                        {
                            sb.Append($"{value.Key.ToString()}:{empties[0]}");
                        }
                    }
                }
                error += $"：{sb.ToString()}";
                filterContext.Result = new JsonResult(ServiceResult.OprateFailed($"请求的数据类型不正确：{error}"));
            }
            else
            {
                filterContext.Result = new JsonResult(ServiceResult.OprateFailed(error));
            }
            //LoggerHelper.SendLog(error);
            return;
        }
        if (filterContext.Result is ObjectResult objectResult && objectResult.Value == null)
        {
            filterContext.Result = new JsonResult(ServiceResult.OprateSuccess(ResponseText.QUERY_SUCCESS));
        }

        base.OnResultExecuting(filterContext);
    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="filterContext"></param>
    public override void OnResultExecuted(ResultExecutedContext filterContext)
    {
        base.OnResultExecuted(filterContext);
    }
}
