using TGDev.StepS01.Shared.Models;

namespace TGDev.StepS01.Shared.Services;

public class SharedService : ISharedService
{
    public string FeedFetcherJsonResult { get; set; } = string.Empty;
    public IEnumerable<NewsItemModel> DatabaseStorageResult { get; set; } = new List<NewsItemModel>();

    public SharedService() { }
}
