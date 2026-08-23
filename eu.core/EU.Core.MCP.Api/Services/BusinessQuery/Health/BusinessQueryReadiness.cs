using EU.Core.Api.MCP.Services.BusinessQuery.Auditing;
using EU.Core.Api.MCP.Services.BusinessQuery.Configuration;
using EU.Core.Api.MCP.Services.BusinessQuery.Policy;
using EU.Core.Api.MCP.Services.BusinessQuery.Security;
using Microsoft.Extensions.Options;
using SqlSugar;

namespace EU.Core.Api.MCP.Services.BusinessQuery.Health;

public sealed class BusinessQueryReadiness(
    SqliteBusinessQueryAuditRepository auditRepository,
    SqliteBusinessQueryReplayRepository replayRepository,
    SqliteBusinessQueryQuotaStore quotaStore,
    ISqlSugarClient database,
    BusinessQueryExecutionContextKeyResolver executionContextKeys,
    BusinessQueryServiceTokenResolver serviceTokenResolver,
    IOptions<BusinessQueryOptions> options)
{
    public async Task<bool> IsReadyAsync(CancellationToken cancellationToken)
    {
        try
        {
            await auditRepository.VerifyAsync(cancellationToken);
            await replayRepository.VerifyAsync(cancellationToken);
            await quotaStore.VerifyAsync(cancellationToken);
            BusinessQueryOptions configuration = options.Value;
            database.Ado.CommandTimeOut = configuration.CommandTimeoutSeconds;
            IReadOnlyList<int> probe = await database.Ado.SqlQueryAsync<int>(
                "SELECT 1",
                new { },
                cancellationToken);
            if (probe.Count != 1 || probe[0] != 1)
            {
                return false;
            }
            string serviceToken = serviceTokenResolver.Resolve();
            IReadOnlyList<byte[]> keys = executionContextKeys.ResolveVerificationKeys();
            foreach (byte[] key in keys)
            {
                System.Security.Cryptography.CryptographicOperations.ZeroMemory(key);
            }

            return serviceToken.Length >= 32
                && keys.Count > 0;
        }
        catch
        {
            return false;
        }
    }
}
