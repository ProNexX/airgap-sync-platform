using Gateway.Api.Models;
using Gateway.Api.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Gateway.Api.Services;

namespace Gateway.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly DevLoginOptions _devLogin;
    private readonly JwtTokenIssuer _tokenIssuer;

    public AuthController(IOptions<DevLoginOptions> devLogin, JwtTokenIssuer tokenIssuer)
    {
        _devLogin = devLogin.Value;
        _tokenIssuer = tokenIssuer;
    }

    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        if (!string.Equals(request.Username, _devLogin.Username, StringComparison.Ordinal)
            || !string.Equals(request.Password, _devLogin.Password, StringComparison.Ordinal))
        {
            return Unauthorized();
        }

        var (token, expiresAt) = _tokenIssuer.CreateAccessToken(request.Username);
        return Ok(new { token, expiresAt });
    }
}
