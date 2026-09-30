using EnterpriseRag.Application.Abstractions;
using EnterpriseRag.Domain;

namespace EnterpriseRag.Application.Documents.UploadDocuments
{
    public class UploadDocumentHandler
    {
        private readonly IDocumentStorage _documentStorage;
        private readonly IDocumentRepository _documentRepository;

        public UploadDocumentHandler(IDocumentStorage documentStorage, IDocumentRepository documentRepository)
        {
            _documentStorage = documentStorage;
            _documentRepository = documentRepository;
        }

        public async Task<Document> HandleAsync(UploadDocumentRequest request, CancellationToken cancellationToken)
        {
            var storagePath = await _documentStorage.SaveAsync(request.Content, request.FileName, cancellationToken);
            var document = new Document
            {
                Id = Guid.NewGuid(),
                FileName = request.FileName,
                StoragePath = storagePath,
                CreatedAtUtc = DateTime.UtcNow
            };
            
            await _documentRepository.AddAsync(document, cancellationToken);
            return document;
        }
    }
}
