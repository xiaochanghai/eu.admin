using Microsoft.Extensions.Options;

namespace EU.Core.Api.Agent.Configuration;

public sealed class AgentHttpSecurityOptions
{
    public const string SectionName = "AgentHttpSecurity";
    public const string CorsPolicyName = "AgentCors";

    public IReadOnlyList<string> AllowedOrigins { get; init; } = [];

    public bool AllowDevelopmentHttpOrigins { get; init; }
}

public sealed class AgentHttpSecurityOptionsValidator(IHostEnvironment environment)
    : IValidateOptions<AgentHttpSecurityOptions>
{
    public ValidateOptionsResult Validate(string? name, AgentHttpSecurityOptions options)
    {
        if (options.AllowDevelopmentHttpOrigins && !environment.IsDevelopment())
            return ValidateOptionsResult.Fail(
                "AgentHttpSecurity:AllowDevelopmentHttpOrigins is allowed only in Development.");
        if (options.AllowedOrigins.Count > 16)
            return ValidateOptionsResult.Fail(
                "AgentHttpSecurity:AllowedOrigins supports at most 16 origins.");

        var normalized = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (string candidate in options.AllowedOrigins)
        {
            if (!Uri.TryCreate(candidate, UriKind.Absolute, out Uri? origin)
                || !string.IsNullOrEmpty(origin.UserInfo)
                || origin.AbsolutePath != "/"
                || !string.IsNullOrEmpty(origin.Query)
                || !string.IsNullOrEmpty(origin.Fragment)
                || (origin.Scheme != Uri.UriSchemeHttps
                    && !(environment.IsDevelopment()
                        && options.AllowDevelopmentHttpOrigins
                        && origin.Scheme == Uri.UriSchemeHttp)))
            {
                return ValidateOptionsResult.Fail(
                    "AgentHttpSecurity:AllowedOrigins must contain only exact HTTPS origins without paths, queries, fragments, user info, or wildcards.");
            }

            if (!normalized.Add(origin.GetLeftPart(UriPartial.Authority)))
                return ValidateOptionsResult.Fail(
                    "AgentHttpSecurity:AllowedOrigins cannot contain duplicates.");
        }

        return ValidateOptionsResult.Success;
    }
}
