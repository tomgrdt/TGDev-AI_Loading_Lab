using Microsoft.AspNetCore.Mvc;
using System.Net.Mime;
using System.Text.Json;
using TGDev.Step02.DatabaseStorage.Models;
using TGDev.Step02.DatabaseStorage.Services;

namespace TGDev.Step02.DatabaseStorage.Controllers;


[ApiController]
[Route("api/[controller]")]
public class DatabaseStorageController : ControllerBase
{
    private readonly ILogger<DatabaseStorageController> _logger;

    public DatabaseStorageController(ILogger<DatabaseStorageController> logger)
    {
        _logger = logger;
    }

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
