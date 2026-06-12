using Shared.CL.DTOs;

namespace SampleService.Repositories;

public interface ISampleRepository
{
    Task<IList<SampleListDto>> GetAllAsync();
    Task<SampleListDto?> GetByIdAsync(Guid id);
    Task<IList<SampleListDto>> GetByEnrollmentAsync(Guid enrollmentId);
    Task<SampleListDto> CreateAsync(SampleCreateDto dto);
    Task<SampleListDto?> UpdateAsync(Guid id, SampleUpdateDto dto);
    Task<bool> DeleteAsync(Guid id);
    Task<bool> UpdateStatusAsync(Guid id, string status);
}
//list out all the operations