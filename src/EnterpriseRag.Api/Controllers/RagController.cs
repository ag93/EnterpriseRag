using EnterpriseRag.Application.Abstractions;
using EnterpriseRag.Application.Models;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseRag.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RagController : ControllerBase
    {
        private readonly IRagService _ragService;

        public RagController(IRagService ragService)
        {
            _ragService = ragService;
        }

        [HttpPost]
        public async Task<IActionResult> Ask([FromBody] RagRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Question))
            {
                return BadRequest("Question is required.");
            }

            var answer = await _ragService.AskAsync(request.Question, cancellationToken);

            return Ok(answer);
        }
    }
}
