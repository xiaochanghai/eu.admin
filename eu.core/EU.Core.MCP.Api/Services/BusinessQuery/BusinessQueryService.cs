using System.Text.Json;
using System.Text.Json.Serialization;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using EU.Core.Api.MCP.Services.BusinessQuery.Catalog;
using EU.Core.Api.MCP.Services.BusinessQuery.Compilation;
using EU.Core.Api.MCP.Services.BusinessQuery.Contracts;
using EU.Core.Api.MCP.Services.BusinessQuery.Execution;
using EU.Core.Api.MCP.Services.BusinessQuery.Configuration;
using EU.Core.Api.MCP.Services.BusinessQuery.Auditing;
using EU.Core.Api.MCP.Services.BusinessQuery.Security;
using EU.Core.Api.MCP.Services.BusinessQuery.Policy;
using EU.Core.Api.MCP.Services.BusinessQuery.Presentation;
using EU.Core.Api.MCP.Services.BusinessQuery.Time;
using EU.Core.Api.MCP.Services.BusinessQuery.Tooling;
using EU.Core.Api.MCP.Services.BusinessQuery.Validation;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using EU.Core.Model;

namespace EU.Core.Api.MCP.Services.BusinessQuery;

public sealed record QueryBusinessDataResponse(
    bool Succeeded,
    string? ErrorCode,
    BusinessQueryResult? Result,
    BusinessQueryPresentation? Presentation,
    BusinessQueryReceipt? Receipt);

public sealed class BusinessQueryService(
    ILogger<BusinessQueryService> logger,
    IBaseRepository<BdSupplier> baseDal,
    BusinessCatalogSnapshot catalog,
    BusinessQueryToolDefinition definition,
    IOptions<BusinessQueryOptions> options,
    IBusinessQueryQuotaStore quotaStore,
    IBusinessQueryAuditRepository auditRepository,
    BusinessQueryExecutionContextAccessor executionContextAccessor,
    BusinessQueryExecutionContextVerifier executionContextVerifier,
    IBusinessQueryExecutor executor,
    BusinessProjectCallerResolver projectCallerResolver,
    TimeProvider timeProvider) : BaseService<BusinessQueryService, BdSupplier>(logger, baseDal),
    IBusinessQueryService
{
    private static readonly JsonSerializerOptions PlanSerializer = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, false) }
    };

    public override object HandleInitialize(JsonElement? parameters) => new
    {
        protocolVersion = "2025-11-25",
        capabilities = new { tools = new { } },
        serverInfo = new
        {
            name = "EU.Core Business Query MCP Server",
            version = "1.0.0"
        }
    };

    public override object GetAvailableTools() => new
    {
        tools = new[]
        {
            new
            {
                name = definition.Name,
                description = definition.Description,
                inputSchema = JsonSerializer.Deserialize<JsonElement>(definition.InputSchemaJson),
                annotations = new
                {
                    readOnlyHint = true,
                    destructiveHint = false,
                    idempotentHint = true,
                    openWorldHint = false
                }
            }
        }
    };

    public override async Task<object> HandleToolCallAsync(
        JsonElement? parameters,
        CancellationToken cancellationToken)
    {
        if (parameters is not { ValueKind: JsonValueKind.Object } value
            || !value.TryGetProperty("name", out JsonElement nameElement))
        {
            throw new ArgumentException("Tool name is required.");
        }

        string toolName = nameElement.GetString() ?? string.Empty;
        string executionContextToken = ReadExecutionContextToken(value);
        BusinessQueryExecutionContextValidation validation =
            await executionContextVerifier.ValidateAsync(
                executionContextToken,
                toolName,
                cancellationToken);
        if (!validation.Succeeded)
        {
            return await executionContextVerifier.RejectAsync(validation.ErrorCode);
        }

        if (!value.TryGetProperty("arguments", out JsonElement arguments))
        {
            throw new ArgumentException("Tool arguments are required.");
        }

        BusinessQueryPlan plan;
        try
        {
            plan = JsonSerializer.Deserialize<BusinessQueryPlan>(
                arguments.GetRawText(),
                PlanSerializer) ?? throw new JsonException();
        }
        catch (JsonException exception)
        {
            throw new ArgumentException("Tool arguments are invalid.", exception);
        }

        using IDisposable scope = executionContextAccessor.Enter(validation.Context!);
        QueryBusinessDataResponse response = await QueryAsync(
            plan.Entity,
            plan.Dimensions,
            plan.Measures,
            plan.Filters,
            plan.TimeRange,
            plan.OrderBy,
            plan.Limit,
            cancellationToken);
        return new CallToolResult
        {
            IsError = !response.Succeeded,
            Content =
            [
                new TextContentBlock
                {
                    Text = JsonSerializer.Serialize(response, PlanSerializer)
                }
            ]
        };
    }

    [McpServerTool(Name = BusinessQueryToolSchemaBuilder.ToolName, ReadOnly = true, Destructive = false, OpenWorld = false)]
    public async Task<QueryBusinessDataResponse> QueryAsync(
        string entity,
        IReadOnlyList<string> dimensions,
        IReadOnlyList<BusinessMeasure> measures,
        IReadOnlyList<BusinessFilter> filters,
        BusinessTimeRange? timeRange,
        IReadOnlyList<BusinessOrder> orderBy,
        int limit,
        CancellationToken cancellationToken)
    {
        Guid queryId = Guid.NewGuid();
        Stopwatch elapsed = Stopwatch.StartNew();
        BusinessQueryOptions configuration = options.Value;
        BusinessQueryExecutionContext trustedContext =
            executionContextAccessor.Current
            ?? throw new InvalidOperationException(
                "BUSINESS_QUERY_EXECUTION_CONTEXT_REQUIRED");
        string userId = trustedContext.UserId;
        var supplied = new BusinessQueryPlan(
            entity, dimensions, measures, filters, timeRange, orderBy, limit);
        BusinessQueryPlanParseResult parsed = new BusinessQueryPlanValidator().Parse(
            JsonSerializer.Serialize(supplied, PlanSerializer));
        if (!parsed.Succeeded)
        {
            return await AuditedAsync(
                queryId,
                userId,
                configuration.TenantId,
                catalog.Revision,
                string.Empty,
                [],
                string.Empty,
                0,
                elapsed,
                Failure(parsed.Error?.Code ?? "BUSINESS_QUERY_PLAN_INVALID"));
        }

        BusinessQueryPlan plan = parsed.Plan!;
        if (!catalog.Entities.TryGetValue(plan.Entity, out BusinessCatalogEntitySnapshot? rootEntity))
        {
            return await AuditedAsync(
                queryId, userId, configuration.TenantId, catalog.Revision,
                string.Empty, [], string.Empty, 0, elapsed,
                Failure("BUSINESS_QUERY_ENTITY_UNKNOWN"));
        }

        BusinessQueryTimeResolutionResult resolution =
            new BusinessQueryTimeRangeResolver(timeProvider).Resolve(plan, catalog);
        if (!resolution.Succeeded)
        {
            return await AuditedAsync(
                queryId, userId, configuration.TenantId, catalog.Revision,
                string.Empty, [], string.Empty, 0, elapsed,
                Failure(resolution.Error?.Code ?? "BUSINESS_QUERY_TIME_RANGE_INVALID"));
        }

        var policy = new BusinessQueryPolicy(
            new BusinessQueryPolicyOptions
            {
                TenantId = configuration.TenantId,
                DataSourceCode = configuration.DataSourceCode,
                MaximumResultRows = configuration.MaximumResultRows,
                MinimumGroupSize = configuration.MinimumGroupSize,
                MaximumComplexity = configuration.MaximumComplexity
            },
            quotaStore);
        // 每次查询使用独立客户端，避免并发请求互相覆盖 ADO 取消令牌和超时。
        using var queryDatabase = Db.CopyNew();
        queryDatabase.Ado.CommandTimeOut = configuration.CommandTimeoutSeconds;
        BusinessCallerContext? caller;
        try
        {
            string actualDialect = queryDatabase.CurrentConnectionConfig.DbType.ToString();
            if (!string.Equals(actualDialect, catalog.Dialect.ToString(), StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("The project database dialect does not match the catalog.");
            caller = await projectCallerResolver.ResolveAsync(trustedContext, rootEntity, configuration.TenantId,
                configuration.DataSourceCode, queryDatabase, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // 此时尚未预留配额或执行 SQL，仍须用独立于请求取消的令牌写入终态审计。
            await WriteAuditOrThrowAsync(new BusinessQueryAuditRecord(
                queryId, userId, configuration.TenantId, catalog.Revision,
                BusinessQueryPlanFingerprint.Compute(plan), [], string.Empty, 0,
                elapsed.ElapsedMilliseconds, "cancelled", "BUSINESS_QUERY_CANCELLED", timeProvider.GetUtcNow()));
            throw;
        }
        catch
        {
            return await AuditedAsync(queryId, userId, configuration.TenantId, catalog.Revision,
                string.Empty, [], string.Empty, 0, elapsed, Failure("BUSINESS_QUERY_AUTHORIZATION_UNAVAILABLE"));
        }
        if (caller is null)
            return await AuditedAsync(queryId, userId, configuration.TenantId, catalog.Revision,
                string.Empty, [], string.Empty, 0, elapsed, Failure("BUSINESS_QUERY_PERMISSION_DENIED"));
        BusinessQueryPolicyDecision decision;
        try
        {
            decision = await policy.AuthorizeAsync(caller, catalog, plan, resolution.EvaluationTime!, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // 策略检查/配额预留同样属于已验证请求的生命周期，取消后必须记录终态。
            // 未取得预留 ID，不能臆造 ID 结算；存储侧可能残留的预留由既有 TTL 机制失效。
            await WriteAuditOrThrowAsync(new BusinessQueryAuditRecord(
                queryId, userId, configuration.TenantId, catalog.Revision,
                BusinessQueryPlanFingerprint.Compute(plan), [], string.Empty, 0,
                elapsed.ElapsedMilliseconds, "cancelled", "BUSINESS_QUERY_CANCELLED", timeProvider.GetUtcNow()));
            throw;
        }
        catch
        {
            return await AuditedAsync(queryId, userId, configuration.TenantId, catalog.Revision,
                BusinessQueryPlanFingerprint.Compute(plan), [], string.Empty, 0, elapsed,
                Failure("BUSINESS_QUERY_POLICY_UNAVAILABLE"));
        }
        if (!decision.Allowed)
        {
            return await AuditedAsync(
                queryId, userId, configuration.TenantId, decision.CatalogRevision,
                decision.PlanHash, decision.AppliedRuleIds, string.Empty, 0, elapsed,
                Failure(decision.ErrorCode ?? "BUSINESS_QUERY_DENIED"));
        }

        QueryBusinessDataResponse response;
        string sqlTemplateHash = string.Empty;
        int rowCount = 0;
        BusinessQueryQuotaOutcome quotaOutcome = BusinessQueryQuotaOutcome.Failed;
        try
        {
            CompiledBusinessQuery compiled = new BusinessSqlCompiler().Compile(
                catalog, plan, decision, resolution.EvaluationTime!);
            sqlTemplateHash = Convert.ToHexStringLower(
                SHA256.HashData(Encoding.UTF8.GetBytes(compiled.CommandText)));
            BusinessQueryExecutionResult execution = await executor.ExecuteAsync(
                compiled,
                new BusinessDataSourceDescriptor(
                    configuration.DataSourceCode,
                    catalog.Dialect switch
                    {
                        BusinessCatalogDialect.Sqlite => "Microsoft.Data.Sqlite",
                        BusinessCatalogDialect.MySql => "SqlSugar.MySql",
                        _ => "Microsoft.Data.SqlClient"
                    },
                    catalog.Dialect,
                    configuration.CredentialAlias,
                    true),
                queryDatabase,
                new BusinessQueryExecutionLimits
                {
                    CommandTimeoutSeconds = configuration.CommandTimeoutSeconds,
                    MaximumRows = configuration.MaximumResultRows
                },
                cancellationToken);
            rowCount = execution.Result.Rows.Count;
            BusinessQueryReceipt receipt = BusinessQueryReceipt.Create(
                queryId, compiled, definition.ToolVersionHash, execution);
            BusinessQueryPresentation presentation =
                new BusinessQueryPresentationFormatter().Format(compiled, execution.Result,
                    rootEntity.ProjectModuleCode == "SD_SALES_ORDER_MNG" && compiled.Entity == "salesOrder"
                        ? await ProjectBusinessQueryPresentation.CreateAsync(compiled, execution.Result, queryDatabase, cancellationToken)
                        : null);
            quotaOutcome = BusinessQueryQuotaOutcome.Succeeded;
            response = new QueryBusinessDataResponse(
                true, null, execution.Result, presentation, receipt);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            await SettleWithoutRequestCancellationAsync(
                decision.QuotaReservationId!.Value,
                BusinessQueryQuotaOutcome.Cancelled);
            await WriteAuditOrThrowAsync(
                new BusinessQueryAuditRecord(
                    queryId, userId, configuration.TenantId, decision.CatalogRevision,
                    decision.PlanHash, decision.AppliedRuleIds, sqlTemplateHash, rowCount,
                    elapsed.ElapsedMilliseconds, "cancelled", "BUSINESS_QUERY_CANCELLED",
                    timeProvider.GetUtcNow()));
            throw;
        }
        catch (BusinessQueryExecutionException exception)
        {
            response = Failure(exception.Code);
        }
        catch (BusinessQueryCompilationException exception)
        {
            response = Failure(exception.Code);
        }
        catch (Exception)
        {
            response = Failure(BusinessQueryExecutionErrorCodes.ExecutionFailed);
        }

        try
        {
            await quotaStore.SettleAsync(
                decision.QuotaReservationId!.Value,
                quotaOutcome,
                CancellationToken.None);
        }
        catch
        {
            response = Failure("BUSINESS_QUERY_QUOTA_SETTLEMENT_FAILED");
        }

        return await AuditedAsync(
            queryId, userId, configuration.TenantId, decision.CatalogRevision,
            decision.PlanHash, decision.AppliedRuleIds, sqlTemplateHash, rowCount,
            elapsed, response);
    }

    private async Task<QueryBusinessDataResponse> AuditedAsync(
        Guid queryId,
        string userId,
        string tenantId,
        long catalogRevision,
        string planHash,
        IReadOnlyList<string> policyRuleIds,
        string sqlTemplateHash,
        int rowCount,
        Stopwatch elapsed,
        QueryBusinessDataResponse response)
    {
        var record = new BusinessQueryAuditRecord(
            queryId,
            userId,
            tenantId,
            catalogRevision,
            planHash,
            policyRuleIds,
            sqlTemplateHash,
            rowCount,
            elapsed.ElapsedMilliseconds,
            response.Succeeded ? "succeeded" : "failed",
            response.ErrorCode,
            timeProvider.GetUtcNow());
        try
        {
            await auditRepository.WriteTerminalAsync(record, CancellationToken.None);
            return response;
        }
        catch
        {
            return Failure("BUSINESS_QUERY_AUDIT_UNAVAILABLE");
        }
    }

    private async Task SettleWithoutRequestCancellationAsync(
        Guid reservationId,
        BusinessQueryQuotaOutcome outcome)
    {
        try
        {
            await quotaStore.SettleAsync(reservationId, outcome, CancellationToken.None);
        }
        catch
        {
            // Terminal audit remains mandatory even if quota settlement is unavailable.
        }
    }

    private async Task WriteAuditOrThrowAsync(BusinessQueryAuditRecord record)
    {
        try
        {
            await auditRepository.WriteTerminalAsync(record, CancellationToken.None);
        }
        catch
        {
            throw new InvalidOperationException("BUSINESS_QUERY_AUDIT_UNAVAILABLE");
        }
    }

    private static QueryBusinessDataResponse Failure(string code) =>
        new(false, code, null, null, null);

    private static string ReadExecutionContextToken(JsonElement parameters)
    {
        if (!parameters.TryGetProperty("_meta", out JsonElement metadata)
            || metadata.ValueKind != JsonValueKind.Object
            || !metadata.TryGetProperty(
                BusinessQueryExecutionContextVerifier.MetadataKey,
                out JsonElement token)
            || token.ValueKind != JsonValueKind.String)
        {
            return string.Empty;
        }

        return token.GetString() ?? string.Empty;
    }
}
