using TGDev.StepS01.Shared.Models;

namespace TGDev.Step07.Simulator.Services;

public interface ISimulatorService
{
    Task<SimulatorResultModel?> GetSimulationAsync(double capital, CancellationToken cancellationToken);
}
