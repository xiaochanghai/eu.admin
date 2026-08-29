#nullable enable

using System.Reflection;
using System.Text.Json;
using EU.Core.Api.Agent.Configuration;
using EU.Core.Api.Agent.Controllers;
using EU.Core.Model;
using EU.Core.Model.Models;
using EU.Core.Model.ViewModels.Extend;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Routing;
using Xunit;

namespace EU.Core.Tests.Service_Test;

public sealed class AgAgentApiMigrationCompletion_Should
{
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
    public void Publish_service_envelopes_and_special_protocols_to_api_explorer()
    {
        ApplicationModel application = BuildApplicationModel();

        new AgentApiResponseMetadataConvention().Apply(application);

        string[] expectedSpecial =
        [
            "AgentRunsController.Run",
            "AgentsController.Export",
            "ChatRunsController.Start",
            "MetricsController.Get",
            "SkillsController.ReadFile"
        ];
        Assert.Equal(
            expectedSpecial,
            AgentApiResponseMetadataConvention.SpecialActionKeys.Order().ToArray());

        string[] expectedExplicitServiceResponses =
        [
            "AgentsController.Create",
            "AgentsController.Import",
            "ChatRunsController.Cancel",
            "EvaluationSuitesController.Create",
            "OrchestrationsController.Cancel",
            "OrchestrationsController.Create",
            "OrchestrationsController.Start",
            "SkillsController.Create"
        ];
        Assert.Equal(
            expectedExplicitServiceResponses,
            AgentApiResponseMetadataConvention.ServiceActionDataTypes.Keys
                .Order()
                .ToArray());

        foreach (ControllerModel controller in application.Controllers)
        {
            foreach (ActionModel action in controller.Actions)
            {
                string key = $"{controller.ControllerType.Name}.{action.ActionMethod.Name}";
                ProducesResponseTypeAttribute[] responses = action.Filters
                    .OfType<ProducesResponseTypeAttribute>()
                    .ToArray();
                if (expectedSpecial.Contains(key, StringComparer.Ordinal))
                {
                    Assert.Contains(responses, response =>
                        response.StatusCode == StatusCodes.Status200OK
                        && response.Type != typeof(ServiceResult<object>));
                    Assert.DoesNotContain(
                        key,
                        AgentApiResponseMetadataConvention.ServiceActionDataTypes.Keys);
                }
                else
                {
                    ProducesResponseTypeAttribute serviceResponse = Assert.Single(
                        responses,
                        response =>
                            response.Type?.IsGenericType == true
                            && response.Type.GetGenericTypeDefinition() == typeof(ServiceResult<>)
                            && response.StatusCode is >= 200 and < 300);
                    Type dataType = serviceResponse.Type!.GetGenericArguments()[0];
                    Assert.NotEqual(typeof(object), dataType);

                    if (AgentApiResponseMetadataConvention.ServiceActionDataTypes.TryGetValue(
                            key,
                            out Type? configuredDataType))
                    {
                        Assert.Equal(configuredDataType, dataType);
                    }
                }

                if (!string.Equals(key, "MetricsController.Get", StringComparison.Ordinal))
                {
                    Assert.Contains(
                        action.Filters.OfType<ProducesDefaultResponseTypeAttribute>(),
                        response => response.Type == typeof(ServiceResult<AgentApiErrorData>));
                }
            }
        }

        Assert.DoesNotContain(
            "AgentsController.Get",
            AgentApiResponseMetadataConvention.ServiceActionDataTypes.Keys);
        Assert.DoesNotContain(
            "ChatRunsController.GetConversation",
            AgentApiResponseMetadataConvention.ServiceActionDataTypes.Keys);
        Assert.DoesNotContain(
            "OrchestrationsController.List",
            AgentApiResponseMetadataConvention.ServiceActionDataTypes.Keys);
    }

    [Fact]
    public void Remove_the_controller_problem_result_migration_helper()
    {
        Assembly assembly = typeof(AgentsController).Assembly;

        Assert.Null(assembly.GetType(
            "EU.Core.Api.Agent.Controllers.ApiProblemResults",
            throwOnError: false));
    }

    private static ApplicationModel BuildApplicationModel()
    {
        var application = new ApplicationModel();
        Type[] controllerTypes = typeof(AgentsController).Assembly.GetTypes()
            .Where(type =>
                !type.IsAbstract
                && typeof(ControllerBase).IsAssignableFrom(type)
                && string.Equals(
                    type.Namespace,
                    "EU.Core.Api.Agent.Controllers",
                    StringComparison.Ordinal))
            .ToArray();
        foreach (Type controllerType in controllerTypes)
        {
            var controller = new ControllerModel(
                controllerType.GetTypeInfo(),
                controllerType.GetCustomAttributes(inherit: true).ToArray());
            MethodInfo[] actions = controllerType.GetMethods(
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .Where(method => method.GetCustomAttributes(inherit: true)
                    .OfType<HttpMethodAttribute>()
                    .Any())
                .ToArray();
            foreach (MethodInfo method in actions)
            {
                controller.Actions.Add(new ActionModel(
                    method,
                    method.GetCustomAttributes(inherit: true).ToArray()));
            }
            application.Controllers.Add(controller);
        }
        return application;
    }

    private sealed record SerializationProbe(
        string Name,
        IReadOnlyDictionary<string, bool> DynamicData);
}
