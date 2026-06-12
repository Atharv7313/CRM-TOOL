using Microsoft.EntityFrameworkCore;
using PatientService.Data;
using PatientService.Data.Entities;

namespace PatientService.Repository;

public class EnrollmentRepository : IEnrollmentRepository
{
    private readonly ServicesDbContext _ctx;
    public EnrollmentRepository(ServicesDbContext ctx) => _ctx = ctx;

    // ── Enrollment CRUD ──────────────────────────────────────

    public async Task<IEnumerable<PatientEnrollment>> GetAllAsync()
        => await _ctx.PatientEnrollments
               .Include(e => e.Patient)
               .Include(e => e.ProtocolSite).ThenInclude(ps => ps.Protocol)
               .Include(e => e.ProtocolSite).ThenInclude(ps => ps.Site)
               .OrderByDescending(e => e.EnrolledAt)
               .ToListAsync();

    public async Task<PatientEnrollment?> GetByIdAsync(Guid id)
        => await _ctx.PatientEnrollments
               .Include(e => e.Patient)
               .Include(e => e.ProtocolSite).ThenInclude(ps => ps.Protocol)
               .Include(e => e.ProtocolSite).ThenInclude(ps => ps.Site)
               .Include(e => e.Visits)
               .FirstOrDefaultAsync(e => e.EnrollmentId == id);

    public async Task<bool> HasActiveEnrollmentAsync(Guid patientId)
        => await _ctx.PatientEnrollments.AnyAsync(e =>
               e.PatientId == patientId &&
               e.EnrollmentStatus == "ACTIVE");

    public async Task<bool> HasEnrolledInProtocolAsync(Guid patientId, Guid protocolId)
        => await _ctx.PatientEnrollments.AnyAsync(e =>
               e.PatientId == patientId &&
               e.ProtocolSite.ProtocolId == protocolId);

    public async Task<PatientEnrollment?> GetActiveByPatientAsync(Guid patientId)
        => await _ctx.PatientEnrollments
               .Include(e => e.ProtocolSite).ThenInclude(ps => ps.Protocol)
               .Include(e => e.ProtocolSite).ThenInclude(ps => ps.Site)
               .FirstOrDefaultAsync(e =>
                   e.PatientId == patientId &&
                   e.EnrollmentStatus == "ACTIVE");

    public async Task<PatientEnrollment> CreateAsync(PatientEnrollment enrollment)
    {
        _ctx.PatientEnrollments.Add(enrollment);
        await _ctx.SaveChangesAsync();
        return enrollment;
    }

    public async Task UpdateAsync(PatientEnrollment enrollment)
    {
        _ctx.PatientEnrollments.Update(enrollment);
        await _ctx.SaveChangesAsync();
    }

    // ── Protocol & Site lookups ──────────────────────────────

    public async Task<Protocol?> GetProtocolByIdAsync(Guid protocolId)
        => await _ctx.Protocols.FirstOrDefaultAsync(p => p.ProtocolId == protocolId);

    public async Task<ProtocolSite?> GetProtocolSiteByIdAsync(Guid protocolSiteId)
        => await _ctx.ProtocolSites
               .Include(x => x.Protocol)
               .Include(x => x.Site)
               .FirstOrDefaultAsync(x =>
                   x.ProtocolSiteId == protocolSiteId &&
                   x.Status == "ACTIVE");

    public async Task<IEnumerable<Protocol>> GetActiveProtocolsAsync()
        => await _ctx.Protocols
               .Where(p => p.Status == "ONGOING" || p.Status == "UPCOMING")
               .ToListAsync();

    public async Task<IEnumerable<ProtocolSite>> GetActiveSitesByProtocolAsync(Guid protocolId)
        => await _ctx.ProtocolSites
               .Include(ps => ps.Site)
               .Where(ps => ps.ProtocolId == protocolId && ps.Status == "ACTIVE")
               .ToListAsync();

    // ── Aggregate queries ────────────────────────────────────

    public async Task<int> GetActivePatientCountAsync(Guid protocolId)
        => await _ctx.PatientEnrollments
               .Where(e =>
                   e.ProtocolSite.ProtocolId == protocolId &&
                   e.EnrollmentStatus == "ACTIVE")
               .CountAsync();

    public async Task<IEnumerable<PatientEnrollment>> GetActiveByProtocolAsync(Guid protocolId)
        => await _ctx.PatientEnrollments
               .Where(e =>
                   e.ProtocolSite.ProtocolId == protocolId &&
                   e.EnrollmentStatus == "ACTIVE")
               .ToListAsync();
}
