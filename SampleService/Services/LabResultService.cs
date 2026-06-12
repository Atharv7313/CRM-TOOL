using SampleService.Repositories;
using Shared.CL.DTOs;

namespace SampleService.Services;

public class LabResultService : ILabResultService
{
    private readonly ILabResultRepository _repo;

    public LabResultService(ILabResultRepository repo) => _repo = repo;

    public Task<IList<LabResultListDto>> GetBySampleAsync(Guid sampleId) =>
        _repo.GetBySampleAsync(sampleId);

    public Task<LabResultListDto?> GetByIdAsync(Guid id) =>
        _repo.GetByIdAsync(id);

    public Task<LabResultListDto> CreateAsync(LabResultCreateDto dto) =>
        _repo.CreateAsync(dto);

    public Task<LabResultListDto?> UpdateAsync(Guid id, LabResultUpdateDto dto) =>
        _repo.UpdateAsync(id, dto);

    public Task<bool> DeleteAsync(Guid id) =>
        _repo.DeleteAsync(id);
}
