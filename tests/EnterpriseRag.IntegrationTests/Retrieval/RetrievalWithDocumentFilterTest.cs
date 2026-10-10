using EnterpriseRag.Application;
using EnterpriseRag.Application.Abstractions;
using EnterpriseRag.Application.Documents;
using EnterpriseRag.Application.Models;
using EnterpriseRag.Infrastructure.AI;
using EnterpriseRag.Infrastructure.Search;
using EnterpriseRag.Infrastructure.VectorStore;
using Microsoft.Extensions.DependencyInjection;
using Qdrant.Client;

namespace EnterpriseRag.IntegrationTests.Retrieval
{
    public class RetrievalWithDocumentFilterTest
    {
        [Fact]
        public async Task Search_WithOneDocumentFilter_ReturnsOnlyMatchingDocument()
        {
            var services = new ServiceCollection();
            services.AddHttpClient<IEmbeddingService, OllamaEmbeddingService>(
               client =>
               {
                   client.BaseAddress =
                       new Uri("http://localhost:11434");
               });
            services.AddSingleton(new QdrantClient("localhost", 6334));

            services.AddScoped<IVectorStore, QdrantVectorStore>();

            services.AddScoped<IDocumentSearch, DocumentSearch>();

            services.AddScoped<IReranker, KeywordReranker>();

            services.AddScoped<IDocumentSearch, DocumentSearch>();
            await using var serviceProvider = services.BuildServiceProvider();
            using var scope = serviceProvider.CreateScope();
            var documentSearch = scope.ServiceProvider.GetRequiredService<IDocumentSearch>();

            var a350DocumentId = Guid.Parse("98C8407A-48AD-408F-AFDE-43123734FF0D");

            var request = new SearchRequest
            {
                Query =
                    "How many passengers does the Airbus A350 have, and how many are in CloudLux, CloudElite, and CloudSaver?",
                TopK = 5,
                CandidateCount = 20,
                Filter = new DocumentSearchFilter
                {
                    DocumentIds = [a350DocumentId]
                }
            };

            var results = await documentSearch.SearchAsync(
                request,
                CancellationToken.None);

            Assert.NotEmpty(results);
            Assert.All(
                results,
                result => Assert.Equal(
                    "Airbus A350.pdf",
                    result.FileName));

            Assert.Contains(
                results,
                result => result.Content.Contains(
                    "CloudElite",
                    StringComparison.OrdinalIgnoreCase));
        }


        [Fact]
        public async Task Search_WithMultipleDocumentFilter_ReturnsOnlyMatchingDocuments()
        {
            var services = new ServiceCollection();
            services.AddHttpClient<IEmbeddingService, OllamaEmbeddingService>(
               client =>
               {
                   client.BaseAddress =
                       new Uri("http://localhost:11434");
               });
            services.AddSingleton(new QdrantClient("localhost", 6334));

            services.AddScoped<IVectorStore, QdrantVectorStore>();

            services.AddScoped<IDocumentSearch, DocumentSearch>();

            services.AddScoped<IReranker, KeywordReranker>();

            services.AddScoped<IDocumentSearch, DocumentSearch>();
            await using var serviceProvider = services.BuildServiceProvider();
            using var scope = serviceProvider.CreateScope();
            var documentSearch = scope.ServiceProvider.GetRequiredService<IDocumentSearch>();

            var a350DocumentId = Guid.Parse("98C8407A-48AD-408F-AFDE-43123734FF0D");
            var a380DocumentId = Guid.Parse("F609DE00-284E-4738-8D0C-9B8BA6026901");
            var b787DocumentId = Guid.Parse("21DEE4E0-1C97-42A4-876D-4783C2AE7C9E");

            var request = new SearchRequest
            {
                Query =
                    "Which plane has higher total seat count, the A350, B787 or A380?",
                TopK = 5,
                CandidateCount = 20,
                Filter = new DocumentSearchFilter
                {
                    DocumentIds = [a350DocumentId, a380DocumentId, b787DocumentId]
                }
            };

            var results = await documentSearch.SearchAsync(
                request,
                CancellationToken.None);

            Assert.NotEmpty(results);
            List<Guid> allowedDocumentIds = [a380DocumentId, a350DocumentId, b787DocumentId];
            Assert.All(results, result => Assert.Contains(result.DocumentId, allowedDocumentIds));
        }

        [Fact]
        public async Task Search_WithExplicitEmptyDocumentFilter_ReturnsNoResults()
        {
            var services = new ServiceCollection();
            services.AddHttpClient<IEmbeddingService, OllamaEmbeddingService>(
               client =>
               {
                   client.BaseAddress =
                       new Uri("http://localhost:11434");
               });
            services.AddSingleton(new QdrantClient("localhost", 6334));

            services.AddScoped<IVectorStore, QdrantVectorStore>();

            services.AddScoped<IDocumentSearch, DocumentSearch>();

            services.AddScoped<IReranker, KeywordReranker>();

            services.AddScoped<IDocumentSearch, DocumentSearch>();
            await using var serviceProvider = services.BuildServiceProvider();
            using var scope = serviceProvider.CreateScope();
            var documentSearch = scope.ServiceProvider.GetRequiredService<IDocumentSearch>();

            var request = new SearchRequest
            {
                Query =
                    "Which plane has higher total seat count, the A350, B787 or A380?",
                TopK = 5,
                CandidateCount = 20,
                Filter = new DocumentSearchFilter
                {
                    DocumentIds = []
                }
            };

            var results = await documentSearch.SearchAsync(
                request,
                CancellationToken.None);

            Assert.Empty(results);
        }
    }
}
