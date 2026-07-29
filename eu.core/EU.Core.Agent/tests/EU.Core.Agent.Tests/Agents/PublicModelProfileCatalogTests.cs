using EU.Core.Agent.Application.Agents;
using Xunit;

namespace EU.Core.Agent.Tests.Agents;

public sealed class PublicModelProfileCatalogTests
{
    [Fact]
    public void Safe_public_identifiers_are_trimmed_deduplicated_and_sorted()
    {
        var catalog = new PublicModelProfileCatalog(
        [
            " qwen2.5 ",
            "org/model-name_v2.1",
            "deepseek-chat",
            "qwen2.5"
        ]);

        Assert.Equal(
            new[] { "deepseek-chat", "org/model-name_v2.1", "qwen2.5" },
            catalog.ProfileIds);
    }

    [Theory]
    [InlineData("alias:production-credential")]
    [InlineData("C:\\private\\model.json")]
    [InlineData("/etc/private/model.json")]
    [InlineData("https://models.example.test/v1")]
    [InlineData("sk-live-value")]
    [InlineData("Bearer-live-value")]
    [InlineData("eyJhbGciOiJIUzI1NiJ9")]
    [InlineData("password-model")]
    [InlineData("apiKey-model")]
    [InlineData("token-model")]
    [InlineData("connection-model")]
    [InlineData("org//model")]
    [InlineData("org/../model")]
    [InlineData("/org/model")]
    [InlineData("org/model/")]
    public async Task Unsafe_values_fail_closed_without_echo_and_never_become_references(string value)
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => new PublicModelProfileCatalog([value]));

        Assert.Contains("public model profile", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(value, exception.Message, StringComparison.Ordinal);

        var safeCatalog = new PublicModelProfileCatalog(["org/model-name"]);
        Assert.False(await safeCatalog.ExistsAsync(value));
    }
}
