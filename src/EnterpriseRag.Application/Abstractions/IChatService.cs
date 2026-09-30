using EnterpriseRag.Application.Models;

namespace EnterpriseRag.Application.Abstractions
{
    public interface IChatService
    {
        Task<RagAnswer> GenerateAsync(RagContext context, CancellationToken cancellationToken);
    }
}
