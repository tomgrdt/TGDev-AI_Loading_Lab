using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.AI;
using TGDev.StepS01.Shared.Database;
using TGDev.StepS01.Shared.Models;
using TGDev.StepS01.Shared.Services;

namespace TGDev.Step03.Vectorization.Services;

public class VectorizationService : IVectorizationService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<VectorizationService> _logger;
    private readonly ISharedService _sharedService;

    public VectorizationService(
        IMemoryCache cache,
        ILogger<VectorizationService> logger,
        ISharedService sharedService)
    {
        _cache = cache;
        _logger = logger;
        
        _sharedService = sharedService;
    }
    public async Task<IEnumerable<NewsItemModel>> GetVectorizationAsync(CancellationToken cancellationToken)
    {
        KernelModel kernelModel = _sharedService.KernelModel;

        var collectionsList = await kernelModel.VectorStore.ListCollectionNamesAsync(cancellationToken).ToListAsync(cancellationToken);

        var context = new NewsItemDbContext();
        var noIndexedNewsItems = context.NewsItems.Where(n => !n.IsVectorized).ToList();

        if (collectionsList.Contains(kernelModel.QdrantCollectionName))
        {
            foreach (NewsItemModel newsItem in noIndexedNewsItems)
            {
                newsItem.DescriptionEmbedding = await kernelModel.EmbeddingGenerator!.GenerateVectorAsync(newsItem.Summary);
                await kernelModel.NewsItemVectorStore.UpsertAsync(newsItem);
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
}
