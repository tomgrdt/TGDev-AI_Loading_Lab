namespace TGDev.StepS01.Shared.Models;


public class PositionResultModel
{
    public string Stock { get; set; } = string.Empty;
    public double Weight { get; set; }
    public double AllocatedValue { get; set; }
    public double FinalValue { get; set; }
    public double PercentPerformance => AllocatedValue == 0 ? 0 : (FinalValue - AllocatedValue) / AllocatedValue * 100;

}

/// <summary>
/// Result of running the investment simulator(step 6), built
/// based on the recommendations from step 5. Market data is
/// randomly generated (demo) — see the disclaimer displayed in the UI.
/// </summary>
public class SimulatorResultModel
{
    public DateTime ExecutionDate { get; set; } = DateTime.Now;
    public double InitialCapital { get; set; }
    public double FinalCapital { get; set; }
    public double PercentGlobalPerformance => InitialCapital == 0 ? 0 : (FinalCapital - InitialCapital) / InitialCapital * 100;

    public List<PositionResultModel> Positions { get; set; } = new();

    /// <summary>Simulated portfolio value, point by point, for the chart.</summary>
    public List<double> ValueCurve { get; set; } = new();

    public PositionResultModel? BestPosition => Positions.OrderByDescending(p => p.PercentPerformance).FirstOrDefault();
    public PositionResultModel? WorstPosition => Positions.OrderBy(p => p.PercentPerformance).FirstOrDefault();
}
