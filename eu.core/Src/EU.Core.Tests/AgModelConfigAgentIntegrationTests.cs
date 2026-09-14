#nullable enable
using System.Linq.Expressions;
using System.Reflection;
using System.Security.Cryptography;
using EU.Core.Common.Caches;
using StackExchange.Redis;
using EU.Core.Api.Agent.Configuration;
using EU.Core.IRepository.Base;
using EU.Core.Model.Entity;
using EU.Core.Services;
using EU.Core.Agent.Runtime;
using EU.Core.IServices;
using EU.Core.IServices.Runtime;
using EU.Core.Model.ViewModels.Extend;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace EU.Core.Tests;

/// <summary>生产 AgModelConfigServices 的离线解析测试；仓储与 Redis 连接均为内存代理，不访问外部服务。</summary>
public sealed class AgModelConfigAgentIntegrationTests
{
    [Fact]
    public async Task Add_writes_complete_encrypted_record_once_before_runtime_use()
    {
        var fixture = Create();
        var input = Newtonsoft.Json.JsonConvert.SerializeObject(new
        {
            ProfileCode = "new-model", ModelName = "qwen-new", Endpoint = "https://models.example.test/v1",
            Enabled = true, TimeoutSeconds = 60, ApiKey = "new-offline-value",
            ApiKeyCiphertext = "must-not-be-trusted", CredentialRevision = 999, LogicalRevision = 999
        });
        var id = await fixture.Service.Add((object)input);
        var row = Assert.Single(fixture.Rows, value => value.ID == id);
        Assert.StartsWith("amc:v1:", row.ApiKeyCiphertext);
        Assert.Equal(1L, row.CredentialRevision);
        Assert.Equal(1L, row.LogicalRevision);
        Assert.Equal(1, fixture.Repository.AddCalls);
        Assert.Equal(0, fixture.Repository.UpdateCalls);
        Assert.Equal("new-offline-value", (await fixture.Service.ResolveRuntimeProfileAsync("new-model")).ApiKey);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task Invalid_api_key_does_not_insert_partial_configuration(string? key)
    {
        var fixture = Create();
        var input = Newtonsoft.Json.JsonConvert.SerializeObject(new { ProfileCode = "new-model", ApiKey = key });
        await Assert.ThrowsAsync<ArgumentException>(() => fixture.Service.Add((object)input));
        Assert.Equal(0, fixture.Repository.AddCalls);
        Assert.Single(fixture.Rows);
    }

    [Fact]
    public async Task Invalid_master_key_does_not_insert_partial_configuration()
    {
        var fixture = Create();
        fixture.Redis.Key = "invalid";
        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.Add((object)"{\"ApiKey\":\"offline-value\"}"));
        Assert.Equal(0, fixture.Repository.AddCalls);
    }

    [Fact]
    public async Task Key_update_targets_route_id_and_next_runtime_reads_new_credential()
    {
        var fixture = Create();
        var row = fixture.Rows[0];
        var routeId = row.ID;
        var input = Newtonsoft.Json.JsonConvert.SerializeObject(new
        {
            ID = Guid.NewGuid(), ApiKey = "rotated-offline-value", ApiKeyCiphertext = "untrusted",
            CredentialRevision = 999, LogicalRevision = 999
        });
        Assert.True(await fixture.Service.Update(routeId, (object)input));
        Assert.Equal(routeId, row.ID);
        Assert.Equal(2L, row.CredentialRevision);
        Assert.Equal(2L, row.LogicalRevision);
        Assert.Contains("ApiKeyCiphertext", fixture.Repository.LastColumns);
        Assert.DoesNotContain("ApiKey", fixture.Repository.LastColumns);
        Assert.DoesNotContain("ID", fixture.Repository.LastColumns);
        Assert.Equal("rotated-offline-value", (await fixture.Service.ResolveRuntimeProfileAsync("sales-model")).ApiKey);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public async Task Blank_key_preserves_credential_and_ignores_submitted_ciphertext(string? key)
    {
        var fixture = Create();
        var row = fixture.Rows[0];
        var ciphertext = row.ApiKeyCiphertext;
        var input = Newtonsoft.Json.JsonConvert.SerializeObject(new
        {
            DisplayName = "updated-name", ApiKey = key, ApiKeyCiphertext = "untrusted", CredentialRevision = 999
        });
        Assert.True(await fixture.Service.Update(row.ID, (object)input));
        Assert.Equal("updated-name", row.DisplayName);
        Assert.Equal(ciphertext, row.ApiKeyCiphertext);
        Assert.Equal(1L, row.CredentialRevision);
        Assert.Equal(2L, row.LogicalRevision);
        Assert.Equal("offline-test-value", (await fixture.Service.ResolveRuntimeProfileAsync("sales-model")).ApiKey);
    }

    [Fact]
    public async Task Missing_deleted_and_empty_targets_are_not_updated()
    {
        var fixture = Create();
        const string input = "{\"DisplayName\":\"updated\"}";
        Assert.False(await fixture.Service.Update(Guid.Empty, (object)input));
        Assert.False(await fixture.Service.Update(Guid.NewGuid(), (object)input));
        fixture.Rows[0].IsDeleted = true;
        Assert.False(await fixture.Service.Update(fixture.Rows[0].ID, (object)input));
        Assert.Equal(0, fixture.Repository.UpdateCalls);
    }

    [Fact]
    public async Task Encryption_failure_on_update_keeps_existing_credential()
    {
        var fixture = Create();
        var row = fixture.Rows[0];
        var ciphertext = row.ApiKeyCiphertext;
        fixture.Redis.Key = "invalid";
        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.Update(row.ID, (object)"{\"ApiKey\":\"new-offline-value\"}"));
        Assert.Equal(ciphertext, row.ApiKeyCiphertext);
        Assert.Equal(0, fixture.Repository.UpdateCalls);
    }

    [Fact]
    public async Task Host_catalog_opens_fresh_service_scopes_and_observes_disable()
    {
        var fixture = Create();
        int scopes = 0;
        var services = new ServiceCollection();
        services.AddScoped<IAgModelConfigServices>(_ => { scopes++; return fixture.Service; });
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        var catalog = new AgModelProfileCatalog(provider.GetRequiredService<IServiceScopeFactory>());
        Assert.True(await catalog.ExistsAsync("sales-model"));
        Assert.False(await catalog.ExistsAsync("Sales-model"));
        Assert.Equal("qwen-offline", (await catalog.ResolveAsync("sales-model")).ModelName);
        fixture.Rows[0].Enabled = false;
        Assert.Empty(await catalog.ListAsync());
        await Assert.ThrowsAsync<InvalidOperationException>(() => catalog.ResolveAsync("sales-model"));
        Assert.Equal(5, scopes);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Runtime_and_judge_never_fall_back_when_database_resolution_fails(bool nullResult)
    {
        var options = new AgentRuntimeOptions(new Uri("https://unused.invalid"), "unused", TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(5));
        var legacy = new RejectLegacyCredentials();
        var resolver = new UnavailableResolver(nullResult);
        var engine = new MicrosoftAgentRuntimeEngine(options, legacy, null!, NullLogger<MicrosoftAgentRuntimeEngine>.Instance, resolver);
        var snapshot = new AgentVersionSnapshot(Guid.NewGuid(), "sales-agent", "instructions", "sales-model", default, null, [], []);
        var context = new AgentRunContext(Guid.NewGuid(), Guid.NewGuid(), snapshot, "hello", "", DateTimeOffset.UtcNow, []);
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await foreach (var item in engine.StreamAsync(context)) Assert.Fail("No model request should be made.");
        });
        var judge = new MicrosoftExtensionsModelJudgeEngine(options, legacy, resolver);
        await Assert.ThrowsAsync<InvalidOperationException>(() => judge.EvaluateAsync("input", "output", "sales-model", []));
        Assert.Equal(0, legacy.Calls);
        Assert.Equal(new[] { "sales-model", "sales-model" }, resolver.Codes);
    }

    private sealed class RejectLegacyCredentials : IModelCredentialResolver
    {
        public int Calls { get; private set; }
        public ValueTask<string?> ResolveAsync(string credentialAlias, CancellationToken cancellationToken = default)
        {
            Calls++;
            throw new NotSupportedException("Legacy credentials must not be read.");
        }
    }

    private sealed class UnavailableResolver(bool nullResult) : IAgentModelProfileResolver
    {
        public List<string> Codes { get; } = [];
        public Task<AgentModelRuntimeProfile> ResolveAsync(string profileCode, CancellationToken cancellationToken = default)
        {
            Codes.Add(profileCode);
            return nullResult ? Task.FromResult<AgentModelRuntimeProfile>(null!)
                : Task.FromException<AgentModelRuntimeProfile>(new InvalidOperationException("Unavailable model."));
        }
    }

    [Fact]
    public async Task Catalog_contains_only_enabled_live_safe_codes_without_credentials()
    {
        var fixture = Create();
        fixture.Rows.Add(new AgModelConfig { ProfileCode = "disabled", Enabled = false });
        fixture.Rows.Add(new AgModelConfig { ProfileCode = "deleted", Enabled = true, IsDeleted = true });
        fixture.Rows.Add(new AgModelConfig { ProfileCode = "sk-private", Enabled = true });
        fixture.Rows.Add(new AgModelConfig { ProfileCode = " padded ", Enabled = true });
        Assert.Equal(new[] { "sales-model" }, await fixture.Service.ListAvailableProfilesAsync());
    }

    [Fact]
    public async Task Resolves_actual_model_endpoint_timeout_thinking_and_bound_credential()
    {
        var fixture = Create();
        var profile = await fixture.Service.ResolveRuntimeProfileAsync("sales-model");
        Assert.Equal("qwen-offline", profile.ModelName);
        Assert.Equal(new Uri("https://models.example.test/v1"), profile.Endpoint);
        Assert.Equal(TimeSpan.FromSeconds(45), profile.Timeout);
        Assert.False(profile.EnableThinking);
        Assert.Equal("offline-test-value", profile.ApiKey);
        Assert.DoesNotContain(profile.ApiKey, System.Text.Json.JsonSerializer.Serialize(profile));
        Assert.DoesNotContain(profile.ApiKey, Newtonsoft.Json.JsonConvert.SerializeObject(profile));
        Assert.DoesNotContain(profile.ApiKey, profile.ToString());
        fixture.Rows[0].ModelName = "changed-model";
        Assert.Equal("changed-model", (await fixture.Service.ResolveRuntimeProfileAsync("sales-model")).ModelName);
    }

    [Theory]
    [InlineData("Sales-model")]
    [InlineData("missing")]
    [InlineData("sk-private")]
    public async Task Rejects_missing_wrong_case_and_unsafe_references(string code)
    {
        var fixture = Create();
        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.ResolveRuntimeProfileAsync(code));
    }

    [Theory]
    [InlineData("disabled")]
    [InlineData("deleted")]
    [InlineData("duplicate")]
    [InlineData("credential")]
    [InlineData("binding")]
    [InlineData("timeout")]
    [InlineData("provider")]
    public async Task Rejects_unavailable_or_invalid_configuration_without_secret_in_error(string reason)
    {
        var fixture = Create();
        var row = fixture.Rows[0];
        switch (reason)
        {
            case "disabled": row.Enabled = false; break;
            case "deleted": row.IsDeleted = true; break;
            case "duplicate": fixture.Rows.Add(row); break;
            case "credential": row.ApiKeyCiphertext = "invalid"; break;
            case "binding": row.ID = Guid.NewGuid(); break;
            case "timeout": row.TimeoutSeconds = 0; break;
            case "provider": row.Provider = "unsupported"; break;
        }
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.ResolveRuntimeProfileAsync("sales-model"));
        Assert.DoesNotContain("offline-test-value", error.ToString());
        Assert.DoesNotContain(row.ApiKeyCiphertext, error.ToString());
    }

    [Theory]
    [InlineData("http://models.example.test/v1")]
    [InlineData("https://localhost/v1")]
    [InlineData("https://models.example.test/v1#fragment")]
    [InlineData("https://models.example.test/v1?apiKey=example")]
    [InlineData("https://user:pass@models.example.test/v1")]
    public async Task Rejects_unsafe_endpoint(string endpoint)
    {
        var fixture = Create();
        fixture.Rows[0].Endpoint = endpoint;
        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.ResolveRuntimeProfileAsync("sales-model"));
    }

    [Fact]
    public async Task Cancellation_does_not_query_repository()
    {
        var fixture = Create();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => fixture.Service.ListAvailableProfilesAsync(cancellation.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => fixture.Service.ResolveRuntimeProfileAsync("sales-model", cancellation.Token));
    }

    [Fact]
    public void Database_models_do_not_require_legacy_endpoint_or_credential_alias()
    {
        var configuration = new ConfigurationBuilder().Build();
        Assert.True(new AgentPlatformOptionsValidator(configuration).Validate(null, new AgentPlatformOptions { ServiceName = "agent-api" }).Succeeded);
    }

    [Fact]
    public async Task Resolves_new_https_host_without_host_allowlist_or_legacy_endpoint()
    {
        var fixture = Create();
        fixture.Rows[0].Endpoint = "https://new-provider.example.test/v1";
        Assert.Null(fixture.Configuration["AgentPlatform:ModelEndpoint"]);
        Assert.Empty(fixture.Configuration.GetSection("ModelConfig:AllowedHosts").GetChildren());
        var profile = await fixture.Service.ResolveRuntimeProfileAsync("sales-model");
        Assert.Equal(new Uri("https://new-provider.example.test/v1"), profile.Endpoint);
    }

    private static (AgModelConfigServices Service, List<AgModelConfig> Rows, MemoryRepository Repository, IConfiguration Configuration, MemoryRedis Redis) Create()
    {
        string key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var id = Guid.NewGuid();
        List<AgModelConfig> rows = [new()
        {
            ID = id, ProfileCode = "sales-model", Provider = "OpenAICompatible", Enabled = true,
            Endpoint = "https://models.example.test/v1", ModelName = "qwen-offline", TimeoutSeconds = 45,
            EnableThinking = false, CredentialRevision = 1, LogicalRevision = 1,
            ApiKeyCiphertext = ModelConfigCredentialCipher.Protect(key, id, "offline-test-value")
        }];
        var repository = DispatchProxy.Create<IBaseRepository<AgModelConfig>, MemoryRepository>();
        ((MemoryRepository)(object)repository).Rows = rows;
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Redis:InstanceName"] = "offline:"
        }).Build();
        var database = DispatchProxy.Create<IDatabase, MemoryRedis>();
        var redis = (MemoryRedis)(object)database;
        redis.Key = key;
        var connection = DispatchProxy.Create<IConnectionMultiplexer, MemoryRedisConnection>();
        ((MemoryRedisConnection)(object)connection).Database = database;
        var factory = new RedisCacheServiceFactory(connection, configuration);
        return (new AgModelConfigServices(repository, factory), rows, (MemoryRepository)(object)repository, configuration, redis);
    }

    public class MemoryRedisConnection : DispatchProxy
    {
        public IDatabase Database { get; set; } = null!;

        protected override object? Invoke(MethodInfo? method, object?[]? args)
        {
            if (method?.Name == "GetDatabase" && args is { Length: 2 })
            {
                Assert.Equal(9, (int)args[0]!);
                return Database;
            }
            throw new NotSupportedException(method?.Name);
        }
    }

    public class MemoryRedis : DispatchProxy
    {
        public string? Key { get; set; }
        public Exception? Failure { get; set; }
        public int Reads { get; private set; }

        protected override object? Invoke(MethodInfo? method, object?[]? args)
        {
            if (method?.Name == "HashGetAsync" && args is { Length: 3 })
            {
                Assert.Equal("offline:ModelConfig", ((RedisKey)args[0]!).ToString());
                Assert.Equal("EncryptionKey", ((RedisValue)args[1]!).ToString());
                Reads++;
                return Failure is null ? Task.FromResult((RedisValue)Key) : Task.FromException<RedisValue>(Failure);
            }
            throw new NotSupportedException(method?.Name);
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("invalid")]
    [InlineData("YWJj")]
    public async Task Missing_or_invalid_master_key_rejects_all_credential_operations(string? key)
    {
        var fixture = Create();
        fixture.Redis.Key = key;
        var row = fixture.Rows[0];
        var ciphertext = row.ApiKeyCiphertext;
        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.Add((object)"{\"ApiKey\":\"offline-value\"}"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.Update(row.ID, (object)"{\"ApiKey\":\"offline-value\"}"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.ResolveRuntimeProfileAsync("sales-model"));
        Assert.Equal(3, fixture.Redis.Reads);
        Assert.Equal(0, fixture.Repository.AddCalls);
        Assert.Equal(0, fixture.Repository.UpdateCalls);
        Assert.Equal(ciphertext, row.ApiKeyCiphertext);
    }

    [Fact]
    public async Task Redis_failure_propagates_without_writing_or_falling_back()
    {
        var fixture = Create();
        fixture.Redis.Failure = new RedisConnectionException(ConnectionFailureType.UnableToConnect, "Offline simulated failure.");
        await Assert.ThrowsAsync<RedisConnectionException>(() => fixture.Service.Add((object)"{\"ApiKey\":\"offline-value\"}"));
        await Assert.ThrowsAsync<RedisConnectionException>(() => fixture.Service.Update(fixture.Rows[0].ID, (object)"{\"ApiKey\":\"offline-value\"}"));
        await Assert.ThrowsAsync<RedisConnectionException>(() => fixture.Service.ResolveRuntimeProfileAsync("sales-model"));
        Assert.Equal(0, fixture.Repository.AddCalls);
        Assert.Equal(0, fixture.Repository.UpdateCalls);
    }

    [Fact]
    public async Task Independent_services_do_not_share_keys_and_reread_on_each_use()
    {
        var first = Create();
        var second = Create();
        await first.Service.ResolveRuntimeProfileAsync("sales-model");
        await second.Service.ResolveRuntimeProfileAsync("sales-model");
        first.Redis.Key = second.Redis.Key;
        await Assert.ThrowsAsync<InvalidOperationException>(() => first.Service.ResolveRuntimeProfileAsync("sales-model"));
        Assert.Equal("offline-test-value", (await second.Service.ResolveRuntimeProfileAsync("sales-model")).ApiKey);
        Assert.Equal(2, first.Redis.Reads);
        Assert.Equal(2, second.Redis.Reads);
    }

    public class MemoryRepository : DispatchProxy
    {
        public List<AgModelConfig> Rows { get; set; } = [];
        public int AddCalls { get; private set; }
        public int UpdateCalls { get; private set; }
        public List<string> LastColumns { get; private set; } = [];
        protected override object? Invoke(MethodInfo? method, object?[]? args)
        {
            if (method?.Name == "Add" && args is { Length: 2 } && args[1] is Guid id)
            {
                AddCalls++;
                var row = (AgModelConfig)args[0]!;
                Assert.Equal(id, row.ID);
                Assert.StartsWith("amc:v1:", row.ApiKeyCiphertext);
                Rows.Add(row);
                return Task.FromResult(id);
            }
            if (method?.Name == "QuerySingle" && args is { Length: 1 })
            {
                var filter = ((Expression<Func<AgModelConfig, bool>>)args[0]!).Compile();
                return Task.FromResult(Rows.SingleOrDefault(filter));
            }
            if (method?.Name == "Update" && args is { Length: 4 } && args[0] is AgModelConfig update)
            {
                UpdateCalls++;
                LastColumns = (List<string>)args[1]!;
                var row = Rows.Single(value => value.ID == update.ID);
                foreach (var column in LastColumns)
                {
                    var property = typeof(AgModelConfig).GetProperty(column)!;
                    property.SetValue(row, property.GetValue(update));
                }
                return Task.FromResult(true);
            }
            if (method?.Name == "Query" && args is { Length: 3 })
            {
                var select = ((Expression<Func<AgModelConfig, string>>)args[0]!).Compile();
                var filter = ((Expression<Func<AgModelConfig, bool>>)args[1]!).Compile();
                return Task.FromResult(Rows.Where(filter).Select(select).ToList());
            }
            if (method?.Name == "Query" && args is { Length: 1 })
            {
                var filter = ((Expression<Func<AgModelConfig, bool>>)args[0]!).Compile();
                return Task.FromResult(Rows.Where(filter).ToList());
            }
            throw new NotSupportedException(method?.Name);
        }
    }
}
