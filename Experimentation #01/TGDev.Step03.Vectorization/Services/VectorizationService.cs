using Microsoft.Extensions.Caching.Memory;
using Qdrant.Client.Grpc;
using TGDev.StepS01.Shared.Database;
using TGDev.StepS01.Shared.Models;

namespace TGDev.Step03.Vectorization.Services;

public class VectorizationService : IVectorizationService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<VectorizationService> _logger;

    public VectorizationService(
        IMemoryCache cache,
        ILogger<VectorizationService> logger)
    {
        _cache = cache;
        _logger = logger;
    }
    public async Task<IEnumerable<NewsItemModel>> GetVectorizationAsync(CancellationToken cancellationToken)
    {
        LocalQdrantClient localQdrantClient = new LocalQdrantClient();
        EmbeddingClient embeddingClient = new EmbeddingClient();
        RagService ragService = new RagService(localQdrantClient, embeddingClient);
        Indexer indexer = new Indexer(localQdrantClient, embeddingClient);

        var qdrantClient = new Qdrant.Client.QdrantClient("localhost", 6334);
        var newsCollection = await qdrantClient.ListCollectionsAsync();

        if (!newsCollection.Contains("TGDev.Experimentation01.News"))
            await qdrantClient.CreateCollectionAsync("TGDev.Experimentation01.News", new VectorParams { Size = 768, Distance = Distance.Cosine });

        var context = new NewsItemDbContext();
        var noIndexedNewsItems = context.NewsItems.Where(n => !n.IsVectorized).ToList();

        foreach (NewsItemModel newsItem in noIndexedNewsItems)
        {
            await indexer.IndexDatabaseAsync(newsItem.Title!, newsItem.Summary!, newsItem.Link!, newsItem.Category!, newsItem.PublishedAt, newsItem.SourceName!, newsItem.SourceUrl!);
            newsItem.IsVectorized = true;
        }

        await context.SaveChangesAsync();

        return noIndexedNewsItems;
    }
}
