using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Qdrant.Client;
using TGDev.StepS01.Shared.Plugins;
using TGDev.StepS01.Shared.Services;

namespace TGDev.StepS01.Shared.Models;

public class KernelModel
{
    public Uri OllamaEndpoint { get; set; }
    public Uri QdrantEndpoint { get; set; }
    public string OllamaModelName { get; set; }
    public string QdrantEmbeddingModelName { get; set; }
    public string QdrantCollectionName { get; set; }
    public Kernel Kernel { get; set; }

    public VectorStore VectorStore => Kernel.Services.GetRequiredService<VectorStore>();
    public IEmbeddingGenerator<string, Embedding<float>> EmbeddingGenerator => Kernel.Services.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>();
    public IChatCompletionService ChatClient => Kernel.GetRequiredService<IChatCompletionService>();
    public VectorStoreCollection<Guid, NewsItemModel> NewsItemVectorStore => VectorStore.GetCollection<Guid, NewsItemModel>(QdrantCollectionName);
    public IAsyncEnumerable<string> ListCollectionNamesAsync(CancellationToken cancellationToken) => VectorStore.ListCollectionNamesAsync(cancellationToken);

    private IConfiguration _configuration { get; set; }

    public KernelModel(IConfiguration configuration)
    {
        _configuration = configuration;

        OllamaEndpoint = new Uri(_configuration["Ollama:Endpoint"] ?? "http://localhost:11434");
        QdrantEndpoint = new Uri(_configuration["Qdrant:Endpoint"] ?? "http://localhost:6334");
        OllamaModelName = _configuration["Ollama:ModelName"] ?? "qwen2.5:7b";
        QdrantEmbeddingModelName = _configuration["Qdrant:EmbeddingModelName"] ?? "nomic-embed-text";
        QdrantCollectionName = _configuration["Qdrant:CollectionName"] ?? "TGDev.Experimentation01.News";

        IKernelBuilder kernelBuilder = Kernel.CreateBuilder();

        kernelBuilder.AddOllamaChatCompletion(OllamaModelName, OllamaEndpoint);
        kernelBuilder.AddOllamaEmbeddingGenerator(QdrantEmbeddingModelName, OllamaEndpoint);

        kernelBuilder.Services.AddQdrantVectorStore();

        kernelBuilder.Plugins.AddFromType<NewsItemPlugin>();
        kernelBuilder.Services.AddSingleton<NewsItemService>();

        kernelBuilder.Services.AddSingleton(_ => new QdrantClient(QdrantEndpoint));

        Kernel = kernelBuilder.Build();
    }
}
