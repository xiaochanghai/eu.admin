using System.Security.Claims;
using Microsoft.Extensions.Options;

namespace EU.Core.Api.Agent.Configuration;

public sealed class AgentAuthenticationOptions
{
    public const string SectionName = "AgentAuthentication";

    public string TenantId { get; init; } = string.Empty;

    public bool DevelopmentBypassEnabled { get; init; }

    public bool EnforcePermissionClaims { get; init; }

    public string PermissionClaimType { get; init; } = "permission";

    public string TenantClaimType { get; init; } = "tenant_id";

    public string UserIdClaimType { get; init; } = "sub";

    public string SharedTokenUserIdClaimType { get; init; } = ClaimTypes.Name;

    public string SharedTokenTenantClaimType { get; init; } = "TenantId";

    public string SharedTokenTenantId { get; init; } = "0";

    public string[] SharedTokenPermissions { get; init; } = [];
}

public sealed class AgentAuthenticationOptionsValidator(IHostEnvironment environment)
    : IValidateOptions<AgentAuthenticationOptions>
{
    public ValidateOptionsResult Validate(
        string? name,
        AgentAuthenticationOptions options)
    {
        List<string> failures = [];
        if (string.IsNullOrWhiteSpace(options.TenantId) ||
            options.TenantId.Length > 64 ||
            options.TenantId.Any(character =>
                !char.IsAsciiLetterOrDigit(character) &&
                character is not '.' and not '_' and not '-'))
        {
            failures.Add(
                "AgentAuthentication:TenantId must be a safe identifier containing from 1 through 64 characters.");
        }

        if (options.DevelopmentBypassEnabled)
        {
            if (!environment.IsDevelopment())
            {
                failures.Add(
                    "AgentAuthentication:DevelopmentBypassEnabled is allowed only in Development.");
            }
        }
        ValidateClaimType(options.PermissionClaimType, "PermissionClaimType", failures);
        ValidateClaimType(options.TenantClaimType, "TenantClaimType", failures);
        ValidateClaimType(options.UserIdClaimType, "UserIdClaimType", failures);
        ValidateClaimType(
            options.SharedTokenUserIdClaimType,
            "SharedTokenUserIdClaimType",
            failures);
        ValidateClaimType(
            options.SharedTokenTenantClaimType,
            "SharedTokenTenantClaimType",
            failures);
        ValidateSafeIdentifier(
            options.SharedTokenTenantId,
            "SharedTokenTenantId",
            failures);
        if (new HashSet<string>(
            [
                options.PermissionClaimType,
                options.TenantClaimType,
                options.UserIdClaimType
            ],
            StringComparer.Ordinal).Count != 3)
        {
            failures.Add(
                "AgentAuthentication claim type names must be distinct.");
        }

        if (options.SharedTokenPermissions is null)
        {
            failures.Add("AgentAuthentication:SharedTokenPermissions must not be null.");
            return ValidateOptionsResult.Fail(failures);
        }

        foreach (string permission in options.SharedTokenPermissions)
        {
            ValidateClaimType(permission, "SharedTokenPermissions", failures);
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }

    private static void ValidateClaimType(
        string value,
        string optionName,
        ICollection<string> failures)
    {
        if (string.IsNullOrWhiteSpace(value) ||
            value.Length > 128 ||
            value.Any(char.IsWhiteSpace))
        {
            failures.Add(
                $"AgentAuthentication:{optionName} must contain from 1 through 128 characters.");
        }
    }

    private static void ValidateSafeIdentifier(
        string value,
        string optionName,
        ICollection<string> failures)
    {
        if (string.IsNullOrWhiteSpace(value) ||
            value.Length > 64 ||
            value.Any(character =>
                !char.IsAsciiLetterOrDigit(character) &&
                character is not '.' and not '_' and not '-'))
        {
            failures.Add(
                $"AgentAuthentication:{optionName} must be a safe identifier containing from 1 through 64 characters.");
        }
    }
}
