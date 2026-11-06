using Microsoft.AspNetCore.Mvc;
using UL_ALL_IN_ONE;

namespace ALL_IN_ONE_API.Controllers;

[ApiController, Route("api/auth")]
public sealed class AuthController(IAuthService auth) : ControllerBase
{
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request, CancellationToken ct)
    {
        try { return Ok(await auth.RegisterAsync(request, ct)); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request, CancellationToken ct) =>
        await auth.LoginAsync(request, ct) is { } response ? Ok(response) : Unauthorized(new { message = "Invalid user name or password." });
}
