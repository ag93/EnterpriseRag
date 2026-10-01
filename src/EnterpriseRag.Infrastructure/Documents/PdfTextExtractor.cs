using EnterpriseRag.Application.Abstractions;
using EnterpriseRag.Application.Models;
using UglyToad.PdfPig;

namespace EnterpriseRag.Infrastructure.Documents
{
    public class PdfTextExtractor : IPdfTextExtractor
    {
        public Task<IReadOnlyList<PdfPageText>> ExtractAsync(Stream pdfStream, CancellationToken cancellationToken)
        {
            var pages = new List<PdfPageText>();
            using var document = PdfDocument.Open(pdfStream);
            var pageNumber = 0;

            foreach(var page in document.GetPages())
            {
                cancellationToken.ThrowIfCancellationRequested();
                pageNumber++;
                var text = page.Text;
                pages.Add(new PdfPageText { PageNumber = pageNumber, Text = text });
            }
            return Task.FromResult<IReadOnlyList<PdfPageText>>(pages);
        }
    }
}
