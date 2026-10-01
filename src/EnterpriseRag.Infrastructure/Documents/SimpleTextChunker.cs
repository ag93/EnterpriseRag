using EnterpriseRag.Application.Abstractions;
using EnterpriseRag.Application.Models;

namespace EnterpriseRag.Infrastructure.Documents
{
    public class SimpleTextChunker : ITextChunker
    {
        private const int MaxCharacters = 1500;
        private const int OverlapCharacters = 200;

        public IReadOnlyList<TextChunk> Chunk(IReadOnlyList<PdfPageText> pages)
        {
            var chunks = new List<TextChunk>();
            int chunkIndex = 0;

            foreach (var page in pages)
            {
                var text = NormalizeWhitespace(page.Text);
                if (string.IsNullOrWhiteSpace(text))
                    continue;

                var start = 0;
                while (start < text.Length)
                {
                    var length = Math.Min(MaxCharacters, text.Length - start);
                    var content = text.Substring(start, length).Trim();

                    if (!string.IsNullOrWhiteSpace(content))
                    {
                        chunks.Add(new TextChunk
                        {
                            ChunkIndex = chunkIndex++,
                            PageNumber = page.PageNumber,
                            Content = content
                        });
                    }

                    if (start + length >= text.Length)
                        break;

                    start += length - OverlapCharacters;
                }
            }
            return chunks;
        }

        private static string NormalizeWhitespace(string text)
        {
            return string.Join(
                ' ',
                text.Split(
                    (char[]?)null,
                    StringSplitOptions.RemoveEmptyEntries));
        }
    }
}
