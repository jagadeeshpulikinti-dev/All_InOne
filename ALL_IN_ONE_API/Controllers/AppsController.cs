using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UL_ALL_IN_ONE;

namespace ALL_IN_ONE_API.Controllers;

[ApiController, AllowAnonymous, Route("api/apps")]
public sealed class AppsController(IAppCatalog catalog) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AppInfo>>> Get(CancellationToken ct) => Ok(await catalog.GetPublicAsync(ct));

    [HttpGet("{slug}")]
    public async Task<ActionResult<AppInfo>> GetBySlug(string slug, CancellationToken ct) =>
        await catalog.GetBySlugAsync(slug, ct) is { } app ? Ok(app) : NotFound();
}
