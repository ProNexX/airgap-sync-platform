using System.Text.Json;
using Gateway.Api.Models;
using Gateway.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Api.Controllers;

[ApiController]
[Route("api/sensors")]
public sealed class SensorsController : ControllerBase
{
    private readonly DataServiceClient _data;

    public SensorsController(DataServiceClient data)
    {
        _data = data;
    }

    [HttpPost("telemetry")]
    public async Task<IActionResult> Ingest([FromBody] SensorTelemetryRequest request, CancellationToken cancellationToken)
    {
        var recordedAt = request.RecordedAt ?? DateTimeOffset.UtcNow;
        var payload = JsonSerializer.Serialize(new
        {
            metric = request.Metric,
            value = request.Value,
            unit = request.Unit,
            recordedAt
        });

        var body = new CreateDataRequest
        {
            Name = request.DeviceId,
            Value = payload,
            ClientRequestId = request.ClientRequestId
        };

        var result = await _data.CreateAsync(body, cancellationToken);
        return Ok(result);
    }
}
