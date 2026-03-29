using Gateway.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Api.Controllers;

[ApiController]
[Route("api/sync")]
public sealed class SyncController : ControllerBase
{
    private readonly SyncServiceClient _client;

    public SyncController(SyncServiceClient client)
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
        await _client.TriggerSyncAsync(cancellationToken);
        return Ok();
    }
}
