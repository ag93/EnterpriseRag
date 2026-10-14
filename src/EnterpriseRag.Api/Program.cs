using EnterpriseRag.Application.Abstractions;
using EnterpriseRag.Application.Documents;
using EnterpriseRag.Application.Documents.UploadDocuments;
using EnterpriseRag.Application.RAG;
using EnterpriseRag.Infrastructure.AI;
using EnterpriseRag.Infrastructure.Authorization;
using EnterpriseRag.Infrastructure.Citation;
using EnterpriseRag.Infrastructure.Documents;
using EnterpriseRag.Infrastructure.Persistence;
using EnterpriseRag.Infrastructure.Search;
using EnterpriseRag.Infrastructure.Storage;
using EnterpriseRag.Infrastructure.VectorStore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;
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
    var tenantId = builder.Configuration["EntraId:TenantId"]!;
    var apiAudience = builder.Configuration["EntraId:Audience"]!;
    var apiScope = $"{apiAudience.TrimEnd('/')}/access_as_user";

    options.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.OAuth2,
        Flows = new OpenApiOAuthFlows
        {
            AuthorizationCode = new OpenApiOAuthFlow
            {
                AuthorizationUrl = new Uri(
                    $"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/authorize"),
                TokenUrl = new Uri(
                    $"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/token"),
                Scopes = new Dictionary<string, string>
                {
                    [apiScope] = "Access EnterpriseRag API as the signed-in user"
                }
            }
        }
    });

    options.AddSecurityRequirement(document =>
        new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("oauth2", document)] = [apiScope]
        });
});

var storagePath = Path.Combine(builder.Environment.ContentRootPath, "data", "documents");
builder.Services.AddSingleton<IDocumentStorage>(new LocalFileStorage(storagePath));
builder.Services.AddScoped<UploadDocumentHandler>();
var databasePath = Path.Combine(builder.Environment.ContentRootPath, "data", "enterprise-rag.db");
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite($"Data Source={databasePath}"));
builder.Services.AddScoped<IDocumentRepository, DocumentRepository>();
builder.Services.AddScoped<IDocumentAccessRepository, DocumentAccessRepository>();
builder.Services.AddScoped<IPdfTextExtractor, PdfTextExtractor>();
builder.Services.AddScoped<ITextChunker, SimpleTextChunker>();
builder.Services.AddHttpClient<IEmbeddingService, OllamaEmbeddingService>(client =>
    {
        client.BaseAddress = new Uri("http://localhost:11434");
    });
builder.Services.AddHttpClient<IChatService, OllamaChatService>(client =>
    {
        client.BaseAddress = new Uri("http://localhost:11434");
    });
builder.Services.AddHttpClient<ICitationSupportChecker, OllamaCitationSupportChecker>(client =>
    {
        client.BaseAddress = new Uri("http://localhost:11434");
    });
builder.Services.AddScoped<IRagService, RagService>();
builder.Services.AddScoped<IDocumentAuthorizationService, DocumentAuthorizationService>();
builder.Services.AddSingleton(new QdrantClient("localhost", 6334));
builder.Services.AddScoped<IVectorStore, QdrantVectorStore>();
builder.Services.AddScoped<IDocumentIndexer, DocumentIndexer>();
builder.Services.AddScoped<IDocumentSearch, DocumentSearch>();
builder.Services.AddScoped<IReranker, KeywordReranker>();
builder.Services.AddScoped<ICitationValidator, CitationValidator>();
builder.Services.AddScoped<IIndexReconciliationService, IndexReconciliationService>();
builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<ICurrentUser, HttpContextCurrentUser>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("EntraId"));

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

builder.Services.AddHealthChecks();
var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.OAuthClientId(
        builder.Configuration["Swagger:ClientId"]!);

    options.OAuthUsePkce();
    options.OAuthScopeSeparator(" ");
});
app.UseAuthentication();
app.UseAuthorization();
app.MapHealthChecks("/health").AllowAnonymous();
app.MapControllers();
app.Run();

public partial class Program { }
