using Microsoft.EntityFrameworkCore;
using ProtocolService.Data;
using ProtocolService.Data.Entities;
using ProtocolService.Enums;

namespace ProtocolService.Repositories;

public class SiteRepository : ISiteRepository
{
    private readonly ProtocolDbContext _db;

    public SiteRepository(ProtocolDbContext db)
    {
        _db = db;
    }

    public async Task<bool> NameLocationExistsAsync(string name, string? location)
    {
        return await _db.Sites
            .AnyAsync(s => s.IsActive
                        && s.Name == name
                        && (s.Location ?? "") == (location ?? ""));
    }

    public async Task<bool> NameLocationExistsAsync(string name, string? location, Guid excludeId)
    {
        return await _db.Sites
            .AnyAsync(s => s.IsActive
                        && s.Name == name
                        && (s.Location ?? "") == (location ?? "")
                        && s.SiteId != excludeId);
    }

    public async Task<Site?> GetByIdAsync(Guid id)
    {
        return await _db.Sites
            .Include(s => s.ProtocolSites)
            .FirstOrDefaultAsync(s => s.SiteId == id && s.IsActive);
    }

    public async Task<List<Site>> GetAllAsync(string? name, string? location)
    {
        var query = _db.Sites
            .Include(s => s.ProtocolSites)
            .Where(s => s.IsActive);

        if (!string.IsNullOrWhiteSpace(name))
            query = query.Where(s => EF.Functions.Like(s.Name, "%" + name + "%"));

        if (!string.IsNullOrWhiteSpace(location))
            query = query.Where(s => EF.Functions.Like(s.Location!, "%" + location + "%"));

        return await query.OrderByDescending(s => s.SiteId).ToListAsync();
    }

    public async Task AddAsync(Site site)
    {
        await _db.Sites.AddAsync(site);
    }

    public async Task SaveChangesAsync()
    {
        await _db.SaveChangesAsync();
    }
}

