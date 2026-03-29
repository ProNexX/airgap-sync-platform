using Gateway.Api.Models;
using Gateway.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Api.Controllers;

[ApiController]
[Route("api/data")]
public class DataController : ControllerBase
{
    private readonly DataServiceClient _client;

    public DataController(DataServiceClient client)
    {
        _client = client;
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateDataRequest request)
    {
        var result = await _client.CreateAsync(request);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var result = await _client.GetAsync(id);
        if (result is null)
            return NotFound();
        return Ok(result);
    }
}