#nullable enable

using EU.Core.Agent.Application.Knowledge;
using EU.Core.Api.Agent.Controllers;
using EU.Core.Api.Agent.Errors;
using EU.Core.IServices;
using EU.Core.Model;
using EU.Core.Model.ViewModels.Extend;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EU.Core.Tests.Service_Test;

public sealed class AgKnowledgeApiResponse_Should
{
    [Fact]
    public async Task Wrap_knowledge_queries_and_mutations()
    {
        KnowledgeBaseDefinition value = CreateKnowledgeBase();
        var repository = new KnowledgeRepository([value]);
        var controller = WithHttpContext(new KnowledgeBasesController(
            repository));

        AssertServiceSuccess<IReadOnlyList<KnowledgeBaseListItem>>(
            await controller.List(null, CancellationToken.None),
            StatusCodes.Status200OK);
        AssertServiceSuccess<KnowledgeBaseDetailResponse>(
            await controller.Get(value.Id, CancellationToken.None),
            StatusCodes.Status200OK);

        ServiceResult<KnowledgeBaseDetailResponse> created =
            AssertServiceSuccess<KnowledgeBaseDetailResponse>(
                await controller.Create(
                    new CreateKnowledgeBaseRequest("new-base", "New base", "Description"),
                    CancellationToken.None),
                StatusCodes.Status201Created);
        Assert.Equal(
            $"/api/knowledge-bases/{created.Data.Id}",
            controller.Response.Headers.Location);

        ServiceResult<KnowledgeBaseDetailResponse> updated =
            AssertServiceSuccess<KnowledgeBaseDetailResponse>(
                await controller.Update(
                    value.Id,
                    new UpdateKnowledgeBaseRequest(
                        0,
                        "Updated",
                        "Updated description",
                        KnowledgeBaseStatus.Disabled),
                    CancellationToken.None),
                StatusCodes.Status200OK);

        ServiceResult<KnowledgeBaseDetailResponse> imported =
            AssertServiceSuccess<KnowledgeBaseDetailResponse>(
                await controller.ImportDocument(
                    value.Id,
                    new ImportKnowledgeDocumentRequest(
                        updated.Data.LogicalRevision,
                        "guide.md",
                        "text/markdown",
                        "# Guide"),
                    CancellationToken.None),
                StatusCodes.Status200OK);

        byte[] pdf = "%PDF-1.7"u8.ToArray();
        var file = new FormFile(new MemoryStream(pdf), 0, pdf.Length, "file", "guide.pdf")
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/pdf"
        };
        ServiceResult<KnowledgeBaseDetailResponse> pdfImported =
            AssertServiceSuccess<KnowledgeBaseDetailResponse>(
                await controller.ImportPdfDocument(
                    value.Id,
                    imported.Data.LogicalRevision,
                    file,
                    CancellationToken.None),
                StatusCodes.Status200OK);

        AssertServiceSuccess<KnowledgeBaseDetailResponse>(
            await controller.SetArchived(
                value.Id,
                new SetKnowledgeBaseArchiveRequest(pdfImported.Data.LogicalRevision, true),
                CancellationToken.None),
            StatusCodes.Status200OK);
        AssertServiceSuccess<IReadOnlyList<KnowledgeDocumentListItemResponse>>(
            await controller.ListDocuments(value.Id, CancellationToken.None),
            StatusCodes.Status200OK);
        AssertServiceSuccess<KnowledgeChunkPageResponse>(
            await controller.ListDocumentChunks(
                value.Id,
                value.Documents[0].Id,
                0,
                10,
                CancellationToken.None),
            StatusCodes.Status200OK);
        AssertServiceSuccess<IReadOnlyList<KnowledgeSearchResult>>(
            await controller.Search(
                value.Id,
                new SearchKnowledgeRequest("Atlas", 6),
                CancellationToken.None),
            StatusCodes.Status200OK);
    }

    [Fact]
    public async Task Return_fixed_knowledge_errors()
    {
        var repository = new KnowledgeRepository([]);
        var controller = WithHttpContext(new KnowledgeBasesController(
            repository));

        AssertServiceError(
            await controller.Get(Guid.NewGuid(), CancellationToken.None),
            StatusCodes.Status404NotFound,
            640001,
            KnowledgeErrorCodes.NotFound);
        AssertServiceError(
            await controller.List("invalid", CancellationToken.None),
            StatusCodes.Status400BadRequest,
            600001,
            "REQUEST_INVALID");
        AssertServiceError(
            await controller.ImportPdfDocument(
                Guid.NewGuid(),
                0,
                null,
                CancellationToken.None),
            StatusCodes.Status400BadRequest,
            640005,
            KnowledgeErrorCodes.DocumentInvalid);

        AgentApiErrorDescriptor unavailable =
            AgentApiErrorCatalog.Resolve(KnowledgeErrorCodes.Unavailable);
        Assert.Equal(640007, unavailable.Status);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, unavailable.HttpStatus);
    }

    [Fact]
    public async Task Wrap_published_knowledge_references()
    {
        KnowledgeBaseDefinition definition = CreateKnowledgeBase();
        PublishedKnowledgeReference reference =
            new(definition.Id, definition.Code, definition.Name, definition.LogicalRevision);
        var controller = WithHttpContext(new KnowledgeBaseReferencesController(
            new KnowledgeRepository([definition])));

        ServiceResult<IReadOnlyList<PublishedKnowledgeReference>> result =
            AssertServiceSuccess<IReadOnlyList<PublishedKnowledgeReference>>(
                await controller.List(CancellationToken.None),
                StatusCodes.Status200OK);

        Assert.Equal(reference, Assert.Single(result.Data));
    }

    private static KnowledgeBaseDefinition CreateKnowledgeBase()
    {
        Guid documentId = Guid.NewGuid();
        var document = new KnowledgeDocument(
            documentId,
            "atlas.md",
            "text/markdown",
            new string('a', 64),
            "Atlas escalation code is ORCHID-7319.",
            DateTimeOffset.UtcNow.AddMinutes(-1));
        var chunk = new KnowledgeChunk(
            Guid.NewGuid(),
            documentId,
            0,
            document.Content);
        return new KnowledgeBaseDefinition(
            Guid.NewGuid(),
            "atlas",
            "Atlas",
            "Knowledge base",
            KnowledgeBaseStatus.Enabled,
            0,
            [document],
            [chunk],
            DateTimeOffset.UtcNow);
    }

    private static TController WithHttpContext<TController>(TController controller)
        where TController : ControllerBase
    {
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                TraceIdentifier = "trace-knowledge-contract",
                RequestServices = new ServiceCollection().BuildServiceProvider()
            }
        };
        return controller;
    }

    private static ServiceResult<T> AssertServiceSuccess<T>(
        IActionResult action,
        int httpStatus)
    {
        JsonResult json = Assert.IsType<JsonResult>(action);
        Assert.Equal(httpStatus, json.StatusCode);
        Assert.Null(json.SerializerSettings);
        ServiceResult<T> body = Assert.IsType<ServiceResult<T>>(json.Value);
        Assert.Equal(200, body.Status);
        Assert.True(body.Success);
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
        Assert.Equal("trace-knowledge-contract", body.Data.TraceId);
    }

    private sealed class KnowledgeRepository(
        IEnumerable<KnowledgeBaseDefinition> values) : IAgKnowledgeBaseDefinitionServices
    {
        private readonly Dictionary<Guid, KnowledgeBaseDefinition> _values =
            values.ToDictionary(value => value.Id);

        public Task<KnowledgePdfExtractionResult> ExtractAsync(
            ReadOnlyMemory<byte> content,
            int maximumPages,
            int maximumCharacters,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(KnowledgePdfExtractionResult.Failed(
                KnowledgePdfExtractionFailure.Invalid));

        public Task<KnowledgeBaseDefinition?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_values.GetValueOrDefault(id));

        public Task<KnowledgeBaseDefinition?> GetByCodeAsync(
            string code,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_values.Values.FirstOrDefault(value => value.Code == code));

        public Task<IReadOnlyList<KnowledgeBaseDefinition>> ListAsync(
            KnowledgeBaseQuery query,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<KnowledgeBaseDefinition>>(_values.Values
                .Where(value => query.Status is null || value.Status == query.Status)
                .ToArray());

        public Task<IReadOnlyList<PublishedKnowledgeReference>> ListPublishedAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<PublishedKnowledgeReference>>(_values.Values
                .Where(value => value.Status == KnowledgeBaseStatus.Enabled && value.Chunks.Count > 0)
                .OrderBy(value => value.Code, StringComparer.Ordinal)
                .Select(value => new PublishedKnowledgeReference(
                    value.Id,
                    value.Code,
                    value.Name,
                    value.LogicalRevision))
                .ToArray());

        public Task<IReadOnlyList<KnowledgeSearchResult>> SearchAsync(
            IReadOnlyList<Guid> knowledgeBaseIds,
            string query,
            int take,
            CancellationToken cancellationToken = default)
        {
            KnowledgeBaseDefinition? value = _values.Values.FirstOrDefault(candidate =>
                knowledgeBaseIds.Contains(candidate.Id) &&
                candidate.Status == KnowledgeBaseStatus.Enabled &&
                candidate.Chunks.Count > 0);
            IReadOnlyList<KnowledgeSearchResult> result = value is null
                ? []
                :
                [
                    new KnowledgeSearchResult(
                        value.Id,
                        value.Code,
                        value.Documents[0].Id,
                        value.Documents[0].FileName,
                        value.Chunks[0].Id,
                        value.Chunks[0].Sequence,
                        value.Chunks[0].Content,
                        1)
                ];
            return Task.FromResult(result);
        }

        public Task<bool> TryCreateAsync(
            KnowledgeBaseDefinition value,
            CancellationToken cancellationToken = default)
        {
            if (_values.Values.Any(existing => existing.Code == value.Code))
                return Task.FromResult(false);
            _values[value.Id] = value;
            return Task.FromResult(true);
        }

        public Task<bool> TryReplaceAsync(
            KnowledgeBaseDefinition value,
            long expectedLogicalRevision,
            CancellationToken cancellationToken = default)
        {
            if (!_values.TryGetValue(value.Id, out KnowledgeBaseDefinition? existing)
                || existing.LogicalRevision != expectedLogicalRevision)
            {
                return Task.FromResult(false);
            }
            _values[value.Id] = value;
            return Task.FromResult(true);
        }

        public async Task<ServiceResult<KnowledgeBaseDefinition>> CreateAsync(
            CreateKnowledgeBaseCommand command, CancellationToken cancellationToken = default)
        {
            var value = new KnowledgeBaseDefinition(
                Guid.NewGuid(), command.Code, command.Name, command.Description,
                KnowledgeBaseStatus.Enabled, 0, [], [], null);
            await TryCreateAsync(value, cancellationToken);
            return ServiceResult<KnowledgeBaseDefinition>.OprateSuccess(value);
        }

        public async Task<ServiceResult<KnowledgeBaseDefinition>> UpdateAsync(
            UpdateKnowledgeBaseCommand command, CancellationToken cancellationToken = default)
        {
            KnowledgeBaseDefinition current = _values[command.Id];
            KnowledgeBaseDefinition value = current with
            {
                Name = command.Name, Description = command.Description, Status = command.Status,
                LogicalRevision = current.LogicalRevision + 1
            };
            await TryReplaceAsync(value, command.ExpectedLogicalRevision, cancellationToken);
            return ServiceResult<KnowledgeBaseDefinition>.OprateSuccess(value);
        }

        public Task<ServiceResult<KnowledgeBaseDefinition>> ImportDocumentAsync(
            ImportKnowledgeDocumentCommand command, CancellationToken cancellationToken = default) =>
            ImportAsync(command.KnowledgeBaseId, command.ExpectedLogicalRevision,
                command.FileName, command.MediaType, command.Content, cancellationToken);

        public Task<ServiceResult<KnowledgeBaseDefinition>> ImportPdfDocumentAsync(
            ImportPdfKnowledgeDocumentCommand command, CancellationToken cancellationToken = default) =>
            ImportAsync(command.KnowledgeBaseId, command.ExpectedLogicalRevision,
                command.FileName, command.MediaType, "PDF extracted content.", cancellationToken);

        public async Task<ServiceResult<KnowledgeBaseDefinition>> SetArchivedAsync(
            SetKnowledgeBaseArchiveCommand command, CancellationToken cancellationToken = default)
        {
            KnowledgeBaseDefinition current = _values[command.Id];
            KnowledgeBaseDefinition value = current with
            {
                Status = command.Archived ? KnowledgeBaseStatus.Archived : KnowledgeBaseStatus.Disabled,
                LogicalRevision = current.LogicalRevision + 1
            };
            await TryReplaceAsync(value, command.ExpectedLogicalRevision, cancellationToken);
            return ServiceResult<KnowledgeBaseDefinition>.OprateSuccess(value);
        }

        private async Task<ServiceResult<KnowledgeBaseDefinition>> ImportAsync(
            Guid id, long revision, string fileName, string mediaType, string content,
            CancellationToken cancellationToken)
        {
            KnowledgeBaseDefinition current = _values[id];
            Guid documentId = Guid.NewGuid();
            var document = new KnowledgeDocument(
                documentId, fileName, mediaType, new string('a', 64), content, DateTimeOffset.UtcNow);
            var chunk = new KnowledgeChunk(Guid.NewGuid(), documentId, 0, content);
            KnowledgeBaseDefinition value = current with
            {
                LogicalRevision = current.LogicalRevision + 1,
                Documents = [.. current.Documents, document],
                Chunks = [.. current.Chunks, chunk],
                IndexedAtUtc = DateTimeOffset.UtcNow
            };
            await TryReplaceAsync(value, revision, cancellationToken);
            return ServiceResult<KnowledgeBaseDefinition>.OprateSuccess(value);
        }
    }

}

