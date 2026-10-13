#nullable enable

using System.Reflection;
using EU.Core.Api.Agent.Controllers;
using EU.Core.IServices;
using EU.Core.Model;
using EU.Core.Model.Edit;
using EU.Core.Model.Entity;
using EU.Core.Model.Insert;
using EU.Core.Model.Models;
using EU.Core.Model.ViewModels.Extend;
using Xunit;

namespace EU.Core.Tests.Service_Test;

/// <summary>验证 Agent 草稿只能引用可用模型配置。</summary>
public sealed class AgentsControllerModelProfileValidation_Should
{
    [Fact]
    public async Task Reject_model_profile_with_different_casing()
    {
        IAgModelConfigServices modelConfigs = Proxy<IAgModelConfigServices>((method, arguments) =>
        {
            if (method.Name == nameof(IAgModelConfigServices.ListAvailableProfilesAsync))
            {
                return Task.FromResult<IReadOnlyList<string>>(["available-model"]);
            }

            throw new InvalidOperationException(method.Name);
        });
        IAgAgentDefinitionServices agentDefinitions = Proxy<IAgAgentDefinitionServices>((method, _) =>
            throw new InvalidOperationException(method.Name));
        var controller = new AgentsController(agentDefinitions, modelConfigs);

        Exception exception = await Assert.ThrowsAsync<Exception>(() => controller.SaveDraft(
            Guid.NewGuid(),
            new SaveAgentDraftRequest(
                0, "Main", string.Empty, "instructions", "AVAILABLE-MODEL",
                AgentOutputMode.Text, null, [], [], []),
            CancellationToken.None));

        Assert.Equal("The selected model profile is not available.", exception.Message);
    }

    private static T Proxy<T>(Func<MethodInfo, object?[]?, object?> handler) where T : class
    {
        var proxy = DispatchProxy.Create<T, DelegateProxy>();
        ((DelegateProxy)(object)proxy).Handler = handler;
        return proxy;
    }

    private class DelegateProxy : DispatchProxy
    {
        public Func<MethodInfo, object?[]?, object?> Handler { get; set; } = null!;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            Handler(targetMethod ?? throw new InvalidOperationException(), args);
    }
}
