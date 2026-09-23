using Microsoft.AspNetCore.Mvc;
using System.Net.Mime;
using TGDev.Step01.NewsRetrieval.Models;
using TGDev.Step01.NewsRetrieval.Services;

namespace TGDev.Step01.NewsRetrieval.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FeedFetcherController : Controller
{
    private readonly ILogger<FeedFetcherController> _logger;

    public FeedFetcherController(ILogger<FeedFetcherController> logger)
    {
        _logger = logger;
    }

    [HttpGet(Name = "GetFeedFetcher"),
        Tags(["Feed Fetcher API"]),
        EndpointSummary("Fetches and returns a list of feed items."),
        Produces(MediaTypeNames.Application.Json),
        ProducesResponseType(typeof(IEnumerable<FeedItemModel>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(IFeedFetcherService feedFetcherService)
    {
        var feedItems = await feedFetcherService.GetFeedFetcherAsync();
        return Ok(feedItems);
    }
}
