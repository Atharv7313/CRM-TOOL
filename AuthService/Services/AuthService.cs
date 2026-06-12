using AuthService.Data.Entities;
using AuthService.Helpers;
using AuthService.Repositories;
using Shared.CL.DTOs;
using Shared.CL.Services;
using Shared.DTOs;

namespace AuthService.Services;

public class AuthService : IAuthService
{
    private readonly IAuthRepository _authRepo;
    private readonly JwtHelper       _jwtHelper;
    private readonly IConfiguration  _config;
    private readonly IAuditClient    _audit;

    public AuthService(
        IAuthRepository authRepo,
        JwtHelper jwtHelper,
        IConfiguration config,
        IAuditClient audit)
    {
        _authRepo  = authRepo;
        _jwtHelper = jwtHelper;
        _config    = config;
        _audit     = audit;
    }

    // ── Login ────────────────────────────────────────────────
    public async Task<LoginResponseDto> LoginAsync(LoginRequestDto request, string? ipAddress, string? userAgent)
    {
        var user = await _authRepo.GetUserByEmailAsync(request.Email);
        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            await _authRepo.AddAuditLogAsync(new AuditLog
            {
                ActorName   = "Unknown",
                ActorEmail  = request.Email,
                Action      = "LOGIN_FAILED",
                Description = $"Failed login attempt for {request.Email}",
                IpAddress   = ipAddress,
                IsSuccess   = false,
                CreatedAt   = DateTime.UtcNow
            });

            _audit.Log(new AuditLogCreateDto
            {
                ActorName   = "Unknown",
                ActorEmail  = request.Email,
                Action      = "LOGIN_FAILED",
                ServiceName = "AuthService",
                Description = $"Failed login attempt for {request.Email}",
                IpAddress   = ipAddress,
                IsSuccess   = false
            });

            throw new UnauthorizedAccessException("Invalid email or password");
        }

        var token   = _jwtHelper.GenerateToken(user.UserId, user.Email, user.Role.RoleName);
        var jti     = _jwtHelper.GetJtiFromToken(token);
        var expMins = _config.GetValue<int>("Jwt:ExpiryMinutes", 60);

        await _authRepo.CreateSessionAsync(new UserSession
        {
            UserId    = user.UserId,
            TokenJti  = jti,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            IsRevoked = false,
            ExpiresAt = DateTime.UtcNow.AddMinutes(expMins)
        });

        await _authRepo.AddAuditLogAsync(new AuditLog
        {
            ActorUserId = user.UserId,
            ActorName   = user.Name,
            ActorEmail  = user.Email,
            Action      = "LOGIN",
            Description = $"{user.Name} ({user.Email}) logged in",
            IpAddress   = ipAddress,
            IsSuccess   = true,
            CreatedAt   = DateTime.UtcNow
        });

        _audit.Log(new AuditLogCreateDto
        {
            ActorUserId = user.UserId,
            ActorName   = user.Name,
            ActorEmail  = user.Email,
            Action      = "LOGIN",
            ServiceName = "AuthService",
            Description = $"{user.Name} ({user.Email}) logged in",
            IpAddress   = ipAddress
        });

        return new LoginResponseDto
        {
            Token     = token,
            UserId    = user.UserId,
            Name      = user.Name,
            Email     = user.Email,
            Role      = user.Role.RoleName,
            ExpiresIn = expMins * 60
        };
    }

    // ── Logout ───────────────────────────────────────────────
    public async Task LogoutAsync(string jti, Guid userId, string? ipAddress)
    {
        var session = await _authRepo.GetSessionByJtiAsync(jti);
        await _authRepo.RevokeSessionAsync(jti);

        if (session?.User != null)
        {
            await _authRepo.AddAuditLogAsync(new AuditLog
            {
                ActorUserId = userId,
                ActorName   = session.User.Name,
                ActorEmail  = session.User.Email,
                Action      = "LOGOUT",
                Description = $"{session.User.Name} ({session.User.Email}) logged out",
                IpAddress   = ipAddress,
                IsSuccess   = true,
                CreatedAt   = DateTime.UtcNow
            });

            _audit.Log(new AuditLogCreateDto
            {
                ActorUserId = userId,
                ActorName   = session.User.Name,
                ActorEmail  = session.User.Email,
                Action      = "LOGOUT",
                ServiceName = "AuthService",
                Description = $"{session.User.Name} ({session.User.Email}) logged out",
                IpAddress   = ipAddress
            });
        }
    }

    public async Task LogoutAllAsync(Guid userId) =>
        await _authRepo.RevokeAllUserSessionsAsync(userId);

    // ── Reset password ───────────────────────────────────────
    public async Task ResetPasswordAsync(ResetPasswordDto dto, string? ipAddress)
    {
        var user = await _authRepo.GetUserByEmailAsync(dto.Email);
        if (user == null)
            throw new KeyNotFoundException("No active account found with that email.");

        var newHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        await _authRepo.UpdatePasswordAsync(dto.Email, newHash);
        await _authRepo.RevokeAllUserSessionsAsync(user.UserId);

        await _authRepo.AddAuditLogAsync(new AuditLog
        {
            ActorUserId = user.UserId,
            ActorName   = user.Name,
            ActorEmail  = user.Email,
            Action      = "PASSWORD_RESET",
            Description = $"{user.Name} ({user.Email}) reset their password",
            IpAddress   = ipAddress,
            IsSuccess   = true,
            CreatedAt   = DateTime.UtcNow
        });

        _audit.Log(new AuditLogCreateDto
        {
            ActorUserId = user.UserId,
            ActorName   = user.Name,
            ActorEmail  = user.Email,
            Action      = "PASSWORD_RESET",
            ServiceName = "AuthService",
            Description = $"{user.Name} ({user.Email}) reset their password",
            IpAddress   = ipAddress
        });
    }

    // ── Enroll ───────────────────────────────────────────────
    public async Task<EnrollUserResponseDto> EnrollUserAsync(
        EnrollUserDto dto, Guid actorUserId, string actorName, string actorEmail)
    {
        if (await _authRepo.EmailExistsAsync(dto.Email))
            throw new InvalidOperationException($"A user with email '{dto.Email}' already exists.");

        var role = await _authRepo.GetRoleByNameAsync(dto.RoleName)
            ?? throw new InvalidOperationException($"Role '{dto.RoleName}' not found.");

        var user = new User
        {
            Name         = dto.Name,
            Email        = dto.Email,
            Phone        = dto.Phone,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            RoleId       = role.RoleId,
            IsActive     = true,
            CreatedAt    = DateTime.UtcNow
        };

        var created = await _authRepo.CreateUserAsync(user);

        await _authRepo.AddAuditLogAsync(new AuditLog
        {
            ActorUserId    = actorUserId,
            ActorName      = actorName,
            ActorEmail     = actorEmail,
            Action         = "USER_ENROLLED",
            Description    = $"{actorName} enrolled {dto.Name} ({dto.Email}) as {dto.RoleName}",
            TargetUserId   = created.UserId,
            TargetUserName = dto.Name,
            IsSuccess      = true,
            CreatedAt      = DateTime.UtcNow
        });

        _audit.Log(new AuditLogCreateDto
        {
            ActorUserId = actorUserId,
            ActorName   = actorName,
            ActorEmail  = actorEmail,
            Action      = "USER_ENROLLED",
            ServiceName = "AuthService",
            Description = $"{actorName} enrolled {dto.Name} ({dto.Email}) as {dto.RoleName}",
            EntityId    = created.UserId.ToString(),
            EntityName  = dto.Name
        });

        return new EnrollUserResponseDto
        {
            UserId    = created.UserId,
            Name      = created.Name,
            Email     = created.Email,
            Phone     = created.Phone,
            Role      = role.RoleName,
            IsActive  = created.IsActive,
            CreatedAt = created.CreatedAt
        };
    }

    // ── Update user ──────────────────────────────────────────
    public async Task<EnrollUserResponseDto> UpdateUserAsync(
        Guid userId, UpdateUserDto dto, Guid actorUserId, string actorName, string actorEmail, string actorRole)
    {
        var user = await _authRepo.GetUserByIdAsync(userId)
            ?? throw new KeyNotFoundException($"User '{userId}' not found.");

        if (user.Role.RoleName == "SYSTEM_ADMIN")
            throw new UnauthorizedAccessException("System admin cannot be edited.");

        var role = await _authRepo.GetRoleByNameAsync(dto.RoleName)
            ?? throw new InvalidOperationException($"Role '{dto.RoleName}' not found.");

        var oldRole   = user.Role.RoleName;
        user.Name     = dto.Name;
        user.Phone    = dto.Phone;
        user.RoleId   = role.RoleId;
        user.IsActive = dto.IsActive;

        await _authRepo.UpdateUserAsync(user);

        await _authRepo.AddAuditLogAsync(new AuditLog
        {
            ActorUserId    = actorUserId,
            ActorName      = actorName,
            ActorEmail     = actorEmail,
            Action         = "USER_UPDATED",
            Description    = $"{actorName} updated {user.Name} ({user.Email}) — role: {oldRole} → {role.RoleName}, active: {dto.IsActive}",
            TargetUserId   = user.UserId,
            TargetUserName = user.Name,
            IsSuccess      = true,
            CreatedAt      = DateTime.UtcNow
        });

        _audit.Log(new AuditLogCreateDto
        {
            ActorUserId = actorUserId,
            ActorName   = actorName,
            ActorEmail  = actorEmail,
            Action      = "USER_UPDATED",
            ServiceName = "AuthService",
            Description = $"{actorName} updated {user.Name} ({user.Email}) — role: {oldRole} → {role.RoleName}, active: {dto.IsActive}",
            EntityId    = user.UserId.ToString(),
            EntityName  = user.Name
        });

        return new EnrollUserResponseDto
        {
            UserId    = user.UserId,
            Name      = user.Name,
            Email     = user.Email,
            Phone     = user.Phone,
            Role      = role.RoleName,
            IsActive  = user.IsActive,
            CreatedAt = user.CreatedAt
        };
    }

    // ── Users / Roles ────────────────────────────────────────
    public async Task<List<EnrollUserResponseDto>> GetAllUsersAsync()
    {
        var users = await _authRepo.GetAllUsersAsync();
        return users.Select(u => new EnrollUserResponseDto
        {
            UserId    = u.UserId,
            Name      = u.Name,
            Email     = u.Email,
            Phone     = u.Phone,
            Role      = u.Role.RoleName,
            IsActive  = u.IsActive,
            CreatedAt = u.CreatedAt
        }).ToList();
    }

    public async Task<List<RoleDto>> GetRolesAsync()
    {
        var roles = await _authRepo.GetAllRolesAsync();
        return roles.Select(r => new RoleDto { RoleId = r.RoleId, RoleName = r.RoleName }).ToList();
    }

    // ── Dashboard stats ──────────────────────────────────────
    public async Task<DashboardStatsDto> GetDashboardStatsAsync() =>
        new DashboardStatsDto
        {
            ActiveUsers      = await _authRepo.GetActiveUserCountAsync(),
            TotalEnrolled    = await _authRepo.GetTotalEnrolledCountAsync(),
            ActiveSessions   = await _authRepo.GetActiveSessionCountAsync(),
            AuditEventsToday = await _authRepo.GetAuditEventsTodayCountAsync()
        };

    // ── Active sessions ──────────────────────────────────────
    public async Task<List<ActiveSessionDto>> GetActiveSessionsAsync()
    {
        var sessions = await _authRepo.GetActiveSessionsAsync();
        return sessions.Select(s => new ActiveSessionDto
        {
            Name      = s.User.Name,
            Email     = s.User.Email,
            Role      = s.User.Role.RoleName,
            IpAddress = s.IpAddress,
            LoginTime = s.CreatedAt,
            ExpiresAt = s.ExpiresAt,
            Status    = "Active"
        }).ToList();
    }

    // ── Audit logs (AuthService local store — kept for backward compat) ───────
    public async Task<List<AuditLogDto>> GetAuditLogsAsync()
    {
        var logs = await _authRepo.GetAuditLogsAsync(100);
        return logs.Select(l => new AuditLogDto
        {
            LogId          = l.LogId,
            ActorName      = l.ActorName,
            ActorEmail     = l.ActorEmail,
            Action         = l.Action,
            Description    = l.Description,
            TargetUserName = l.TargetUserName,
            IpAddress      = l.IpAddress,
            IsSuccess      = l.IsSuccess,
            CreatedAt      = l.CreatedAt
        }).ToList();
    }
}
