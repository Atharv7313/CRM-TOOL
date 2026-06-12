using AuthService.Data.Entities;

namespace AuthService.Repositories;

public interface IAuthRepository
{
    // Auth
    Task<User?> GetUserByEmailAsync(string email);
    Task<UserSession> CreateSessionAsync(UserSession session);
    Task<UserSession?> GetSessionByJtiAsync(string jti);
    Task RevokeSessionAsync(string jti);
    Task RevokeAllUserSessionsAsync(Guid userId);

    // Password reset
    Task<bool> UpdatePasswordAsync(string email, string newPasswordHash);

    // Enrollment
    Task<bool> EmailExistsAsync(string email);
    Task<Role?> GetRoleByNameAsync(string roleName);
    Task<List<Role>> GetAllRolesAsync();
    Task<User> CreateUserAsync(User user);
    Task<User?> GetUserByIdAsync(Guid userId);
    Task<User> UpdateUserAsync(User user);
    Task<List<User>> GetAllUsersAsync();

    // Sessions (real)
    Task<List<UserSession>> GetActiveSessionsAsync();

    // Stats
    Task<int> GetActiveUserCountAsync();
    Task<int> GetTotalEnrolledCountAsync();
    Task<int> GetActiveSessionCountAsync();
    Task<int> GetAuditEventsTodayCountAsync();

    // Audit log
    Task AddAuditLogAsync(AuditLog log);
    Task<List<AuditLog>> GetAuditLogsAsync(int take = 100);
}
