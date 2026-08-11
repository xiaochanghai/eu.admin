using Microsoft.Extensions.Options;

namespace EU.Core.Api.Agent.Configuration;

public sealed record BusinessQueryResultRetentionOptions
{
    public const string SectionName = "BusinessQueryResultRetention";
    public int RetentionDays { get; init; } = 30;
    public int MaximumResultBytes { get; init; } = 1_048_576;
    public int MaximumConversationBytes { get; init; } = 10_485_760;
    public string ExpiredResultAction { get; init; } = "RedactPresentation";
}

public sealed class BusinessQueryResultRetentionOptionsValidator
    : IValidateOptions<BusinessQueryResultRetentionOptions>
{
    public ValidateOptionsResult Validate(string? name, BusinessQueryResultRetentionOptions options) =>
        options.RetentionDays is < 1 or > 3650
        || options.MaximumResultBytes is < 4096 or > 4_194_304
        || options.MaximumConversationBytes < options.MaximumResultBytes
        || options.MaximumConversationBytes > 104_857_600
        || !string.Equals(options.ExpiredResultAction, "RedactPresentation", StringComparison.Ordinal)
            ? ValidateOptionsResult.Fail("BusinessQueryResultRetention configuration is invalid.")
            : ValidateOptionsResult.Success;
}
