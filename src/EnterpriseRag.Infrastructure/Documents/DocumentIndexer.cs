using EnterpriseRag.Application.Abstractions;
using EnterpriseRag.Application.Models;

namespace EnterpriseRag.Infrastructure.Documents
{
    public class DocumentIndexer : IDocumentIndexer
    {
        private readonly IDocumentRepository _documentRepository;
        private readonly IEmbeddingService _embeddingService;
        private readonly IVectorStore _vectorStore;

        public DocumentIndexer(IDocumentRepository documentRepository, IEmbeddingService embeddingService, IVectorStore vectorStore)
        {
            _documentRepository = documentRepository;
            _embeddingService = embeddingService;
            _vectorStore = vectorStore;
        }

        public async Task IndexAsync(Guid documentId, CancellationToken cancellationToken)
        {
            var chunks = await _documentRepository.GetChunksAsync(documentId, cancellationToken);

            foreach (var chunk in chunks)
            {
                var embedding = await _embeddingService.GenerateEmbeddingAsync(chunk.Content, cancellationToken);

                var record = new VectorRecord
                {
                    ChunkId = chunk.Id,
                    DocumentId = chunk.DocumentId,
                    Content = chunk.Content,
                    PageNumber = chunk.PageNumber,
                    Embedding = embedding
                };

                await _vectorStore.UpsertAsync(record, cancellationToken);
            }
        }
    }
}
