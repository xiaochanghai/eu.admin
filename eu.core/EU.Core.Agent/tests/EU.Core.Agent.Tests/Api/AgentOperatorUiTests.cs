using System.Net;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace EU.Core.Agent.Tests.Api;

public sealed class AgentOperatorUiTests
{
    [Fact]
    public async Task Operator_shell_and_javascript_modules_are_served_by_test_server()
    {
        using TestHost host = CreateHost();

        using HttpResponseMessage home = await host.Client.GetAsync("/");
        using HttpResponseMessage styles = await host.Client.GetAsync("/css/app.css");
        using HttpResponseMessage app = await host.Client.GetAsync("/js/app.js");
        using HttpResponseMessage apiClient = await host.Client.GetAsync("/js/api-client.js");
        using HttpResponseMessage editor = await host.Client.GetAsync("/js/agent-editor.js");
        using HttpResponseMessage skillsApi = await host.Client.GetAsync("/js/skills-api.js");
        using HttpResponseMessage skillsPage = await host.Client.GetAsync("/js/skills-page.js");
        using HttpResponseMessage skillEditor = await host.Client.GetAsync("/js/skill-editor.js");
        using HttpResponseMessage mcpApi = await host.Client.GetAsync("/js/mcp-api.js");
        using HttpResponseMessage mcpPage = await host.Client.GetAsync("/js/mcp-page.js");
        using HttpResponseMessage runner = await host.Client.GetAsync("/js/agent-runner.js");
        using HttpResponseMessage dom = await host.Client.GetAsync("/js/dom.js");

        Assert.Equal(HttpStatusCode.OK, home.StatusCode);
        Assert.Equal("text/html", home.Content.Headers.ContentType?.MediaType);
        Assert.Equal(HttpStatusCode.OK, styles.StatusCode);
        Assert.Equal("text/css", styles.Content.Headers.ContentType?.MediaType);
        Assert.All(
            new[] { app, apiClient, editor, skillsApi, skillsPage, skillEditor, mcpApi, mcpPage, runner, dom },
            response => Assert.Equal(HttpStatusCode.OK, response.StatusCode));
        Assert.All(
            new[] { app, apiClient, editor, skillsApi, skillsPage, skillEditor, mcpApi, mcpPage, runner, dom },
            response => Assert.Contains(
                "javascript",
                response.Content.Headers.ContentType?.MediaType ?? "",
                StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Operator_shell_exposes_the_p3_agent_and_skill_control_surfaces()
    {
        using TestHost host = CreateHost();

        string html = await host.Client.GetStringAsync("/");

        Assert.Contains("Agent 控制台", html, StringComparison.Ordinal);
        Assert.Contains("SQLite", html, StringComparison.Ordinal);
        Assert.Contains("Skill 管理", html, StringComparison.Ordinal);
        Assert.Contains("受控文件", html, StringComparison.Ordinal);
        Assert.Contains("Server", html, StringComparison.Ordinal);
        Assert.Contains("EU.Core.Agent.Api", html, StringComparison.Ordinal);
        Assert.Contains("P3 已开放", html, StringComparison.Ordinal);
        Assert.Contains("P4 开放", html, StringComparison.Ordinal);
        Assert.Contains("id=\"mcpPage\"", html, StringComparison.Ordinal);
        Assert.Contains("P4 已开放", html, StringComparison.Ordinal);
        Assert.Contains("默认拒绝网络访问", html, StringComparison.Ordinal);
        Assert.Contains("Credential Alias", html, StringComparison.Ordinal);
        Assert.Contains("P6 已开放：文本检索与引用", html, StringComparison.Ordinal);
        Assert.Contains("id=\"knowledgePage\"", html, StringComparison.Ordinal);
        Assert.Contains("仅支持 UTF-8 文本与 Markdown", html, StringComparison.Ordinal);
        Assert.Contains("id=\"orchestrationPage\"", html, StringComparison.Ordinal);
        Assert.Contains("P7 仅支持手动触发", html, StringComparison.Ordinal);
        Assert.DoesNotContain("Canary", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("生产环境", html, StringComparison.Ordinal);
        Assert.DoesNotContain("客户端部署", html, StringComparison.Ordinal);
        Assert.DoesNotContain("删除 Agent", html, StringComparison.Ordinal);
        Assert.Contains("运行 Agent", html, StringComparison.Ordinal);
        Assert.Contains("仅 ReadOnly 工具允许自动执行", html, StringComparison.Ordinal);
        Assert.Contains("最近运行", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Javascript_uses_the_accepted_agent_routes_without_unsafe_html_rendering()
    {
        using TestHost host = CreateHost();

        string apiClient = await host.Client.GetStringAsync("/js/api-client.js");
        string app = await host.Client.GetStringAsync("/js/app.js");
        string editor = await host.Client.GetStringAsync("/js/agent-editor.js");
        string skillsApi = await host.Client.GetStringAsync("/js/skills-api.js");
        string skillsPage = await host.Client.GetStringAsync("/js/skills-page.js");
        string skillEditor = await host.Client.GetStringAsync("/js/skill-editor.js");
        string mcpApi = await host.Client.GetStringAsync("/js/mcp-api.js");
        string mcpPage = await host.Client.GetStringAsync("/js/mcp-page.js");
        string runner = await host.Client.GetStringAsync("/js/agent-runner.js");
        string knowledgePage = await host.Client.GetStringAsync("/js/knowledge-page.js");
        string orchestrationPage = await host.Client.GetStringAsync("/js/orchestration-page.js");
        string dom = await host.Client.GetStringAsync("/js/dom.js");
        string javascript = string.Join(
            '\n',
            apiClient,
            app,
            editor,
            skillsApi,
            skillsPage,
            skillEditor,
            mcpApi,
            mcpPage,
            runner,
            knowledgePage,
            orchestrationPage,
            dom);

        Assert.Contains("/api/platform/capabilities", apiClient, StringComparison.Ordinal);
        Assert.Contains("/api/agents", apiClient, StringComparison.Ordinal);
        Assert.Contains("/draft", apiClient, StringComparison.Ordinal);
        Assert.Contains("/publish", apiClient, StringComparison.Ordinal);
        Assert.Contains("/status", apiClient, StringComparison.Ordinal);
        Assert.Contains("/export", apiClient, StringComparison.Ordinal);
        Assert.Contains("/import", apiClient, StringComparison.Ordinal);
        Assert.Contains("problem.detail || problem.title", apiClient, StringComparison.Ordinal);
        Assert.Contains("/api/skills", skillsApi, StringComparison.Ordinal);
        Assert.Contains("/files/content", skillsApi, StringComparison.Ordinal);
        Assert.Contains("skillVersionIds: value.skillVersionIds", editor, StringComparison.Ordinal);
        Assert.Contains("发布前必须填写 Instructions", editor, StringComparison.Ordinal);
        Assert.Contains("发布前必须选择 Model Profile", editor, StringComparison.Ordinal);
        Assert.Contains("textContent", javascript, StringComparison.Ordinal);
        Assert.DoesNotContain("innerHTML", javascript, StringComparison.Ordinal);
        Assert.Contains("/runs", apiClient, StringComparison.Ordinal);
        Assert.Contains("text/event-stream", apiClient, StringComparison.Ordinal);
        Assert.Contains("row.result.textContent", runner, StringComparison.Ordinal);
        Assert.Contains("JSON.stringify(JSON.parse(value.text)", runner, StringComparison.Ordinal);
        Assert.Contains("const base = \"/api/mcp\"", mcpApi, StringComparison.Ordinal);
        Assert.Contains("/servers", mcpApi, StringComparison.Ordinal);
        Assert.Contains("/api/mcp/tool-versions", apiClient, StringComparison.Ordinal);
        Assert.DoesNotContain("/run", mcpApi, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("/api/knowledge-bases", apiClient, StringComparison.Ordinal);
        Assert.Contains("knowledgeBaseIds: value.knowledgeBaseIds", editor, StringComparison.Ordinal);
        Assert.Contains("/api/orchestrations", apiClient, StringComparison.Ordinal);
        Assert.Contains("/details", apiClient, StringComparison.Ordinal);
        Assert.Contains("orchestrationRunDetails", orchestrationPage, StringComparison.Ordinal);
        Assert.Contains("tool.ArgumentsJson", orchestrationPage, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("tool.ResultContent", orchestrationPage, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("/schedules", javascript, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("const agents = [", javascript, StringComparison.OrdinalIgnoreCase);
    }

    private static TestHost CreateHost()
    {
        var variables = new EnvironmentVariableScope(
            ("AgentPlatform__ServiceName", "agent-ui"),
            ("AgentPlatform__ModelEndpoint", "https://model.example.test/v1"),
            ("AgentPlatform__ModelCredentialAlias", "alias:development-agent-model"),
            ("AgentPlatform__ModelApiKey", null),
            ("AgentStorage__Provider", "InMemory"),
            ("AgentControl__ModelProfileIds__0", "qwen-safe"),
            ("AgentControl__ModelProfileIds__1", "structured-safe"));
        try
        {
            Type entryPoint = Assembly.Load("EU.Core.Agent.Api").GetType("Program")
                ?? throw new InvalidOperationException("The API entry point was not found.");
            Type factoryType = typeof(WebApplicationFactory<>).MakeGenericType(entryPoint);
            IDisposable factory = (IDisposable)(Activator.CreateInstance(factoryType)
                ?? throw new InvalidOperationException("The API test host could not be created."));
            MethodInfo createClient = factoryType.GetMethod(
                "CreateClient",
                [typeof(WebApplicationFactoryClientOptions)])
                ?? throw new InvalidOperationException("The API test host has no client factory.");
            HttpClient client = (HttpClient)createClient.Invoke(factory, [new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            }])!;
            return new TestHost(client, factory, variables);
        }
        catch
        {
            variables.Dispose();
            throw;
        }
    }

    private sealed class TestHost(
        HttpClient client,
        IDisposable factory,
        EnvironmentVariableScope variables) : IDisposable
    {
        public HttpClient Client { get; } = client;

        public void Dispose()
        {
            Client.Dispose();
            factory.Dispose();
            variables.Dispose();
        }
    }

    private sealed class EnvironmentVariableScope : IDisposable
    {
        private readonly Dictionary<string, string?> _originalValues;

        public EnvironmentVariableScope(params (string Name, string? Value)[] values)
        {
            _originalValues = values.ToDictionary(
                value => value.Name,
                value => Environment.GetEnvironmentVariable(value.Name),
                StringComparer.Ordinal);
            foreach ((string name, string? value) in values)
            {
                Environment.SetEnvironmentVariable(name, value);
            }
        }

        public void Dispose()
        {
            foreach ((string name, string? value) in _originalValues)
            {
                Environment.SetEnvironmentVariable(name, value);
            }
        }
    }
}
