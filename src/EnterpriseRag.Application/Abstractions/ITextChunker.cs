using EnterpriseRag.Application.Models;

namespace EnterpriseRag.Application.Abstractions
{
    public interface ITextChunker
    {
        IReadOnlyList<TextChunk> Chunk(IReadOnlyList<PdfPageText> pages);
    }
}
