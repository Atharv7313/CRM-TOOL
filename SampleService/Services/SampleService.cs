using SampleService.Repositories;
using Shared.CL.DTOs;

namespace SampleService.Services;

public class SampleService : ISampleService
{
    private readonly ISampleRepository _repo;

    public SampleService(ISampleRepository repo) => _repo = repo;

    public Task<IList<SampleListDto>> GetAllAsync() =>
        _repo.GetAllAsync();

    public Task<SampleListDto?> GetByIdAsync(Guid id) =>
        _repo.GetByIdAsync(id);

    public Task<IList<SampleListDto>> GetByEnrollmentAsync(Guid enrollmentId) =>
        _repo.GetByEnrollmentAsync(enrollmentId);

    public Task<SampleListDto> CreateAsync(SampleCreateDto dto) =>
        _repo.CreateAsync(dto);

    public Task<SampleListDto?> UpdateAsync(Guid id, SampleUpdateDto dto) =>
        _repo.UpdateAsync(id, dto);

    public Task<bool> UpdateStatusAsync(Guid id, string status) =>
        _repo.UpdateStatusAsync(id, status);

    public Task<bool> DeleteAsync(Guid id) =>
        _repo.DeleteAsync(id);
}
