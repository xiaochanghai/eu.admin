#nullable enable

using System.Reflection;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using EU.Core.Common;
using EU.Core.Common.HttpContextUser;
using EU.Core.IServices.Abstractions.Auditing;
using EU.Core.IServices.Abstractions.Security;
using EU.Core.Model;
using EU.Core.Model.Entity;
using EU.Core.Extensions.Filters;
using EU.Core.Api.Agent.Configuration;
using EU.Core.Api.Agent.Controllers;
using EU.Core.Api.Agent.Errors;
using EU.Core.Api.Agent.Health;
using EU.Core.Api.Agent.Observability;
using EU.Core.Api.Agent.Security;
using EU.Core.IServices;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace EU.Core.Tests.Service_Test;

public sealed class AgAgentHostErrorResponse_Should
{
    [Theory]
    [InlineData("REQUEST_INVALID", 400, 600001)]
    [InlineData("AUTHENTICATION_REQUIRED", 401, 600006)]
    [InlineData("AUTHORIZATION_DENIED", 403, 600007)]
    [InlineData("AGENT_NOT_FOUND", 404, 610001)]
    [InlineData("SKILL_ARCHIVE_BLOCKED", 409, 620013)]
    [InlineData("REQUEST_BODY_TOO_LARGE", 413, 600002)]
    [InlineData("REQUEST_UNSUPPORTED_MEDIA_TYPE", 415, 600012)]
    [InlineData("AGENT_OUTPUT_INVALID", 422, 610016)]
    [InlineData("AGENT_RATE_LIMIT_EXCEEDED", 429, 600008)]
    [InlineData("MODEL_INVOCATION_FAILED", 502, 660003)]
    [InlineData("AGENT_AUDIT_UNAVAILABLE", 503, 680001)]
    [InlineData("MCP_TOOL_CALL_TIMEOUT", 504, 630016)]
    [InlineData("UNEXPECTED_ERROR", 500, 690001)]
    [InlineData("CUSTOM_UNKNOWN_ERROR", 500, 699999)]
    public async Task Write_fixed_service_error_contract(
        string errorCode,
        int httpStatus,
        int businessStatus)
    {
        DefaultHttpContext context = Context();

        await AgentApiErrorResponseWriter.WriteAsync(context, errorCode, "Failed.");

        await AssertErrorAsync(context, httpStatus, businessStatus, errorCode);
    }

    [Fact]
    public async Task Log_an_unregistered_error_code_before_using_the_fallback()
    {
        var loggerProvider = new CapturingLoggerProvider();
        DefaultHttpContext context = Context(loggerProvider);

        await AgentApiErrorResponseWriter.WriteAsync(
            context,
            "CUSTOM_UNKNOWN_ERROR",
            "Failed.");

        Assert.Contains(loggerProvider.Messages, message =>
            message.Contains("CUSTOM_UNKNOWN_ERROR", StringComparison.Ordinal)
            && message.Contains("not registered", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(false, 400, 600001, "REQUEST_INVALID")]
    [InlineData(true, 415, 600012, "REQUEST_UNSUPPORTED_MEDIA_TYPE")]
    public async Task Map_model_binding_failures(
        bool unsupported,
        int httpStatus,
        int businessStatus,
        string errorCode)
    {
        DefaultHttpContext httpContext = Context();
        var modelState = new ModelStateDictionary();
        if (unsupported)
            modelState.TryAddModelException(
                "body",
                new UnsupportedContentTypeException("unsupported"));
        else
            modelState.AddModelError("body", "invalid");
        var actionContext = new ActionContext(
            httpContext,
            new RouteData(),
            new ActionDescriptor(),
            modelState);
        Type resultType = typeof(AgentRunsController).Assembly.GetType(
            "EU.Core.Api.Agent.Errors.AgentApiValidationResultFilter",
            throwOnError: true)!;
        MethodInfo method = resultType.GetMethod(
            "InvalidModelState",
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)!;

        JsonResult result = Assert.IsType<JsonResult>(method.Invoke(null, [actionContext]));
        await result.ExecuteResultAsync(actionContext);

        await AssertErrorAsync(httpContext, httpStatus, businessStatus, errorCode);
    }

    [Fact]
    public async Task Replace_the_mvc_unsupported_media_type_result()
    {
        DefaultHttpContext httpContext = Context();
        var actionContext = new ActionContext(
            httpContext,
            new RouteData(),
            new ActionDescriptor(),
            new ModelStateDictionary());
        var resultContext = new ResultExecutingContext(
            actionContext,
            [],
            new UnsupportedMediaTypeResult(),
            new object());
        Type filterType = typeof(AgentRunsController).Assembly.GetType(
            "EU.Core.Api.Agent.Errors.AgentApiValidationResultFilter",
            throwOnError: true)!;
        var filter = Assert.IsAssignableFrom<IAlwaysRunResultFilter>(
            Activator.CreateInstance(filterType));

        filter.OnResultExecuting(resultContext);
        JsonResult result = Assert.IsType<JsonResult>(resultContext.Result);
        await result.ExecuteResultAsync(actionContext);

        await AssertErrorAsync(
            httpContext,
            StatusCodes.Status415UnsupportedMediaType,
            600012,
            "REQUEST_UNSUPPORTED_MEDIA_TYPE");
    }

    [Fact]
    public async Task Replace_the_framework_problem_details_for_unsupported_media_type()
    {
        DefaultHttpContext httpContext = Context();
        var actionContext = new ActionContext(
            httpContext,
            new RouteData(),
            new ActionDescriptor(),
            new ModelStateDictionary());
        var resultContext = new ResultExecutingContext(
            actionContext,
            [],
            new ObjectResult(new ProblemDetails
            {
                Status = StatusCodes.Status415UnsupportedMediaType,
                Title = "Unsupported Media Type"
            })
            {
                StatusCode = StatusCodes.Status415UnsupportedMediaType
            },
            new object());
        Type filterType = typeof(AgentRunsController).Assembly.GetType(
            "EU.Core.Api.Agent.Errors.AgentApiValidationResultFilter",
            throwOnError: true)!;
        var filter = Assert.IsAssignableFrom<IAlwaysRunResultFilter>(
            Activator.CreateInstance(filterType));

        filter.OnResultExecuting(resultContext);
        JsonResult result = Assert.IsType<JsonResult>(resultContext.Result);
        await result.ExecuteResultAsync(actionContext);

        await AssertErrorAsync(
            httpContext,
            StatusCodes.Status415UnsupportedMediaType,
            600012,
            "REQUEST_UNSUPPORTED_MEDIA_TYPE");
    }

    [Theory]
    [InlineData(typeof(RequestBodyTooLargeException), 413, 600002, "REQUEST_BODY_TOO_LARGE")]
    [InlineData(typeof(JsonException), 400, 600001, "REQUEST_INVALID")]
    [InlineData(typeof(InvalidOperationException), 500, 690001, "UNEXPECTED_ERROR")]
    public async Task Map_unhandled_host_exceptions(
        Type exceptionType,
        int httpStatus,
        int businessStatus,
        string errorCode)
    {
        Exception exception = (Exception)Activator.CreateInstance(exceptionType)!;
        var middleware = new ProblemDetailsMiddleware(
            _ => Task.FromException(exception),
            NullLoggerFactory.Instance);
        DefaultHttpContext context = Context();

        await middleware.InvokeAsync(context);

        await AssertErrorAsync(context, httpStatus, businessStatus, errorCode);
    }

    [Fact]
    public async Task Map_controller_exceptions_with_the_core_api_service_result_format()
    {
        DefaultHttpContext context = Context();
        var actionContext = new ActionContext(
            context,
            new RouteData(),
            new ActionDescriptor(),
            new ModelStateDictionary());
        var exceptionContext = new ExceptionContext(actionContext, [])
        {
            Exception = new Exception("The status filter is invalid.")
        };
        var filter = new GlobalExceptionsFilter(
            new TestHostEnvironment(),
            NullLogger<GlobalExceptionsFilter>.Instance,
            []);

        await filter.OnExceptionAsync(exceptionContext);

        Assert.True(exceptionContext.ExceptionHandled);
        ContentResult result = Assert.IsType<ContentResult>(exceptionContext.Result);
        Assert.Null(result.StatusCode);
        Assert.StartsWith("application/json", result.ContentType);
        using JsonDocument document = JsonDocument.Parse(result.Content!);
        JsonElement root = document.RootElement;
        Assert.Equal(500, root.GetProperty("Status").GetInt32());
        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Equal(
            "The status filter is invalid.",
            root.GetProperty("Message").GetString());
        Assert.Equal(JsonValueKind.Null, root.GetProperty("MessageDev").ValueKind);
    }

    [Fact]
    public async Task Replay_the_exact_completed_service_response()
    {
        var repository = new MemoryIdempotencyRepository();
        using var metrics = new AgentMetrics();
        var middleware = new HttpIdempotencyMiddleware(
            async context =>
            {
                context.Response.StatusCode = StatusCodes.Status201Created;
                await AgentApiErrorResponseWriter.WriteAsync(
                    context,
                    "REQUEST_INVALID",
                    "Deterministic response.",
                    httpStatus: StatusCodes.Status201Created,
                    cancellationToken: context.RequestAborted);
            },
            Options.Create(new AgentIdempotencyOptions()),
            TestUser.Instance,
            TimeProvider.System,
            metrics);
        DefaultHttpContext first = IdempotentContext();
        await middleware.InvokeAsync(first, repository);
        byte[] expected = ((MemoryStream)first.Response.Body).ToArray();
        string expectedContentType = first.Response.ContentType!;

        DefaultHttpContext replay = IdempotentContext();
        await middleware.InvokeAsync(replay, repository);

        Assert.Equal(first.Response.StatusCode, replay.Response.StatusCode);
        Assert.Equal(expectedContentType, replay.Response.ContentType);
        Assert.Equal(expected, ((MemoryStream)replay.Response.Body).ToArray());
        Assert.Equal("true", replay.Response.Headers[HttpIdempotencyMiddleware.ReplayedHeaderName]);
    }

    [Fact]
    public async Task Reject_an_invalid_idempotency_key_with_the_fixed_contract()
    {
        using var metrics = new AgentMetrics();
        var middleware = IdempotencyMiddleware(
            _ => throw new InvalidOperationException("The endpoint must not execute."),
            metrics);
        DefaultHttpContext context = IdempotentContext();
        context.Request.Headers[HttpIdempotencyMiddleware.HeaderName] = "short";

        await middleware.InvokeAsync(context, new MemoryIdempotencyRepository());

        await AssertErrorAsync(context, 400, 680002, "IDEMPOTENCY_KEY_INVALID");
    }

    [Fact]
    public async Task Reject_a_reused_idempotency_key_with_a_different_request()
    {
        using var metrics = new AgentMetrics();
        var middleware = IdempotencyMiddleware(
            context => AgentApiErrorResponseWriter.WriteAsync(
                context,
                "REQUEST_INVALID",
                "Deterministic response.",
                httpStatus: StatusCodes.Status201Created,
                cancellationToken: context.RequestAborted),
            metrics);
        var repository = new MemoryIdempotencyRepository();
        DefaultHttpContext first = IdempotentContext();
        await middleware.InvokeAsync(first, repository);
        DefaultHttpContext reused = IdempotentContext("{\"different\":true}");

        await middleware.InvokeAsync(reused, repository);

        await AssertErrorAsync(reused, 409, 680005, "IDEMPOTENCY_KEY_REUSED");
    }

    [Theory]
    [InlineData(false, 401, 600006, "AUTHENTICATION_REQUIRED")]
    [InlineData(true, 403, 600007, "AUTHORIZATION_DENIED")]
    public async Task Map_authentication_and_authorization_failures(
        bool forbidden,
        int httpStatus,
        int businessStatus,
        string errorCode)
    {
        DefaultHttpContext context = Context();
        var services = new ServiceCollection();
        services.AddSingleton<IAuthenticationService>(new PassiveAuthenticationService());
        AddAgentJsonOptions(services);
        context.RequestServices = services.BuildServiceProvider();
        var handler = new AgentAuthorizationResultHandler();
        AuthorizationPolicy policy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .Build();
        PolicyAuthorizationResult result = forbidden
            ? PolicyAuthorizationResult.Forbid(AuthorizationFailure.ExplicitFail())
            : PolicyAuthorizationResult.Challenge();

        await handler.HandleAsync(
            _ => throw new InvalidOperationException("The endpoint must not execute."),
            context,
            policy,
            result);

        await AssertErrorAsync(context, httpStatus, businessStatus, errorCode);
    }

    [Fact]
    public async Task Map_audit_dependency_failure()
    {
        using var metrics = new AgentMetrics();
        var middleware = new AgentOperationAuditMiddleware(
            _ => Task.CompletedTask,
            TestUser.Instance,
            TimeProvider.System,
            metrics,
            NullLogger<AgentOperationAuditMiddleware>.Instance);
        DefaultHttpContext context = Context();
        context.Request.Path = "/api/test";

        await middleware.InvokeAsync(context, new FailingAuditRepository());

        await AssertErrorAsync(
            context,
            StatusCodes.Status503ServiceUnavailable,
            680001,
            "AGENT_AUDIT_UNAVAILABLE");
    }

    [Fact]
    public async Task Map_draining_host_capacity_failure()
    {
        using var metrics = new AgentMetrics();
        IOptions<AgentCapacityOptions> options = Options.Create(
            new AgentCapacityOptions { RetryAfterSeconds = 3 });
        var drainState = new HostDrainState(metrics);
        drainState.BeginDrain();
        var middleware = new ExpensiveRequestAdmissionMiddleware(
            _ => throw new InvalidOperationException("The endpoint must not execute."),
            new ExpensiveRequestAdmissionGate(options),
            options,
            metrics,
            drainState);
        DefaultHttpContext context = Context();
        context.Request.Method = HttpMethods.Post;
        context.Request.Path = "/api/evaluation-batches";

        await middleware.InvokeAsync(context);

        Assert.Equal("3", context.Response.Headers.RetryAfter);
        await AssertErrorAsync(
            context,
            StatusCodes.Status503ServiceUnavailable,
            600010,
            "AGENT_INSTANCE_DRAINING");
    }

    [Fact]
    public async Task Execute_the_registered_rate_limit_rejection_callback()
    {
        using var metrics = new AgentMetrics();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(metrics);
        AddAgentJsonOptions(services);
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AgentRateLimit:WindowSeconds"] = "7",
                ["Audience:Secret"] = "agent-test-shared-jwt-signing-secret-32",
                ["Audience:Issuer"] = "agent-test",
                ["Audience:Audience"] = "agent-test-client"
            })
            .Build();
        _ = new AppSettings(configuration);
        Type extensions = typeof(AgentAuthorizationResultHandler).Assembly.GetType(
            "EU.Core.Api.Agent.Security.AgentApiSecurityServiceCollectionExtensions",
            throwOnError: true)!;
        MethodInfo addSecurity = extensions.GetMethod(
            "AddAgentApiHttpSecurity",
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)!;
        addSecurity.Invoke(null, [services, configuration]);
        using ServiceProvider provider = services.BuildServiceProvider();
        IAuthenticationSchemeProvider schemeProvider = provider
            .GetRequiredService<IAuthenticationSchemeProvider>();
        AuthenticationScheme? defaultScheme =
            await schemeProvider.GetDefaultAuthenticateSchemeAsync();
        Assert.Equal(JwtBearerDefaults.AuthenticationScheme, defaultScheme?.Name);
        Assert.Null(await schemeProvider.GetSchemeAsync("AgentDevelopment"));
        AuthorizationOptions authorization = provider
            .GetRequiredService<IOptions<AuthorizationOptions>>()
            .Value;
        AssertAuthenticatedOnly(authorization.FallbackPolicy);
        string[] agentPolicies =
        [
            AgentAuthorizationPolicies.Admin,
            AgentAuthorizationPolicies.Debug,
            AgentAuthorizationPolicies.Chat,
            AgentAuthorizationPolicies.AuditRead,
            AgentAuthorizationPolicies.HistoryRead,
            AgentAuthorizationPolicies.ApprovalRead,
            AgentAuthorizationPolicies.ApprovalDecide,
            AgentAuthorizationPolicies.ApprovalDecideHighRisk
        ];
        foreach (string policyName in agentPolicies)
        {
            AssertAuthenticatedOnly(authorization.GetPolicy(policyName));
        }
        RateLimiterOptions options = provider
            .GetRequiredService<IOptions<RateLimiterOptions>>()
            .Value;
        Assert.NotNull(options.OnRejected);
        using var limiter = new ConcurrencyLimiter(new ConcurrencyLimiterOptions
        {
            PermitLimit = 1,
            QueueLimit = 0
        });
        using RateLimitLease acquired = limiter.AttemptAcquire();
        using RateLimitLease rejected = limiter.AttemptAcquire();
        Assert.True(acquired.IsAcquired);
        Assert.False(rejected.IsAcquired);
        DefaultHttpContext context = Context();
        context.RequestServices = provider;

        await options.OnRejected!(new OnRejectedContext
        {
            HttpContext = context,
            Lease = rejected
        }, CancellationToken.None);

        Assert.Equal("7", context.Response.Headers.RetryAfter);
        await AssertErrorAsync(
            context,
            StatusCodes.Status429TooManyRequests,
            600008,
            "AGENT_RATE_LIMIT_EXCEEDED");
    }

    private static void AssertAuthenticatedOnly(AuthorizationPolicy? policy)
    {
        Assert.NotNull(policy);
        Assert.Single(policy.Requirements);
        Assert.IsType<Microsoft.AspNetCore.Authorization.Infrastructure.DenyAnonymousAuthorizationRequirement>(
            policy.Requirements[0]);
    }

    private static DefaultHttpContext Context(ILoggerProvider? loggerProvider = null)
    {
        var context = new DefaultHttpContext
        {
            TraceIdentifier = "trace-host-error"
        };
        var services = new ServiceCollection();
        services.AddLogging(builder =>
        {
            if (loggerProvider is not null)
                builder.AddProvider(loggerProvider);
        });
        AddAgentJsonOptions(services);
        context.RequestServices = services.BuildServiceProvider();
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static void AddAgentJsonOptions(IServiceCollection services) =>
        services.AddMvcCore()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy = null;
                options.JsonSerializerOptions.DictionaryKeyPolicy = null;
                options.JsonSerializerOptions.Converters.Add(
                    new JsonStringEnumConverter(namingPolicy: null, allowIntegerValues: false));
            });

    private static HttpIdempotencyMiddleware IdempotencyMiddleware(
        RequestDelegate next,
        AgentMetrics metrics) => new(
            next,
            Options.Create(new AgentIdempotencyOptions()),
            TestUser.Instance,
            TimeProvider.System,
            metrics);

    private static DefaultHttpContext IdempotentContext(string body = "{}")
    {
        DefaultHttpContext context = Context();
        context.Request.Method = HttpMethods.Post;
        context.Request.Path = "/api/evaluation-batches";
        context.Request.Headers[HttpIdempotencyMiddleware.HeaderName] = "request-1234";
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body));
        context.User = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.Name, "operator"),
                new Claim("TenantId", "0")
            ],
            "test"));
        return context;
    }

    private static async Task AssertErrorAsync(
        DefaultHttpContext context,
        int httpStatus,
        int businessStatus,
        string errorCode)
    {
        Assert.Equal(httpStatus, context.Response.StatusCode);
        Assert.StartsWith("application/json", context.Response.ContentType);
        Assert.Equal("trace-host-error", context.Response.Headers[CorrelationIdMiddleware.HeaderName]);
        context.Response.Body.Position = 0;
        using JsonDocument document = await JsonDocument.ParseAsync(context.Response.Body);
        JsonElement root = document.RootElement;
        Assert.Equal(businessStatus, root.GetProperty("Status").GetInt32());
        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Equal(JsonValueKind.Null, root.GetProperty("MessageDev").ValueKind);
        Assert.Equal(errorCode, root.GetProperty("Data").GetProperty("ErrorCode").GetString());
        Assert.Equal("trace-host-error", root.GetProperty("Data").GetProperty("TraceId").GetString());
    }

    private sealed class TestUser : IUser
    {
        public static readonly TestUser Instance = new();
        public string Name => "operator";
        public Guid? ID { get; } = Guid.Parse("879beff4-716f-4c18-b952-92f60a9e71d9");
        public SmUsers UserInfo => new();
        public Guid? CompanyId => null;
        public Guid? GroupId => null;
        public long TenantId => 0;
        public long? SessionId => null;
        public ServiceResult<string> MessageModel { get; set; } = new();
        public bool IsAuthenticated() => true;
        public IEnumerable<Claim> GetClaimsIdentity() => [];
        public List<string> GetClaimValueByType(string ClaimType) => [];
        public string GetToken() => string.Empty;
        public string GetPlatform() => string.Empty;
        public List<string> GetUserInfoFromToken(string ClaimType) => [];
    }

    private sealed class MemoryIdempotencyRepository : IHttpIdempotencyRepository
    {
        private HttpIdempotencyRecord? _record;

        public Task<HttpIdempotencyBeginResult> BeginAsync(
            HttpIdempotencyRecord pending,
            DateTimeOffset nowUtc,
            CancellationToken cancellationToken = default)
        {
            if (_record is not null)
                return Task.FromResult(new HttpIdempotencyBeginResult(false, _record));
            _record = pending;
            return Task.FromResult(new HttpIdempotencyBeginResult(true, pending));
        }

        public Task<bool> CompleteAsync(
            string scopeSha256,
            string requestSha256,
            int responseStatusCode,
            string responseContentType,
            string responseLocation,
            byte[] responseBody,
            CancellationToken cancellationToken = default)
        {
            _record = _record! with
            {
                Status = HttpIdempotencyStatus.Completed,
                ResponseStatusCode = responseStatusCode,
                ResponseContentType = responseContentType,
                ResponseLocation = responseLocation,
                ResponseBody = responseBody
            };
            return Task.FromResult(true);
        }

        public Task MarkIndeterminateAsync(
            string scopeSha256,
            string requestSha256,
            CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task AbandonAsync(
            string scopeSha256,
            string requestSha256,
            CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class PassiveAuthenticationService : IAuthenticationService
    {
        public Task<AuthenticateResult> AuthenticateAsync(
            HttpContext context,
            string? scheme) => Task.FromResult(AuthenticateResult.NoResult());

        public Task ChallengeAsync(
            HttpContext context,
            string? scheme,
            AuthenticationProperties? properties)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        }

        public Task ForbidAsync(
            HttpContext context,
            string? scheme,
            AuthenticationProperties? properties)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        }

        public Task SignInAsync(
            HttpContext context,
            string? scheme,
            ClaimsPrincipal principal,
            AuthenticationProperties? properties) => Task.CompletedTask;

        public Task SignOutAsync(
            HttpContext context,
            string? scheme,
            AuthenticationProperties? properties) => Task.CompletedTask;
    }

    private sealed class FailingAuditRepository : IAgAgentOperationAuditServices
    {
        public Task SaveAsync(
            AgentOperationAuditRecord record,
            CancellationToken cancellationToken = default) =>
            Task.FromException(new InvalidOperationException("audit unavailable"));

        public Task<IReadOnlyList<AgentOperationAuditRecord>> ListAsync(
            string tenantId,
            int take,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<AgentOperationAuditRecord>>([]);
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Production;
        public string ApplicationName { get; set; } = "EU.Core.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    private sealed class CapturingLoggerProvider : ILoggerProvider
    {
        public List<string> Messages { get; } = [];

        public ILogger CreateLogger(string categoryName) => new CapturingLogger(Messages);

        public void Dispose()
        {
        }

        private sealed class CapturingLogger(List<string> messages) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(
                LogLevel logLevel,
                EventId eventId,
                TState state,
                Exception? exception,
                Func<TState, Exception?, string> formatter)
            {
                lock (messages)
                    messages.Add(formatter(state, exception));
            }
        }
    }
}
