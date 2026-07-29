using EU.Core.Agent.Application.Agents;
using EU.Core.Agent.Application.Knowledge;
using EU.Core.Agent.Application.Runtime;
using EU.Core.Agent.Application.Validation;
using EU.Core.Agent.Infrastructure.Persistence;
using System.Runtime.CompilerServices;
using Xunit;

namespace EU.Core.Agent.Tests.Knowledge;

public sealed class KnowledgeRetrievalTests
{
    [Fact]
    public async Task Text_import_chunks_and_retrieves_Chinese_supplier_content()
    {
        var repository = new InMemoryKnowledgeBaseRepository();
        var service = new KnowledgeLifecycleService(repository, repository);
        KnowledgeBaseDefinition created = Successful(await service.CreateAsync(
            new CreateKnowledgeBaseCommand("supplier-guide", "供应商手册", "")));
        string content = string.Join("\n\n", Enumerable.Repeat(
            "供应商列表可按名称、编码和启用状态查询。采购人员使用供应商模块查看联系信息。", 60));

        KnowledgeBaseDefinition imported = Successful(await service.ImportDocumentAsync(
            new ImportKnowledgeDocumentCommand(
                created.Id, created.LogicalRevision, "supplier.md", "text/markdown", content)));
        IReadOnlyList<KnowledgeSearchResult> results =
            await service.SearchAsync(imported.Id, "查询供应商列表", 6);

        Assert.True(imported.Chunks.Count > 1);
        KnowledgeSearchResult result = Assert.Single(results.Take(1));
        Assert.Equal("supplier.md", result.FileName);
        Assert.Contains("供应商列表", result.Content, StringComparison.Ordinal);
        Assert.True(result.Score > 0);
    }

    [Fact]
    public async Task Agent_publish_freezes_authorized_knowledge_revision()
    {
        var knowledge = new InMemoryKnowledgeBaseRepository();
        var knowledgeService = new KnowledgeLifecycleService(knowledge, knowledge);
        KnowledgeBaseDefinition created = Successful(await knowledgeService.CreateAsync(
            new CreateKnowledgeBaseCommand("policies", "政策", "")));
        KnowledgeBaseDefinition indexed = Successful(await knowledgeService.ImportDocumentAsync(
            new ImportKnowledgeDocumentCommand(
                created.Id, 0, "policy.txt", "text/plain", "差旅报销需要提交发票和审批单。")));
        var agents = new InMemoryAgentRepository();
        var lifecycle = new AgentLifecycleService(
            agents, knowledgeBases: knowledge);
        AgentDefinition agent = Successful(await lifecycle.CreateAsync(
            new CreateAgentCommand("policy-agent")));
        agent = Successful(await lifecycle.SaveDraftAsync(new SaveAgentDraftCommand(
            agent.Id, agent.LogicalRevision, "回答政策问题", "qwen",
            AgentOutputMode.Text, null, KnowledgeBaseIds: [indexed.Id])));
        agent = Successful(await lifecycle.PublishAsync(
            new PublishAgentCommand(agent.Id, agent.LogicalRevision)));

        AgentKnowledgeBindingSnapshot binding = Assert.Single(
            Assert.Single(agent.PublishedVersions).Snapshot!.KnowledgeBases);
        Assert.Equal(indexed.Id, binding.KnowledgeBaseId);
        Assert.Equal(indexed.LogicalRevision, binding.LogicalRevision);
    }

    [Fact]
    public async Task Runtime_preparation_retrieves_only_authorized_knowledge()
    {
        var knowledge = new InMemoryKnowledgeBaseRepository();
        var knowledgeService = new KnowledgeLifecycleService(knowledge, knowledge);
        KnowledgeBaseDefinition kb = Successful(await knowledgeService.CreateAsync(
            new CreateKnowledgeBaseCommand("expenses", "报销", "")));
        kb = Successful(await knowledgeService.ImportDocumentAsync(
            new ImportKnowledgeDocumentCommand(
                kb.Id, 0, "expenses.md", "text/markdown", "报销申请需要发票。")));
        var agents = new InMemoryAgentRepository();
        Guid versionId = Guid.NewGuid();
        var snapshot = new AgentVersionSnapshot(
            versionId, "expense-agent", "回答问题", "qwen", AgentOutputMode.Text,
            null, [], [])
        {
            KnowledgeBases = [new AgentKnowledgeBindingSnapshot(kb.Id, kb.LogicalRevision)]
        };
        var draft = new AgentVersion(Guid.NewGuid(), "0.1.0", true, "", "", AgentOutputMode.Text, null, null, null);
        var published = new AgentVersion(
            versionId, "1.0.0", false, "回答问题", "qwen", AgentOutputMode.Text, null, null, snapshot);
        var agent = new AgentDefinition(
            Guid.NewGuid(), "expense-agent", "", "", AgentRuntimeStatus.Enabled, 0,
            draft, [published]);
        Assert.True(await agents.TryCreateAsync(agent));
        var runtime = new AgentRuntimeService(
            agents,
            new EmptyToolCatalog(),
            new EmptyRuntimeEngine(),
            new InMemoryAgentRunAuditRepository(),
            new JsonSchemaValidator(),
            knowledge,
            knowledge);

        AgentRunPreparationResult preparation =
            await runtime.PrepareAsync(agent.Id, "发票怎么报销");

        Assert.True(preparation.Succeeded);
        Assert.Single(preparation.Context!.Knowledge);
        Assert.Equal(kb.Id, preparation.Context.Knowledge[0].KnowledgeBaseId);

        Successful(await knowledgeService.UpdateAsync(new UpdateKnowledgeBaseCommand(
            kb.Id, kb.LogicalRevision, "报销（修订）", "", KnowledgeBaseStatus.Enabled)));
        AgentRunPreparationResult stale =
            await runtime.PrepareAsync(agent.Id, "发票怎么报销");
        Assert.Equal(AgentRunErrorCodes.KnowledgeUnavailable, stale.Error?.Code);
    }

    private static T Successful<T>(KnowledgeOperationResult<T> result)
    {
        Assert.True(result.Succeeded, result.Error?.Message);
        return result.Value!;
    }

    private static T Successful<T>(AgentOperationResult<T> result)
    {
        Assert.True(result.Succeeded, result.Error?.Message);
        return result.Value!;
    }

    private sealed class EmptyToolCatalog : EU.Core.Agent.Application.Mcp.IPublishedMcpToolCatalog
    {
        public Task<bool> ExistsAsync(Guid versionId, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);
        public Task<IReadOnlyList<EU.Core.Agent.Application.Mcp.PublishedMcpToolReference>> ListAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<EU.Core.Agent.Application.Mcp.PublishedMcpToolReference>>([]);
    }

    private sealed class EmptyRuntimeEngine : IAgentRuntimeEngine
    {
        public async IAsyncEnumerable<AgentRunEvent> StreamAsync(
            AgentRunContext context,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            yield break;
        }
    }
}
