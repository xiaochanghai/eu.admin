#nullable enable
using System.Reflection;
using System.Text;
using System.Text.Json;
using EU.Core.Agent.Infrastructure.Mcp;
using EU.Core.Api.Agent.Configuration;
using EU.Core.Api.MCP.Services.BusinessQuery;
using EU.Core.Api.MCP.Services.BusinessQuery.Auditing;
using EU.Core.Api.MCP.Services.BusinessQuery.Catalog;
using EU.Core.Api.MCP.Services.BusinessQuery.Configuration;
using EU.Core.Api.MCP.Services.BusinessQuery.Errors;
using EU.Core.Api.MCP.Services.BusinessQuery.Persistence;
using EU.Core.Api.MCP.Services.BusinessQuery.Security;
using EU.Core.Api.MCP.Services.BusinessQuery.Tooling;
using EU.Core.Api.MCP.Services.BusinessQuery.Validation;
using EU.Core.IRepository.Base;
using EU.Core.IServices.Mcp;
using EU.Core.IServices.Runtime;
using EU.Core.IServices.UnifiedEntry;
using EU.Core.Model.Entity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using ModelContextProtocol.Protocol;
using Xunit;

namespace EU.Core.Tests;

/// <summary>真实工具入口原始 JSON 回归；仅测试凭据和唯一临时 SQLite 防重放库，禁止访问业务库。</summary>
public sealed class BusinessQueryRawPlanTests
{
    private const string ValidPlan = """
        {"entity":"unknownEntity","dimensions":["salesOrder.currencyId"],"measures":[],"filters":[],"timeRange":null,"orderBy":[],"limit":10}
        """;

    public static IEnumerable<object[]> InvalidPlans()
    {
        yield return [ValidPlan.Replace("\"filters\":[]", "\"filters\":[{\"field\":\"salesOrder.orderNo\",\"operator\":\"equal\",\"value\":\"restricted\"}],\"filters\":[]"), BusinessQueryErrorCodes.PlanDuplicateProperty];
        yield return [ValidPlan.Replace("\"measures\":[]", "\"measures\":[{\"field\":\"salesOrder.netAmount\",\"field\":\"salesOrder.grossAmount\",\"aggregation\":\"sum\",\"resultKey\":\"total\"}]"), BusinessQueryErrorCodes.PlanDuplicateProperty];
        yield return [ValidPlan.Replace("\"limit\":10", "\"limit\":10,\"sql\":\"ignored\""), BusinessQueryErrorCodes.PlanUnknownProperty];
        yield return [ValidPlan.Replace("\"filters\":[]", "\"filters\":[{\"field\":\"salesOrder.orderNo\",\"operator\":\"equal\",\"value\":\"A\",\"unknown\":true}]"), BusinessQueryErrorCodes.PlanUnknownProperty];
        yield return [ValidPlan.Insert(1, new string(' ', BusinessQueryPlanValidator.MaximumUtf8Bytes)), BusinessQueryErrorCodes.PlanTooLarge];
        yield return [ValidPlan.Replace("\"filters\":[]", "\"filters\":[{\"field\":\"salesOrder.orderNo\",\"operator\":\"equal\",\"value\":" + new string('[', 20) + "1" + new string(']', 20) + "}]"), BusinessQueryErrorCodes.PlanInvalidJson];
        yield return [ValidPlan.Replace("\"limit\":10", "\"limit\":101"), BusinessQueryErrorCodes.PlanLimitExceeded];
        yield return ["[]", BusinessQueryErrorCodes.PlanInvalid];
        yield return ["null", BusinessQueryErrorCodes.PlanInvalid];
    }

    [Theory]
    [MemberData(nameof(InvalidPlans))]
    public async Task Original_arguments_are_rejected_and_terminally_audited_before_database_access(string json, string errorCode)
    {
        using var fixture = new Fixture();
        QueryBusinessDataResponse response = await fixture.Invoke(json);
        Assert.False(response.Succeeded);
        Assert.Equal(errorCode, response.ErrorCode);
        Assert.Null(response.Result);
        Assert.Null(response.Receipt);
        var record = Assert.Single(fixture.Audit.Records);
        Assert.Equal(errorCode, record.ErrorCode);
        Assert.Equal("failed", record.TerminalStatus);
        Assert.Equal(fixture.Identity.UserId, record.UserId);
        Assert.Equal(0, record.RowCount);
        Assert.Empty(record.SqlTemplateHash);
        Assert.False(fixture.Audit.Token.CanBeCanceled);
        Assert.Null(fixture.Accessor.Current);
    }

    [Fact]
    public async Task Canonical_plan_still_reaches_catalog_validation()
    {
        using var fixture = new Fixture();
        var response = await fixture.Invoke(ValidPlan);
        Assert.Equal("BUSINESS_QUERY_ENTITY_UNKNOWN", response.ErrorCode);
        Assert.Equal(response.ErrorCode, Assert.Single(fixture.Audit.Records).ErrorCode);
    }

    [Fact]
    public async Task Invalid_plan_audit_failure_is_not_reported_as_a_normal_plan_rejection()
    {
        using var fixture = new Fixture();
        fixture.Audit.Fail = true;
        var response = await fixture.Invoke(ValidPlan.Replace("\"limit\":10", "\"limit\":101"));
        Assert.Equal("BUSINESS_QUERY_AUDIT_UNAVAILABLE", response.ErrorCode);
        Assert.Equal(BusinessQueryErrorCodes.PlanLimitExceeded, Assert.Single(fixture.Audit.Records).ErrorCode);
    }

    private sealed class Fixture : IDisposable
    {
        private readonly ServiceProvider _provider;
        private readonly BusinessQueryStorePath _store;
        private readonly string _root = Path.Combine(Path.GetTempPath(), "eu-bq-raw-plan-" + Guid.NewGuid().ToString("N"));
        private readonly BusinessQueryToolPolicy _policy;
        private readonly PublishedMcpToolReference _tool;
        private readonly BusinessQueryService _service;
        public AuditRecorder Audit { get; } = new();
        public BusinessQueryExecutionContextAccessor Accessor { get; } = new();
        public AgentExecutionIdentity Identity { get; } = new(Guid.NewGuid().ToString("D"), "7", ["business.project.query"], "raw-plan-test");

        public Fixture()
        {
            var catalogResult = new BusinessSemanticCatalogLoader().Load(File.ReadAllText(Path.Combine(
                AppContext.BaseDirectory, "ProjectCatalogs", "sales-order.sqlserver.json")));
            Assert.True(catalogResult.Succeeded, catalogResult.Error?.Message);
            var catalog = catalogResult.Snapshot!;
            var definition = new BusinessQueryToolSchemaBuilder().Build(catalog);
            var services = new ServiceCollection();
            services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme).Configure(options =>
                options.TokenValidationParameters.IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.ASCII.GetBytes("offline-raw-plan-key-not-a-real-secret-1234567890")));
            _provider = services.BuildServiceProvider();
            var configuration = Options.Create(new BusinessQueryOptions
            {
                TenantId = Identity.TenantId, ServerCode = "business-query", ExecutionContextIssuer = "eu-agent",
                ExecutionContextAudience = "eu-mcp", AuditDatabasePath = "replay.db"
            });
            _store = new BusinessQueryStorePath(configuration, new HostingEnvironment { ContentRootPath = _root });
            var jwtOptions = _provider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>();
            var verifier = new BusinessQueryExecutionContextVerifier(configuration, definition,
                new BusinessQueryExecutionContextKeyResolver(jwtOptions),
                new SqliteBusinessQueryReplayRepository(_store, configuration, TimeProvider.System), Accessor, Audit, TimeProvider.System);
            _policy = new BusinessQueryToolPolicy("business-query", definition.Name, new Uri("https://localhost"), "eu-agent", "eu-mcp",
                "alias:test-jwt", definition.CatalogRevision, definition.CatalogHash, definition.ToolVersionHash, TimeSpan.FromSeconds(45), false);
            _tool = new PublishedMcpToolReference(Guid.NewGuid(), "business-query", "Business Query", Guid.NewGuid(),
                definition.Name, definition.Description, definition.InputSchemaJson, McpToolRisk.ReadOnly, definition.ToolVersionHash);
            _service = new BusinessQueryService(NullLogger<BusinessQueryService>.Instance,
                DispatchProxy.Create<IBaseRepository<BdSupplier>, NoDatabaseRepository>(), catalog, definition, configuration,
                null!, Audit, Accessor, verifier, null!, null!, TimeProvider.System);
        }

        public async Task<QueryBusinessDataResponse> Invoke(string json)
        {
            var tokenProvider = new BusinessQueryContextTokenProvider(new DevelopmentBusinessQuerySigningKeyResolver(
                _provider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()), TimeProvider.System);
            string token = await tokenProvider.CreateAsync(new McpInvocationContext(Identity, Guid.NewGuid()), _policy, _tool);
            // 直接构造原始请求，保留 arguments 的重复键、空白和嵌套深度。
            string metadata = JsonSerializer.Serialize(new Dictionary<string, string>
            {
                [BusinessQueryExecutionContextVerifier.MetadataKey] = token
            });
            using var request = JsonDocument.Parse("{\"name\":\"query_business_data\",\"arguments\":" + json + ",\"_meta\":" + metadata + "}");
            var result = Assert.IsType<CallToolResult>(await _service.HandleToolCallAsync(request.RootElement, default));
            Assert.True(result.IsError);
            var text = Assert.IsType<TextContentBlock>(Assert.Single(result.Content)).Text;
            return JsonSerializer.Deserialize<QueryBusinessDataResponse>(text,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        }

        public void Dispose()
        {
            _provider.Dispose();
            using var connection = new SqliteConnection(_store.ConnectionString);
            SqliteConnection.ClearPool(connection);
            Directory.Delete(_root, recursive: true);
        }
    }

    public class NoDatabaseRepository : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new InvalidOperationException("Raw plan rejection must not access the business database.");
    }

    private sealed class AuditRecorder : IBusinessQueryAuditRepository
    {
        public bool Fail { get; set; }
        public List<BusinessQueryAuditRecord> Records { get; } = [];
        public CancellationToken Token { get; private set; }
        public Task WriteTerminalAsync(BusinessQueryAuditRecord record, CancellationToken cancellationToken)
        {
            Records.Add(record);
            Token = cancellationToken;
            return Fail ? Task.FromException(new InvalidOperationException("offline audit failure")) : Task.CompletedTask;
        }
        public Task WriteSecurityRejectionAsync(BusinessQuerySecurityAuditRecord record, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Expected a valid signed execution context.");
    }
}
