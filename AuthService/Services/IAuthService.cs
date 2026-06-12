using Shared.DTOs;

namespace AuthService.Services;

public interface IAuthService
{
    Task<LoginResponseDto> LoginAsync(LoginRequestDto request, string? ipAddress, string? userAgent);
    Task LogoutAsync(string jti, Guid userId, string? ipAddress);
    Task LogoutAllAsync(Guid userId);
    Task ResetPasswordAsync(ResetPasswordDto dto, string? ipAddress);

    // Enrollment
    Task<EnrollUserResponseDto> EnrollUserAsync(EnrollUserDto dto, Guid actorUserId, string actorName, string actorEmail);
    Task<EnrollUserResponseDto> UpdateUserAsync(Guid userId, UpdateUserDto dto, Guid actorUserId, string actorName, string actorEmail, string actorRole);
    Task<List<EnrollUserResponseDto>> GetAllUsersAsync();
    Task<List<RoleDto>> GetRolesAsync();

    // Dashboard
    Task<DashboardStatsDto> GetDashboardStatsAsync();
    Task<List<ActiveSessionDto>> GetActiveSessionsAsync();

    // Audit
    Task<List<AuditLogDto>> GetAuditLogsAsync();
}
