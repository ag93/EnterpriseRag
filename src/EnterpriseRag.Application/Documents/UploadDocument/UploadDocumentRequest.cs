namespace EnterpriseRag.Application.Documents.UploadDocuments
{
    public class UploadDocumentRequest
    {
        public required Stream Content { get; init; }

        public required string FileName { get; init; }

        public required string ContentType { get; init; }
    }
}
