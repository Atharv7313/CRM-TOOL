using ProtocolService.Data.Entities;

namespace ProtocolService.Repositories;

public interface ISiteRepository
{
    Task<bool> NameLocationExistsAsync(string name, string? location);
    Task<bool> NameLocationExistsAsync(string name, string? location, Guid excludeId);
    Task<Site?> GetByIdAsync(Guid id);
    Task<List<Site>> GetAllAsync(string? name, string? location);
    Task AddAsync(Site site);
    Task SaveChangesAsync();
}
