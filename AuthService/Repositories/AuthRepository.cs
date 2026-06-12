using AuthService.Data;
using AuthService.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Repositories;

public class AuthRepository : IAuthRepository
{
    private readonly ServicesDbContext _context;
    public AuthRepository(ServicesDbContext context) => _context = context;

    // ── Auth ────────────────────────────────────────────────
    public async Task<User?> GetUserByEmailAsync(string email) =>
        await _context.Users.Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Email == email && u.IsActive);

    public async Task<UserSession> CreateSessionAsync(UserSession session)
    {
        _context.UserSessions.Add(session);
        await _context.SaveChangesAsync();
        return session;
    }

    public async Task<UserSession?> GetSessionByJtiAsync(string jti) =>
        await _context.UserSessions.FirstOrDefaultAsync(s => s.TokenJti == jti);

    public async Task RevokeSessionAsync(string jti)
    {
        var session = await _context.UserSessions.FirstOrDefaultAsync(s => s.TokenJti == jti);
        if (session != null) { session.IsRevoked = true; await _context.SaveChangesAsync(); }
    }

    public async Task RevokeAllUserSessionsAsync(Guid userId)
    {
        var sessions = await _context.UserSessions
            .Where(s => s.UserId == userId && !s.IsRevoked).ToListAsync();
        sessions.ForEach(s => s.IsRevoked = true);
        await _context.SaveChangesAsync();
    }

    // ── Password reset ───────────────────────────────────────
    public async Task<bool> UpdatePasswordAsync(string email, string newPasswordHash)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email && u.IsActive);
        if (user == null) return false;
        user.PasswordHash = newPasswordHash;
        await _context.SaveChangesAsync();
        return true;
    }

    // ── Enrollment ───────────────────────────────────────────
    public async Task<bool> EmailExistsAsync(string email) =>
        await _context.Users.AnyAsync(u => u.Email == email);

    public async Task<Role?> GetRoleByNameAsync(string roleName) =>
        await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == roleName && r.IsActive);

    public async Task<List<Role>> GetAllRolesAsync() =>
        await _context.Roles
            .Where(r => r.IsActive && r.RoleName != "PATIENT" && r.RoleName != "SYSTEM_ADMIN")
            .OrderBy(r => r.RoleName).ToListAsync();

    public async Task<User> CreateUserAsync(User user)
    {
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user;
    }

    public async Task<User?> GetUserByIdAsync(Guid userId) =>
        await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.UserId == userId);

    public async Task<User> UpdateUserAsync(User user)
    {
        // Entity is already tracked by this context (loaded via GetUserByIdAsync).
        // Calling Update() on a tracked entity causes FK/nav-property conflicts.
        // Change tracking detects the property changes automatically.
        await _context.SaveChangesAsync();
        return user;
    }

    public async Task<List<User>> GetAllUsersAsync() =>
        await _context.Users.Include(u => u.Role)
            .Where(u => u.Role.RoleName != "PATIENT")
            .OrderBy(u => u.CreatedAt).ToListAsync();

    // ── Real sessions ────────────────────────────────────────
    public async Task<List<UserSession>> GetActiveSessionsAsync() =>
        await _context.UserSessions
            .Include(s => s.User).ThenInclude(u => u.Role)
            .Where(s => !s.IsRevoked && s.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();

    // ── Stats ────────────────────────────────────────────────
    public async Task<int> GetActiveUserCountAsync() =>
        await _context.Users.CountAsync(u => u.IsActive);

    public async Task<int> GetTotalEnrolledCountAsync() =>
        await _context.Users.CountAsync();

    public async Task<int> GetActiveSessionCountAsync() =>
        await _context.UserSessions.CountAsync(s => !s.IsRevoked && s.ExpiresAt > DateTime.UtcNow);

    public async Task<int> GetAuditEventsTodayCountAsync()
    {
        var today = DateTime.UtcNow.Date;
        return await _context.AuditLogs.CountAsync(a => a.CreatedAt >= today);
    }

    // ── Audit log ────────────────────────────────────────────
    public async Task AddAuditLogAsync(AuditLog log)
    {
        _context.AuditLogs.Add(log);
        await _context.SaveChangesAsync();
    }

    public async Task<List<AuditLog>> GetAuditLogsAsync(int take = 100) =>
        await _context.AuditLogs
            .OrderByDescending(a => a.CreatedAt)
            .Take(take)
            .ToListAsync();
}
