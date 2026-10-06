using Microsoft.AspNetCore.Mvc;
using System.Net.Mime;
using TGDev.StepS01.Shared.Models;
using TGDev.Step02.DatabaseStorage.Services;

namespace TGDev.Step02.DatabaseStorage.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DatabaseStorageController(ILogger<DatabaseStorageController> logger) : ControllerBase
{
    private readonly ILogger<DatabaseStorageController> _logger = logger;

    [HttpPost(Name = "PostDatabaseStorage"),
        Tags(["Database Storage API"]),
        EndpointSummary("Fetches and returns a list of database items."),
        Produces(MediaTypeNames.Application.Json),
        ProducesResponseType(typeof(IEnumerable<NewsItemModel>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Post([FromBody] List<NewsItemModel> listNewsItems, IDatabaseStorageService databaseStorageService)
    {

        var newsItems = await databaseStorageService.PostDatabaseStorageAsync(listNewsItems);
        return Ok(newsItems);
    }
}
