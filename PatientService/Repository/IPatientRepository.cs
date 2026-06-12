using PatientService.Data.Entities;

namespace PatientService.Repository;

public interface IPatientRepository
{
    Task<IEnumerable<Patient>> GetAllAsync();
    Task<Patient?> GetByIdAsync(Guid id);
    Task<Patient> CreateAsync(Patient patient);
    Task UpdateAsync(Patient patient);
    Task<bool> EmailExistsAsync(string email, Guid? excludeId = null);

    /// Returns the patient with the given email (any status), or null if not found.
    /// The service decides whether to block based on the patient's status.
    Task<Patient?> GetByEmailAsync(string email, Guid? excludeId = null);
}