using Shared.CL.DTOs;

namespace SampleService.Services;

public interface ISampleService
{
    Task<IList<SampleListDto>> GetAllAsync();
    Task<SampleListDto?> GetByIdAsync(Guid id);
    Task<IList<SampleListDto>> GetByEnrollmentAsync(Guid enrollmentId);
    Task<SampleListDto> CreateAsync(SampleCreateDto dto);
    Task<SampleListDto?> UpdateAsync(Guid id, SampleUpdateDto dto);
    Task<bool> UpdateStatusAsync(Guid id, string status);
    Task<bool> DeleteAsync(Guid id);
}
