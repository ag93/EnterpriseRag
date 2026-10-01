using EnterpriseRag.Application.Abstractions;
using EnterpriseRag.Application.Models;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace EnterpriseRag.Infrastructure.Documents
{
    public class PdfTextExtractor : IPdfTextExtractor
    {
        public Task<IReadOnlyList<PdfPageText>> ExtractAsync(Stream pdfStream, CancellationToken cancellationToken)
        {
            var pages = new List<PdfPageText>();
            using var document = PdfDocument.Open(pdfStream);

            foreach(var page in document.GetPages())
            {
                cancellationToken.ThrowIfCancellationRequested();
                var text = ContentOrderTextExtractor.GetText(page);
                if (string.IsNullOrWhiteSpace(text))
                    continue;
                
                pages.Add(new PdfPageText { PageNumber = page.Number, Text = text });
            }
            return Task.FromResult<IReadOnlyList<PdfPageText>>(pages);
        }
    }
}
