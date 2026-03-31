using Microsoft.AspNetCore.Mvc;
using Telemetry.Api.Services;

namespace Telemetry.Api.Controllers;

[ApiController]
[Route("telemetry")]
public sealed class TelemetryController : ControllerBase
{
    private readonly ConsumerState _consumerState;

    public TelemetryController(ConsumerState consumerState)
    {
        _consumerState = consumerState;
    }

    [HttpGet("status")]
    public IActionResult Status() => Ok(_consumerState.Snapshot());

    [HttpPost("trigger")]
    public IActionResult Trigger() =>
        Ok(new { triggered = true, note = "Consumer runs continuously; check SignalR stream for live readings." });
}
