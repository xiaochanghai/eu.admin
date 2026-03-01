using Autofac;
using Autofac.Extensions.DependencyInjection;
using EU.Core.Common.Core;
using EU.Core.Domain;
using EU.Core.Extensions;
using EU.Core.MCP.Api.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Configure URLs explicitly
//builder.WebHost.UseUrls("http://localhost:5196");


// 1、配置host与容器
builder.Host
    .UseServiceProviderFactory(new AutofacServiceProviderFactory())
    .ConfigureContainer<ContainerBuilder>(builder =>
    {
        builder.RegisterModule(new AutofacModuleRegister());
        builder.RegisterModule(new AutofacMCPModuleRegister());
        //builder.RegisterModule<AutofacPropertityModuleReg>();

        //注册仓储，所有IRepository接口到Repository的映射
        builder.RegisterGeneric(typeof(BaseCRUDVM<>))
            //InstancePerDependency：默认模式，每次调用，都会重新实例化对象；每次请求都创建一个新的对象；
            .As(typeof(IBaseCRUDVM<>)).InstancePerDependency();
        //builder.RegisterType<UnitOfWorkManage>().As<IUnitOfWorkManage>()
        //               .AsImplementedInterfaces()
        //               .InstancePerLifetimeScope()
        //               .PropertiesAutowired();
    })
    .ConfigureAppConfiguration((hostingContext, config) =>
    {
        hostingContext.Configuration.ConfigureApplication();
        config.Sources.Clear();
        config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: false);
        //config.AddConfigurationApollo("appsettings.apollo.json");
    });
builder.ConfigureApplication();

// 2、配置服务
builder.Services.AddSingleton(new AppSettings(builder.Configuration));
builder.Services.AddAllOptionRegister();
builder.Services.AddHttpPollySetup();

// Add services to the container
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = null; // Keep original property names
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

ServiceExtensions.Init();


builder.Services.AddCacheSetup();
builder.Services.AddSqlsugarSetup();
builder.Services.AddDataContextSetup();
builder.Services.AddDbSetup();
builder.Services.AddAuthorizationSetup();
builder.Services.AddAuthentication_JWTSetup();

// Register our services
builder.Services.AddMcpServices(); 

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Enable HTTPS redirection
app.UseHttpsRedirection();

// Add request logging middleware
app.Use(async (context, next) =>
{
    if (context.Request.Method == "POST" && context.Request.Path.StartsWithSegments("/mcp"))
    {
        context.Request.EnableBuffering();
        var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
        context.Request.Body.Position = 0;
        
        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
        logger.LogInformation($"Incoming MCP request: {body}");
    }
    await next();
});

// Map controllers
app.MapControllers();

app.Run();