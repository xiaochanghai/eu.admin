using System.Reflection;
using EU.Core.Agent.Api.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Xunit;

namespace EU.Core.Agent.Tests.Api;

public sealed class ControllerArchitectureTests
{
    [Fact]
    public void Public_api_is_exposed_by_attribute_routed_api_controllers()
    {
        var expectedRoutes = new Dictionary<Type, string>
        {
            [typeof(AgentsController)] = "api/agents",
            [typeof(SkillsController)] = "api/skills",
            [typeof(SkillVersionsController)] = "api/skill-versions",
            [typeof(PlatformController)] = "api/platform",
            [typeof(McpServersController)] = "api/mcp/servers",
            [typeof(McpToolVersionsController)] = "api/mcp/tool-versions",
            [typeof(AgentRunsController)] = "api/agents/{agentId:guid}"
        };

        foreach ((Type controllerType, string expectedRoute) in expectedRoutes)
        {
            Assert.True(typeof(ControllerBase).IsAssignableFrom(controllerType));
            Assert.NotNull(controllerType.GetCustomAttribute<ApiControllerAttribute>());
            Assert.Equal(
                expectedRoute,
                controllerType.GetCustomAttribute<RouteAttribute>()?.Template);

            MethodInfo[] actions = controllerType.GetMethods(
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .Where(method => !method.IsSpecialName)
                .ToArray();
            Assert.NotEmpty(actions);
            Assert.All(
                actions,
                action => Assert.Contains(
                    action.GetCustomAttributes(),
                    attribute => attribute is HttpMethodAttribute));
        }
    }

    [Fact]
    public void Legacy_minimal_api_endpoint_types_are_removed()
    {
        Assembly api = typeof(AgentsController).Assembly;

        Assert.Null(api.GetType("EU.Core.Agent.Api.Agents.AgentApiEndpoints"));
        Assert.Null(api.GetType("EU.Core.Agent.Api.Skills.SkillApiEndpoints"));
    }
}
