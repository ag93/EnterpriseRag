using EnterpriseRag.Application.Abstractions;
using EnterpriseRag.Application.Documents;
using EnterpriseRag.Application.Documents.UploadDocuments;
using EnterpriseRag.Application.RAG;
using EnterpriseRag.Infrastructure.AI;
using EnterpriseRag.Infrastructure.Documents;
using EnterpriseRag.Infrastructure.Persistence;
using EnterpriseRag.Infrastructure.Search;
using EnterpriseRag.Infrastructure.Storage;
using EnterpriseRag.Infrastructure.VectorStore;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using Qdrant.Client;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Enterprise RAG API",
        Version = "v1",
        Description = "Enterprise document search and RAG API"
    });
});

var storagePath = Path.Combine(
    builder.Environment.ContentRootPath,
    "data",
    "documents");
builder.Services.AddSingleton<IDocumentStorage>(new LocalFileStorage(storagePath));
builder.Services.AddScoped<UploadDocumentHandler>();

var databasePath = Path.Combine(
    builder.Environment.ContentRootPath,
    "data",
    "enterprise-rag.db");
builder.Services.AddDbContext<ApplicationDbContext>(
    options =>
        options.UseSqlite($"Data Source={databasePath}"));
builder.Services.AddScoped<IDocumentRepository, DocumentRepository>();

builder.Services.AddScoped<IPdfTextExtractor, PdfTextExtractor>();
builder.Services.AddScoped<ITextChunker, SimpleTextChunker>();
builder.Services.AddHttpClient<IEmbeddingService, OllamaEmbeddingService>(
    client =>
    {
        client.BaseAddress = new Uri("http://localhost:11434");
    });

builder.Services.AddHttpClient<IChatService, OllamaChatService>(
    client =>
    {
        client.BaseAddress = new Uri("http://localhost:11434");
    });
builder.Services.AddScoped<IRagService, RagService>();
builder.Services.AddSingleton(
    new QdrantClient("localhost", 6334));

builder.Services.AddScoped<IVectorStore, QdrantVectorStore>();
builder.Services.AddScoped<IDocumentIndexer, DocumentIndexer>();
builder.Services.AddScoped<IDocumentSearch, DocumentSearch>();
builder.Services.AddScoped<IReranker, KeywordReranker>(); 

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();
app.Run();

