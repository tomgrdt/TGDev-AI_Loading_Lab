using Microsoft.AspNetCore.Mvc;
using System.Net.Mime;
using TGDev.Step04.LocalLLM.Services;

namespace TGDev.Step04.LocalLLM.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LocalLLMController(ILogger<LocalLLMController> logger) : Controller
{
    private readonly ILogger<LocalLLMController> _logger = logger;

    [HttpGet(Name = "RunLocalLLM"),
        Tags(["Local LLM API"]),
        EndpointSummary("Runs the local LLM to launch analysis."),
        Produces(MediaTypeNames.Application.Json),
        ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Run(ILocalLLMService localLLMService)
    {
        await localLLMService.GetLocalLLMAsync(CancellationToken.None);
        return Ok();
    }
}
