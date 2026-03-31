using Gateway.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Api.Controllers;

[ApiController]
[Route("api/telemetry")]
public sealed class TelemetryController : ControllerBase
{
    private readonly TelemetryServiceClient _client;

    public TelemetryController(TelemetryServiceClient client)
    {
        _client = client;
    }

    [HttpGet("status")]
    public async Task<IActionResult> Status(CancellationToken cancellationToken)
    {
        return Ok(await _client.GetStatusAsync(cancellationToken));
    }

    [HttpPost("trigger")]
    public async Task<IActionResult> Trigger(CancellationToken cancellationToken)
    {
        await _client.TriggerAsync(cancellationToken);
        return Ok();
    }
}
