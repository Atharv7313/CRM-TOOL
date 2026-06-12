using Shared.CL.DTOs;

namespace SampleService.Services;

public interface ILabResultService
{
    Task<IList<LabResultListDto>> GetBySampleAsync(Guid sampleId);
    Task<LabResultListDto?> GetByIdAsync(Guid id);
    Task<LabResultListDto> CreateAsync(LabResultCreateDto dto);
    Task<LabResultListDto?> UpdateAsync(Guid id, LabResultUpdateDto dto);
    Task<bool> DeleteAsync(Guid id);
}
