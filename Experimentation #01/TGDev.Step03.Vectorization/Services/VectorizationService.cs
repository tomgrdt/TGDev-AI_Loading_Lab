using Microsoft.Extensions.Caching.Memory;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.Extensions.VectorData;
using Microsoft.Extensions.AI;
using Qdrant.Client;
using TGDev.StepS01.Shared.Database;
using TGDev.StepS01.Shared.Models;
using TGDev.StepS01.Shared.Plugins;
using TGDev.StepS01.Shared.Services;

namespace TGDev.Step03.Vectorization.Services;

public class VectorizationService : IVectorizationService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<VectorizationService> _logger;
    private readonly IConfiguration _configuration;

    private readonly Uri _ollamaEndpoint;
    private readonly Uri _qdrantEndpoint;
    private readonly string _modelName;
    private readonly string _embeddingModelName;
    private readonly string _collectionName;

    public VectorizationService(
        IMemoryCache cache,
        ILogger<VectorizationService> logger,
        IConfiguration configuration)
    {
        _cache = cache;
        _logger = logger;
        _configuration = configuration;

        _ollamaEndpoint = new Uri(configuration["Ollama:Endpoint"] ?? "http://localhost:11434");
        _qdrantEndpoint = new Uri(configuration["Qdrant:Endpoint"] ?? "http://localhost:6334");

        _modelName = configuration["Ollama:ModelName"] ?? "qwen2.5:7b";
        _embeddingModelName = configuration["Qdrant:EmbeddingModelName"] ?? "nomic-embed-text";
        _collectionName = configuration["Qdrant:CollectionName"] ?? "TGDev.Experimentation01.News";
    }
    public async Task<IEnumerable<NewsItemModel>> GetVectorizationAsync(CancellationToken cancellationToken)
    {
        Kernel kernel = CreateKernel();

        var vectorStore = kernel.Services.GetRequiredService<VectorStore>();
        var embeddingGenerator = kernel.Services.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>();

        var chatClient = kernel.GetRequiredService<IChatCompletionService>();

        var newsItemVectorStore = vectorStore.GetCollection<Guid, NewsItemModel>(_collectionName);

        var collections = vectorStore.ListCollectionNamesAsync(cancellationToken);
        var collectionsList = await collections.ToListAsync(cancellationToken);

        var context = new NewsItemDbContext();
        var noIndexedNewsItems = context.NewsItems.Where(n => !n.IsVectorized).ToList();

        if (collectionsList.Contains(_collectionName))
        {
            foreach (NewsItemModel newsItem in noIndexedNewsItems)
            {
                newsItem.DescriptionEmbedding = await embeddingGenerator!.GenerateVectorAsync(newsItem.Summary);
                await newsItemVectorStore.UpsertAsync(newsItem);
                newsItem.IsVectorized = true;
            }
        }

        try
        {
            await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving changes to the database.");
            throw;
        }

        return noIndexedNewsItems;
    }

    private Kernel CreateKernel()
    {
        IKernelBuilder kernelBuilder = Kernel.CreateBuilder();

        kernelBuilder.AddOllamaChatCompletion(_modelName, _ollamaEndpoint);
        kernelBuilder.AddOllamaEmbeddingGenerator(_embeddingModelName, _ollamaEndpoint);

        kernelBuilder.Services.AddQdrantVectorStore();

        kernelBuilder.Plugins.AddFromType<NewsItemPlugin>();
        kernelBuilder.Services.AddSingleton<NewsItemService>();

        kernelBuilder.Services.AddSingleton(_ => new QdrantClient(_qdrantEndpoint));

        return kernelBuilder.Build();
    }
}
