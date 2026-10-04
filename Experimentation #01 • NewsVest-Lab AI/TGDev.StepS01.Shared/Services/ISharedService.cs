using TGDev.StepS01.Shared.Models;

namespace TGDev.StepS01.Shared.Services;

public interface ISharedService
{
    string FeedFetcherJsonResult { get; set; }
    IEnumerable<NewsItemModel> DatabaseStorageResult { get; set; }
    IEnumerable<NewsItemModel> NoIndexedNewsItems { get; set; }
    KernelModel KernelModel { get; set; }
    List<InvestmentRecommendationModel> Recommendations { get; }
    SimulatorResultModel? LastSimulationResults { get; set; }
}
