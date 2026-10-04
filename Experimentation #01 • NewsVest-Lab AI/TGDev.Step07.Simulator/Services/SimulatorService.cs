using Microsoft.Extensions.Caching.Memory;
using TGDev.StepS01.Shared.Models;
using TGDev.StepS01.Shared.Services;

namespace TGDev.Step07.Simulator.Services;

public class SimulatorService : ISimulatorService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<SimulatorService> _logger;
    private readonly ISharedService _sharedService;

    public SimulatorService(
        IMemoryCache cache,
        ILogger<SimulatorService> logger,
        ISharedService sharedService)
    {
        _cache = cache;
        _logger = logger;

        _sharedService = sharedService;
    }
    public async Task<SimulatorResultModel?> GetSimulationAsync(double capital, CancellationToken cancellationToken)
    {
        // Si l'étape 5 n'a pas encore tourné, on simule sur un portefeuille
        // générique pour que le simulateur reste utilisable indépendamment.
        var positionsSource = _sharedService.Recommendations.Count > 0
            ? _sharedService.Recommendations.Where(r => r.Action != ActionRecommandee.Vente).ToList()
            : new List<InvestmentRecommendationModel>
            {
                new() { Stock = "Portefeuille diversifié", Action = ActionRecommandee.Conserver, Weight = 100, Confidence = 50 }
            };

        // Normalise les poids des positions conservées/achetées à 100%.
        var poidsTotal = positionsSource.Sum(r => r.Weight);

        const int nbPasDeTemps = 24; // ex. 24 "séances" simulées
        var courbe = new List<double> { capital };
        var valeurParPosition = positionsSource.ToDictionary(
            r => r.Stock,
            r => capital * (poidsTotal == 0 ? 0 : r.Weight / poidsTotal));

        var rng = new Random();

        for (int pas = 1; pas <= nbPasDeTemps; pas++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Delay(60, cancellationToken);

            foreach (var titre in valeurParPosition.Keys.ToList())
            {
                // Marche aléatoire simple avec un léger biais positif — à but
                // de démonstration uniquement (voir disclaimer dans l'UI).
                var variation = (rng.NextDouble() - 0.47) * 0.035;
                valeurParPosition[titre] *= 1 + variation;
            }

            var valeurPortefeuille = valeurParPosition.Values.Sum();
            courbe.Add(valeurPortefeuille);
        }

        var positions = positionsSource.Select(r => new PositionResultModel
        {
            Stock = r.Stock,
            Weight = poidsTotal == 0 ? 0 : r.Weight / poidsTotal * 100,
            AllocatedValue = capital * (poidsTotal == 0 ? 0 : r.Weight / poidsTotal),
            FinalValue = valeurParPosition[r.Stock]
        }).ToList();

        var capitalFinal = positions.Sum(p => p.FinalValue);

        _sharedService.LastSimulationResults = new SimulatorResultModel
        {
            InitialCapital = capital,
            FinalCapital = capitalFinal,
            Positions = positions,
            ValueCurve = courbe
        };

        return _sharedService.LastSimulationResults;
    }

    //public async Task<SimulatorResultModel> GetSimulationAsync(CancellationToken cancellationToken)
    //{
    //    // Simulate some processing delay
    //    await Task.Delay(1000, cancellationToken);
    //    // Generate random simulation results for demonstration purposes
    //    var random = new Random();
    //    var initialCapital = 10000.0;
    //    var finalCapital = initialCapital * (1 + random.NextDouble() * 0.2 - 0.1); // Random performance between -10% and +10%
    //    var positions = new List<PositionResultModel>
    //    {
    //        new PositionResultModel { Stock = "AAPL", Weight = 0.3, AllocatedValue = initialCapital * 0.3, FinalValue = initialCapital * 0.3 * (1 + random.NextDouble() * 0.2 - 0.1) },
    //        new PositionResultModel { Stock = "GOOGL", Weight = 0.4, AllocatedValue = initialCapital * 0.4, FinalValue = initialCapital * 0.4 * (1 + random.NextDouble() * 0.2 - 0.1) },
    //        new PositionResultModel { Stock = "MSFT", Weight = 0.3, AllocatedValue = initialCapital * 0.3, FinalValue = initialCapital * 0.3 * (1 + random.NextDouble() * 0.2 - 0.1) }
    //    };
    //    var valueCurve = new List<double>();
    //    for (int i = 0; i < 10; i++)
    //    {
    //        valueCurve.Add(initialCapital + (finalCapital - initialCapital) * i / 9);
    //    }
    //    return new SimulatorResultModel
    //    {
    //        ExecutionDate = DateTime.Now,
    //        InitialCapital = initialCapital,
    //        FinalCapital = finalCapital,
    //        Positions = positions,
    //        ValueCurve = valueCurve
    //    };
    //}
}
