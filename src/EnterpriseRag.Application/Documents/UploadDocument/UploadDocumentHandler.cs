using EnterpriseRag.Application.Abstractions;
using EnterpriseRag.Domain;

namespace EnterpriseRag.Application.Documents.UploadDocuments
{
    public class UploadDocumentHandler
    {
        private readonly IDocumentStorage _documentStorage;
        private readonly IDocumentRepository _documentRepository;
        private readonly IPdfTextExtractor _pdfTextExtractor;
        private readonly ITextChunker _textChunker;

        public UploadDocumentHandler(IDocumentStorage documentStorage,
                                     IDocumentRepository documentRepository,
                                     IPdfTextExtractor pdfTextExtractor,
                                     ITextChunker textChunker)
        {
            _documentStorage = documentStorage;
            _documentRepository = documentRepository;
            _pdfTextExtractor = pdfTextExtractor;
            _textChunker = textChunker;
        }

        public async Task<Document> HandleAsync(UploadDocumentRequest request, CancellationToken cancellationToken)
        {
            var storagePath = await _documentStorage.SaveAsync(request.Content, request.FileName, cancellationToken);
            var document = new Document
            {
                Id = Guid.NewGuid(),
                FileName = request.FileName,
                StoragePath = storagePath,
                ContentType = request.ContentType,
                CreatedAtUtc = DateTime.UtcNow
            };

            await using var storedDocument = await _documentStorage.OpenAsync(storagePath, cancellationToken);
            var pages = await _pdfTextExtractor.ExtractAsync(storedDocument, cancellationToken);
            var chunks = _textChunker.Chunk(pages);
            document.Chunks = chunks
                .Select(chunk => new DocumentChunk
                {
                    Id = Guid.NewGuid(),
                    DocumentId = document.Id,
                    ChunkIndex = chunk.ChunkIndex,
                    Content = chunk.Content,
                    PageNumber = chunk.PageNumber
                }).ToList();

            await _documentRepository.AddAsync(document, cancellationToken);
            return document;
        }
    }
}
