using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UL_ALL_IN_ONE;

namespace ALL_IN_ONE_API.Controllers;

[ApiController, Route("api/admin")]
public sealed class AdminController(IAuthService auth, IAppCatalog catalog) : ControllerBase
{
    [AllowAnonymous, HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request, CancellationToken ct)
    {
        var result = await auth.LoginAsync(request, ct);
        return result is { Role: "Admin" } ? Ok(result) : Unauthorized(new ApiMessage("Invalid administrator credentials."));
    }

    [Authorize(Roles = "Admin"), HttpGet("apps")]
    public async Task<ActionResult<IReadOnlyList<AppInfo>>> GetApps(CancellationToken ct) => Ok(await catalog.GetAdminListAsync(ct));

    [Authorize(Roles = "Admin"), HttpPost("apps")]
    public async Task<ActionResult<AppInfo>> CreateApp(AppUpsertRequest request, CancellationToken ct)
    {
        try { var app = await catalog.CreateAsync(request, ct); return Created($"/api/apps/{app.Slug}", app); }
        catch (ArgumentException ex) { return BadRequest(new ApiMessage(ex.Message)); }
        catch (InvalidOperationException ex) { return Conflict(new ApiMessage(ex.Message)); }
    }

    [Authorize(Roles = "Admin"), HttpPut("apps/{id:int}")]
    public async Task<ActionResult<AppInfo>> UpdateApp(int id, AppUpsertRequest request, CancellationToken ct)
    {
        try { return await catalog.UpdateAsync(id, request, ct) is { } app ? Ok(app) : NotFound(); }
        catch (ArgumentException ex) { return BadRequest(new ApiMessage(ex.Message)); }
    }

    [Authorize(Roles = "Admin"), HttpDelete("apps/{id:int}")]
    public async Task<IActionResult> DeleteApp(int id, CancellationToken ct) => await catalog.DeleteAsync(id, ct) ? NoContent() : NotFound();
}
