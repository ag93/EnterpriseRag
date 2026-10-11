using EnterpriseRag.Application.Models;

namespace EnterpriseRag.Application.Abstractions
{
    public interface IRagService
    {
        Task<RagAnswer> AskAsync(string question, CancellationToken cancellationToken);
        Task<RagExecutionResult> AskWithDiagnosticsAsync(string question, CancellationToken cancellationToken);
    }
}
