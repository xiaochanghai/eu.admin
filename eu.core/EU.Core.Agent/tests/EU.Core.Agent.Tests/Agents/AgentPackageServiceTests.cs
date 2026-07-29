using System.Text;
using System.Text.Json;
using EU.Core.Agent.Application.Agents;
using EU.Core.Agent.Infrastructure.Persistence;
using Xunit;

namespace EU.Core.Agent.Tests.Agents;

public sealed class AgentPackageServiceTests
{
    private const string StructuredSchema = """
        {"required":["answer"],"properties":{"answer":{"type":"string"}},"type":"object"}
        """;

    [Fact]
    public async Task V1_round_trip_preserves_editable_fields_and_disabled_status_without_history()
    {
        var sourceRepository = new InMemoryAgentRepository();
        var sourceLifecycle = new AgentLifecycleService(sourceRepository);
        AgentDefinition source = (await sourceLifecycle.CreateAsync(new CreateAgentCommand(
            "Support Concierge", "Support Concierge", "Answers customer questions."))).Value!;
        source = (await sourceLifecycle.SaveDraftAsync(new SaveAgentDraftCommand(
            source.Id,
            source.LogicalRevision,
            "Answer accurately.",
            "standard-model",
            AgentOutputMode.Structured,
            StructuredSchema,
            "Customer Support",
            "Owns the customer support responsibility."))).Value!;
        source = (await sourceLifecycle.SetRuntimeStatusAsync(new SetAgentRuntimeStatusCommand(
            source.Id, source.LogicalRevision, AgentRuntimeStatus.Disabled))).Value!;

        var sourcePackages = new AgentPackageService(
            sourceRepository,
            sourceLifecycle,
            new FixedModelProfileCatalog("standard-model"));
        string json = (await sourcePackages.ExportAsync(source.Id)).Value!;

        var destinationRepository = new InMemoryAgentRepository();
        var destinationPackages = new AgentPackageService(
            destinationRepository,
            new AgentLifecycleService(destinationRepository),
            new FixedModelProfileCatalog("standard-model"));
        AgentOperationResult<AgentDefinition> importedResult = await destinationPackages.ImportAsync(json);

        Assert.True(importedResult.Succeeded);
        AgentDefinition imported = importedResult.Value!;
        Assert.Equal(source.Code, imported.Code);
        Assert.Equal("Customer Support", imported.Name);
        Assert.Equal("Owns the customer support responsibility.", imported.Description);
        Assert.Equal(AgentRuntimeStatus.Disabled, imported.RuntimeStatus);
        Assert.Equal("0.1.0", imported.Draft.Label);
        Assert.Equal("Answer accurately.", imported.Draft.Instructions);
        Assert.Equal("standard-model", imported.Draft.ModelProfileId);
        Assert.Equal(AgentOutputMode.Structured, imported.Draft.OutputMode);
        Assert.Equal(StructuredSchema, imported.Draft.OutputJsonSchema);
        Assert.Empty(imported.PublishedVersions);
        Assert.Equal(0, imported.LogicalRevision);
    }

    [Fact]
    public async Task Export_is_deterministic_camel_case_and_excludes_internal_or_sensitive_state()
    {
        var repository = new InMemoryAgentRepository();
        var lifecycle = new AgentLifecycleService(repository);
        AgentDefinition definition = (await lifecycle.CreateAsync(new CreateAgentCommand(
            "writer", "Writer", "Creates summaries."))).Value!;
        definition = (await lifecycle.SaveDraftAsync(new SaveAgentDraftCommand(
            definition.Id, 0, "Summarize.", "standard-model", AgentOutputMode.Text, null))).Value!;
        definition = (await lifecycle.PublishAsync(new PublishAgentCommand(
            definition.Id, definition.LogicalRevision))).Value!;
        var service = new AgentPackageService(repository, lifecycle, new FixedModelProfileCatalog("standard-model"));

        string first = (await service.ExportAsync(definition.Id)).Value!;
        string second = (await service.ExportAsync(definition.Id)).Value!;

        Assert.Equal(first, second);
        Assert.Equal(first, Encoding.UTF8.GetString(Encoding.UTF8.GetBytes(first)));
        Assert.Contains("\"format\":\"eu.core.agent-package\"", first, StringComparison.Ordinal);
        Assert.Contains("\"version\":\"1.0.0\"", first, StringComparison.Ordinal);
        Assert.Contains("\"runtimeStatus\":\"Enabled\"", first, StringComparison.Ordinal);
        Assert.DoesNotContain(definition.Id.ToString(), first, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(definition.LogicalRevision.ToString(), first, StringComparison.Ordinal);
        Assert.DoesNotContain("logicalRevision", first, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("publishedVersions", first, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("credential", first, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("endpoint", first, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("connectionString", first, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(":\\", first, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("{")]
    [InlineData("[]")]
    [InlineData("""{"format":"wrong","version":"1.0.0"}""")]
    public async Task Malformed_or_wrong_format_package_is_rejected_without_partial_agent(string json)
    {
        (AgentPackageService service, InMemoryAgentRepository repository) = CreateDestination();

        AgentOperationResult<AgentDefinition> result = await service.ImportAsync(json);

        Assert.Equal(AgentErrorCodes.PackageInvalid, result.Error?.Code);
        Assert.Empty(await repository.ListAsync(new AgentDefinitionQuery()));
    }

    [Fact]
    public async Task Unknown_major_is_rejected_without_partial_agent()
    {
        (AgentPackageService service, InMemoryAgentRepository repository) = CreateDestination();
        string json = ValidTextPackage().Replace("\"1.0.0\"", "\"2.0.0\"", StringComparison.Ordinal);

        AgentOperationResult<AgentDefinition> result = await service.ImportAsync(json);

        Assert.Equal(AgentErrorCodes.PackageVersionUnsupported, result.Error?.Code);
        Assert.Empty(await repository.ListAsync(new AgentDefinitionQuery()));
    }

    [Fact]
    public async Task Oversized_and_overdeep_packages_are_rejected_without_partial_agent()
    {
        (AgentPackageService service, InMemoryAgentRepository repository) = CreateDestination();
        string oversized = ValidTextPackage().Replace("\"Summarize.\"", $"\"{new string('a', 140_000)}\"", StringComparison.Ordinal);
        string nestedPackageValue =
            string.Concat(Enumerable.Repeat("{\"child\":", 30)) +
            "\"leaf\"" +
            string.Concat(Enumerable.Repeat("}", 30));
        string overdeep = ValidTextPackage().Replace(
            "\"name\":\"Writer\"",
            $"\"name\":\"Writer\",\"unsupportedNestedValue\":{nestedPackageValue}",
            StringComparison.Ordinal);

        Assert.Equal(AgentErrorCodes.PackageInvalid, (await service.ImportAsync(oversized)).Error?.Code);
        Assert.Equal(AgentErrorCodes.PackageInvalid, (await service.ImportAsync(overdeep)).Error?.Code);
        Assert.Empty(await repository.ListAsync(new AgentDefinitionQuery()));
    }

    [Fact]
    public async Task Code_conflict_leaves_existing_agent_unchanged()
    {
        var repository = new InMemoryAgentRepository();
        var lifecycle = new AgentLifecycleService(repository);
        AgentDefinition existing = (await lifecycle.CreateAsync(new CreateAgentCommand(
            "writer", "Existing", "Keep this."))).Value!;
        var service = new AgentPackageService(repository, lifecycle, new FixedModelProfileCatalog("standard-model"));

        AgentOperationResult<AgentDefinition> result = await service.ImportAsync(ValidTextPackage());

        Assert.Equal(AgentErrorCodes.CodeConflict, result.Error?.Code);
        AgentDefinition unchanged = (await repository.GetByIdAsync(existing.Id))!;
        Assert.Equal("Existing", unchanged.Name);
        Assert.Equal("Keep this.", unchanged.Description);
        Assert.Equal(0, unchanged.LogicalRevision);
        Assert.Single(await repository.ListAsync(new AgentDefinitionQuery()));
    }

    [Fact]
    public async Task Missing_model_profile_reference_creates_no_agent()
    {
        var repository = new InMemoryAgentRepository();
        var service = new AgentPackageService(
            repository,
            new AgentLifecycleService(repository),
            new FixedModelProfileCatalog());

        AgentOperationResult<AgentDefinition> result = await service.ImportAsync(ValidTextPackage());

        Assert.Equal(AgentErrorCodes.ReferenceMissing, result.Error?.Code);
        Assert.Empty(await repository.ListAsync(new AgentDefinitionQuery()));
    }

    [Theory]
    [InlineData("Client", "EU.Core.Agent.Api")]
    [InlineData("Server", "EU.Core.Api")]
    public async Task Deployment_and_host_are_fixed(string target, string host)
    {
        (AgentPackageService service, InMemoryAgentRepository repository) = CreateDestination();
        string json = ValidTextPackage()
            .Replace("\"target\":\"Server\"", $"\"target\":\"{target}\"", StringComparison.Ordinal)
            .Replace("\"host\":\"EU.Core.Agent.Api\"", $"\"host\":\"{host}\"", StringComparison.Ordinal);

        AgentOperationResult<AgentDefinition> result = await service.ImportAsync(json);

        Assert.Equal(AgentErrorCodes.PackageInvalid, result.Error?.Code);
        Assert.Empty(await repository.ListAsync(new AgentDefinitionQuery()));
    }

    [Theory]
    [InlineData("Text", "{\"type\":\"object\"}")]
    [InlineData("Structured", null)]
    [InlineData("Structured", "{\"properties\":{}}")]
    public async Task Text_and_structured_schema_rules_are_enforced(string mode, string? schema)
    {
        (AgentPackageService service, InMemoryAgentRepository repository) = CreateDestination();
        string json = PackageJson("writer", mode, schema, "Enabled", [], []);

        AgentOperationResult<AgentDefinition> result = await service.ImportAsync(json);

        Assert.Equal(
            mode == "Structured" ? AgentErrorCodes.OutputSchemaInvalid : AgentErrorCodes.PackageInvalid,
            result.Error?.Code);
        Assert.Empty(await repository.ListAsync(new AgentDefinitionQuery()));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Nonempty_deferred_skill_or_tool_references_are_rejected(bool skill)
    {
        (AgentPackageService service, InMemoryAgentRepository repository) = CreateDestination();
        string[] skills = skill ? ["skill-v1"] : [];
        string[] tools = skill ? [] : ["tool-v1"];

        AgentOperationResult<AgentDefinition> result = await service.ImportAsync(
            PackageJson("writer", "Text", null, "Enabled", skills, tools));

        Assert.Equal(AgentErrorCodes.PackageInvalid, result.Error?.Code);
        Assert.Empty(await repository.ListAsync(new AgentDefinitionQuery()));
    }

    [Theory]
    [InlineData("alias:production-model")]
    [InlineData("C:\\private\\model.json")]
    [InlineData("/etc/private/model.json")]
    public async Task Secret_or_absolute_path_shaped_references_are_rejected(string modelProfileId)
    {
        var repository = new InMemoryAgentRepository();
        var service = new AgentPackageService(
            repository,
            new AgentLifecycleService(repository),
            new FixedModelProfileCatalog(modelProfileId));
        string json = ValidTextPackage().Replace("standard-model", JsonEncodedText.Encode(modelProfileId).ToString(), StringComparison.Ordinal);

        AgentOperationResult<AgentDefinition> result = await service.ImportAsync(json);

        Assert.Equal(AgentErrorCodes.PackageInvalid, result.Error?.Code);
        Assert.Empty(await repository.ListAsync(new AgentDefinitionQuery()));
    }

    [Theory]
    [InlineData("\"runtimeStatus\":\"Paused\"")]
    [InlineData("\"runtimeStatus\":\"1\"")]
    [InlineData("\"outputMode\":\"Binary\"")]
    [InlineData("\"outputMode\":\"1\"")]
    [InlineData("\"code\":\"Bad!Code\"")]
    [InlineData("\"apiKey\":\"secret-value\"")]
    public async Task Invalid_enum_code_or_secret_shaped_property_creates_no_partial_agent(string mutation)
    {
        (AgentPackageService service, InMemoryAgentRepository repository) = CreateDestination();
        string json = mutation.StartsWith("\"apiKey\"", StringComparison.Ordinal)
            ? ValidTextPackage().Replace("\"name\":\"Writer\"", "\"name\":\"Writer\",\"apiKey\":\"secret-value\"", StringComparison.Ordinal)
            : mutation.StartsWith("\"runtimeStatus\"", StringComparison.Ordinal)
                ? ValidTextPackage().Replace("\"runtimeStatus\":\"Enabled\"", mutation, StringComparison.Ordinal)
                : mutation.StartsWith("\"outputMode\"", StringComparison.Ordinal)
                    ? ValidTextPackage().Replace("\"outputMode\":\"Text\"", mutation, StringComparison.Ordinal)
                    : ValidTextPackage().Replace("\"code\":\"writer\"", mutation, StringComparison.Ordinal);

        AgentOperationResult<AgentDefinition> result = await service.ImportAsync(json);

        Assert.Equal(AgentErrorCodes.PackageInvalid, result.Error?.Code);
        Assert.Empty(await repository.ListAsync(new AgentDefinitionQuery()));
    }

    [Fact]
    public async Task Export_missing_agent_returns_typed_not_found()
    {
        (AgentPackageService service, _) = CreateDestination();

        AgentOperationResult<string> result = await service.ExportAsync(Guid.NewGuid());

        Assert.Equal(AgentErrorCodes.NotFound, result.Error?.Code);
    }

    [Theory]
    [InlineData("Read C:\\private\\agent.txt before answering.")]
    [InlineData("Use alias:production-credential.")]
    public async Task Export_refuses_to_emit_absolute_paths_or_secret_shaped_values(string instructions)
    {
        var repository = new InMemoryAgentRepository();
        var lifecycle = new AgentLifecycleService(repository);
        AgentDefinition definition = (await lifecycle.CreateAsync(new CreateAgentCommand("safe-export"))).Value!;
        definition = (await lifecycle.SaveDraftAsync(new SaveAgentDraftCommand(
            definition.Id,
            definition.LogicalRevision,
            instructions,
            "standard-model",
            AgentOutputMode.Text,
            null))).Value!;
        var service = new AgentPackageService(repository, lifecycle, new FixedModelProfileCatalog("standard-model"));

        AgentOperationResult<string> result = await service.ExportAsync(definition.Id);

        Assert.Equal(AgentErrorCodes.PackageInvalid, result.Error?.Code);
        Assert.Null(result.Value);
    }

    [Theory]
    [InlineData("Text", "{\"type\":\"object\"}", true, AgentErrorCodes.PackageInvalid)]
    [InlineData("Structured", null, true, AgentErrorCodes.OutputSchemaInvalid)]
    [InlineData("Structured", "{\"properties\":{}}", true, AgentErrorCodes.OutputSchemaInvalid)]
    [InlineData("Text", null, false, AgentErrorCodes.ReferenceMissing)]
    public async Task Export_reuses_full_import_validation_for_reachable_invalid_drafts(
        string mode,
        string? schema,
        bool catalogContainsProfile,
        string expectedError)
    {
        var repository = new InMemoryAgentRepository();
        var lifecycle = new AgentLifecycleService(repository);
        AgentDefinition definition = (await lifecycle.CreateAsync(new CreateAgentCommand("invalid-export"))).Value!;
        definition = (await lifecycle.SaveDraftAsync(new SaveAgentDraftCommand(
            definition.Id,
            definition.LogicalRevision,
            "Answer.",
            "standard-model",
            Enum.Parse<AgentOutputMode>(mode),
            schema))).Value!;
        var catalog = catalogContainsProfile
            ? new FixedModelProfileCatalog("standard-model")
            : new FixedModelProfileCatalog();
        var service = new AgentPackageService(repository, lifecycle, catalog);

        AgentOperationResult<string> result = await service.ExportAsync(definition.Id);

        Assert.Equal(expectedError, result.Error?.Code);
        Assert.Null(result.Value);
    }

    [Theory]
    [InlineData("Text", null, "Enabled")]
    [InlineData("Structured", StructuredSchema, "Disabled")]
    public async Task Every_successful_export_imports_into_empty_repository_with_same_catalog(
        string mode,
        string? schema,
        string status)
    {
        var sourceRepository = new InMemoryAgentRepository();
        var sourceLifecycle = new AgentLifecycleService(sourceRepository);
        AgentDefinition source = (await sourceLifecycle.CreateAsync(new CreateAgentCommand("round-trip-invariant"))).Value!;
        source = (await sourceLifecycle.SaveDraftAsync(new SaveAgentDraftCommand(
            source.Id,
            source.LogicalRevision,
            "Answer.",
            "standard-model",
            Enum.Parse<AgentOutputMode>(mode),
            schema))).Value!;
        if (status == "Disabled")
        {
            source = (await sourceLifecycle.SetRuntimeStatusAsync(new SetAgentRuntimeStatusCommand(
                source.Id,
                source.LogicalRevision,
                AgentRuntimeStatus.Disabled))).Value!;
        }

        var catalog = new FixedModelProfileCatalog("standard-model");
        var sourceService = new AgentPackageService(sourceRepository, sourceLifecycle, catalog);
        AgentOperationResult<string> exported = await sourceService.ExportAsync(source.Id);
        Assert.True(exported.Succeeded);

        var destinationRepository = new InMemoryAgentRepository();
        var destinationService = new AgentPackageService(
            destinationRepository,
            new AgentLifecycleService(destinationRepository),
            catalog);
        AgentOperationResult<AgentDefinition> imported = await destinationService.ImportAsync(exported.Value!);

        Assert.True(imported.Succeeded);
        Assert.Equal(source.Code, imported.Value!.Code);
        Assert.Equal(source.RuntimeStatus, imported.Value.RuntimeStatus);
        Assert.Equal(source.Draft.OutputMode, imported.Value.Draft.OutputMode);
        Assert.Equal(source.Draft.OutputJsonSchema, imported.Value.Draft.OutputJsonSchema);
    }

    [Theory]
    [InlineData("01.0.0")]
    [InlineData("1.00.000")]
    [InlineData("-1.0.0")]
    [InlineData("+1.0.0")]
    [InlineData("999999999999999999999999.0.0")]
    public async Task Noncanonical_semantic_versions_are_package_invalid(string version)
    {
        (AgentPackageService service, InMemoryAgentRepository repository) = CreateDestination();
        string json = ValidTextPackage().Replace("\"1.0.0\"", $"\"{version}\"", StringComparison.Ordinal);

        AgentOperationResult<AgentDefinition> result = await service.ImportAsync(json);

        Assert.Equal(AgentErrorCodes.PackageInvalid, result.Error?.Code);
        Assert.Empty(await repository.ListAsync(new AgentDefinitionQuery()));
    }

    [Fact]
    public async Task Supported_major_accepts_canonical_minor_and_patch()
    {
        (AgentPackageService service, _) = CreateDestination();
        string json = ValidTextPackage().Replace("\"1.0.0\"", "\"1.12.34\"", StringComparison.Ordinal);

        AgentOperationResult<AgentDefinition> result = await service.ImportAsync(json);

        Assert.True(result.Succeeded);
    }

    [Theory]
    [InlineData("\"code\":\"writer\",\"code\":\"other-writer\"")]
    [InlineData("\"code\":\"writer\",\"note\":\"ordinary unknown property\"")]
    public async Task Duplicate_or_ordinary_unknown_property_is_invalid_and_creates_no_agent(string codeFragment)
    {
        (AgentPackageService service, InMemoryAgentRepository repository) = CreateDestination();
        string json = ValidTextPackage().Replace("\"code\":\"writer\"", codeFragment, StringComparison.Ordinal);

        AgentOperationResult<AgentDefinition> result = await service.ImportAsync(json);

        Assert.Equal(AgentErrorCodes.PackageInvalid, result.Error?.Code);
        Assert.Empty(await repository.ListAsync(new AgentDefinitionQuery()));
    }

    private static (AgentPackageService Service, InMemoryAgentRepository Repository) CreateDestination()
    {
        var repository = new InMemoryAgentRepository();
        return (
            new AgentPackageService(repository, new AgentLifecycleService(repository), new FixedModelProfileCatalog("standard-model")),
            repository);
    }

    private static string ValidTextPackage() =>
        PackageJson("writer", "Text", null, "Enabled", [], []);

    private static string ValidStructuredPackage(string schema) =>
        PackageJson("writer", "Structured", schema, "Enabled", [], []);

    private static string PackageJson(
        string code,
        string outputMode,
        string? schema,
        string status,
        string[] skills,
        string[] tools) =>
        JsonSerializer.Serialize(new
        {
            format = "eu.core.agent-package",
            version = "1.0.0",
            agent = new
            {
                code,
                name = "Writer",
                description = "Creates summaries.",
                runtimeStatus = status,
                draft = new
                {
                    instructions = "Summarize.",
                    modelProfileId = "standard-model",
                    outputMode,
                    outputJsonSchema = schema
                },
                deployment = new { target = "Server", host = "EU.Core.Agent.Api" },
                skills,
                tools
            }
        });

    private sealed class FixedModelProfileCatalog(params string[] ids) : IModelProfileReferenceCatalog
    {
        private readonly HashSet<string> _ids = new(ids, StringComparer.Ordinal);

        public Task<bool> ExistsAsync(string modelProfileId, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(_ids.Contains(modelProfileId));
        }
    }
}
