using EU.Core.Agent.Runtime;
using Microsoft.Extensions.Logging;
using Xunit;

namespace EU.Core.Tests.Service_Test;

public sealed class AgAgentRuntimeObservability_Should
{
    [Fact]
    public void Require_structured_logger_for_model_runtime()
    {
        Type loggerType = typeof(ILogger<MicrosoftAgentRuntimeEngine>);
        System.Reflection.ConstructorInfo constructor = Assert.Single(
            typeof(MicrosoftAgentRuntimeEngine).GetConstructors());

        Assert.Contains(
            constructor.GetParameters(),
            parameter => parameter.ParameterType == loggerType);
    }
}
