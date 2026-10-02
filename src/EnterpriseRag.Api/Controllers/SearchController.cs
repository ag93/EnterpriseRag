using EnterpriseRag.Application.Abstractions;
using EnterpriseRag.Application.Models;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseRag.Api.Controllers;

[ApiController]
[Route("api/search")]
public class SearchController : ControllerBase
{
    private readonly IDocumentSearch _documentSearch;

    public SearchController(IDocumentSearch documentSearch)
    {
        _documentSearch = documentSearch;
    }

    [HttpPost]
    public async Task<IActionResult> Search(
        [FromBody] SearchRequest request,
        CancellationToken cancellationToken)
    {
        var results =
            await _documentSearch.SearchAsync(
                request,
                cancellationToken);

        return Ok(results);
    }
}