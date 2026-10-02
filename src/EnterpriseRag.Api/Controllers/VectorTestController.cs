using EnterpriseRag.Application.Abstractions;
using EnterpriseRag.Application.Models;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseRag.Api.Controllers;

[ApiController]
[Route("api/vector-test")]
public class VectorTestController : ControllerBase
{
    private readonly IEmbeddingService _embeddingService;
    private readonly IVectorStore _vectorStore;
    private readonly IDocumentIndexer _documentIndexer;

    public VectorTestController(
        IEmbeddingService embeddingService,
        IVectorStore vectorStore,
        IDocumentIndexer documentIndexer)
    {
        _embeddingService = embeddingService;
        _vectorStore = vectorStore;
        _documentIndexer = documentIndexer;
    }

    [HttpPost("index-document/{documentId:guid}")]
    public async Task<IActionResult> IndexDocument(
    Guid documentId,
    CancellationToken cancellationToken)
    {
        await _documentIndexer.IndexAsync(
            documentId,
            cancellationToken);

        return Ok(new
        {
            documentId,
            indexed = true
        });
    }

    [HttpPost("index")]
    public async Task<IActionResult> Index(
        [FromBody] VectorTestRequest request,
        CancellationToken cancellationToken)
    {
        var embedding =
            await _embeddingService.GenerateEmbeddingAsync(
                request.Text,
                cancellationToken);

        var chunkId = Guid.NewGuid();
        var documentId = Guid.NewGuid();

        await _vectorStore.UpsertAsync(
            new VectorRecord
            {
                ChunkId = chunkId,
                DocumentId = documentId,
                Content = request.Text,
                PageNumber = 1,
                Embedding = embedding
            },
            cancellationToken);

        return Ok(new
        {
            chunkId,
            documentId,
            dimensions = embedding.Length
        });
    }
}

public class VectorTestRequest
{
    public string Text { get; set; } = string.Empty;
}