using ClaudeMCP.API.Services.Interfaces;
using ClaudeMCP.API.Services.Implementations;

var builder = WebApplication.CreateBuilder(args);

// Configure URLs explicitly
//builder.WebHost.UseUrls("http://localhost:5196");

// Add services to the container
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = null; // Keep original property names
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register our services
builder.Services.AddScoped<IMcpService, McpService>();
builder.Services.AddScoped<IToolService, TestToolService>();

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