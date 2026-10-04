using TGDev.StepS01.Shared.Models;

namespace TGDev.Step03.Vectorization.Services;

public interface IVectorizationService
{
    Task<IEnumerable<NewsItemModel>> GetVectorizationAsync(CancellationToken cancellationToken);
}
