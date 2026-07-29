using EU.Core.Agent.Application.Abstractions.Persistence;
using EU.Core.Agent.Infrastructure.Persistence;
using SqlSugar;
using System.Reflection;
using Xunit;

namespace EU.Core.Agent.Tests.Persistence;

public sealed class DatabaseFoundationContractTests
{
    [Fact]
    public void Migration_0001_is_an_idempotent_sql_server_2014_schema_only_script()
    {
        string migration = File.ReadAllText(GetSolutionFilePath("database", "migrations", "0001_agent_schema.sql"));

        Assert.Contains("IF SCHEMA_ID(N'agent') IS NULL", migration, StringComparison.Ordinal);
        Assert.Contains("CREATE SCHEMA [agent]", migration, StringComparison.Ordinal);
        Assert.DoesNotContain("CREATE TABLE", migration, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("[dbo]", migration, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("CREATE OR ALTER", migration, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DROP IF EXISTS", migration, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("AgentDefinition", migration, StringComparison.Ordinal);
        Assert.DoesNotContain("SkillDefinition", migration, StringComparison.Ordinal);
        Assert.DoesNotContain("McpService", migration, StringComparison.Ordinal);
        Assert.DoesNotContain("Conversation", migration, StringComparison.Ordinal);
        Assert.DoesNotContain("ExecutionStep", migration, StringComparison.Ordinal);
    }

    [Fact]
    public void Two_stage_mapping_resolves_entities_to_agent_and_never_dbo()
    {
        using var database = new SqlSugarClient(new ConnectionConfig
        {
            DbType = DbType.SqlServer,
            IsAutoCloseConnection = true
        });

        SqlSugarAgentUnitOfWork.ConfigureAgentEntity<ProbeEntity>(database, "PersistenceProbe");

        var sql = database.Queryable<ProbeEntity>().ToSql();
        Assert.Contains("[agent].[PersistenceProbe]", sql.Key, StringComparison.Ordinal);
        Assert.DoesNotContain("[dbo].[PersistenceProbe]", sql.Key, StringComparison.Ordinal);
    }

    [Fact]
    public void Entity_base_is_a_schema_agnostic_marker_without_predefined_persistence_fields()
    {
        Assert.Empty(typeof(EntityBase).GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public));
        Assert.Empty(typeof(EntityBase).GetFields(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic));
    }

    [Fact]
    public async Task Provider_neutral_unit_of_work_expresses_the_transaction_lifecycle_without_a_database()
    {
        var operations = new RecordingTransactionOperations();
        await using var unitOfWork = new SqlSugarAgentUnitOfWork(operations);

        await unitOfWork.BeginAsync();
        Assert.True(unitOfWork.IsTransactionActive);
        await unitOfWork.CommitAsync();

        Assert.False(unitOfWork.IsTransactionActive);
        Assert.Equal(["begin", "commit"], operations.Calls);
        Assert.IsAssignableFrom<IAgentUnitOfWork>(unitOfWork);
    }

    [Fact]
    public async Task Cancellation_prevents_a_transaction_from_starting_without_a_database()
    {
        var operations = new RecordingTransactionOperations();
        await using var unitOfWork = new SqlSugarAgentUnitOfWork(operations);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() => unitOfWork.BeginAsync(cancellation.Token));

        Assert.Empty(operations.Calls);
        Assert.False(unitOfWork.IsTransactionActive);
    }

    [Fact]
    public async Task Disposing_an_open_unit_of_work_rolls_back_safely_without_a_database()
    {
        var operations = new RecordingTransactionOperations();
        var unitOfWork = new SqlSugarAgentUnitOfWork(operations);
        await unitOfWork.BeginAsync();

        await unitOfWork.DisposeAsync();

        Assert.Equal(["begin", "rollback", "dispose"], operations.Calls);
    }

    [Fact]
    public void Typed_concurrency_conflict_preserves_the_entity_identity_for_later_repositories()
    {
        Guid entityId = Guid.NewGuid();
        var conflict = new AgentConcurrencyConflictException("AgentDefinition", entityId);

        Assert.Equal("AgentDefinition", conflict.EntityName);
        Assert.Equal(entityId, conflict.EntityId);
        Assert.Contains("concurrency", conflict.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static string GetSolutionFilePath(params string[] segments)
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null)
        {
            string candidate = Path.Combine([directory.FullName, .. segments]);
            if (File.Exists(Path.Combine(directory.FullName, "Directory.Build.props")) && File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not locate the formal solution root.");
    }

    private sealed class ProbeEntity : EntityBase;

    private sealed class RecordingTransactionOperations : ISqlSugarTransactionOperations
    {
        public List<string> Calls { get; } = [];

        public void Begin() => Calls.Add("begin");

        public void Commit() => Calls.Add("commit");

        public void Rollback() => Calls.Add("rollback");

        public void Dispose() => Calls.Add("dispose");
    }
}
