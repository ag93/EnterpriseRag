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

            if (topK <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(topK),
                    "TopK must be greater than zero.");
            }

            await EnsureCollectionAsync();
                        
            var results = await _client.QueryAsync(
                collectionName: CollectionName,
                query: embedding,
                payloadSelector: true,
                limit: (ulong)topK,
                filter: QdrantFilterBuilder.Build(filter),
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

        public async Task<IReadOnlyCollection<Guid>> GetExistingChunkIdsAsync(CancellationToken cancellationToken)
        {
            var collections = await _client.ListCollectionsAsync(cancellationToken);
            if (!collections.Any(x => x == CollectionName))
            {
                return Array.Empty<Guid>();
            }

            const uint pageSize = 256;
            var chunkIds = new List<Guid>();
            PointId? offset = null;

            do
            {
                var response = await _client.ScrollAsync(
                    collectionName: CollectionName,
                    limit: pageSize,
                    offset: offset,
                    payloadSelector: true,
                    vectorsSelector: false,
                    cancellationToken: cancellationToken);

                foreach (var point in response.Result)
                {
                    if (!point.Payload.TryGetValue("chunkId", out var value) || !Guid.TryParse(value.StringValue, out var chunkId))
                    {
                        throw new InvalidOperationException($"Qdrant point '{point.Id}' has a missing or invalid chunkId payload.");
                    }

                    if (point.Id.Uuid != chunkId.ToString())
                    {
                        throw new InvalidOperationException(
                            $"Qdrant point ID '{point.Id.Uuid}' does not match its chunkId payload '{chunkId}'.");
                    }

                    chunkIds.Add(chunkId);
                }

                offset = response.NextPageOffset;
            }
            while (offset is not null);

            return chunkIds;
        }

        public async Task DeleteAsync(IReadOnlyCollection<Guid> chunkIds, CancellationToken cancellationToken)
        {
            if (chunkIds.Count == 0)
                return;

            var filter = new Filter
            {
                Should =
                {
                    chunkIds.Select(chunkId => new Condition
                    {
                        Field = new FieldCondition
                        {
                            Key = "chunkId",
                            Match = new Match
                            {
                                Keyword = chunkId.ToString()
                            }
                        }
                    })
                }
            };

            await _client.DeleteAsync(collectionName: CollectionName, filter: filter, cancellationToken: cancellationToken);
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

        //---------------------- Used for Diagnostics --------------------------------
        public async Task VerifyPointIdsAsync(CancellationToken cancellationToken)
        {
            var collections = await _client.ListCollectionsAsync(cancellationToken);

            if (!collections.Any(x => x == CollectionName))
            {
                Console.WriteLine($"Qdrant collection '{CollectionName}' does not exist.");
                return;
            }

            const uint pageSize = 256;
            PointId? offset = null;
            var verifiedCount = 0;

            do
            {
                var response = await _client.ScrollAsync(
                    collectionName: CollectionName,
                    limit: pageSize,
                    offset: offset,
                    payloadSelector: true,
                    vectorsSelector: false,
                    cancellationToken: cancellationToken);

                foreach (var point in response.Result)
                {
                    if (!point.Payload.TryGetValue("chunkId", out var value)
                        || !Guid.TryParse(value.StringValue, out var chunkId))
                    {
                        throw new InvalidOperationException(
                            $"Qdrant point '{point.Id}' has a missing or invalid chunkId payload.");
                    }

                    if (!Guid.TryParse(point.Id.Uuid, out var pointId)
                        || pointId != chunkId)
                    {
                        throw new InvalidOperationException(
                            $"Qdrant point ID '{point.Id.Uuid}' does not match its chunkId payload '{chunkId}'.");
                    }

                    verifiedCount++;
                }

                offset = response.NextPageOffset;
            }
            while (offset is not null);

            Console.WriteLine(
                $"Verified {verifiedCount} Qdrant points: point IDs match their chunkId payloads.");
        }
    }
}
