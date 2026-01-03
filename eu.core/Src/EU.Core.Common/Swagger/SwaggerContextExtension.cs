using EU.Core.Common.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;

namespace EU.Core.Common.Swagger;

/// <summary>
/// Swagger 上下文扩展类
/// 提供 Swagger 文档访问权限验证、JWT Token 存储和登录重定向功能
/// </summary>
public static class SwaggerContextExtension
{
    /// <summary>
    /// Swagger 访问授权状态的 Session Key
    /// </summary>
    public const string SwaggerCodeKey = "swagger-code";

    /// <summary>
    /// Swagger JWT Token 的 Session Key
    /// </summary>
    public const string SwaggerJwtKey = "swagger-jwt";

    /// <summary>
    /// Swagger 登录页面路径
    /// </summary>
    private const string SwaggerLoginPath = "/swg-login.html";

    /// <summary>
    /// Swagger 授权成功标识
    /// </summary>
    private const string SuccessFlag = "success";

    #region 授权状态检查

    /// <summary>
    /// 检查当前请求是否已通过 Swagger 访问验证（静态方法）
    /// </summary>
    /// <returns>true：已授权；false：未授权或 Session 不可用</returns>
    public static bool IsSuccessSwagger()
    {
        return App.HttpContext?.GetSession()?.GetString(SwaggerCodeKey) == SuccessFlag;
    }

    /// <summary>
    /// 检查当前请求是否已通过 Swagger 访问验证（扩展方法）
    /// </summary>
    /// <param name="context">HTTP 上下文</param>
    /// <returns>true：已授权；false：未授权或 Session 不可用</returns>
    public static bool IsSuccessSwagger(this HttpContext context)
    {
        if (context == null)
        {
            return false;
        }

        return context.GetSession()?.GetString(SwaggerCodeKey) == SuccessFlag;
    }

    #endregion

    #region 设置授权状态

    /// <summary>
    /// 标记当前请求已通过 Swagger 访问验证（静态方法）
    /// </summary>
    public static void SuccessSwagger()
    {
        App.HttpContext?.GetSession()?.SetString(SwaggerCodeKey, SuccessFlag);
    }

    /// <summary>
    /// 标记当前请求已通过 Swagger 访问验证（扩展方法）
    /// </summary>
    /// <param name="context">HTTP 上下文</param>
    public static void SuccessSwagger(this HttpContext context)
    {
        if (context == null)
        {
            return;
        }

        context.GetSession()?.SetString(SwaggerCodeKey, SuccessFlag);
    }

    #endregion

    #region JWT Token 管理

    /// <summary>
    /// 保存 Swagger 访问的 JWT Token 到 Session
    /// </summary>
    /// <param name="context">HTTP 上下文</param>
    /// <param name="token">JWT Token 字符串</param>
    public static void SetSwaggerJwtToken(this HttpContext context, string token)
    {
        if (context == null || string.IsNullOrWhiteSpace(token))
        {
            return;
        }

        context.GetSession()?.SetString(SwaggerJwtKey, token);
    }

    /// <summary>
    /// 从 Session 获取 Swagger 访问的 JWT Token
    /// </summary>
    /// <param name="context">HTTP 上下文</param>
    /// <returns>JWT Token 字符串，未设置时返回 null</returns>
    public static string GetSwaggerJwtToken(this HttpContext context)
    {
        if (context == null)
        {
            return null;
        }

        return context.GetSession()?.GetString(SwaggerJwtKey);
    }

    #endregion

    #region 登录重定向

    /// <summary>
    /// 重定向到 Swagger 登录页面，并携带当前页面地址作为返回 URL
    /// </summary>
    /// <param name="context">HTTP 上下文</param>
    /// <remarks>
    /// 重定向到 /swg-login.html?returnUrl={currentUrl}
    /// 登录成功后可以根据 returnUrl 参数返回到原页面
    /// </remarks>
    public static void RedirectToSwaggerLogin(this HttpContext context)
    {
        if (context == null)
        {
            return;
        }

        // 获取当前完整 URL 地址（包含协议、主机、路径和查询字符串）
        var returnUrl = context.Request.GetDisplayUrl();

        // 重定向到登录页，并携带返回地址
        context.Response.Redirect($"{SwaggerLoginPath}?returnUrl={returnUrl}");
    }

    #endregion

    #region 废弃的方法（保持向后兼容）

    /// <summary>
    /// 保存 Swagger JWT Token（已废弃，请使用 SetSwaggerJwtToken）
    /// </summary>
    /// <param name="context">HTTP 上下文</param>
    /// <param name="token">JWT Token</param>
    [Obsolete("请使用 SetSwaggerJwtToken 替代此方法")]
    public static void SuccessSwaggerJwt(this HttpContext context, string token)
    {
        SetSwaggerJwtToken(context, token);
    }

    /// <summary>
    /// 获取 Swagger JWT Token（已废弃，请使用 GetSwaggerJwtToken）
    /// </summary>
    /// <param name="context">HTTP 上下文</param>
    /// <returns>JWT Token</returns>
    [Obsolete("请使用 GetSwaggerJwtToken 替代此方法")]
    public static string GetSuccessSwaggerJwt(this HttpContext context)
    {
        return GetSwaggerJwtToken(context);
    }

    /// <summary>
    /// 重定向到 Swagger 登录页（已废弃，请使用 RedirectToSwaggerLogin）
    /// </summary>
    /// <param name="context">HTTP 上下文</param>
    [Obsolete("请使用 RedirectToSwaggerLogin 替代此方法")]
    public static void RedirectSwaggerLogin(this HttpContext context)
    {
        RedirectToSwaggerLogin(context);
    }

    #endregion
}
