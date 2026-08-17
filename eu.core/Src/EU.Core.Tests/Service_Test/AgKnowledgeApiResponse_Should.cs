#nullable enable

using EU.Core.Agent.Application.Knowledge;
using EU.Core.Api.Agent.Configuration;
using EU.Core.Api.Agent.Controllers;
using EU.Core.Api.Agent.Errors;
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
        var lifecycle = new KnowledgeLifecycleService(
            repository,
            new KnowledgeRetriever(value),
            pdfTextExtractor: new PdfTextExtractor());
        var controller = WithHttpContext(new KnowledgeBasesController(lifecycle));

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
        var lifecycle = new KnowledgeLifecycleService(
            new KnowledgeRepository([]),
            new KnowledgeRetriever());
        var controller = WithHttpContext(new KnowledgeBasesController(lifecycle));

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
        PublishedKnowledgeReference reference =
            new(Guid.NewGuid(), "atlas", "Atlas", 3);
        var controller = WithHttpContext(new KnowledgeBaseReferencesController(
            new KnowledgeCatalog([reference])));

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
        Assert.Same(AgentJsonSerialization.PascalCase, json.SerializerSettings);
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
        Assert.Same(AgentJsonSerialization.PascalCase, json.SerializerSettings);
        ServiceResult<AgentApiErrorData> body =
            Assert.IsType<ServiceResult<AgentApiErrorData>>(json.Value);
        Assert.False(body.Success);
        Assert.Equal(businessStatus, body.Status);
        Assert.Equal(errorCode, body.Data.ErrorCode);
        Assert.Equal("trace-knowledge-contract", body.Data.TraceId);
    }

    private sealed class KnowledgeRepository(
        IEnumerable<KnowledgeBaseDefinition> values) : IKnowledgeBaseRepository
    {
        private readonly Dictionary<Guid, KnowledgeBaseDefinition> _values =
            values.ToDictionary(value => value.Id);

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
    }

    private sealed class KnowledgeRetriever(KnowledgeBaseDefinition? value = null)
        : IKnowledgeRetriever
    {
        public Task<IReadOnlyList<KnowledgeSearchResult>> SearchAsync(
            IReadOnlyList<Guid> knowledgeBaseIds,
            string query,
            int take,
            CancellationToken cancellationToken = default)
        {
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
    }

    private sealed class PdfTextExtractor : IKnowledgePdfTextExtractor
    {
        public Task<KnowledgePdfExtractionResult> ExtractAsync(
            ReadOnlyMemory<byte> content,
            int maximumPages,
            int maximumCharacters,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(KnowledgePdfExtractionResult.Success(
                "PDF extracted content.",
                1));
    }

    private sealed class KnowledgeCatalog(
        IReadOnlyList<PublishedKnowledgeReference> values) : IPublishedKnowledgeCatalog
    {
        public Task<IReadOnlyList<PublishedKnowledgeReference>> ListAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult(values);
    }
}
