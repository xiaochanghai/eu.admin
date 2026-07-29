using System.Text.RegularExpressions;
using Xunit;

namespace EU.Core.Agent.Tests.Persistence;

public sealed class MigrationArtifactsContractTests
{
    [Fact]
    public void Manual_migration_artifacts_are_present_in_the_documented_layout()
    {
        AssertArtifactExists("database", "migrations", "0000_migration_history.sql");
        AssertArtifactExists("database", "migrations", "0001_agent_schema.sql");
        AssertArtifactExists("database", "migrations", "down", "0001_agent_schema.down.sql");
        AssertArtifactExists("database", "migrations", "README.md");
    }

    [Fact]
    public void Migration_0000_is_an_operator_owned_placeholder_without_database_definitions()
    {
        string sql = ReadSql("0000_migration_history.sql");

        Assert.Contains("operator-owned placeholder", sql, StringComparison.OrdinalIgnoreCase);
        foreach (string forbiddenDefinition in new[]
        {
            "CREATE SCHEMA", "CREATE TABLE", "ALTER TABLE", "CREATE INDEX", "CONSTRAINT", "SchemaMigrationHistory",
            "MigrationVersion", "ScriptName", "ApplicationVersion", "AppliedUtc", "INSERT INTO"
        })
        {
            Assert.DoesNotContain(forbiddenDefinition, sql, StringComparison.OrdinalIgnoreCase);
        }

        Assert.DoesNotContain("[dbo]", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Schema_migration_stays_idempotent_and_does_not_create_domain_tables()
    {
        string sql = ReadSql("0001_agent_schema.sql");

        Assert.Contains("IF SCHEMA_ID(N'agent') IS NULL", sql, StringComparison.Ordinal);
        Assert.Contains("CREATE SCHEMA [agent]", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("CREATE TABLE", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("[dbo]", sql, StringComparison.OrdinalIgnoreCase);

        foreach (string domainTable in new[]
        {
            "AgentDefinition", "AgentVersion", "SkillDefinition", "SkillVersion", "AgentSkillBinding",
            "McpService", "ToolSnapshot", "HealthCheck", "AgentMcpBinding", "ToolPermission",
            "Conversation", "ConversationMessage", "ConversationAttachment", "Execution", "ExecutionStep", "AgentTestCase"
        })
        {
            Assert.DoesNotContain(domainTable, sql, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Theory]
    [InlineData("ALTER TABLE [agent].[UserOwned] ADD [Extra] int NULL;")]
    [InlineData("CREATE VIEW [agent].[Unexpected] AS SELECT 1 AS [Value];")]
    [InlineData("CREATE PROCEDURE [agent].[Unexpected] AS SELECT 1;")]
    [InlineData("CREATE TYPE [agent].[Unexpected] FROM int;")]
    public void Strict_sql_grammar_rejects_representative_non_schema_ddl(string forbiddenStatement)
    {
        var exception = Assert.Throws<Xunit.Sdk.XunitException>(() =>
            AssertSqlArtifactConformsToAllowedGrammar("0001_agent_schema.sql", $"SET NOCOUNT ON; {forbiddenStatement}"));

        Assert.Contains("outside the allowed grammar", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Migration_scripts_conform_to_the_strict_allowed_grammar_after_comments_are_removed()
    {
        AssertSqlArtifactConformsToAllowedGrammar("0000_migration_history.sql", ReadSql("0000_migration_history.sql"));
        AssertSqlArtifactConformsToAllowedGrammar("0001_agent_schema.sql", ReadSql("0001_agent_schema.sql"));
        AssertSqlArtifactConformsToAllowedGrammar("0001_agent_schema.down.sql", ReadSql("down", "0001_agent_schema.down.sql"));
    }

    [Fact]
    public void Down_script_is_a_non_destructive_operator_placeholder_without_history_assumptions()
    {
        string sql = ReadSql("down", "0001_agent_schema.down.sql");

        Assert.Contains("operator-owned placeholder", sql, StringComparison.OrdinalIgnoreCase);
        foreach (string destructiveOrCoupledToken in new[]
        {
            "DROP ", "ALTER ", "DELETE ", "TRUNCATE ", "CASCADE", "SchemaMigrationHistory", "sys.objects", "THROW"
        })
        {
            Assert.DoesNotContain(destructiveOrCoupledToken, sql, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void Manual_migration_readme_prescribes_operator_owned_application_and_recording()
    {
        string readme = ReadMigrationArtifact("README.md");

        Assert.Contains("0000_migration_history.sql", readme, StringComparison.Ordinal);
        Assert.Contains("0001_agent_schema.sql", readme, StringComparison.Ordinal);
        Assert.Contains("0001_agent_schema.down.sql", readme, StringComparison.Ordinal);
        Assert.Contains("SHA-256", readme, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("not automatically executed", readme, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("operator", readme, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("checksum validation", readme, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("concurrency serialization", readme, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("execution evidence", readme, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("rollback", readme, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("SELECT", readme, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("operator-controlled evidence record", readme, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("sys.schemas", readme, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("sys.objects", readme, StringComparison.OrdinalIgnoreCase);
        foreach (string userOwnedDefinition in new[]
        {
            "SchemaMigrationHistory", "MigrationVersion", "ScriptName", "ApplicationVersion", "AppliedUtc", "INSERT INTO"
        })
        {
            Assert.DoesNotContain(userOwnedDefinition, readme, StringComparison.OrdinalIgnoreCase);
        }

        AssertAppearsInOrder(
            readme,
            "1. Calculate the SHA-256 for `0000_migration_history.sql`.",
            "2. Apply `0000_migration_history.sql`.",
            "3. Calculate the SHA-256 for `0001_agent_schema.sql`.",
            "4. Apply `0001_agent_schema.sql`.",
            "5. Record the hashes and execution evidence in the operator-controlled evidence record.");
    }

    [Fact]
    public void Manual_sql_server_migration_delivery_contains_no_secrets_or_automatic_execution_wiring()
    {
        foreach (string artifact in new[]
        {
            ReadMigrationArtifact("0000_migration_history.sql"),
            ReadMigrationArtifact("0001_agent_schema.sql"),
            ReadMigrationArtifact("down", "0001_agent_schema.down.sql"),
            ReadMigrationArtifact("README.md")
        })
        {
            Assert.DoesNotMatch(@"(?i)(password|pwd|user\s*id|server\s*=|data\s*source|initial\s*catalog|sqlcmd)", artifact);
        }

        string sourceRoot = GetSolutionFilePath("src");
        string source = string.Join('\n', Directory.EnumerateFiles(sourceRoot, "*", SearchOption.AllDirectories)
            .Where(path => Path.GetExtension(path) is ".cs" or ".csproj" or ".json" or ".config")
            .Select(File.ReadAllText));

        Assert.DoesNotContain("AgentMigrationRunner", source, StringComparison.Ordinal);
        Assert.DoesNotContain("sp_getapplock", source, StringComparison.OrdinalIgnoreCase);
        string codeSource = string.Join('\n', Directory.EnumerateFiles(sourceRoot, "*.cs", SearchOption.AllDirectories)
            .Select(File.ReadAllText));
        Assert.DoesNotMatch(
            @"(?i)(Microsoft\.Data\.SqlClient|System\.Data\.SqlClient|SqlConnection|ExecuteSqlCommand)",
            codeSource);
        Assert.Contains("SqliteAgentRepository", source, StringComparison.Ordinal);
    }

    private static void AssertAppearsInOrder(string text, params string[] fragments)
    {
        int previousIndex = -1;
        foreach (string fragment in fragments)
        {
            int currentIndex = text.IndexOf(fragment, previousIndex + 1, StringComparison.Ordinal);
            Assert.True(currentIndex > previousIndex, $"Expected manual procedure fragment in order: {fragment}");
            previousIndex = currentIndex;
        }
    }

    private static void AssertSqlArtifactConformsToAllowedGrammar(string artifactName, string sql)
    {
        string commentFreeSql = Regex.Replace(sql, @"--[^\r\n]*|/\*.*?\*/", string.Empty, RegexOptions.Singleline);
        string allowedGrammar = artifactName switch
        {
            "0000_migration_history.sql" or "0001_agent_schema.down.sql" =>
                @"\A\s*SET\s+NOCOUNT\s+ON\s*;\s*\z",
            "0001_agent_schema.sql" =>
                @"\A\s*SET\s+NOCOUNT\s+ON\s*;\s*IF\s+SCHEMA_ID\s*\(\s*N'agent'\s*\)\s+IS\s+NULL\s+BEGIN\s+EXEC\s*\(\s*N'CREATE\s+SCHEMA\s+\[agent\]'\s*\)\s*;\s*END\s*;\s*\z",
            _ => throw new ArgumentOutOfRangeException(nameof(artifactName), artifactName, "Unknown migration artifact.")
        };

        if (!Regex.IsMatch(commentFreeSql, allowedGrammar, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            throw new Xunit.Sdk.XunitException($"{artifactName} contains SQL outside the allowed grammar.");
        }
    }

    private static void AssertArtifactExists(params string[] segments)
    {
        string path = GetSolutionFilePath(segments);
        Assert.True(File.Exists(path), $"Required manual migration artifact is missing: {path}");
    }

    private static string ReadSql(params string[] relativePath) => ReadMigrationArtifact(relativePath);

    private static string ReadMigrationArtifact(params string[] relativePath) => ReadArtifact(["database", "migrations", .. relativePath]);

    private static string ReadArtifact(params string[] relativePath)
    {
        string path = GetSolutionFilePath(relativePath);
        if (!File.Exists(path))
        {
            throw new Xunit.Sdk.XunitException($"Required manual migration artifact is missing: {path}");
        }

        return File.ReadAllText(path);
    }

    private static string GetSolutionFilePath(params string[] segments)
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null)
        {
            string candidate = Path.Combine([directory.FullName, .. segments]);
            if (File.Exists(Path.Combine(directory.FullName, "Directory.Build.props")))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not locate the formal solution root.");
    }
}
