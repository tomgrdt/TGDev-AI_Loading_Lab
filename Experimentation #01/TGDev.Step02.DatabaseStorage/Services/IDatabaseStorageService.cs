using TGDev.StepS01.Shared.Models;

namespace TGDev.Step02.DatabaseStorage.Services;

public interface IDatabaseStorageService
{
    Task<IEnumerable<NewsItemModel>> PostDatabaseStorageAsync(List<NewsItemModel> listNewsItems, CancellationToken cancellationToken = default);
}
