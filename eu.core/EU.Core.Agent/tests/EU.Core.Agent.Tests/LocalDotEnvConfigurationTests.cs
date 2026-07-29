using EU.Core.Agent.Api.Configuration;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace EU.Core.Agent.Tests;

public sealed class LocalDotEnvConfigurationTests
{
    [Fact]
    public void Apply_maps_only_non_secret_local_model_settings()
    {
        string directory = CreateTemporaryDirectory();
        try
        {
            File.WriteAllText(
                Path.Combine(directory, ".env"),
                """
                EUCORE_AGENT_SQLSERVER=must-not-be-loaded
                AGENT_MODEL_API_KEY=must-not-be-loaded
                AGENT_MODEL_ENDPOINT=https://model.example.test/v1
                AGENT_MODEL_DEFAULT_ID=qwen-safe
                AgentStorage__Provider=Sqlite
                AgentStorage__DatabasePath=data/local-agent.db
                AgentStorage__SkillRootPath=agent-data/local-skills
                AgentMcp__AllowedHosts__0=mcp.example.test
                AgentMcp__AllowedPorts__0=443
                AgentMcp__EnableStdio=false
                AgentMcp__ConnectionTimeoutSeconds=12
                AgentMcp__DiscoveryTimeoutSeconds=14
                AgentExecution__ModelTimeoutSeconds=90
                AgentExecution__ToolCallTimeoutSeconds=45
                AgentMcp__Credentials__catalog=must-not-be-loaded
                """);
            ConfigurationManager configuration = new();
            configuration["AgentPlatform:LoadDotEnv"] = "true";

            LocalDotEnvConfiguration.Apply(configuration, directory, directory);

            Assert.Equal("agent-api", configuration["AgentPlatform:ServiceName"]);
            Assert.Equal(
                "https://model.example.test/v1",
                configuration["AgentPlatform:ModelEndpoint"]);
            Assert.Equal(
                "alias:local-agent-model",
                configuration["AgentPlatform:ModelCredentialAlias"]);
            Assert.Equal("qwen-safe", configuration["AgentControl:ModelProfileIds:0"]);
            Assert.Equal("Sqlite", configuration["AgentStorage:Provider"]);
            Assert.Equal("data/local-agent.db", configuration["AgentStorage:DatabasePath"]);
            Assert.Equal("agent-data/local-skills", configuration["AgentStorage:SkillRootPath"]);
            Assert.Equal("mcp.example.test", configuration["AgentMcp:AllowedHosts:0"]);
            Assert.Equal("443", configuration["AgentMcp:AllowedPorts:0"]);
            Assert.Equal("false", configuration["AgentMcp:EnableStdio"]);
            Assert.Equal("12", configuration["AgentMcp:ConnectionTimeoutSeconds"]);
            Assert.Equal("14", configuration["AgentMcp:DiscoveryTimeoutSeconds"]);
            Assert.Equal("90", configuration["AgentExecution:ModelTimeoutSeconds"]);
            Assert.Equal("45", configuration["AgentExecution:ToolCallTimeoutSeconds"]);
            Assert.Null(configuration["AgentMcp:Credentials:catalog"]);
            Assert.Null(configuration["AGENT_MODEL_API_KEY"]);
            Assert.Null(configuration["EUCORE_AGENT_SQLSERVER"]);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void Apply_preserves_process_configuration_and_requires_explicit_opt_in()
    {
        string directory = CreateTemporaryDirectory();
        try
        {
            File.WriteAllText(
                Path.Combine(directory, ".env"),
                """
                AGENT_MODEL_ENDPOINT=https://file.example.test/v1
                AGENT_MODEL_DEFAULT_ID=file-model
                """);
            ConfigurationManager disabled = new();

            LocalDotEnvConfiguration.Apply(disabled, directory, directory);

            Assert.Null(disabled["AgentPlatform:ModelEndpoint"]);

            ConfigurationManager enabled = new();
            enabled["AgentPlatform:LoadDotEnv"] = "true";
            enabled["AgentPlatform:ModelEndpoint"] = "https://process.example.test/v1";
            enabled["AgentControl:ModelProfileIds:0"] = "process-model";

            LocalDotEnvConfiguration.Apply(enabled, directory, directory);

            Assert.Equal(
                "https://process.example.test/v1",
                enabled["AgentPlatform:ModelEndpoint"]);
            Assert.Equal("process-model", enabled["AgentControl:ModelProfileIds:0"]);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static string CreateTemporaryDirectory()
    {
        string path = Path.Combine(
            Path.GetTempPath(),
            $"eu-core-agent-api-dotenv-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }
}
