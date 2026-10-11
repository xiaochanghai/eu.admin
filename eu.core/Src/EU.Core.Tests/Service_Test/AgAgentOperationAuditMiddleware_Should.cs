using EU.Core.IServices.Abstractions.Auditing;
using EU.Core.IServices;
using EU.Core.Api.Agent.Security;
using EU.Core.Api.Agent.Observability;
using EU.Core.Common.HttpContextUser;
using EU.Core.Model;
using EU.Core.Model.Entity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging.Abstractions;
using System.Security.Claims;
using Xunit;

namespace EU.Core.Tests.Service_Test;

public sealed class AgAgentOperationAuditMiddleware_Should
{
    [Fact]
    public async Task Persist_terminal_audit_only_after_the_response_completes()
    {
        using var metrics = new AgentMetrics();
        var records = new RecordingAuditRepository();
        var middleware = new AgentOperationAuditMiddleware(
            _ => Task.CompletedTask,
            TestUser.Instance,
            TimeProvider.System,
            metrics,
            NullLogger<AgentOperationAuditMiddleware>.Instance);
        var context = new DefaultHttpContext();
        var response = new CompletingResponseFeature();
        context.Features.Set<IHttpResponseFeature>(response);
        context.Request.Path = "/api/chat/conversations";

        await middleware.InvokeAsync(context, records);

        Assert.Collection(records.Records,
            record => Assert.Equal("Started", record.Outcome));

        await response.CompleteAsync();

        Assert.Collection(
            records.Records,
            record => Assert.Equal("Started", record.Outcome),
            record => Assert.Equal("Succeeded", record.Outcome));
    }

    [Fact]
    public void Resolve_audit_repository_per_request()
    {
        Type middlewareType = typeof(AgentOperationAuditMiddleware);

        Assert.DoesNotContain(
            middlewareType.GetConstructors().SelectMany(constructor =>
                constructor.GetParameters()),
            parameter => parameter.ParameterType ==
                typeof(IAgAgentOperationAuditServices));

        System.Reflection.MethodInfo invokeMethod = Assert.Single(
            middlewareType.GetMethods(),
            method => method.Name == nameof(AgentOperationAuditMiddleware.InvokeAsync));
        Type[] parameterTypes = invokeMethod
            .GetParameters()
            .Select(parameter => parameter.ParameterType)
            .ToArray();

        Assert.Equal(
            [typeof(HttpContext), typeof(IAgAgentOperationAuditServices)],
            parameterTypes);
    }

    private sealed class RecordingAuditRepository : IAgAgentOperationAuditServices
    {
        public List<AgentOperationAuditRecord> Records { get; } = [];

        public Task SaveAsync(AgentOperationAuditRecord record, CancellationToken cancellationToken = default)
        {
            Records.Add(record);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<AgentOperationAuditRecord>> ListAsync(
            string tenantId,
            int take,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<AgentOperationAuditRecord>>([]);
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
        public List<string> GetClaimValueByType(string claimType) => [];
        public string GetToken() => string.Empty;
        public string GetPlatform() => string.Empty;
        public List<string> GetUserInfoFromToken(string claimType) => [];
    }

    private sealed class CompletingResponseFeature : IHttpResponseFeature
    {
        private readonly List<(Func<object, Task> Callback, object State)> _completed = [];

        public int StatusCode { get; set; } = StatusCodes.Status200OK;
        public string ReasonPhrase { get; set; } = string.Empty;
        public IHeaderDictionary Headers { get; set; } = new HeaderDictionary();
        public Stream Body { get; set; } = Stream.Null;
        public bool HasStarted => false;

        public void OnStarting(Func<object, Task> callback, object state)
        {
        }

        public void OnCompleted(Func<object, Task> callback, object state) =>
            _completed.Add((callback, state));

        public async Task CompleteAsync()
        {
            foreach ((Func<object, Task> callback, object state) in _completed.AsEnumerable().Reverse())
                await callback(state);
        }
    }
}
