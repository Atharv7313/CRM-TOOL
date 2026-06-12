using PatientService.Data.Entities;

namespace PatientService.Repository;

public interface IVisitRepository
{
    Task<IEnumerable<Visit>> GetByEnrollmentIdAsync(Guid enrollmentId);
    Task<IEnumerable<Visit>> GetFilteredAsync(
        DateTime? date, Guid? protocolSiteId, string? status);
    Task<Visit?> GetByIdAsync(Guid id);
    Task<Visit> CreateAsync(Visit visit);
    Task BulkCreateAsync(IEnumerable<Visit> visits);
    Task UpdateAsync(Visit visit);
    Task BulkUpdateAsync(IEnumerable<Visit> visits);

    /// Returns all SCHEDULED/RESCHEDULED visits across all enrollments
    /// of the given protocol — used to copy template visits for new patients.
    Task<IEnumerable<Visit>> GetScheduledByProtocolAsync(Guid protocolId);
}