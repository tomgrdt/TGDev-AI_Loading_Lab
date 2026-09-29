using TGDev.StepS01.Shared.Models;

namespace TGDev.StepS01.Shared.Services;

public class SharedService(IConfiguration configuration) : ISharedService
{
    public string FeedFetcherJsonResult { get; set; } = string.Empty;
    public IEnumerable<NewsItemModel> DatabaseStorageResult { get; set; } = new List<NewsItemModel>();
    public IEnumerable<NewsItemModel> NoIndexedNewsItems { get; set; } = new List<NewsItemModel>();

    public string CollectionName { get; set; } = configuration["QdrantCollection"] ?? string.Empty;

}
