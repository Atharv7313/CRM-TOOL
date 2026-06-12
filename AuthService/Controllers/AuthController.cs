using AuthService.Helpers;
using AuthService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.DTOs;
using System.Security.Claims;

namespace AuthService.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly JwtHelper _jwtHelper;

    public AuthController(IAuthService authService, JwtHelper jwtHelper)
    {
        _authService = authService;
        _jwtHelper = jwtHelper;
    }

    // POST api/auth/login
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        try
        {
            var ip        = HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();
            var result    = await _authService.LoginAsync(request, ip, userAgent);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    // POST api/auth/reset-password
    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        try
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            await _authService.ResetPasswordAsync(dto, ip);
            return Ok(new { message = "Password updated successfully." });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    // POST api/auth/logout
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        var token  = HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
        var jti    = _jwtHelper.GetJtiFromToken(token);
        var userId = GetCurrentUserId();
        var ip     = HttpContext.Connection.RemoteIpAddress?.ToString();
        await _authService.LogoutAsync(jti, userId, ip);
        return Ok(new { message = "Logged out successfully" });
    }

    // POST api/auth/logout-all
    [HttpPost("logout-all")]
    [Authorize]
    public async Task<IActionResult> LogoutAll()
    {
        await _authService.LogoutAllAsync(GetCurrentUserId());
        return Ok(new { message = "All sessions revoked" });
    }

    // POST api/auth/enroll  — Admin only
    [HttpPost("enroll")]
    [Authorize]
    public async Task<IActionResult> EnrollUser([FromBody] EnrollUserDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        if (!IsAdmin()) return StatusCode(403, new { message = "Only administrators can enroll new users." });

        var actorId    = GetCurrentUserId();
        var actorName  = User.FindFirst("name")?.Value ?? User.FindFirst(ClaimTypes.Name)?.Value ?? "Admin";
        var actorEmail = User.FindFirst("email")?.Value ?? User.FindFirst(ClaimTypes.Email)?.Value ?? "";

        var result = await _authService.EnrollUserAsync(dto, actorId, actorName, actorEmail);
        return CreatedAtAction(nameof(GetAllUsers), null, result);
    }

    // PUT api/auth/users/{userId}  — Admin only
    [HttpPut("users/{userId:guid}")]
    [Authorize]
    public async Task<IActionResult> UpdateUser(Guid userId, [FromBody] UpdateUserDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        if (!IsAdmin()) return StatusCode(403, new { message = "Only administrators can edit users." });

        var actorId    = GetCurrentUserId();
        var actorName  = User.FindFirst("name")?.Value ?? User.FindFirst(ClaimTypes.Name)?.Value ?? "Admin";
        var actorEmail = User.FindFirst("email")?.Value ?? User.FindFirst(ClaimTypes.Email)?.Value ?? "";
        var actorRole  = GetActorRole();

        var result = await _authService.UpdateUserAsync(userId, dto, actorId, actorName, actorEmail, actorRole);
        return Ok(result);
    }

    // GET api/auth/users  — Admin only
    [HttpGet("users")]
    [Authorize]
    public async Task<IActionResult> GetAllUsers()
    {
        if (!IsAdmin()) return StatusCode(403, new { message = "Only administrators can view all users." });
        return Ok(await _authService.GetAllUsersAsync());
    }

    // GET api/auth/roles
    [HttpGet("roles")]
    [Authorize]
    public async Task<IActionResult> GetRoles() =>
        Ok(await _authService.GetRolesAsync());

    // GET api/auth/stats  — Admin only
    [HttpGet("stats")]
    [Authorize]
    public async Task<IActionResult> GetStats()
    {
        if (!IsAdmin()) return StatusCode(403, new { message = "Forbidden." });
        return Ok(await _authService.GetDashboardStatsAsync());
    }

    // GET api/auth/sessions  — Admin only
    [HttpGet("sessions")]
    [Authorize]
    public async Task<IActionResult> GetActiveSessions()
    {
        if (!IsAdmin()) return StatusCode(403, new { message = "Forbidden." });
        return Ok(await _authService.GetActiveSessionsAsync());
    }

    // GET api/auth/audit-logs  — Admin only
    [HttpGet("audit-logs")]
    [Authorize]
    public async Task<IActionResult> GetAuditLogs()
    {
        if (!IsAdmin()) return StatusCode(403, new { message = "Forbidden." });
        return Ok(await _authService.GetAuditLogsAsync());
    }

    // ── Helpers ──────────────────────────────────────────────
    private Guid GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
        return claim != null ? Guid.Parse(claim.Value) : Guid.Empty;
    }

    private string GetActorRole() =>
        User.FindFirst("role")?.Value ?? User.FindFirst(ClaimTypes.Role)?.Value ?? "";

    private bool IsAdmin()
    {
        var role = GetActorRole();
        return role == "ADMIN" || role == "SYSTEM_ADMIN";
    }
}
