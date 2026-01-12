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
    public const string SwaggerJwt = "swagger-jwt";

    /// <summary>
    /// 检查当前请求是否已通过 Swagger 访问验证（静态方法）
    /// </summary>
    /// <returns>true：已授权；false：未授权或 Session 不可用</returns>
    public static bool IsSuccessSwagger()
    {
        return App.HttpContext?.GetSession()?.GetString(SwaggerCodeKey) == "success";
    }

    /// <summary>
    /// 检查当前请求是否已通过 Swagger 访问验证（扩展方法）
    /// </summary>
    /// <param name="context">HTTP 上下文</param>
    /// <returns>true：已授权；false：未授权或 Session 不可用</returns>
    public static bool IsSuccessSwagger(this HttpContext context)
    {
        return context.GetSession()?.GetString(SwaggerCodeKey) == "success";
    }


    /// <summary>
    /// 标记当前请求已通过 Swagger 访问验证（静态方法）
    /// </summary>
    public static void SuccessSwagger()
    {
        App.HttpContext?.GetSession()?.SetString(SwaggerCodeKey, "success");
    }

    /// <summary>
    /// 标记当前请求已通过 Swagger 访问验证（扩展方法）
    /// </summary>
    /// <param name="context">HTTP 上下文</param>
    public static void SuccessSwagger(this HttpContext context)
    {
        context.GetSession()?.SetString(SwaggerCodeKey, "success");
    }

    public static void SuccessSwaggerJwt(this HttpContext context, string token)
    {
        context.GetSession()?.SetString(SwaggerJwt, token);
    }

    public static string GetSuccessSwaggerJwt(this HttpContext context)
    {
        return context.GetSession()?.GetString(SwaggerJwt);
    }


    public static void RedirectSwaggerLogin(this HttpContext context)
    {
        var returnUrl = context.Request.GetDisplayUrl(); //获取当前url地址 
        context.Response.Redirect("/swg-login.html?returnUrl=" + returnUrl);
    }
}