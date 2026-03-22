using Microsoft.AspNetCore.Mvc;

namespace Gateway.Api.Controllers;

[ApiController]
[Route("api/sync")]
public class SyncController : ControllerBase
{
    private readonly SyncServiceClient _client;

    public SyncController(SyncServiceClient client)
    {
        _client = client;
    }

    [HttpGet("status")]
    public async Task<IActionResult> Status()
    {
        return Ok(await _client.GetStatus());
    }

    [HttpPost("trigger")]
    public async Task<IActionResult> Trigger()
    {
        await _client.TriggerSync();
        return Ok();
    }
}
