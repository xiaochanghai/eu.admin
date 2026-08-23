using System.Security.Cryptography;
using System.Text;
using EU.Core.Api.MCP.Services.BusinessQuery.Contracts;

namespace EU.Core.Api.MCP.Services.BusinessQuery.Policy;

public static class BusinessQueryEvaluationTimeFingerprint
{
    public static string Compute(BusinessQueryEvaluationTime value)
    {
        ArgumentNullException.ThrowIfNull(value);
        string canonical = string.Join(
            "|",
            value.EvaluatedAtUtc.ToUniversalTime().ToString("O"),
            value.TimeZoneId,
            value.StartUtc?.ToUniversalTime().ToString("O") ?? string.Empty,
            value.EndUtc?.ToUniversalTime().ToString("O") ?? string.Empty);
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
    }
}
