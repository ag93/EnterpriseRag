using EnterpriseRag.Domain;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseRag.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options): base(options){}

    public DbSet<Document> Documents => Set<Document>();

    public DbSet<DocumentChunk> DocumentChunks => Set<DocumentChunk>();

    public DbSet<DocumentAccess> DocumentAccess => Set<DocumentAccess>();
}