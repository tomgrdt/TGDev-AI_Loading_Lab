using TGDev.Step01.NewsRetrieval.Models;
namespace TGDev.Step01.NewsRetrieval.Services;

public interface IFeedFetcherService
{
    Task<IEnumerable<FeedItemModel>> GetFeedFetcherAsync(CancellationToken cancellationToken = default);
}
