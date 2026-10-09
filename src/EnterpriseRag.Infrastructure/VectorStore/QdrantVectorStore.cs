using EnterpriseRag.Application;
using EnterpriseRag.Application.Abstractions;
using EnterpriseRag.Application.Models;
using Qdrant.Client;
using Qdrant.Client.Grpc;

namespace EnterpriseRag.Infrastructure.VectorStore
{
    public class QdrantVectorStore : IVectorStore
    {
        private const string CollectionName = "document_chunks";
        private const ulong VectorSize = 768;
        private readonly QdrantClient _client;
        public QdrantVectorStore(QdrantClient client)
        {
            _client = client;
        }

        public async Task UpsertAsync(VectorRecord record, CancellationToken cancellationToken)
        {
            await EnsureCollectionAsync();

            var point = new PointStruct
            {
                Id = record.ChunkId,
                Vectors = record.Embedding,
                Payload =
                {
                    ["fileName"] = record.FileName.ToString(),
                    ["chunkId"] = record.ChunkId.ToString(),
                    ["documentId"] = record.DocumentId.ToString(),
                    ["content"] = record.Content,
                    ["pageNumber"] = record.PageNumber
                }
            };

            await _client.UpsertAsync(
                collectionName:  CollectionName,
                points: new[] {point},
                cancellationToken: cancellationToken);
        }

        public async Task<IReadOnlyList<VectorSearchResult>> SearchAsync(float[] embedding, int topK, DocumentSearchFilter? filter, CancellationToken cancellationToken)
        {
            if (filter?.DocumentIds is { Count: 0 })
                return Array.Empty<VectorSearchResult>();
            
            await EnsureCollectionAsync();
                        
            var results = await _client.QueryAsync(
                collectionName: CollectionName,
                query: embedding,
                payloadSelector: true,
                limit: (ulong)topK,
                filter: BuildFilter(filter),
                cancellationToken: cancellationToken);

            return results
            .Select(result => new VectorSearchResult
            {
                ChunkId = Guid.Parse(
                    result.Payload["chunkId"].StringValue),

                DocumentId = Guid.Parse(
                    result.Payload["documentId"].StringValue),

                FileName = result.Payload["fileName"].StringValue,

                Content = result.Payload["content"].StringValue,

                PageNumber = (int)result.Payload["pageNumber"].IntegerValue,

                Score = result.Score
            }).ToList();
        }

        private async Task EnsureCollectionAsync()
        {
            var collections = await _client.ListCollectionsAsync();
            if (collections.Any(x => x == CollectionName))
                return;

            await _client.CreateCollectionAsync(
                collectionName: CollectionName,
                vectorsConfig: new VectorParams
                {
                    Size = VectorSize,
                    Distance = Distance.Cosine
                });
        }
        private static Filter? BuildFilter(DocumentSearchFilter? filter)
        {
            //null means no filter, so all documents are eligible.
            //One ID creates one condition.
            //Multiple IDs create multiple Should conditions. Qdrant treats these as OR conditions, so a chunk can match any allowed document.
            if (filter?.DocumentIds is null)
            {
                return null;
            }

            return new Filter
            {
                Should =
                {
                    filter.DocumentIds.Select(documentId =>
                        new Condition
                        {
                            Field = new FieldCondition
                            {
                                Key = "documentId",
                                Match = new Match
                                {
                                    Keyword = documentId.ToString()
                                }
                            }
                        })
                }
            };
        }
    }
}
