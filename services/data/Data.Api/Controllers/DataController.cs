using Data.Api.Models;
using Data.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Data.Api.Controllers;

[ApiController]
[Route("data")]
public sealed class DataController : ControllerBase
{
    private readonly DataWriteService _data;

    public DataController(DataWriteService data)
    {
        _data = data;
    }

    [HttpPost]
    public async Task<ActionResult<DataRecordDto>> Create(
        [FromBody] CreateDataRequest request,
        CancellationToken cancellationToken)
    {
        var created = await _data.CreateAsync(request, cancellationToken);
        return Ok(created);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<DataRecordDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        var row = await _data.GetAsync(id, cancellationToken);
        if (row is null)
            return NotFound();
        return Ok(row);
    }
}
