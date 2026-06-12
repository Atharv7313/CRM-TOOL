using Shared.DTOs;

namespace PatientService.Services;

public interface IEnrollmentService
{
    Task<IEnumerable<EnrollmentDto>> GetAllAsync();
    Task<EnrollmentDto?> GetByIdAsync(Guid id);
    Task<(bool Success, string? Error, EnrollmentDto? Data)> EnrollAsync(
        Guid patientId, Guid protocolSiteId);
    Task<(bool Success, string? Error)> WithdrawAsync(Guid id);
    Task<IEnumerable<ProtocolDto>> GetProtocolsAsync();
    Task<IEnumerable<ProtocolSiteDto>> GetSitesByProtocolAsync(Guid protocolId);
    Task<int> GetActivePatientCountAsync(Guid protocolId);
}
