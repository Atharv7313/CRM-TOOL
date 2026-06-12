using PatientService.Data.Entities;

namespace PatientService.Repository;

public interface IEnrollmentRepository
{
    // ── Enrollment CRUD ──────────────────────────────────────
    Task<IEnumerable<PatientEnrollment>> GetAllAsync();
    Task<PatientEnrollment?> GetByIdAsync(Guid id);
    Task<bool> HasActiveEnrollmentAsync(Guid patientId);
    Task<bool> HasEnrolledInProtocolAsync(Guid patientId, Guid protocolId);
    Task<PatientEnrollment?> GetActiveByPatientAsync(Guid patientId);
    Task<PatientEnrollment> CreateAsync(PatientEnrollment enrollment);
    Task UpdateAsync(PatientEnrollment enrollment);

    // ── Protocol & Site lookups ──────────────────────────────
    Task<Protocol?> GetProtocolByIdAsync(Guid protocolId);
    Task<ProtocolSite?> GetProtocolSiteByIdAsync(Guid protocolSiteId);
    Task<IEnumerable<Protocol>> GetActiveProtocolsAsync();
    Task<IEnumerable<ProtocolSite>> GetActiveSitesByProtocolAsync(Guid protocolId);

    // ── Aggregate queries ────────────────────────────────────
    Task<int> GetActivePatientCountAsync(Guid protocolId);
    Task<IEnumerable<PatientEnrollment>> GetActiveByProtocolAsync(Guid protocolId);
}
