using ProtocolService.Data.Entities;

namespace ProtocolService.Repositories;

public interface IProtocolRepository
{
    Task<bool> TitlePhaseExistsAsync(string title, string phase);
    Task<bool> TitlePhaseExistsAsync(string title, string phase, Guid excludeId);
    Task<Protocol?> GetByIdAsync(Guid id);
    Task<List<Protocol>> GetAllAsync(string? status, string? phase, string? title);
    Task AddAsync(Protocol protocol);
    Task SaveChangesAsync();
    Task CancelEnrollmentsForProtocolAsync(Guid protocolId);
}
