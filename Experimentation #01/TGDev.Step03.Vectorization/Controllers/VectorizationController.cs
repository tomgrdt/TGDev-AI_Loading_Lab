using Microsoft.AspNetCore.Mvc;
using System.Net.Mime;
using TGDev.Step03.Vectorization.Services;

namespace TGDev.Step03.Vectorization.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VectorizationController : Controller
{
    private readonly ILogger<VectorizationController> _logger;

    public VectorizationController(ILogger<VectorizationController> logger)
    {
        _logger = logger;
    }

    [HttpGet(Name = "RunVectorization"),
        Tags(["Vectorization API"]),
        EndpointSummary("Runs vectorization on a list of news articles."),
        Produces(MediaTypeNames.Application.Json),
        ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Run(IVectorizationService vectorizationService)
    {
        var newsItems = await vectorizationService.GetVectorizationAsync(CancellationToken.None);
        return Ok(newsItems);
    }
}

