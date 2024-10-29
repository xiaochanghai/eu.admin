using EU.Core.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Serilog;
using Swashbuckle.AspNetCore.Filters;
using Microsoft.AspNetCore.Builder;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace EU.Core.Extensions;

/// <summary>
/// Swagger 启动服务
/// </summary>
public static class SwaggerSetup
{
    public static void AddSwaggerSetup(this IServiceCollection services)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));

        var basePath = AppContext.BaseDirectory;

        services.AddSwaggerGen(c =>
        {
            ApiInfos.ForEach(x =>
            {
                c.SwaggerDoc(x.UrlPrefix, x.OpenApiInfo);
                c.OrderActionsBy(o => o.RelativePath);
            });
            c.UseInlineDefinitionsForEnums();
            // 开启加权小锁
            c.OperationFilter<AddResponseHeadersFilter>();
            c.OperationFilter<AppendAuthorizeToSummaryOperationFilter>();

            // 在header中添加token，传递到后台
            c.OperationFilter<SecurityRequirementsOperationFilter>();

            // API注释所需XML文件
            try
            {

                c.IncludeXmlComments(Path.Combine(basePath, "EU.Core.xml"), true);
                c.IncludeXmlComments(Path.Combine(basePath, "EU.Core.Model.xml"), true);
            }
            catch (Exception ex)
            {
                Log.Error("EU.Core.xml和EU.Core.Model.xml 丢失，请检查并拷贝。\n" + ex.Message);
            }

            c.MapType<QueryFilter>(() => new OpenApiSchema { Type = "string", Format = "string" });


            // ids4和jwt切换
            if (Permissions.IsUseIds4)
            {
                //接入identityserver4
                c.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.OAuth2,
                    Flows = new OpenApiOAuthFlows
                    {
                        Implicit = new OpenApiOAuthFlow
                        {
                            AuthorizationUrl = new Uri($"{AppSettings.app(new string[] { "Startup", "IdentityServer4", "AuthorizationUrl" })}/connect/authorize"),
                            Scopes = new Dictionary<string, string>
                            {
                                {
                                    "EU.core.api", "ApiResource id"
                                }
                            }
                        }
                    }
                });
            }
            else
            {
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.ApiKey,
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Scheme = "Bearer",
                    Description = "JWT授权(数据将在请求头中进行传输) 直接在下框中输入Bearer {token}（注意两者之间是一个空格）\"",

                });
            }

            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    new List<string>()
                }
            });

        });
        services.AddSwaggerGenNewtonsoftSupport();
    }

    public static void UseSwaggerMiddle(this IApplicationBuilder app, Func<Stream> streamHtml)
    {
        if (app == null) throw new ArgumentNullException(nameof(app));

        SwaggerBuilderExtensions.UseSwagger(app);
        app.UseSwaggerUI(options =>
        {
            // 遍历分组信息，生成Json
            ApiInfos.ForEach(x =>
            {
                options.SwaggerEndpoint($"/swagger/{x.UrlPrefix}/swagger.json", x.Name);
            });
            // 模型的默认扩展深度，设置为 -1 完全隐藏模型
            options.DefaultModelsExpandDepth(-1);
            // API文档仅展开标记
            options.DocExpansion(DocExpansion.List);
            // API前缀设置为空
            options.RoutePrefix = string.Empty;
            // API页面Title
            options.DocumentTitle = "接口文档";
            if (streamHtml.Invoke() == null)
            {
                var msg = "index.html的属性，必须设置为嵌入的资源";
                Log.Error(msg);
                throw new Exception(msg);
            }

            options.IndexStream = streamHtml;
            options.DocExpansion(DocExpansion.None); //->修改界面打开时自动折叠

            if (Permissions.IsUseIds4)
            {
                options.OAuthClientId("Tiobonadminjs");
            }

            //增加令牌本地缓存 reload不会丢失
            options.ConfigObject.AdditionalItems.Add("persistAuthorization", "true");

            // 路径配置，设置为空，表示直接在根域名（localhost:8001）访问该文件,注意localhost:8001/swagger是访问不到的，去launchSettings.json把launchUrl去掉，如果你想换一个路径，直接写名字即可，比如直接写c.RoutePrefix = "doc";
            options.RoutePrefix = "";
        });
    }


    /// <summary>
    /// 当前API版本
    /// </summary>
    private static readonly string version = $"V1.0";
    /// <summary>
    /// Swagger分组信息，将进行遍历使用
    /// </summary>
    private static readonly List<SwaggerApiInfo> ApiInfos = new List<SwaggerApiInfo>()
    {
        new SwaggerApiInfo
        {
            UrlPrefix = Grouping.GroupName_Auth,
            Name = "认证授权",
            OpenApiInfo = new OpenApiInfo
            {
                Version = version,
                Title = "认证授权",
                Description = "登录/注销",
            }
        },
        new SwaggerApiInfo
        {
            UrlPrefix = Grouping.GroupName_BD,
            Name = "基础数据",
            OpenApiInfo = new OpenApiInfo
            {
                Version = version,
                Title = "基础数据",
                Description = "基础数据...",
            }
        },
        new SwaggerApiInfo
        {
            UrlPrefix = Grouping.GroupName_AP,
            Name = "应付模块",
            OpenApiInfo = new OpenApiInfo
            {
                Version = version,
                Title = "应付模块",
                Description = "应付期初建账、应付对账单...",
            }
        },
        new SwaggerApiInfo
        {
            UrlPrefix = Grouping.GroupName_PO,
            Name = "采购模块",
            OpenApiInfo = new OpenApiInfo
            {
                Version = version,
                Title = "采购模块",
                Description = "采购...",
            }
        },
        new SwaggerApiInfo
        {
            UrlPrefix = Grouping.GroupName_EM,
            Name = "设备模块",
            OpenApiInfo = new OpenApiInfo
            {
                Version = version,
                Title = "设备模块",
                Description = "设备...",
            }
        },
        new SwaggerApiInfo
        {
            UrlPrefix = Grouping.GroupName_IV,
            Name = "库存模块",
            OpenApiInfo = new OpenApiInfo
            {
                Version = version,
                Title = "库存模块",
                Description = "库存...",
            }
        },
        new SwaggerApiInfo
        {
            UrlPrefix = Grouping.GroupName_MF,
            Name = "工模模块",
            OpenApiInfo = new OpenApiInfo
            {
                Version = version,
                Title = "工模模块",
                Description = "工模...",
            }
        },
        new SwaggerApiInfo
        {
            UrlPrefix = Grouping.GroupName_PD,
            Name = "生产模块",
            OpenApiInfo = new OpenApiInfo
            {
                Version = version,
                Title = "生产模块",
                Description = "生产...",
            }
        },
        new SwaggerApiInfo
        {
            UrlPrefix = Grouping.GroupName_SD,
            Name = "销售模块",
            OpenApiInfo = new OpenApiInfo
            {
                Version = version,
                Title = "销售模块",
                Description = "销售...",
            }
        },
        new SwaggerApiInfo
        {
            UrlPrefix = Grouping.GroupName_PS,
            Name = "产品结构",
            OpenApiInfo = new OpenApiInfo
            {
                Version = version,
                Title = "产品结构",
                Description = "产品结构...",
            }
        },
        new SwaggerApiInfo
        {
            UrlPrefix = Grouping.GroupName_SM,
            Name = "系统模块",
            OpenApiInfo = new OpenApiInfo
            {
                Version = version,
                Title = "系统模块",
                Description = "用户/角色/权限...",
            }
        },
        new SwaggerApiInfo
        {
            UrlPrefix = Grouping.GroupName_Assistant,
            Name = "工具模块",
            OpenApiInfo = new OpenApiInfo
            {
                Version = version,
                Title = "工具模块",
                Description = "自助测量程序/中央站...",
            }
        },
        new SwaggerApiInfo
        {
            UrlPrefix = Grouping.GroupName_Other,
            Name = "其他模块",
            OpenApiInfo = new OpenApiInfo
            {
                Version = version,
                Title = "其他模块",
                Description = "其他...",
            }
        }, new SwaggerApiInfo
        {
            UrlPrefix = Grouping.GroupName_WX,
            Name = "微信模块",
            OpenApiInfo = new OpenApiInfo
            {
                Version = version,
                Title = "微信模块",
                Description = "小程序..."
            }
        }
    };
    private class SwaggerApiInfo
    {
        /// <summary>
        /// URL前缀
        /// </summary>
        public string UrlPrefix { get; set; }

        /// <summary>
        /// 名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// OpenApiInfo
        /// </summary>
        public OpenApiInfo OpenApiInfo { get; set; }
    }

    /// <summary>
    /// swagger分组
    /// </summary>
    public static class Grouping
    {

        /// <summary>
        /// 基础数据
        /// </summary>
        public const string GroupName_BD = "basedata";

        /// <summary>
        /// 应收
        /// </summary>
        public const string GroupName_AR = "ar";

        /// <summary>
        /// 应付
        /// </summary>
        public const string GroupName_AP = "ap";

        /// <summary>
        /// 设备
        /// </summary>
        public const string GroupName_EM = "em";

        /// <summary>
        /// 采购
        /// </summary>
        public const string GroupName_PO = "po";

        /// <summary>
        /// 库存
        /// </summary>
        public const string GroupName_IV = "iv";

        /// <summary>
        /// 工模
        /// </summary>
        public const string GroupName_MF = "mf";

        /// <summary>
        /// 生产
        /// </summary>
        public const string GroupName_PD = "pd";

        /// <summary>
        /// 产品结构 
        /// </summary>
        public const string GroupName_PS = "ps";

        /// <summary>
        /// 销售
        /// </summary>
        public const string GroupName_SD = "sd";

        /// <summary>
        /// 系统
        /// </summary>
        public const string GroupName_SM = "system";
        /// <summary>
        /// 认证授权
        /// </summary>
        public const string GroupName_Auth = "auth";
        /// <summary>
        /// 其他
        /// </summary>
        public const string GroupName_Other = "other";

        /// <summary>
        /// Ghra
        /// </summary>
        public const string GroupName_Ghra = "Ghra";
        /// <summary>
        /// 辅助工具
        /// </summary>
        public const string GroupName_Assistant = "assistant";
        /// <summary>
        /// 微信
        /// </summary>
        public const string GroupName_WX = " wechat";



    }
}

/// <summary>
/// 自定义版本
/// </summary>
public class CustomApiVersion
{
    /// <summary>
    /// Api接口版本 自定义
    /// </summary>
    public enum ApiVersions
    {
        /// <summary>
        /// Ghra 版本
        /// </summary>
        Ghra = 0,
        /// <summary>
        /// V1 版本
        /// </summary>
        V1 = 1,

        /// <summary>
        /// V2 版本
        /// </summary>
        V2 = 2,
    }
}