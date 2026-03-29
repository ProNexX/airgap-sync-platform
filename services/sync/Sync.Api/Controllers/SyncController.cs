using Microsoft.AspNetCore.Mvc;
using Sync.Api.Services;

namespace Sync.Api.Controllers;

[ApiController]
[Route("sync")]
public sealed class SyncController : ControllerBase
{
    private readonly ConsumerState _consumerState;

    public SyncController(ConsumerState consumerState)
    {
        _consumerState = consumerState;
    }

    [HttpGet("status")]
    public IActionResult Status()
    {
        return Ok(_consumerState.Snapshot());
    }

    [HttpPost("trigger")]
    public IActionResult Trigger()
    {
        return Ok(new
        {
            triggered = true,
            note = "Consumer runs continuously; use status to verify ingestion."
        });
    }
}
