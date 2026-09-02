#nullable enable

using System.Reflection;
using System.Text;
using EU.Core.IServices.Agents;
using EU.Core.IServices.MainAgent;
using EU.Core.IServices.Skills;
using EU.Core.Api.Agent.Controllers;
using EU.Core.Api.Agent.Errors;
using EU.Core.IServices;
using EU.Core.Model;
using EU.Core.Model.Models;
using EU.Core.Model.ViewModels.Extend;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;
using EU.Core.Services;

namespace EU.Core.Tests.Service_Test;

public sealed class AgAgentAndSkillApiResponse_Should
{
    [Fact]
    public async Task Return_fixed_service_error_for_invalid_agent_status()
    {
        var controller = WithHttpContext(new AgentsController(
            new PublicModelProfileCatalog([]),
            Proxy<IAgAgentDefinitionServices>((_, _) => throw new InvalidOperationException())));

        Exception exception = await Assert.ThrowsAsync<Exception>(
            async () => await controller.List(null, "Unknown", CancellationToken.None));

        Assert.Equal("The status filter is invalid.", exception.Message);
    }

    [Fact]
    public async Task Reject_unavailable_agent_model_profile_through_the_standard_error_pipeline()
    {
        var controller = WithHttpContext(new AgentsController(
            new PublicModelProfileCatalog([]),
            Proxy<IAgAgentDefinitionServices>((_, _) => throw new InvalidOperationException())));

        Exception exception = await Assert.ThrowsAsync<Exception>(
            async () => await controller.SaveDraft(
                Guid.NewGuid(),
                new SaveAgentDraftRequest(
                    0, "Main", string.Empty, "instructions", "missing-profile",
                    AgentOutputMode.Text, null, [], [], []),
                CancellationToken.None));

        Assert.Equal("The selected model profile is not available.", exception.Message);
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

        ServiceResult<AgAgentDefinitionDetailDto> body = await controller.Create(
            new CreateAgentRequest("main", "Main", string.Empty),
            CancellationToken.None);

        AssertServiceSuccess(body);
        Assert.Equal(StatusCodes.Status201Created, controller.Response.StatusCode);
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

        ServiceResult<AgAgentDefinitionDetailDto> failedCreate =
            await agentsController.Create(
                new CreateAgentRequest("duplicate", "Duplicate", string.Empty),
                CancellationToken.None);
        Assert.False(failedCreate.Success);
        Assert.Equal(500, failedCreate.Status);
        Assert.Equal("The Agent code already exists.", failedCreate.Message);
        Exception missingAgent = await Assert.ThrowsAsync<Exception>(
            async () => await agentsController.Get(Guid.NewGuid(), CancellationToken.None));
        Assert.Equal("The Agent was not found.", missingAgent.Message);

        IAgSkillDefinitionServices skillService = Proxy<IAgSkillDefinitionServices>((method, _) =>
            method.Name == nameof(IAgSkillDefinitionServices.GetAsync)
                ? Task.FromResult<SkillDefinition?>(null)
                : throw new InvalidOperationException(method.Name));
        var skillsController = WithHttpContext(new SkillsController(
            skillService,
            new StubAgentDefinitionCatalog()));

        AssertServiceError(
            await skillsController.Get(Guid.NewGuid(), CancellationToken.None),
            StatusCodes.Status404NotFound,
            SkillServiceStatusCodes.NotFound,
            SkillErrorCodes.NotFound);
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
    public async Task Reject_agent_import_with_an_unsupported_content_type_through_the_standard_error_pipeline()
    {
        var controller = WithHttpContext(new AgentsController(
            new PublicModelProfileCatalog([]),
            Proxy<IAgAgentDefinitionServices>((_, _) => throw new InvalidOperationException())));
        controller.Request.ContentType = "text/plain";

        Exception exception = await Assert.ThrowsAsync<Exception>(
            async () => await controller.Import(CancellationToken.None));

        Assert.Equal("The Agent package must use a JSON content type.", exception.Message);
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

        AssertServiceSuccess(
            await controller.List(null, null, CancellationToken.None));
        AssertServiceSuccess(
            await controller.Get(definition.Id, CancellationToken.None));
        AssertServiceSuccess(
            await controller.SaveDraft(
                definition.Id,
                new SaveAgentDraftRequest(
                    0, "Main", string.Empty, "instructions", string.Empty,
                    AgentOutputMode.Text, null, [], [], []),
                CancellationToken.None));
        AssertServiceSuccess<AgentDefinition>(
            await controller.Publish(definition.Id, new ExpectedRevisionRequest(0), CancellationToken.None));
        AssertServiceSuccess<AgentDefinition>(
            await controller.SetStatus(
                definition.Id,
                new SetAgentStatusRequest(0, AgentRuntimeStatus.Disabled),
                CancellationToken.None));

        controller.Request.ContentType = "application/json";
        controller.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("{}"));
        AssertServiceSuccess(
            await controller.Import(CancellationToken.None));
        Assert.Equal(StatusCodes.Status201Created, controller.Response.StatusCode);
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
    public async Task Return_structured_error_when_skill_file_cannot_be_read()
    {
        IAgSkillDefinitionServices lifecycle = Proxy<IAgSkillDefinitionServices>((method, _) =>
            method.Name == nameof(IAgSkillDefinitionServices.ReadFileAsync)
                ? Task.FromResult(ServiceResult<string>.Failure(
                    SkillServiceStatusCodes.FileMissing,
                    "The Skill file was not found."))
                : throw new InvalidOperationException(method.Name));
        var controller = WithHttpContext(new SkillsController(
            lifecycle,
            new StubAgentDefinitionCatalog()));

        AssertServiceError(
            await controller.ReadFile(Guid.NewGuid(), "missing.md", CancellationToken.None),
            StatusCodes.Status404NotFound,
            SkillServiceStatusCodes.FileMissing,
            SkillErrorCodes.FileMissing);
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

        ActionResult<ServiceResult<SkillDefinition>> action = await controller.Create(
            new CreateSkillRequest("test", "Test", string.Empty, "general"),
            CancellationToken.None);

        ServiceResult<SkillDefinition> body =
            AssertServiceSuccess<SkillDefinition>(action, StatusCodes.Status201Created);
        Assert.Same(skill, body.Data);
        Assert.Equal($"/api/skills/{skill.Id}", controller.Response.Headers.Location);
    }

    [Fact]
    public async Task Return_request_error_for_invalid_skill_status_filter()
    {
        var controller = WithHttpContext(new SkillsController(
            Proxy<IAgSkillDefinitionServices>((_, _) => throw new InvalidOperationException()),
            new StubAgentDefinitionCatalog()));

        ActionResult<ServiceResult<IReadOnlyList<SkillListItem>>> action = await controller.List(
            null,
            null,
            "Unknown",
            CancellationToken.None);

        AssertServiceError(
            action,
            StatusCodes.Status400BadRequest,
            SkillServiceStatusCodes.LifecycleTransitionInvalid,
            SkillErrorCodes.LifecycleTransitionInvalid);
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

        ActionResult<ServiceResult<MainAgentAssignment>> action =
            await controller.Get(CancellationToken.None);

        AssertServiceError(
            Assert.IsType<JsonResult>(action.Result),
            StatusCodes.Status404NotFound,
            610004,
            "MAIN_AGENT_NOT_CONFIGURED");
    }

    [Fact]
    public async Task Return_fixed_service_error_when_setting_an_unknown_main_agent()
    {
        var assignments = new MainAgentAssignmentService(
            new StubAgentDefinitionCatalog(),
            new EmptyMainAgentAssignmentRepository());
        var controller = WithHttpContext(new MainAgentController(assignments));

        ActionResult<ServiceResult<MainAgentAssignment>> action = await controller.Set(
            new SetMainAgentRequest(Guid.NewGuid(), null),
            CancellationToken.None);

        AssertServiceError(
            Assert.IsType<JsonResult>(action.Result),
            StatusCodes.Status404NotFound,
            610018,
            MainAgentErrorCodes.AgentNotFound);
    }

    [Fact]
    public async Task Resolve_the_latest_published_version_for_the_configured_main_agent()
    {
        Guid agentId = Guid.NewGuid();
        Guid earlierVersionId = Guid.NewGuid();
        Guid latestVersionId = Guid.NewGuid();
        AgentVersionSnapshot snapshot = new(
            latestVersionId,
            "main",
            "instructions",
            "model",
            AgentOutputMode.Text,
            null,
            [],
            []);
        var draft = new AgentVersion(
            Guid.NewGuid(), "draft", true, "instructions", "model", AgentOutputMode.Text,
            null, null, null);
        var earlierVersion = new AgentVersion(
            earlierVersionId, "1.0.0", false, "instructions", "model", AgentOutputMode.Text,
            null, null, snapshot with { VersionId = earlierVersionId });
        var latestVersion = new AgentVersion(
            latestVersionId, "2.0.0", false, "instructions", "model", AgentOutputMode.Text,
            null, null, snapshot);
        var agent = new AgentDefinition(
            agentId, "main", "Main", string.Empty, AgentRuntimeStatus.Enabled, 0, draft,
            [earlierVersion, latestVersion]);
        var assignment = new MainAgentAssignment(
            agentId, earlierVersionId, 7, DateTimeOffset.UtcNow);
        var service = new MainAgentAssignmentService(
            new StubAgentDefinitionCatalog([agent]),
            new FixedMainAgentAssignmentRepository(assignment));

        ServiceResult<MainAgentAssignment> result = await service.GetAsync(CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(latestVersionId, result.Data?.AgentVersionId);
        Assert.Equal(7, result.Data?.LogicalRevision);
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

        ServiceResult<IReadOnlyList<PublishedSkillReference>> body =
            AssertServiceSuccess(await controller.List(CancellationToken.None));
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

    private static ServiceResult<T> AssertServiceSuccess<T>(ServiceResult<T> body)
    {
        Assert.Equal(200, body.Status);
        Assert.True(body.Success);
        return body;
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

    private static ServiceResult<T> AssertServiceSuccess<T>(
        ActionResult<ServiceResult<T>> action,
        int httpStatus)
    {
        if (action.Result is not null)
            return AssertServiceSuccess<T>((IActionResult)action.Result, httpStatus);

        Assert.Equal(StatusCodes.Status200OK, httpStatus);
        return AssertServiceSuccess(Assert.IsType<ServiceResult<T>>(action.Value));
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

    private static void AssertServiceError<T>(
        ActionResult<ServiceResult<T>> action,
        int httpStatus,
        int businessStatus,
        string errorCode) =>
        AssertServiceError(
            Assert.IsType<JsonResult>(action.Result),
            httpStatus,
            businessStatus,
            errorCode);

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

    private sealed class FixedMainAgentAssignmentRepository(
        MainAgentAssignment assignment) : IMainAgentAssignmentRepository
    {
        public Task<MainAgentAssignment?> GetAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<MainAgentAssignment?>(assignment);

        public Task<bool> TryReplaceAsync(
            MainAgentAssignment value,
            long? expectedLogicalRevision,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
