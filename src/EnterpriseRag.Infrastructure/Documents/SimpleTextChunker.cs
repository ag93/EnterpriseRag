using EnterpriseRag.Application.Abstractions;
using EnterpriseRag.Application.Models;
using static System.Net.Mime.MediaTypeNames;

namespace EnterpriseRag.Infrastructure.Documents
{
    public class SimpleTextChunker : ITextChunker
    {
        private const int MaxCharacters = 1000;
        private const int OverlapCharacters = 150;

        public IReadOnlyList<TextChunk> Chunk(IReadOnlyList<PdfPageText> pages)
        {
            var chunks = new List<TextChunk>();
            int chunkIndex = 0;

            foreach (var page in pages)
            {
                var paragraphs = SplitIntoParagraphs(page.Text);
                var currentParagraphs = new List<string>();
                var currentLength = 0;
                foreach (var paragraph in paragraphs) 
                {
                    if (currentLength + paragraph!.Length <= MaxCharacters)
                    {
                        currentParagraphs.Add(paragraph);
                        currentLength += paragraph.Length + 1;
                        continue;
                    }

                    if (currentParagraphs.Count > 0)
                    {
                        chunks.Add(CreateChunk(currentParagraphs, page.PageNumber, chunkIndex++));
                        var overlap = BuildOverlap(currentParagraphs);
                        currentParagraphs = new List<string>();
                        if (!string.IsNullOrWhiteSpace(overlap))
                        {
                            currentParagraphs.Add(overlap);
                            currentLength = overlap.Length;
                        }
                        else
                        {
                            currentLength = 0;
                        }
                    }

                    if (paragraph.Length > MaxCharacters)
                    {
                        var sentences = SplitIntoSentences(paragraph);
                        foreach (var sentence in sentences)
                        {
                            if (currentLength + sentence.Length <= MaxCharacters)
                            {
                                currentParagraphs.Add(sentence);
                                currentLength += sentence.Length + 1;
                            }
                            else
                            {
                                if (currentParagraphs.Count > 0)
                                {
                                    chunks.Add(CreateChunk(currentParagraphs, page.PageNumber, chunkIndex++));
                                    currentParagraphs = [];
                                    currentLength = 0;
                                }

                                currentParagraphs.Add(sentence);
                                currentLength = sentence.Length + 1;
                            }
                        }
                    }
                    else
                    {
                        currentParagraphs.Add(paragraph);
                        currentLength = paragraph.Length + 1;
                    }
                }
                if (currentParagraphs.Count > 0)
                {
                    chunks.Add(CreateChunk(currentParagraphs, page.PageNumber, chunkIndex++));
                }

            }
            return chunks;
        }

        private static IReadOnlyList<string> SplitIntoSentences(string text)
        {
            return text.Split(['.', '!', '?'],
                    StringSplitOptions.RemoveEmptyEntries)
                .Select(x => NormalizeWhitespace(x))
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x + ".")
                .ToList();
        }

        private static string BuildOverlap(List<string> paragraphs)
        {
            var text = string.Join(" ", paragraphs);
            if (text.Length <= OverlapCharacters)
                return text;
            
            return text[^OverlapCharacters..];
        }

        private static TextChunk CreateChunk(IReadOnlyList<string> paragraphs, int pageNumber, int chunkIndex)
        {
            return new TextChunk
            {
                ChunkIndex = chunkIndex,
                PageNumber = pageNumber,
                Content = string.Join(Environment.NewLine + Environment.NewLine, paragraphs).Trim()
            };
        }

        private static List<string?> SplitIntoParagraphs(string text)
        {
            return text.Split(["\r\n\r\n", "\n\n"], StringSplitOptions.RemoveEmptyEntries)
            .Select(x => NormalizeWhitespace(x))
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();
        }

        private static string? NormalizeWhitespace(string text)
        {
            return string.Join(' ', text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        }
    }
}
