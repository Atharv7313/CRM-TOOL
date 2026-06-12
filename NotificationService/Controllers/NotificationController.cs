using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NotificationService.Interfaces;
using Shared.DTOs;

namespace NotificationService.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationController(INotificationService svc) : ControllerBase
{
    // GET api/notifications
    [HttpGet("")]
    public async Task<IActionResult> GetMine(
        [FromQuery] string? status,
        [FromQuery] string? category) =>
        Ok(await svc.GetForUserAsync(CurrentUserId(), status, category));

    // GET api/notifications/unread-count
    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount() =>
        Ok(new UnreadCountDto(await svc.GetUnreadCountAsync(CurrentUserId())));

    // GET api/notifications/history
    [HttpGet("history")]
    public async Task<IActionResult> GetHistory(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to) =>
        Ok(await svc.GetHistoryAsync(CurrentUserId(), from, to));

    // GET api/notifications/sent
    [HttpGet("sent")]
    public async Task<IActionResult> GetSent() =>
        Ok(await svc.GetSentAsync(CurrentUserId()));

    // POST api/notifications
    [HttpPost("")]
    public async Task<IActionResult> Create([FromBody] CreateNotificationDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var list = await svc.CreateAsync(dto, CurrentUserId());
        return Ok(list);
    }

    // POST api/notifications/broadcast
    [HttpPost("broadcast")]
    public async Task<IActionResult> Broadcast([FromBody] BroadcastNotificationDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var list = await svc.BroadcastAsync(dto, CurrentUserId());
        return Ok(list);
    }

    // PUT api/notifications/{id}/read
    [HttpPut("{id:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid id) =>
        await svc.MarkReadAsync(id, CurrentUserId()) ? Ok() : NotFound();

    // PUT api/notifications/read-all
    [HttpPut("read-all")]
    public async Task<IActionResult> MarkAllRead() =>
        Ok(new { updated = await svc.MarkAllReadAsync(CurrentUserId()) });

    // DELETE api/notifications/{id}
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id) =>
        await svc.DeleteAsync(id, CurrentUserId()) ? NoContent() : NotFound();

    // â”€â”€ Helpers â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    private Guid CurrentUserId()
    {
        var raw = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

        if (string.IsNullOrWhiteSpace(raw))
            throw new UnauthorizedAccessException("Missing user id claim.");

        return Guid.Parse(raw);
    }
}
