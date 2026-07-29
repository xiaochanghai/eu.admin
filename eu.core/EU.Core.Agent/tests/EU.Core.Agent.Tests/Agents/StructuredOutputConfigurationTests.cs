using EU.Core.Agent.Application.Agents;
using EU.Core.Agent.Application.Validation;
using EU.Core.Agent.Infrastructure.Persistence;
using Xunit;

namespace EU.Core.Agent.Tests.Agents;

public sealed class StructuredOutputConfigurationTests
{
    [Fact]
    public void Validator_canonicalizes_object_keys_and_calculates_a_stable_sha256()
    {
        var validator = new JsonSchemaValidator();

        JsonSchemaValidationResult result = validator.Validate("""
            { "required": ["name"], "properties": { "name": { "type": "string" } }, "type": "object" }
            """);

        Assert.True(result.IsValid);
        Assert.Equal("{\"properties\":{\"name\":{\"type\":\"string\"}},\"required\":[\"name\"],\"type\":\"object\"}", result.CanonicalJson);
        Assert.Equal("098974972159a1c508b6fed6baed3bba04cd70d4ee9bbe30f926fb1db0188008", result.Sha256);
    }

    [Theory]
    [InlineData("[]")]
    [InlineData("{}")]
    [InlineData("{\"type\":\"unsupported\"}")]
    [InlineData("{\"type\":\"object\",\"properties\":[]}")]
    [InlineData("{\"type\":\"object\",\"properties\":{\"name\":{\"type\":\"string\"}},\"required\":[\"name\",\"name\"]}")]
    [InlineData("{\"type\":\"object\",\"properties\":{\"name\":{\"type\":\"string\"}},\"required\":[\"missing\"]}")]
    public void Validator_rejects_invalid_schema_shapes(string schema)
    {
        var validator = new JsonSchemaValidator();

        JsonSchemaValidationResult result = validator.Validate(schema);

        Assert.False(result.IsValid);
        Assert.Null(result.CanonicalJson);
        Assert.Null(result.Sha256);
    }

    [Fact]
    public async Task Structured_publish_stores_canonical_schema_hash()
    {
        var service = new AgentLifecycleService(new InMemoryAgentRepository());
        AgentDefinition created = (await service.CreateAsync(new CreateAgentCommand("structured-agent"))).Value!;
        AgentDefinition saved = (await service.SaveDraftAsync(new SaveAgentDraftCommand(
            created.Id, created.LogicalRevision, "Return a name.", "qwen", AgentOutputMode.Structured,
            "{ \"type\": \"object\", \"properties\": { \"name\": { \"type\": \"string\" } } }"))).Value!;

        AgentDefinition published = (await service.PublishAsync(new PublishAgentCommand(saved.Id, saved.LogicalRevision))).Value!;
        AgentVersion version = Assert.Single(published.PublishedVersions);

        Assert.Equal("{\"properties\":{\"name\":{\"type\":\"string\"}},\"type\":\"object\"}", version.Snapshot!.OutputJsonSchema);
        Assert.Equal("2b7196d853bac7cea83330be9c2073848dedc10746eaf403bb5f73687531baf2", version.OutputSchemaSha256);
    }

    [Fact]
    public void Validator_rejects_duplicate_names_inside_properties()
    {
        var validator = new JsonSchemaValidator();

        JsonSchemaValidationResult result = validator.Validate("""
            { "type": "object", "properties": { "result": { "type": "string" }, "result": { "type": "number" } } }
            """);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validator_returns_the_same_canonical_hash_when_equivalent_properties_are_reordered()
    {
        var validator = new JsonSchemaValidator();

        JsonSchemaValidationResult first = validator.Validate("""
            { "type": "object", "properties": { "alpha": { "type": "string" }, "beta": { "type": "number" } } }
            """);
        JsonSchemaValidationResult second = validator.Validate("""
            { "properties": { "beta": { "type": "number" }, "alpha": { "type": "string" } }, "type": "object" }
            """);

        Assert.True(first.IsValid);
        Assert.True(second.IsValid);
        Assert.Equal(first.CanonicalJson, second.CanonicalJson);
        Assert.Equal(first.Sha256, second.Sha256);
    }

    [Theory]
    [InlineData("""{"name":"Ada","score":9}""", true)]
    [InlineData("""{"name":7}""", false)]
    [InlineData("""{"score":9}""", false)]
    [InlineData("""not-json""", false)]
    public void Runtime_instance_validation_enforces_supported_schema(
        string instance,
        bool expected)
    {
        var validator = new JsonSchemaValidator();
        const string schema =
            """
            {
              "type": "object",
              "properties": {
                "name": { "type": "string" },
                "score": { "type": "integer" }
              },
              "required": ["name"]
            }
            """;

        Assert.Equal(
            expected,
            validator.ValidateInstance(schema, instance).Succeeded);
    }
}
