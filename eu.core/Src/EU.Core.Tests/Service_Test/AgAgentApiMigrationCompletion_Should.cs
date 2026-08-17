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
    public void Configure_mvc_json_as_pascal_case_without_renaming_dictionary_keys()
    {
        var options = new JsonOptions();

        AgentJsonSerialization.ConfigureMvc(options);
        string json = JsonSerializer.Serialize(
            new SerializationProbe(
                "value",
                new Dictionary<string, bool> { ["dynamic_key"] = true }),
            options.JsonSerializerOptions);

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
                    Assert.True(
                        AgentApiResponseMetadataConvention.ServiceActionDataTypes.TryGetValue(
                            key,
                            out Type? dataType),
                        $"Missing response type for {key}.");
                    Type envelopeType = typeof(ServiceResult<>).MakeGenericType(dataType!);
                    Assert.Contains(responses, response =>
                        response.Type == envelopeType
                        && response.StatusCode is >= 200 and < 300);
                    Assert.NotEqual(typeof(object), dataType);
                }

                if (!string.Equals(key, "MetricsController.Get", StringComparison.Ordinal))
                {
                    Assert.Contains(
                        action.Filters.OfType<ProducesDefaultResponseTypeAttribute>(),
                        response => response.Type == typeof(ServiceResult<AgentApiErrorData>));
                }
            }
        }

        Assert.Equal(
            typeof(AgAgentDefinitionDetailDto),
            AgentApiResponseMetadataConvention.ServiceActionDataTypes["AgentsController.Get"]);
        Assert.Equal(
            typeof(ChatConversationDetailResponse),
            AgentApiResponseMetadataConvention.ServiceActionDataTypes["ChatRunsController.GetConversation"]);
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
