#nullable enable

using System.Reflection;
using System.Text.Json;
using EU.Core.Api.Agent.Controllers;
using EU.Core.Api.Agent.Security;
using EU.Core.Model;
using EU.Core.Model.Models;
using EU.Core.Model.ViewModels.Extend;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace EU.Core.Tests.Service_Test;

public sealed class AgAgentApiMigrationCompletion_Should
{
    [Fact]
    public void Shared_controller_success_preserves_the_service_result_default_message()
    {
        ServiceResult<string> result = new ResponseProbeController().Succeeded("value");

        Assert.True(result.Success);
        Assert.Equal("操作成功", result.Message);
        Assert.Equal("value", result.Data);
    }

    [Fact]
    public void Main_api_controller_preserves_its_success_message_after_sharing_the_base()
    {
        var controller = new EU.Core.Controllers.BaseApiController();
        ServiceResult<string> result = controller.Success<string>("value");
        ServiceResult<string> failure = controller.Failed();

        Assert.True(result.Success);
        Assert.Equal("成功", result.Message);
        Assert.Equal("value", result.Data);
        Assert.False(failure.Success);
        Assert.Equal("失败", failure.Message);
    }

    [Fact]
    public void All_agent_controllers_use_the_shared_base_and_preserve_policy_boundaries()
    {
        Type sharedBase = typeof(EU.Core.Api.Agent.Base.ControllerBase);
        Type[] controllerTypes = typeof(AgentsController).Assembly.GetTypes()
            .Where(type =>
                !type.IsAbstract
                && typeof(Microsoft.AspNetCore.Mvc.ControllerBase).IsAssignableFrom(type)
                && string.Equals(
                    type.Namespace,
                    "EU.Core.Api.Agent.Controllers",
                    StringComparison.Ordinal))
            .ToArray();

        Assert.All(controllerTypes, type => Assert.True(sharedBase.IsAssignableFrom(type), type.FullName));
        Assert.Empty(sharedBase.GetCustomAttributes<AuthorizeAttribute>(inherit: false));

        AssertPolicy<AgentsController>(AgentAuthorizationPolicies.Admin);
        AssertPolicy<BusinessQueryRetentionController>(AgentAuthorizationPolicies.Admin);
        AssertPolicy<EvaluationSuitesController>(AgentAuthorizationPolicies.Admin);
        AssertPolicy<KnowledgeBaseReferencesController>(AgentAuthorizationPolicies.Admin);
        AssertPolicy<KnowledgeBasesController>(AgentAuthorizationPolicies.Admin);
        AssertPolicy<MainAgentController>(AgentAuthorizationPolicies.Admin);
        AssertPolicy<McpServersController>(AgentAuthorizationPolicies.Admin);
        AssertPolicy<McpToolVersionsController>(AgentAuthorizationPolicies.Admin);
        AssertPolicy<SkillVersionsController>(AgentAuthorizationPolicies.Admin);
        AssertPolicy<SkillsController>(AgentAuthorizationPolicies.Admin);
        AssertPolicy<AgentRunsController>(AgentAuthorizationPolicies.Debug);
        AssertPolicy<EvaluationBatchesController>(AgentAuthorizationPolicies.Debug);
        AssertPolicy<RunEvaluationsController>(AgentAuthorizationPolicies.Debug);
        AssertPolicy<AuditController>(AgentAuthorizationPolicies.AuditRead);
        AssertPolicy<MetricsController>(AgentAuthorizationPolicies.AuditRead);
        AssertPolicy<PlatformController>(AgentAuthorizationPolicies.AuditRead);
    }

    [Fact]
    public void System_text_json_preserves_pascal_case_and_dynamic_dictionary_keys()
    {
        string json = JsonSerializer.Serialize(
            new SerializationProbe(
                "value",
                new Dictionary<string, bool> { ["dynamic_key"] = true }));

        Assert.Equal(
            "{\"Name\":\"value\",\"DynamicData\":{\"dynamic_key\":true}}",
            json);
    }

    [Fact]
    public void Remove_obsolete_agent_api_migration_helpers()
    {
        Assembly assembly = typeof(AgentsController).Assembly;

        Assert.Null(assembly.GetType(
            "EU.Core.Api.Agent.Controllers.ApiProblemResults",
            throwOnError: false));
        Assert.Null(assembly.GetType(
            "EU.Core.Api.Agent.Configuration.AgentApiResponseMetadataConvention",
            throwOnError: false));
    }

    private static void AssertPolicy<TController>(string expectedPolicy)
    {
        AuthorizeAttribute attribute = Assert.Single(
            typeof(TController).GetCustomAttributes<AuthorizeAttribute>(inherit: false));
        Assert.Equal(expectedPolicy, attribute.Policy);
    }

    private sealed record SerializationProbe(
        string Name,
        IReadOnlyDictionary<string, bool> DynamicData);

    private sealed class ResponseProbeController : EU.Core.Api.Agent.Base.ControllerBase
    {
        public ServiceResult<T> Succeeded<T>(T data) => Success(data);
    }
}
