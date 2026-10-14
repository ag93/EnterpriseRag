using EnterpriseRag.Application.Models;

namespace EnterpriseRag.Application.Abstractions;

public interface IIndexReconciliationService
{
    Task<IndexReconciliationResult> ReconcileAsync(CancellationToken cancellationToken);
}