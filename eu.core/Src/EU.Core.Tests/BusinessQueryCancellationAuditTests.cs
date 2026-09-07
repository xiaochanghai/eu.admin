#nullable enable
using System.Reflection;
using EU.Core.Api.MCP.Services.BusinessQuery;
using EU.Core.Api.MCP.Services.BusinessQuery.Auditing;
using EU.Core.Api.MCP.Services.BusinessQuery.Catalog;
using EU.Core.Api.MCP.Services.BusinessQuery.Configuration;
using EU.Core.Api.MCP.Services.BusinessQuery.Contracts;
using EU.Core.Api.MCP.Services.BusinessQuery.Policy;
using EU.Core.Api.MCP.Services.BusinessQuery.Security;
using EU.Core.Api.MCP.Services.BusinessQuery.Tooling;
using EU.Core.IRepository.Base;
using EU.Core.Model.Entity;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SqlSugar;
using Xunit;

namespace EU.Core.Tests;

/// <summary>覆盖调用者解析、策略检查和配额预留的终态审计；所有依赖均离线，不打开数据库连接。</summary>
public sealed class BusinessQueryCancellationAuditTests
{
    [Theory]
    [InlineData("caller", false)]
    [InlineData("caller", true)]
    [InlineData("policy", false)]
    [InlineData("policy", true)]
    [InlineData("quota", false)]
    [InlineData("quota", true)]
    [InlineData("quota-failure", false)]
    [InlineData("quota-failure", true)]
    public async Task Pre_execution_exit_writes_terminal_audit(string stage, bool auditFails)
    {
        var loaded = new BusinessSemanticCatalogLoader().Load(File.ReadAllText(Path.Combine(
            AppContext.BaseDirectory, "ProjectCatalogs", "sales-order.sqlserver.json")));
        Assert.True(loaded.Succeeded, loaded.Error?.Message);
        var catalog = loaded.Snapshot!;
        using var cancellation = new CancellationTokenSource();
        using var database = new SqlSugarClient(new ConnectionConfig
        {
            // 仅构造/复制 ORM 客户端；取消替身在任何 SQL 执行前结束请求。
            ConnectionString = "Server=127.0.0.1;Database=offline_not_connected;Integrated Security=true",
            DbType = SqlSugar.DbType.SqlServer,
            IsAutoCloseConnection = true
        });
        var repository = DispatchProxy.Create<IBaseRepository<BdSupplier>, RepositoryProxy>();
        ((RepositoryProxy)(object)repository).Database = database;
        var audit = new AuditRecorder(auditFails);
        var quota = new ScenarioQuota(stage, cancellation);
        var accessor = new BusinessQueryExecutionContextAccessor();
        var context = new BusinessQueryExecutionContext(Guid.NewGuid().ToString("D"), "7",
            ["business.project.query"], "offline-trace", Guid.NewGuid(), Guid.NewGuid(), "offline-jti");
        using var contextScope = accessor.Enter(context);
        var resolver = new ScenarioResolver(stage, cancellation);
        var service = new BusinessQueryService(NullLogger<BusinessQueryService>.Instance, repository,
            catalog, new BusinessQueryToolSchemaBuilder().Build(catalog), Options.Create(new BusinessQueryOptions
            {
                TenantId = "7", Dialect = "SqlServer", DataSourceCode = catalog.DataSourceCode
            }), quota, audit, accessor, null!, null!, resolver, TimeProvider.System);

        Task<QueryBusinessDataResponse> Invoke() => service.QueryAsync("salesOrder", ["salesOrder.currencyId"],
            [new BusinessMeasure("salesOrder.grossAmount", BusinessAggregation.Sum, "total")], [], null, [], 10, cancellation.Token);

        if (stage == "quota-failure")
        {
            var response = await Invoke();
            Assert.False(response.Succeeded);
            Assert.Equal(auditFails ? "BUSINESS_QUERY_AUDIT_UNAVAILABLE" : "BUSINESS_QUERY_POLICY_UNAVAILABLE", response.ErrorCode);
            Assert.Null(response.Result);
            Assert.Null(response.Receipt);
        }
        else if (auditFails)
        {
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(Invoke);
            Assert.Equal("BUSINESS_QUERY_AUDIT_UNAVAILABLE", exception.Message);
        }
        else
        {
            var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(Invoke);
            Assert.Equal(cancellation.Token, exception.CancellationToken);
        }

        Assert.True(resolver.Called);
        Assert.Equal(stage.StartsWith("quota", StringComparison.Ordinal) ? 1 : 0, quota.Calls);
        Assert.Equal(0, quota.Settlements);
        var record = Assert.Single(audit.Records);
        Assert.Equal(stage == "quota-failure" ? "failed" : "cancelled", record.TerminalStatus);
        Assert.Equal(stage == "quota-failure" ? "BUSINESS_QUERY_POLICY_UNAVAILABLE" : "BUSINESS_QUERY_CANCELLED", record.ErrorCode);
        Assert.Equal(context.UserId, record.UserId);
        Assert.Equal("7", record.TenantId);
        Assert.Equal(catalog.Revision, record.CatalogRevision);
        Assert.NotEmpty(record.QueryPlanHash);
        Assert.Empty(record.SqlTemplateHash);
        Assert.Equal(0, record.RowCount);
        Assert.False(audit.Token.CanBeCanceled);
    }

    public class RepositoryProxy : DispatchProxy
    {
        public ISqlSugarClient Database { get; set; } = null!;
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            targetMethod?.Name == "get_Db" ? Database : throw new InvalidOperationException("Unexpected repository access");
    }

    private sealed class ScenarioResolver(string stage, CancellationTokenSource cancellation) : BusinessProjectCallerResolver(null!)
    {
        public bool Called { get; private set; }
        public override Task<BusinessCallerContext?> ResolveAsync(BusinessQueryExecutionContext context, BusinessCatalogEntitySnapshot entity,
            string tenantId, string dataSourceCode, ISqlSugarClient database, CancellationToken cancellationToken)
        {
            Called = true;
            if (stage is "caller" or "policy") cancellation.Cancel();
            if (stage == "caller") return Task.FromCanceled<BusinessCallerContext?>(cancellationToken);
            return Task.FromResult<BusinessCallerContext?>(new BusinessCallerContext(context.UserId, context.TenantId,
                context.Permissions, [dataSourceCode], new Dictionary<string, IReadOnlyList<string>>()));
        }
    }

    private sealed class AuditRecorder(bool fail) : IBusinessQueryAuditRepository
    {
        public List<BusinessQueryAuditRecord> Records { get; } = [];
        public CancellationToken Token { get; private set; }
        public Task WriteTerminalAsync(BusinessQueryAuditRecord record, CancellationToken cancellationToken)
        {
            Token = cancellationToken;
            Records.Add(record);
            return fail ? Task.FromException(new InvalidOperationException("offline audit failure")) : Task.CompletedTask;
        }
        public Task WriteSecurityRejectionAsync(BusinessQuerySecurityAuditRecord record, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Unexpected security rejection");
    }

    private sealed class ScenarioQuota(string stage, CancellationTokenSource cancellation) : IBusinessQueryQuotaStore
    {
        public int Calls { get; private set; }
        public int Settlements { get; private set; }
        public Task<BusinessQueryQuotaReservationResult> TryReserveAsync(BusinessQueryQuotaRequest request, CancellationToken cancellationToken)
        {
            Calls++;
            if (stage == "quota")
            {
                cancellation.Cancel();
                return Task.FromCanceled<BusinessQueryQuotaReservationResult>(cancellationToken);
            }
            throw new InvalidOperationException("Offline quota failure");
        }
        public Task SettleAsync(Guid reservationId, BusinessQueryQuotaOutcome outcome, CancellationToken cancellationToken)
        {
            Settlements++;
            throw new InvalidOperationException("No quota to settle");
        }
    }
}
