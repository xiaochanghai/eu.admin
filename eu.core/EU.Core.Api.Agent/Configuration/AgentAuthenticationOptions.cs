using Microsoft.Extensions.Options;

namespace EU.Core.Api.Agent.Configuration;

public sealed class AgentAuthenticationOptions
{
    public const string SectionName = "AgentAuthentication";

    public string? Authority { get; init; }

    public string? Audience { get; init; }

    public string TenantId { get; init; } = string.Empty;

    public bool RequireHttpsMetadata { get; init; } = true;

    public bool DevelopmentBypassEnabled { get; init; }

    public string PermissionClaimType { get; init; } = "permission";

    public string TenantClaimType { get; init; } = "tenant_id";

    public string UserIdClaimType { get; init; } = "sub";
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
        else
        {
            if (!Uri.TryCreate(options.Authority, UriKind.Absolute, out Uri? authority) ||
                !string.Equals(authority.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) ||
                !string.IsNullOrEmpty(authority.UserInfo))
            {
                failures.Add(
                    "AgentAuthentication:Authority must be an absolute HTTPS URI without embedded credentials.");
            }

            if (string.IsNullOrWhiteSpace(options.Audience))
            {
                failures.Add("AgentAuthentication:Audience is required.");
            }

            if (!environment.IsDevelopment() && !options.RequireHttpsMetadata)
            {
                failures.Add(
                    "AgentAuthentication:RequireHttpsMetadata must be true outside Development.");
            }
        }

        ValidateClaimType(options.PermissionClaimType, "PermissionClaimType", failures);
        ValidateClaimType(options.TenantClaimType, "TenantClaimType", failures);
        ValidateClaimType(options.UserIdClaimType, "UserIdClaimType", failures);
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
}
