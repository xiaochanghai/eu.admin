#nullable enable

using System.Reflection;
using EU.Core.Api.Agent.Configuration;
using EU.Core.Api.Agent.Controllers;
using EU.Core.IServices;
using EU.Core.Model;
using EU.Core.IServices.MainAgent;
using Microsoft.Extensions.Options;
using Xunit;

namespace EU.Core.Tests.Service_Test;

/// <summary>验证平台能力接口从标准模型配置服务读取可用模型。</summary>
public sealed class PlatformControllerModelProfiles_Should
{
    [Fact]
    public async Task Return_available_profiles_from_model_config_service()
    {
        IAgModelConfigServices modelConfigs = Proxy<IAgModelConfigServices>((method, _) =>
            method.Name == nameof(IAgModelConfigServices.ListAvailableProfilesAsync)
                ? Task.FromResult<IReadOnlyList<string>>(["enabled-model"])
                : throw new InvalidOperationException(method.Name));
        IMainAgentAssignmentService mainAgentAssignments = Proxy<IMainAgentAssignmentService>((method, _) =>
            method.Name == nameof(IMainAgentAssignmentService.GetAsync)
                ? Task.FromResult(ServiceResult<MainAgentAssignment>.QuerySuccess())
                : throw new InvalidOperationException(method.Name));
        var controller = new PlatformController(
            Options.Create(new AgentPlatformOptions()),
            Options.Create(new AgentEvaluationOptions()),
            modelConfigs,
            mainAgentAssignments);

        ServiceResult<PlatformCapabilitiesResponse> result = await controller.Capabilities(CancellationToken.None);

        Assert.Equal(["enabled-model"], result.Data?.ModelProfileIds);
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
