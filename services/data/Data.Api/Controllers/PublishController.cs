using Data.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace Data.Api.Controllers;

[ApiController]
[Route("publish")]
public sealed class PublishController : ControllerBase
{
    private readonly IConnectionStringProvider _connectionString;
    private readonly OutboxPublishState _publishState;
    private readonly OutboxPublishControl _publishControl;

    public PublishController(
        IConnectionStringProvider connectionString,
        OutboxPublishState publishState,
        OutboxPublishControl publishControl)
    {
        _connectionString = connectionString;
        _publishState = publishState;
        _publishControl = publishControl;
    }

    [HttpGet("status")]
    public async Task<IActionResult> Status(CancellationToken cancellationToken)
    {
        await using var conn = new NpgsqlConnection(_connectionString.GetConnectionString());
        await conn.OpenAsync(cancellationToken);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT count(*)::bigint
            FROM outbox_messages
            WHERE processed_at_utc IS NULL
            """;
        var pending = (long)(await cmd.ExecuteScalarAsync(cancellationToken))!;
        return Ok(new
        {
            pendingOutboxMessages = pending,
            publisher = _publishState.Snapshot()
        });
    }

    [HttpPost("trigger")]
    public IActionResult Trigger()
    {
        _publishControl.RequestImmediateRun();
        return Ok(new { triggered = true });
    }
}
