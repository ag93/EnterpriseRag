using EnterpriseRag.Application.Abstractions;
using EnterpriseRag.Application.Documents.UploadDocuments;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseRag.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DocumentsController : ControllerBase
    {
        private readonly UploadDocumentHandler _uploadDocumentHandler;
        private readonly IDocumentIndexer _documentIndexer;

        public DocumentsController(UploadDocumentHandler uploadDocumentHandler, IDocumentIndexer documentIndexer)
        {
            _uploadDocumentHandler = uploadDocumentHandler;
            _documentIndexer = documentIndexer;
        }

        [HttpPost]
        public async Task<IActionResult> Upload(IFormFile uploadedFile, CancellationToken cancellationToken)
        {
            if (uploadedFile is null || uploadedFile.Length == 0)
            {
                return BadRequest("A file is required.");
            }

            await using var stream = uploadedFile.OpenReadStream();

            var request = new UploadDocumentRequest
            {
                Content = stream,
                FileName = uploadedFile.FileName,
                ContentType = uploadedFile.ContentType
            };

            var document = await _uploadDocumentHandler.HandleAsync(
                request,
                cancellationToken);

            return Ok(document);
        }

        [HttpPost("{documentId:guid}/index")]
        public async Task<IActionResult> Index(Guid documentId, CancellationToken cancellationToken)
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
    }
}
