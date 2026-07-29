using EU.Core.Agent.Api.Controllers;
using EU.Core.Agent.Application.Orchestration;
using EU.Core.Agent.Application.Runtime;
using EU.Core.Agent.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace EU.Core.Agent.Tests.Api;

public sealed class OrchestrationDetailsApiTests
{
    [Fact]
    public async Task Details_and_output_are_loaded_from_persisted_repository_content()
    {
        Guid orchestrationId = Guid.NewGuid();
        Guid runId = Guid.NewGuid();
        var repository = new InMemoryOrchestrationRunRepository();
        await repository.SaveAsync(new OrchestrationRunRecord(
            runId,
            orchestrationId,
            Guid.NewGuid(),
            "supplier-flow",
            OrchestrationRunStatus.Completed,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            "input-sha",
            "",
            []));
        var details = new OrchestrationRunDetails(
            runId,
            orchestrationId,
            "查询供应商",
            "正在打开供应商列表",
            [
                new OrchestrationNodeAttemptRecord(
                    "query",
                    1,
                    Guid.NewGuid(),
                    "查询供应商",
                    "input-sha",
                    "正在打开供应商列表",
                    "output-sha",
                    OrchestrationNodeRunStatus.Completed,
                    DateTimeOffset.UtcNow,
                    DateTimeOffset.UtcNow,
                    "",
                    [
                        new OrchestrationToolCallRecord(
                            Guid.NewGuid(),
                            Guid.NewGuid(),
                            Guid.NewGuid(),
                            "get_supplier",
                            AgentRunEventKind.ToolSucceeded,
                            """{"page":1}""",
                            """{"type":"module","id":"1","moduleCode":"supplier"}""",
                            "result-sha",
                            50,
                            DateTimeOffset.UtcNow,
                            DateTimeOffset.UtcNow,
                            "")
                    ])
            ]);
        await repository.SaveDetailsAsync(details);

        var runtime = new OrchestrationRuntimeService(null!, repository, null!, null!);
        var controller = new OrchestrationsController(null!, runtime)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        OkObjectResult detailResult = Assert.IsType<OkObjectResult>(
            await controller.Details(orchestrationId, runId, CancellationToken.None));
        OrchestrationRunDetails restored =
            Assert.IsType<OrchestrationRunDetails>(detailResult.Value);
        Assert.Equal(details.Output, restored.Output);
        Assert.Equal(
            details.Attempts[0].ToolCalls[0].ResultContent,
            restored.Attempts[0].ToolCalls[0].ResultContent);

        OkObjectResult outputResult = Assert.IsType<OkObjectResult>(
            await controller.Output(orchestrationId, runId, CancellationToken.None));
        using System.Text.Json.JsonDocument outputDocument =
            System.Text.Json.JsonDocument.Parse(
                System.Text.Json.JsonSerializer.Serialize(outputResult.Value));
        Assert.Equal(
            "正在打开供应商列表",
            outputDocument.RootElement.GetProperty("output").GetString());
    }

    [Fact]
    public async Task Details_reject_a_run_owned_by_another_orchestration()
    {
        Guid ownerId = Guid.NewGuid();
        Guid runId = Guid.NewGuid();
        var repository = new InMemoryOrchestrationRunRepository();
        await repository.SaveAsync(new OrchestrationRunRecord(
            runId,
            ownerId,
            Guid.NewGuid(),
            "owner",
            OrchestrationRunStatus.Completed,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            "",
            "",
            []));

        var runtime = new OrchestrationRuntimeService(null!, repository, null!, null!);
        var controller = new OrchestrationsController(null!, runtime)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        ObjectResult result = Assert.IsType<ObjectResult>(
            await controller.Details(Guid.NewGuid(), runId, CancellationToken.None));
        Assert.Equal(StatusCodes.Status404NotFound, result.StatusCode);
    }
}
