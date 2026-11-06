using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UL_ALL_IN_ONE;

namespace ALL_IN_ONE_API.Controllers;

[ApiController, Authorize, Route("api/notifications")]
public sealed class NotificationsController(INotificationService notifications) : ControllerBase
{
    private string UserName => User.FindFirstValue(ClaimTypes.Name) ?? throw new UnauthorizedAccessException();
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<UserNotification>>> Get(CancellationToken ct) => Ok(await notifications.GetAsync(UserName, ct));
    [HttpPost]
    public async Task<ActionResult<UserNotification>> Create(NotificationCreateRequest request, CancellationToken ct)
    {
        try { var item = await notifications.CreateAsync(UserName, request.Message, ct); return CreatedAtAction(nameof(Get), new { id = item.Id }, item); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
    }
    [HttpPut("{id}/read")]
    public async Task<IActionResult> MarkRead(string id, CancellationToken ct)
    {
        try { await notifications.MarkReadAsync(UserName, id, ct); return NoContent(); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        catch (KeyNotFoundException) { return NotFound(); }
    }
}
