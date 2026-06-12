using Shared.CL.DTOs;

namespace ProtocolService.Services;

public interface IProtocolService
{
    Task<ProtocolResponseDto> CreateAsync(CreateProtocolDto dto, Guid createdByUserId);
    Task<ProtocolResponseDto?> GetByIdAsync(Guid id);
    Task<List<ProtocolResponseDto>> GetAllAsync(string? status, string? phase, string? title);
    Task<ProtocolResponseDto> UpdateAsync(Guid id, UpdateProtocolDto dto);
    Task<ProtocolResponseDto> UpdateStatusAsync(Guid id, UpdateProtocolStatusDto dto);
    Task<ProtocolResponseDto> SoftDeleteAsync(Guid id);
}