#nullable enable

using System.Reflection;
using System.Text;
using EU.Core.Agent.Application.Agents;
using EU.Core.Agent.Application.MainAgent;
using EU.Core.Agent.Application.Skills;
using EU.Core.Api.Agent.Controllers;
using EU.Core.IServices;
using EU.Core.Model;
using EU.Core.Model.Models;
using EU.Core.Model.ViewModels.Extend;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace EU.Core.Tests.Service_Test;

public sealed class AgAgentAndSkillApiResponse_Should
{
    [Fact]
    public async Task Return_fixed_service_error_for_invalid_agent_status()
    {
        var controller = WithHttpContext(new AgentsController(
            new PublicModelProfileCatalog([]),
            Proxy<IAgAgentDefinitionServices>((_, _) => throw new InvalidOperationException())));

        IActionResult action = await controller.List(null, "Unknown", CancellationToken.None);

        AssertServiceError(action, StatusCodes.Status200OK, 600001, "REQUEST_INVALID");
    }

    [Fact]
    public async Task Return_created_service_result_for_agent_creation()
    {
        Guid id = Guid.NewGuid();
        var detail = new AgAgentDefinitionDetailDto { Id = id, Code = "main", Name = "Main" };
        IAgAgentDefinitionServices service = Proxy<IAgAgentDefinitionServices>((method, _) =>
            method.Name switch
            {
                nameof(IAgAgentDefinitionServices.CreateAsync) =>
                    Task.FromResult(ServiceResult<Guid>.OprateSuccess(id)),
                nameof(IAgAgentDefinitionServices.QueryAgent) =>
                    Task.FromResult<AgAgentDefinitionDetailDto?>(detail),
                _ => throw new InvalidOperationException(method.Name)
            });
        var controller = WithHttpContext(new AgentsController(
            new PublicModelProfileCatalog([]), service));

        IActionResult action = await controller.Create(
            new CreateAgentRequest("main", "Main", string.Empty),
            CancellationToken.None);

        ServiceResult<AgAgentDefinitionDetailDto> body =
            AssertServiceSuccess<AgAgentDefinitionDetailDto>(action, StatusCodes.Status201Created);
        Assert.Same(detail, body.Data);
        Assert.Equal($"/api/agents/{id}", controller.Response.Headers.Location);
    }

    [Fact]
    public async Task Return_fixed_service_errors_for_agent_conflict_and_missing_resources()
    {
        IAgAgentDefinitionServices agentService = Proxy<IAgAgentDefinitionServices>((method, _) =>
            method.Name switch
            {
                nameof(IAgAgentDefinitionServices.CreateAsync) =>
                    Task.FromResult(ServiceResult<Guid>.OprateFailed("The Agent code already exists.")),
                nameof(IAgAgentDefinitionServices.QueryAgent) =>
                    Task.FromResult<AgAgentDefinitionDetailDto?>(null),
                _ => throw new InvalidOperationException(method.Name)
            });
        var agentsController = WithHttpContext(new AgentsController(
            new PublicModelProfileCatalog([]), agentService));

        AssertServiceFailure<Guid>(
            await agentsController.Create(
                new CreateAgentRequest("duplicate", "Duplicate", string.Empty),
                CancellationToken.None),
            "The Agent code already exists.");
        AssertServiceFailure<AgAgentDefinitionDetailDto>(
            await agentsController.Get(Guid.NewGuid(), CancellationToken.None),
            "The Agent was not found.");

        IAgSkillDefinitionServices skillService = Proxy<IAgSkillDefinitionServices>((method, _) =>
            method.Name == nameof(IAgSkillDefinitionServices.GetAsync)
                ? Task.FromResult<SkillDefinition?>(null)
                : throw new InvalidOperationException(method.Name));
        var skillsController = WithHttpContext(new SkillsController(
            skillService,
            new StubAgentDefinitionCatalog()));

        AssertServiceFailure<SkillDefinitionDetailResponse>(
            await skillsController.Get(Guid.NewGuid(), CancellationToken.None),
            "The Skill was not found.");
    }

    [Fact]
    public async Task Keep_agent_export_as_file_response()
    {
        IAgAgentDefinitionServices service = Proxy<IAgAgentDefinitionServices>((method, _) =>
            method.Name == nameof(IAgAgentDefinitionServices.ExportAsync)
                ? Task.FromResult(ServiceResult<string>.OprateSuccess("{}", "操作成功"))
                : throw new InvalidOperationException(method.Name));
        var controller = WithHttpContext(new AgentsController(
            new PublicModelProfileCatalog([]), service));

        IActionResult action = await controller.Export(Guid.NewGuid(), CancellationToken.None);

        FileContentResult file = Assert.IsType<FileContentResult>(action);
        Assert.Equal("application/json", file.ContentType);
        Assert.Equal("agent-package.json", file.FileDownloadName);
        Assert.Equal("{}", Encoding.UTF8.GetString(file.FileContents));
    }

    [Fact]
    public async Task Wrap_agent_queries_mutations_and_import()
    {
        AgentDefinition definition = CreateAgent();
        var detail = new AgAgentDefinitionDetailDto
        {
            Id = definition.Id,
            Code = definition.Code,
            Name = definition.Name
        };
        IAgAgentDefinitionServices service = Proxy<IAgAgentDefinitionServices>((method, _) =>
            method.Name switch
            {
                nameof(IAgAgentDefinitionServices.QueryAgentList) =>
                    Task.FromResult(new List<AgAgentDefinitionDto>()),
                nameof(IAgAgentDefinitionServices.QueryAgent) =>
                    Task.FromResult<AgAgentDefinitionDetailDto?>(detail),
                nameof(IAgAgentDefinitionServices.SaveDraftAsync) or
                nameof(IAgAgentDefinitionServices.PublishAsync) or
                nameof(IAgAgentDefinitionServices.SetRuntimeStatusAsync) or
                nameof(IAgAgentDefinitionServices.ImportAsync) =>
                    Task.FromResult(ServiceResult<AgentDefinition>.OprateSuccess(definition)),
                _ => throw new InvalidOperationException(method.Name)
            });
        var controller = WithHttpContext(new AgentsController(
            new PublicModelProfileCatalog([]), service));

        AssertServiceSuccess<AgentListItem[]>(
            await controller.List(null, null, CancellationToken.None),
            StatusCodes.Status200OK);
        AssertServiceSuccess<AgAgentDefinitionDetailDto>(
            await controller.Get(definition.Id, CancellationToken.None),
            StatusCodes.Status200OK);
        AssertServiceSuccess<AgentDefinition>(
            await controller.SaveDraft(
                definition.Id,
                new SaveAgentDraftRequest(
                    0, "Main", string.Empty, "instructions", string.Empty,
                    AgentOutputMode.Text, null, [], [], []),
                CancellationToken.None),
            StatusCodes.Status200OK);
        AssertServiceSuccess<AgentDefinition>(
            await controller.Publish(definition.Id, new ExpectedRevisionRequest(0), CancellationToken.None),
            StatusCodes.Status200OK);
        AssertServiceSuccess<AgentDefinition>(
            await controller.SetStatus(
                definition.Id,
                new SetAgentStatusRequest(0, AgentRuntimeStatus.Disabled),
                CancellationToken.None),
            StatusCodes.Status200OK);

        controller.Request.ContentType = "application/json";
        controller.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("{}"));
        AssertServiceSuccess<AgentDefinition>(
            await controller.Import(CancellationToken.None),
            StatusCodes.Status201Created);
    }

    [Fact]
    public async Task Keep_skill_file_content_as_text_response()
    {
        IAgSkillDefinitionServices lifecycle = Proxy<IAgSkillDefinitionServices>((method, _) =>
            method.Name == nameof(IAgSkillDefinitionServices.ReadFileAsync)
                ? Task.FromResult(ServiceResult<string>.QuerySuccess("# Skill"))
                : throw new InvalidOperationException(method.Name));
        var controller = WithHttpContext(new SkillsController(
            lifecycle,
            new StubAgentDefinitionCatalog()));

        IActionResult action = await controller.ReadFile(
            Guid.NewGuid(), "SKILL.md", CancellationToken.None);

        ContentResult content = Assert.IsType<ContentResult>(action);
        Assert.Equal("# Skill", content.Content);
        Assert.StartsWith("text/plain", content.ContentType, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Return_created_service_result_for_skill_creation()
    {
        SkillDefinition skill = CreateSkill();
        IAgSkillDefinitionServices lifecycle = Proxy<IAgSkillDefinitionServices>((method, _) =>
            method.Name == nameof(IAgSkillDefinitionServices.CreateAsync)
                ? Task.FromResult(ServiceResult<SkillDefinition>.OprateSuccess(skill))
                : throw new InvalidOperationException(method.Name));
        var controller = WithHttpContext(new SkillsController(
            lifecycle,
            new StubAgentDefinitionCatalog()));

        IActionResult action = await controller.Create(
            new CreateSkillRequest("test", "Test", string.Empty, "general"),
            CancellationToken.None);

        ServiceResult<SkillDefinition> body =
            AssertServiceSuccess<SkillDefinition>(action, StatusCodes.Status200OK);
        Assert.Same(skill, body.Data);
        Assert.Equal($"/api/skills/{skill.Id}", controller.Response.Headers.Location);
    }

    [Fact]
    public async Task Return_request_error_for_invalid_skill_status_filter()
    {
        var controller = WithHttpContext(new SkillsController(
            Proxy<IAgSkillDefinitionServices>((_, _) => throw new InvalidOperationException()),
            new StubAgentDefinitionCatalog()));

        IActionResult action = await controller.List(
            null,
            null,
            "Unknown",
            CancellationToken.None);

        AssertServiceFailure<IReadOnlyList<SkillListItem>>(
            action,
            "Skill status must be Active or Archived.");
    }

    [Fact]
    public async Task Wrap_skill_queries_and_mutations()
    {
        SkillDefinition skill = CreateSkill();
        IReadOnlyList<SkillListItem> list =
        [
            new(skill.Id, skill.Code, skill.Name, skill.Description, skill.Category, 0, null, null)
        ];
        IReadOnlyList<SkillFileEntry> files = [new("SKILL.md", 10)];
        IAgSkillDefinitionServices lifecycle = Proxy<IAgSkillDefinitionServices>((method, _) =>
            method.Name switch
            {
                nameof(IAgSkillDefinitionServices.ListAsync) => Task.FromResult(list),
                nameof(IAgSkillDefinitionServices.GetAsync) => Task.FromResult<SkillDefinition?>(skill),
                nameof(IAgSkillDefinitionServices.ListFilesAsync) =>
                    Task.FromResult(ServiceResult<IReadOnlyList<SkillFileEntry>>.QuerySuccess(files)),
                nameof(IAgSkillDefinitionServices.UpdateAsync) or
                nameof(IAgSkillDefinitionServices.SaveFileAsync) or
                nameof(IAgSkillDefinitionServices.DeleteFileAsync) or
                nameof(IAgSkillDefinitionServices.PublishAsync) or
                nameof(IAgSkillDefinitionServices.SetArchivedAsync) =>
                    Task.FromResult(ServiceResult<SkillDefinition>.OprateSuccess(skill)),
                _ => throw new InvalidOperationException(method.Name)
            });
        var controller = WithHttpContext(new SkillsController(
            lifecycle,
            new StubAgentDefinitionCatalog()));

        AssertServiceSuccess<IReadOnlyList<SkillListItem>>(
            await controller.List(null, null, null, CancellationToken.None),
            StatusCodes.Status200OK);
        AssertServiceSuccess<SkillDefinitionDetailResponse>(
            await controller.Get(skill.Id, CancellationToken.None),
            StatusCodes.Status200OK);
        AssertServiceSuccess<SkillDefinition>(
            await controller.Update(
                skill.Id,
                new UpdateSkillRequest(0, "Test", string.Empty, "general"),
                CancellationToken.None),
            StatusCodes.Status200OK);
        AssertServiceSuccess<IReadOnlyList<SkillFileEntry>>(
            await controller.ListFiles(skill.Id, CancellationToken.None),
            StatusCodes.Status200OK);
        AssertServiceSuccess<SkillDefinition>(
            await controller.SaveFile(
                skill.Id,
                new SaveSkillFileRequest(0, "SKILL.md", "# Skill"),
                CancellationToken.None),
            StatusCodes.Status200OK);
        AssertServiceSuccess<SkillDefinition>(
            await controller.DeleteFile(
                skill.Id,
                new DeleteSkillFileRequest(0, "references/test.md"),
                CancellationToken.None),
            StatusCodes.Status200OK);
        AssertServiceSuccess<SkillDefinition>(
            await controller.Publish(
                skill.Id,
                new PublishSkillRequest(0, "1.0.0"),
                CancellationToken.None),
            StatusCodes.Status200OK);
        AssertServiceSuccess<SkillDefinition>(
            await controller.SetArchived(
                skill.Id,
                new SetSkillArchiveRequest(0, true),
                CancellationToken.None),
            StatusCodes.Status200OK);
    }

    [Fact]
    public async Task Return_fixed_service_error_when_main_agent_is_not_configured()
    {
        var assignments = new MainAgentAssignmentService(
            new StubAgentDefinitionCatalog(),
            new EmptyMainAgentAssignmentRepository());
        var controller = WithHttpContext(new MainAgentController(assignments));

        IActionResult action = await controller.Get(CancellationToken.None);

        AssertServiceError(action, StatusCodes.Status404NotFound, 610004, "MAIN_AGENT_NOT_CONFIGURED");
    }

    [Fact]
    public async Task Return_fixed_service_error_when_setting_an_unknown_main_agent()
    {
        var assignments = new MainAgentAssignmentService(
            new StubAgentDefinitionCatalog(),
            new EmptyMainAgentAssignmentRepository());
        var controller = WithHttpContext(new MainAgentController(assignments));

        IActionResult action = await controller.Set(
            new SetMainAgentRequest(Guid.NewGuid(), null),
            CancellationToken.None);

        AssertServiceError(
            action,
            StatusCodes.Status404NotFound,
            610018,
            MainAgentErrorCodes.AgentNotFound);
    }

    [Fact]
    public async Task Return_service_result_for_published_skill_versions()
    {
        IReadOnlyList<PublishedSkillReference> values =
        [
            new(Guid.NewGuid(), Guid.NewGuid(), "test", "Test", "1.0.0", "sha")
        ];
        IPublishedSkillVersionCatalog catalog = Proxy<IPublishedSkillVersionCatalog>((method, _) =>
            method.Name == nameof(IPublishedSkillVersionCatalog.ListAsync)
                ? Task.FromResult(values)
                : throw new InvalidOperationException(method.Name));
        var controller = WithHttpContext(new SkillVersionsController(catalog));

        IActionResult action = await controller.List(CancellationToken.None);

        ServiceResult<IReadOnlyList<PublishedSkillReference>> body =
            AssertServiceSuccess<IReadOnlyList<PublishedSkillReference>>(
                action,
                StatusCodes.Status200OK);
        Assert.Same(values, body.Data);
    }

    private static TController WithHttpContext<TController>(TController controller)
        where TController : ControllerBase
    {
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { TraceIdentifier = "trace-contract" }
        };
        return controller;
    }

    private static ServiceResult<T> AssertServiceSuccess<T>(IActionResult action, int httpStatus)
    {
        JsonResult json = Assert.IsType<JsonResult>(action);
        Assert.Equal(httpStatus, json.StatusCode);
        Assert.Null(json.SerializerSettings);
        ServiceResult<T> body = Assert.IsType<ServiceResult<T>>(json.Value);
        Assert.Equal(200, body.Status);
        Assert.True(body.Success);
        return body;
    }

    private static ServiceResult<T> AssertServiceFailure<T>(IActionResult action, string message)
    {
        JsonResult json = Assert.IsType<JsonResult>(action);
        Assert.Equal(StatusCodes.Status200OK, json.StatusCode);
        ServiceResult<T> body = Assert.IsType<ServiceResult<T>>(json.Value);
        Assert.False(body.Success);
        Assert.Equal(500, body.Status);
        Assert.Equal(message, body.Message);
        return body;
    }

    private static void AssertServiceError(
        IActionResult action,
        int httpStatus,
        int businessStatus,
        string errorCode)
    {
        JsonResult json = Assert.IsType<JsonResult>(action);
        Assert.Equal(httpStatus, json.StatusCode);
        Assert.Null(json.SerializerSettings);
        ServiceResult<AgentApiErrorData> body =
            Assert.IsType<ServiceResult<AgentApiErrorData>>(json.Value);
        Assert.False(body.Success);
        Assert.Equal(businessStatus, body.Status);
        Assert.Equal(errorCode, body.Data.ErrorCode);
        Assert.Equal("trace-contract", body.Data.TraceId);
    }

    private static T Proxy<T>(Func<MethodInfo, object?[]?, object?> handler)
        where T : class
    {
        T value = DispatchProxy.Create<T, DelegateProxy>();
        ((DelegateProxy)(object)value).Handler = handler;
        return value;
    }

    private static SkillDefinition CreateSkill() => new(
        Guid.NewGuid(),
        "test",
        "Test",
        string.Empty,
        "general",
        0,
        []);

    private static AgentDefinition CreateAgent()
    {
        var draft = new AgentVersion(
            Guid.NewGuid(),
            "draft",
            true,
            "instructions",
            "model",
            AgentOutputMode.Text,
            null,
            null,
            null);
        return new AgentDefinition(
            Guid.NewGuid(),
            "main",
            "Main",
            string.Empty,
            AgentRuntimeStatus.Enabled,
            0,
            draft,
            []);
    }

    private class DelegateProxy : DispatchProxy
    {
        public Func<MethodInfo, object?[]?, object?> Handler { get; set; } = null!;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            Handler(targetMethod ?? throw new InvalidOperationException(), args);
    }

    private sealed class EmptyMainAgentAssignmentRepository : IMainAgentAssignmentRepository
    {
        public Task<MainAgentAssignment?> GetAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<MainAgentAssignment?>(null);

        public Task<bool> TryReplaceAsync(
            MainAgentAssignment value,
            long? expectedLogicalRevision,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
