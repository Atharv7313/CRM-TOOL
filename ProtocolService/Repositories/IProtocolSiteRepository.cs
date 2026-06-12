using ProtocolService.Data.Entities;

namespace ProtocolService.Repositories;

public interface IProtocolSiteRepository
{
    Task<Protocol?> GetProtocolAsync(Guid protocolId);
    Task<Site?> GetSiteAsync(Guid siteId);
    Task<User?> GetUserAsync(Guid userId);
    Task<ProtocolSite?> GetExistingAssignmentAsync(Guid protocolId, Guid siteId);
    Task<ProtocolSite?> GetAssignmentByIdAsync(Guid assignmentId, Guid protocolId);
    Task<List<ProtocolSite>> GetByProtocolAsync(Guid protocolId);
    Task<List<ProtocolSite>> GetBySiteAsync(Guid siteId);
    Task<string?> GetInvestigatorNameAsync(Guid userId);
    Task<Dictionary<Guid, string>> GetInvestigatorNamesAsync(List<Guid> userIds);
    Task<List<User>> GetAllUsersAsync();
    Task AddAssignmentAsync(ProtocolSite assignment);
    Task SaveChangesAsync();
}
