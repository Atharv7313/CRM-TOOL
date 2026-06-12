using Shared.DTOs;

namespace PatientService.Services;

public interface IPatientService
{
    Task<IEnumerable<PatientDto>> GetAllAsync();
    Task<PatientDto?> GetByIdAsync(Guid id);
    Task<(bool Success, string? Error, PatientDto? Data)> CreateAsync(
        string name, DateOnly dateOfBirth, string? contactInfo);
    Task<(bool Success, string? Error, PatientDto? Data)> UpdateAsync(
        Guid id, string name, DateOnly dateOfBirth, string? contactInfo);

    // ✅ NEW
    Task<(bool Success, string? Error)> DeactivateAsync(Guid id);
}