using ProtocolService.DTOs;
using Shared.CL.DTOs;

namespace ProtocolService.Services;

public interface IProtocolSiteService
{
    Task<ProtocolSiteResponseDto> AssignAsync(Guid protocolId, AssignSiteDto dto);
    Task<List<ProtocolSiteResponseDto>> GetByProtocolAsync(Guid protocolId);
    Task<List<ProtocolSiteResponseDto>> GetBySiteAsync(Guid siteId);
    Task<ProtocolSiteResponseDto> UpdateStatusAsync(Guid protocolId, Guid assignmentId, UpdateProtocolSiteStatusDto dto);
    Task<ProtocolSiteResponseDto> RemoveAsync(Guid protocolId, Guid assignmentId);
    Task<List<InvestigatorDto>> GetInvestigatorsAsync();
}