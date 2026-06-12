using PatientService.Data.Entities;
using PatientService.Repository;
using Shared.DTOs;

namespace PatientService.Services;

public class PatientService : IPatientService
{
    private readonly IPatientRepository    _repo;
    private readonly IEnrollmentRepository _enrollRepo;
    private readonly IVisitRepository      _visitRepo;

    public PatientService(
        IPatientRepository    repo,
        IEnrollmentRepository enrollRepo,
        IVisitRepository      visitRepo)
    {
        _repo       = repo;
        _enrollRepo = enrollRepo;
        _visitRepo  = visitRepo;
    }

    public async Task<IEnumerable<PatientDto>> GetAllAsync()
    {
        var patients = await _repo.GetAllAsync();
        return patients.Select(MapToDto);
    }

    public async Task<PatientDto?> GetByIdAsync(Guid id)
    {
        var patient = await _repo.GetByIdAsync(id);
        return patient is null ? null : MapToDto(patient);
    }

    public async Task<(bool Success, string? Error, PatientDto? Data)> CreateAsync(
        string name, DateOnly dateOfBirth, string? contactInfo)
    {
        // ── Field-level validation ───────────────────────────────────────
        if (string.IsNullOrWhiteSpace(name))
            return (false, "Patient name is required.", null);

        if (dateOfBirth >= DateOnly.FromDateTime(DateTime.UtcNow))
            return (false, "Date of birth cannot be today or a future date.", null);

        if (!string.IsNullOrWhiteSpace(contactInfo))
        {
            if (!IsValidEmail(contactInfo))
                return (false, "Please provide a valid email address.", null);

            // Block duplicate email regardless of patient status
            var existing = await _repo.GetByEmailAsync(contactInfo);
            if (existing != null)
                return (false, "A patient with this email already exists.", null);
        }

        var patient = new Patient
        {
            Name          = name,
            DateOfBirth   = dateOfBirth,
            ContactInfo   = contactInfo,
            PatientStatus = "ACTIVE",
            CreatedAt     = DateTime.UtcNow
        };

        var created = await _repo.CreateAsync(patient);
        return (true, null, MapToDto(created));
    }

    public async Task<(bool Success, string? Error, PatientDto? Data)> UpdateAsync(
        Guid id, string name, DateOnly dateOfBirth, string? contactInfo)
    {
        if (string.IsNullOrWhiteSpace(name))
            return (false, "Patient name is required.", null);

        if (dateOfBirth >= DateOnly.FromDateTime(DateTime.UtcNow))
            return (false, "Date of birth cannot be today or a future date.", null);

        var patient = await _repo.GetByIdAsync(id);
        if (patient is null)
            return (false, "Patient not found.", null);

        if (!string.IsNullOrWhiteSpace(contactInfo))
        {
            if (!IsValidEmail(contactInfo))
                return (false, "Please provide a valid email address.", null);

            // Service decides: email is blocked only if another ACTIVE patient uses it
            var existing = await _repo.GetByEmailAsync(contactInfo, excludeId: id);
            if (existing?.PatientStatus == "ACTIVE")
                return (false, "Email already used by another patient.", null);
        }

        patient.Name        = name;
        patient.DateOfBirth = dateOfBirth;
        patient.ContactInfo = contactInfo;

        await _repo.UpdateAsync(patient);
        return (true, null, MapToDto(patient));
    }

    public async Task<(bool Success, string? Error)> DeactivateAsync(Guid id)
    {
        var patient = await _repo.GetByIdAsync(id);
        if (patient is null)
            return (false, "Patient not found.");

        if (patient.PatientStatus == "INACTIVE")
            return (false, "Patient is already inactive.");

        // Find active enrollment through EnrollmentRepository
        var activeEnrollment = await _enrollRepo.GetActiveByPatientAsync(id);
        if (activeEnrollment is not null)
        {
            // Cancel all scheduled/rescheduled visits through VisitRepository
            var visits = (await _visitRepo.GetByEnrollmentIdAsync(activeEnrollment.EnrollmentId))
                .Where(v => v.VisitStatus is "SCHEDULED" or "RESCHEDULED")
                .ToList();

            foreach (var v in visits)
                v.VisitStatus = "CANCELLED";

            if (visits.Any())
                await _visitRepo.BulkUpdateAsync(visits);

            // Withdraw enrollment through EnrollmentRepository
            activeEnrollment.EnrollmentStatus = "WITHDRAWN";
            await _enrollRepo.UpdateAsync(activeEnrollment);
        }

        patient.PatientStatus = "INACTIVE";
        await _repo.UpdateAsync(patient);
        return (true, null);
    }

    // ── Helpers ──────────────────────────────────────────────

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email.Trim();
        }
        catch
        {
            return false;
        }
    }

    // ── Mapping ──────────────────────────────────────────────

    private static PatientDto MapToDto(Patient p)
    {
        var enrollment = p.PatientEnrollments
            .OrderByDescending(e => e.EnrolledAt)
            .FirstOrDefault();

        string enrollmentStatus = p.PatientStatus == "INACTIVE"
            ? "Inactive"
            : enrollment?.EnrollmentStatus switch
            {
                "ACTIVE"    => "Active",
                "COMPLETED" => "Completed",
                "WITHDRAWN" => "Withdrawn",
                "CANCELLED" => "Cancelled",
                _           => "Not enrolled"
            };

        var previousProtocols = p.PatientEnrollments
            .Select(e => e.ProtocolSite?.Protocol?.Title ?? "")
            .Where(t => !string.IsNullOrEmpty(t))
            .Distinct()
            .ToList();

        return new PatientDto
        {
            PatientId        = p.PatientId,
            Name             = p.Name,
            DateOfBirth      = p.DateOfBirth,
            ContactInfo      = p.ContactInfo,
            PatientStatus    = p.PatientStatus,
            CreatedAt        = p.CreatedAt,
            EnrollmentStatus = enrollmentStatus,
            EnrollmentId     = enrollment?.EnrollmentId,
            ProtocolTitle    = enrollment?.ProtocolSite?.Protocol?.Title,
            SiteName         = enrollment?.ProtocolSite?.Site?.Name,
            EnrolledAt       = enrollment?.EnrolledAt,
            PreviousProtocols = previousProtocols
        };
    }
}
