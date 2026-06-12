using PatientService.Data.Entities;
using PatientService.Repository;
using Shared.DTOs;

namespace PatientService.Services;

public class EnrollmentService : IEnrollmentService
{
    private readonly IEnrollmentRepository _enrollRepo;
    private readonly IVisitRepository _visitRepo;

    public EnrollmentService(
        IEnrollmentRepository enrollRepo,
        IVisitRepository visitRepo)
    {
        _enrollRepo = enrollRepo;
        _visitRepo  = visitRepo;
    }

    public async Task<IEnumerable<EnrollmentDto>> GetAllAsync()
    {
        var list = await _enrollRepo.GetAllAsync();
        return list.Select(MapToDto);
    }

    public async Task<EnrollmentDto?> GetByIdAsync(Guid id)
    {
        var e = await _enrollRepo.GetByIdAsync(id);
        return e is null ? null : MapToDto(e);
    }

    public async Task<(bool Success, string? Error, EnrollmentDto? Data)> EnrollAsync(
        Guid patientId, Guid protocolSiteId)
    {
        if (await _enrollRepo.HasActiveEnrollmentAsync(patientId))
            return (false, "Patient already has an active enrollment.", null);

        var ps = await _enrollRepo.GetProtocolSiteByIdAsync(protocolSiteId);
        if (ps is null)
            return (false, "Invalid or inactive protocol-site combination.", null);

        // Block re-enrollment: patient cannot join a protocol they previously participated in
        if (await _enrollRepo.HasEnrolledInProtocolAsync(patientId, ps.ProtocolId))
            return (false,
                "Patient has already participated in this protocol. Re-enrollment in the same protocol is not allowed.",
                null);

        var enrollment = new PatientEnrollment
        {
            PatientId        = patientId,
            ProtocolSiteId   = protocolSiteId,
            EnrollmentStatus = "ACTIVE",
            EnrolledAt       = DateTime.UtcNow
        };

        var created = await _enrollRepo.CreateAsync(enrollment);

        // ── Auto-copy existing protocol visits for the new patient ────────
        // Single query: get all SCHEDULED/RESCHEDULED visits for this protocol
        var protocolVisits = (await _visitRepo.GetScheduledByProtocolAsync(ps.ProtocolId))
            .Where(v => v.EnrollmentId != created.EnrollmentId) // exclude if any were just created
            .ToList();

        if (protocolVisits.Any())
        {
            // Deduplicate by (VisitName, VisitDate) — same visit may exist
            // across multiple patients' enrollments
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var visitsToCreate = new List<Visit>();

            foreach (var v in protocolVisits)
            {
                var key = $"{v.VisitName.Trim().ToLower()}|{v.VisitDate:yyyy-MM-dd}";
                if (seen.Add(key))
                {
                    visitsToCreate.Add(new Visit
                    {
                        EnrollmentId = created.EnrollmentId,
                        VisitName    = v.VisitName,
                        VisitDate    = v.VisitDate,
                        VisitStatus  = "SCHEDULED"
                    });
                }
            }

            if (visitsToCreate.Any())
                await _visitRepo.BulkCreateAsync(visitsToCreate);
        }
        // ─────────────────────────────────────────────────────────────────

        var full = await _enrollRepo.GetByIdAsync(created.EnrollmentId);
        return (true, null, MapToDto(full!));
    }

    public async Task<(bool Success, string? Error)> WithdrawAsync(Guid id)
    {
        var e = await _enrollRepo.GetByIdAsync(id);
        if (e is null)
            return (false, "Enrollment not found.");
        if (e.EnrollmentStatus != "ACTIVE")
            return (false, "Only ACTIVE enrollments can be withdrawn.");

        // Cancel all scheduled/rescheduled visits through VisitRepository
        var visits = (await _visitRepo.GetByEnrollmentIdAsync(id))
            .Where(v => v.VisitStatus is "SCHEDULED" or "RESCHEDULED")
            .ToList();

        foreach (var v in visits)
            v.VisitStatus = "CANCELLED";

        if (visits.Any())
            await _visitRepo.BulkUpdateAsync(visits);

        e.EnrollmentStatus = "WITHDRAWN";
        await _enrollRepo.UpdateAsync(e);
        return (true, null);
    }

    public async Task<IEnumerable<ProtocolDto>> GetProtocolsAsync()
    {
        var protocols = await _enrollRepo.GetActiveProtocolsAsync();
        return protocols.Select(p => new ProtocolDto
        {
            ProtocolId = p.ProtocolId,
            Title      = p.Title,
            Phase      = p.Phase,
            Status     = p.Status,
            StartDate  = p.StartDate,
            EndDate    = p.EndDate
        });
    }

    public async Task<IEnumerable<ProtocolSiteDto>> GetSitesByProtocolAsync(Guid protocolId)
    {
        var sites = await _enrollRepo.GetActiveSitesByProtocolAsync(protocolId);
        return sites.Select(ps => new ProtocolSiteDto
        {
            ProtocolSiteId = ps.ProtocolSiteId,
            SiteId         = ps.SiteId,
            SiteName       = ps.Site?.Name ?? string.Empty,
            SiteLocation   = ps.Site?.Location ?? string.Empty,
            Status         = ps.Status
        });
    }

    public async Task<int> GetActivePatientCountAsync(Guid protocolId)
        => await _enrollRepo.GetActivePatientCountAsync(protocolId);

    // ── Mapping ──────────────────────────────────────────────

    private static EnrollmentDto MapToDto(PatientEnrollment e) => new()
    {
        EnrollmentId     = e.EnrollmentId,
        PatientId        = e.PatientId,
        PatientName      = e.Patient?.Name ?? string.Empty,
        PatientEmail     = e.Patient?.ContactInfo ?? string.Empty,
        ProtocolSiteId   = e.ProtocolSiteId,
        ProtocolTitle    = e.ProtocolSite?.Protocol?.Title ?? string.Empty,
        SiteName         = e.ProtocolSite?.Site?.Name ?? string.Empty,
        EnrollmentStatus = e.EnrollmentStatus,
        EnrolledAt       = e.EnrolledAt
    };
}
