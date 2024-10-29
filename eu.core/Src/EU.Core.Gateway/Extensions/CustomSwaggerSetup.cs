using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Filters;
namespace EU.Core.Gateway.Extensions;

public static class CustomSwaggerSetup
{
    public static void AddCustomSwaggerSetup(this IServiceCollection services)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));

        var basePath = AppContext.BaseDirectory;

        services.AddMvc(option => option.EnableEndpointRouting = false);

        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Version = "v1",
                Title = "自定义网关 接口文档",
            });

            var xmlPath = Path.Combine(basePath, "EU.Core.Gateway.xml");
            c.IncludeXmlComments(xmlPath, true);

            c.OperationFilter<AddResponseHeadersFilter>();
            c.OperationFilter<AppendAuthorizeToSummaryOperationFilter>();

            c.OperationFilter<SecurityRequirementsOperationFilter>();

            c.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
            {
                Description = "JWT授权(数据将在请求头中进行传输) 直接在下框中输入Bearer {token}（注意两者之间是一个空格）\"",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey
            });
        });
    }

    public static void UseCustomSwaggerMildd(this IApplicationBuilder app, Func<Stream> streamHtml)
    {
        if (app == null) throw new ArgumentNullException(nameof(app));

        var apis = new List<string> { "EU-svc" };
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint($"/swagger/v1/swagger.json", "gateway");
            apis.ForEach(m =>
            {
                c.SwaggerEndpoint($"/swagger/apiswg/{m}/swagger.json", m);
            });


            if (streamHtml.Invoke() == null)
            {
                var msg = "index.html的属性，必须设置为嵌入的资源";
                throw new Exception(msg);
            }

            c.IndexStream = streamHtml;

            c.RoutePrefix = "";
        });
    }


}
