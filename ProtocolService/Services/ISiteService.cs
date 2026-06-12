using Shared.CL.DTOs;

namespace ProtocolService.Services;

public interface ISiteService
{
    Task<SiteResponseDto> CreateAsync(CreateSiteDto dto);
    Task<SiteResponseDto?> GetByIdAsync(Guid id);
    Task<List<SiteResponseDto>> GetAllAsync(string? name, string? location);
    Task<SiteResponseDto> UpdateAsync(Guid id, UpdateSiteDto dto);
    Task<SiteResponseDto> SoftDeleteAsync(Guid id);
}