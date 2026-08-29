#nullable enable

using EU.Core.IServices.Knowledge;
using EU.Core.Api.Agent.Controllers;
using EU.Core.Api.Agent.Errors;
using EU.Core.IServices;
using EU.Core.Model;
using EU.Core.Model.Entity;
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
            await controller.Create(
                new CreateKnowledgeBaseRequest("new-base", "New base", "Description"),
                CancellationToken.None);
        Assert.True(created.Success);
        Assert.Equal(StatusCodes.Status200OK, created.Status);
        Assert.Equal(0, created.Data.DocumentCount);
        Assert.Equal(0, created.Data.ChunkCount);

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
        Assert.Equal(2, imported.Data.DocumentCount);
        Assert.Equal(2, imported.Data.ChunkCount);

        byte[] pdf = "%PDF-1.7"u8.ToArray();
        var file = new FormFile(new MemoryStream(pdf), 0, pdf.Length, "file", "guide.pdf")
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/pdf"
        };
        ServiceResult<KnowledgeBaseDetailResponse> pdfImported =
            await controller.ImportPdfDocument(
                value.Id,
                imported.Data.LogicalRevision,
                file,
                CancellationToken.None);
        Assert.True(pdfImported.Success);
        Assert.Equal(200, pdfImported.Status);
        Assert.Equal(3, pdfImported.Data.DocumentCount);

        ServiceResult<IReadOnlyList<KnowledgeDocumentListItemResponse>> documents =
            await controller.ListDocuments(value.Id, CancellationToken.None);
        KnowledgeDocumentListItemResponse pdfDocument = Assert.Single(
            documents.Data,
            document => document.FileName == "guide.pdf");
        ServiceResult<KnowledgeBaseDetailResponse> deleted =
            await controller.DeleteDocument(
                value.Id,
                pdfDocument.Id,
                new DeleteKnowledgeDocumentRequest(pdfImported.Data.LogicalRevision),
                CancellationToken.None);
        Assert.True(deleted.Success);
        Assert.Equal(StatusCodes.Status200OK, deleted.Status);

        ServiceResult<KnowledgeBaseDetailResponse> archived =
            await controller.SetArchived(
                value.Id,
                new SetKnowledgeBaseArchiveRequest(deleted.Data.LogicalRevision, true),
                CancellationToken.None);
        Assert.True(archived.Success);
        Assert.Equal(StatusCodes.Status200OK, archived.Status);
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

        ServiceResult<KnowledgeBaseDetailResponse> missing =
            await controller.Get(Guid.NewGuid(), CancellationToken.None);
        Assert.False(missing.Success);
        Assert.Equal(KnowledgeServiceStatusCodes.NotFound, missing.Status);
        ServiceResult<IReadOnlyList<KnowledgeBaseListItem>> invalidStatus =
            await controller.List("invalid", CancellationToken.None);
        Assert.False(invalidStatus.Success);
        Assert.Equal(KnowledgeServiceStatusCodes.DocumentInvalid, invalidStatus.Status);
        ServiceResult<KnowledgeBaseDetailResponse> invalidPdf =
            await controller.ImportPdfDocument(
                Guid.NewGuid(),
                0,
                null,
                CancellationToken.None);
        Assert.False(invalidPdf.Success);
        Assert.Equal(KnowledgeServiceStatusCodes.DocumentInvalid, invalidPdf.Status);

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
        ServiceResult<T> body,
        int businessStatus)
    {
        Assert.Equal(businessStatus, body.Status);
        Assert.True(body.Success);
        return body;
    }

    private sealed class KnowledgeRepository(
        IEnumerable<KnowledgeBaseDefinition> values)
        : EU.Core.Services.BASE.BaseServices<AgKnowledgeBaseDefinition>,
          IAgKnowledgeBaseDefinitionServices
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
            KnowledgeBaseStatus? status = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<KnowledgeBaseDefinition>>(_values.Values
                .Where(value => status is null || value.Status == status)
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
            string code,
            string name,
            string description,
            CancellationToken cancellationToken = default)
        {
            var value = new KnowledgeBaseDefinition(
                Guid.NewGuid(), code, name, description,
                KnowledgeBaseStatus.Enabled, 0, [], [], null);
            await TryCreateAsync(value, cancellationToken);
            return ServiceResult<KnowledgeBaseDefinition>.OprateSuccess(value);
        }

        public async Task<ServiceResult<KnowledgeBaseDefinition>> UpdateAsync(
            Guid id,
            long expectedLogicalRevision,
            string name,
            string description,
            KnowledgeBaseStatus status,
            CancellationToken cancellationToken = default)
        {
            KnowledgeBaseDefinition current = _values[id];
            KnowledgeBaseDefinition value = current with
            {
                Name = name, Description = description, Status = status,
                LogicalRevision = current.LogicalRevision + 1
            };
            await TryReplaceAsync(value, expectedLogicalRevision, cancellationToken);
            return ServiceResult<KnowledgeBaseDefinition>.OprateSuccess(value);
        }

        public Task<ServiceResult<KnowledgeBaseDefinition>> ImportDocumentAsync(
            Guid knowledgeBaseId,
            long expectedLogicalRevision,
            string fileName,
            string mediaType,
            string content,
            CancellationToken cancellationToken = default) =>
            ImportAsync(knowledgeBaseId, expectedLogicalRevision,
                fileName, mediaType, content, cancellationToken);

        public Task<ServiceResult<KnowledgeBaseDefinition>> ImportPdfDocumentAsync(
            Guid knowledgeBaseId,
            long expectedLogicalRevision,
            string fileName,
            string mediaType,
            ReadOnlyMemory<byte> content,
            CancellationToken cancellationToken = default) =>
            ImportAsync(knowledgeBaseId, expectedLogicalRevision,
                fileName, mediaType, "PDF extracted content.", cancellationToken);

        public async Task<ServiceResult<KnowledgeBaseDefinition>> DeleteDocumentAsync(
            Guid knowledgeBaseId,
            Guid documentId,
            long expectedLogicalRevision,
            CancellationToken cancellationToken = default)
        {
            KnowledgeBaseDefinition current = _values[knowledgeBaseId];
            KnowledgeBaseDefinition value = current with
            {
                LogicalRevision = current.LogicalRevision + 1,
                Documents = current.Documents
                    .Where(document => document.Id != documentId)
                    .ToArray(),
                Chunks = current.Chunks
                    .Where(chunk => chunk.DocumentId != documentId)
                    .ToArray(),
                IndexedAtUtc = DateTimeOffset.UtcNow
            };
            _values[value.Id] = value;
            await Task.CompletedTask;
            return ServiceResult<KnowledgeBaseDefinition>.OprateSuccess(value);
        }

        public async Task<ServiceResult<KnowledgeBaseDefinition>> SetArchivedAsync(
            Guid id,
            long expectedLogicalRevision,
            bool archived,
            CancellationToken cancellationToken = default)
        {
            KnowledgeBaseDefinition current = _values[id];
            KnowledgeBaseDefinition value = current with
            {
                Status = archived ? KnowledgeBaseStatus.Archived : KnowledgeBaseStatus.Disabled,
                LogicalRevision = current.LogicalRevision + 1
            };
            await TryReplaceAsync(value, expectedLogicalRevision, cancellationToken);
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

