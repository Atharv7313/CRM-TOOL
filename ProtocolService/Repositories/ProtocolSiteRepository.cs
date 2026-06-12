using Microsoft.EntityFrameworkCore;
using ProtocolService.Data;
using ProtocolService.Data.Entities;
using ProtocolService.Enums;

namespace ProtocolService.Repositories;

public class ProtocolSiteRepository : IProtocolSiteRepository
{
    private readonly ProtocolDbContext _db;

    public ProtocolSiteRepository(ProtocolDbContext db)
    {
        _db = db;
    }

    public async Task<Protocol?> GetProtocolAsync(Guid protocolId)
    {
        return await _db.Protocols
            .FirstOrDefaultAsync(p => p.ProtocolId == protocolId
                                   && p.Status != ProtocolStatus.Deleted);
    }

    public async Task<Site?> GetSiteAsync(Guid siteId)
    {
        return await _db.Sites
            .FirstOrDefaultAsync(s => s.SiteId == siteId && s.IsActive);
    }

    public async Task<User?> GetUserAsync(Guid userId)
    {
        return await _db.Users
            .FirstOrDefaultAsync(u => u.UserId == userId && u.IsActive);
    }

    public async Task<ProtocolSite?> GetExistingAssignmentAsync(Guid protocolId, Guid siteId)
    {
        return await _db.ProtocolSites
            .Include(x => x.Protocol)
            .Include(x => x.Site)
            .FirstOrDefaultAsync(x => x.ProtocolId == protocolId && x.SiteId == siteId);
    }

    public async Task<ProtocolSite?> GetAssignmentByIdAsync(Guid assignmentId, Guid protocolId)
    {
        return await _db.ProtocolSites
            .Include(x => x.Protocol)
            .Include(x => x.Site)
            .FirstOrDefaultAsync(x => x.ProtocolSiteId == assignmentId
                                   && x.ProtocolId == protocolId);
    }

    public async Task<List<ProtocolSite>> GetByProtocolAsync(Guid protocolId)
    {
        return await _db.ProtocolSites
            .Include(ps => ps.Protocol)
            .Include(ps => ps.Site)
            .Where(ps => ps.ProtocolId == protocolId)
            .ToListAsync();
    }

    public async Task<List<ProtocolSite>> GetBySiteAsync(Guid siteId)
    {
        return await _db.ProtocolSites
            .Include(ps => ps.Protocol)
            .Include(ps => ps.Site)
            .Where(ps => ps.SiteId == siteId)
            .ToListAsync();
    }

    // Returns null if user not found â€” fallback display logic belongs in the service layer
    public async Task<string?> GetInvestigatorNameAsync(Guid userId)
    {
        return await _db.Users
            .Where(u => u.UserId == userId)
            .Select(u => u.Name)
            .FirstOrDefaultAsync();
    }

    public async Task<Dictionary<Guid, string>> GetInvestigatorNamesAsync(List<Guid> userIds)
    {
        var users = await _db.Users
            .Where(u => userIds.Contains(u.UserId))
            .Select(u => new { u.UserId, u.Name })
            .ToListAsync();

        return users.ToDictionary(u => u.UserId, u => u.Name);
    }

    // Returns all active RESEARCH_SCIENTIST users for investigator dropdown
    public async Task<List<User>> GetAllUsersAsync()
    {
        return await _db.Users
            .Include(u => u.Role)
            .Where(u => u.IsActive && u.Role != null && u.Role.RoleName == "RESEARCH_SCIENTIST")
            .OrderBy(u => u.Name)
            .ToListAsync();
    }

    public async Task AddAssignmentAsync(ProtocolSite assignment)
    {
        await _db.ProtocolSites.AddAsync(assignment);
    }

    public async Task SaveChangesAsync()
    {
        await _db.SaveChangesAsync();
    }
}

