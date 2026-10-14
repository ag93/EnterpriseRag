using EnterpriseRag.Application.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseRag.Api.Controllers;

[Route("api/admin")]
[ApiController]
[Authorize(Policy = "AdminsOnly")]
public class AdminController : ControllerBase
{
    private readonly IIndexReconciliationService _reconciliationService;

    public AdminController(IIndexReconciliationService reconciliationService)
    {
        _reconciliationService = reconciliationService;
    }

    [HttpPost("reconcile-index")]
    public async Task<IActionResult> ReconcileIndex(CancellationToken cancellationToken)
    {
        var result = await _reconciliationService.ReconcileAsync(cancellationToken);
        return Ok(result);
    }
}
