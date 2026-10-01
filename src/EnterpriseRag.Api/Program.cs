using EnterpriseRag.Application.Abstractions;
using EnterpriseRag.Application.Documents.UploadDocuments;
using EnterpriseRag.Infrastructure.Documents;
using EnterpriseRag.Infrastructure.Persistence;
using EnterpriseRag.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;

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
var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();
app.Run();

