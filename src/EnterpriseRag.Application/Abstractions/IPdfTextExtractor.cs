using EnterpriseRag.Application.Models;

namespace EnterpriseRag.Application.Abstractions
{
    public interface IPdfTextExtractor
    {
        Task<IReadOnlyList<PdfPageText>> ExtractAsync(Stream pdfStream, CancellationToken cancellationToken);
    }
}
