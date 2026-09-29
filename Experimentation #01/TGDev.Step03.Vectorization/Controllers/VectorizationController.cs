using Microsoft.AspNetCore.Mvc;
using System.Net.Mime;
using Qdrant.Client.Grpc;
using TGDev.Step03.Vectorization.Services;
using TGDev.StepS01.Shared.Database;
using TGDev.StepS01.Shared.Models;

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

        LocalQdrantClient localQdrantClient = new LocalQdrantClient();
        EmbeddingClient embeddingClient = new EmbeddingClient();
        RagService ragService = new RagService(localQdrantClient, embeddingClient);
        Indexer indexer = new Indexer(localQdrantClient, embeddingClient);

        var qdrantClient = new Qdrant.Client.QdrantClient("localhost", 6334);
        var newsCollection = await qdrantClient.ListCollectionsAsync();

        if(!newsCollection.Contains("TGDev.Experimentation01.News"))
            await qdrantClient.CreateCollectionAsync("TGDev.Experimentation01.News", new VectorParams { Size = 768, Distance = Distance.Cosine });

        var context = new NewsItemDbContext();
        var noIndexedNewsItems = context.NewsItems.Where(n => !n.IsVectorized).ToList();

        foreach (NewsItemModel newsItem in noIndexedNewsItems)
        {
            await indexer.IndexDatabaseAsync(newsItem.Title!, newsItem.Summary!, newsItem.Link!, newsItem.Category!, newsItem.PublishedAt, newsItem.SourceName!, newsItem.SourceUrl!);
            newsItem.IsVectorized = true;
        }
            
        await context.SaveChangesAsync();

        return Ok();
    }
}

