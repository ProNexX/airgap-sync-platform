using Microsoft.AspNetCore.Mvc;

namespace Gateway.Api.Controllers;


[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    [HttpPost("login")]
    public IActionResult Login()
    {
        // fake JWT for now
        return Ok(new { token = "dev-token" });
    }
}

