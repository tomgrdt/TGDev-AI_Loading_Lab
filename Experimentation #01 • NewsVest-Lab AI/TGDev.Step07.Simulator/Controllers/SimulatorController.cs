using Microsoft.AspNetCore.Mvc;
using System.Net.Mime;
using TGDev.Step07.Simulator.Services;
using TGDev.StepS01.Shared.Models;

namespace TGDev.Step07.Simulator.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SimulatorController : Controller
{
    private readonly ILogger<SimulatorController> _logger;
    

    public SimulatorController(ILogger<SimulatorController> logger)
    {
        _logger = logger;
    }

    [HttpGet(Name = "RunSimulator"),
        Tags(["Simulator API"]),
        EndpointSummary("Runs simulation on investment recommendations."),
        Produces(MediaTypeNames.Application.Json),
        ProducesResponseType(typeof(SimulatorResultModel), StatusCodes.Status200OK)]
    public async Task<IActionResult> Run([FromQuery] double capitalInitial, ISimulatorService simulatorService)
    {
        SimulatorResultModel? simulatorResult = await simulatorService.GetSimulationAsync(capitalInitial, CancellationToken.None);
        return Ok(simulatorResult);
    }
}
