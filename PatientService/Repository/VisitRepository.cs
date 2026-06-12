using Microsoft.EntityFrameworkCore;
using PatientService.Data;
using PatientService.Data.Entities;

namespace PatientService.Repository;

public class VisitRepository : IVisitRepository
{
    private readonly ServicesDbContext _ctx;
    public VisitRepository(ServicesDbContext ctx) => _ctx = ctx;

    public async Task<IEnumerable<Visit>> GetByEnrollmentIdAsync(Guid enrollmentId)
      => await _ctx.Visits
             .Include(v => v.Enrollment).ThenInclude(e => e.Patient)
             .Include(v => v.Enrollment).ThenInclude(e => e.ProtocolSite)
                 .ThenInclude(ps => ps.Protocol)
             .Include(v => v.Enrollment).ThenInclude(e => e.ProtocolSite)
                 .ThenInclude(ps => ps.Site)
             .Where(v => v.EnrollmentId == enrollmentId)
             .OrderBy(v => v.VisitDate)
             .ToListAsync();

    public async Task<IEnumerable<Visit>> GetFilteredAsync(
        DateTime? date, Guid? protocolSiteId, string? status)
    {
        var q = _ctx.Visits
            .Include(v => v.Enrollment).ThenInclude(e => e.Patient)
            .Include(v => v.Enrollment).ThenInclude(e => e.ProtocolSite)
                .ThenInclude(ps => ps.Protocol)
            .Include(v => v.Enrollment).ThenInclude(e => e.ProtocolSite)
                .ThenInclude(ps => ps.Site)
            .AsQueryable();

        if (date.HasValue)
            q = q.Where(v => v.VisitDate >= date.Value.Date &&
                             v.VisitDate < date.Value.Date.AddDays(1));

        if (protocolSiteId.HasValue)
            q = q.Where(v => v.Enrollment.ProtocolSiteId == protocolSiteId.Value);

        if (!string.IsNullOrWhiteSpace(status))
            q = q.Where(v => v.VisitStatus == status); // caller is responsible for correct casing

        return await q.OrderBy(v => v.VisitDate).ToListAsync();
    }

    public async Task<Visit?> GetByIdAsync(Guid id)
        => await _ctx.Visits
               .Include(v => v.Enrollment).ThenInclude(e => e.Patient)
               .Include(v => v.Enrollment).ThenInclude(e => e.ProtocolSite)
                   .ThenInclude(ps => ps.Protocol)
               .Include(v => v.Enrollment).ThenInclude(e => e.ProtocolSite)
                   .ThenInclude(ps => ps.Site)
               .FirstOrDefaultAsync(v => v.VisitId == id);

    public async Task<Visit> CreateAsync(Visit visit)
    {
        _ctx.Visits.Add(visit);
        await _ctx.SaveChangesAsync();
        return visit;
    }

    public async Task BulkCreateAsync(IEnumerable<Visit> visits)
    {
        await _ctx.Visits.AddRangeAsync(visits);
        await _ctx.SaveChangesAsync();
    }
     
    public async Task UpdateAsync(Visit visit)
    {
        _ctx.Visits.Update(visit);
        await _ctx.SaveChangesAsync();
    }

    public async Task BulkUpdateAsync(IEnumerable<Visit> visits)
    {
        _ctx.Visits.UpdateRange(visits);
        await _ctx.SaveChangesAsync();
    }

    public async Task<IEnumerable<Visit>> GetScheduledByProtocolAsync(Guid protocolId)
        => await _ctx.Visits
               .Where(v =>
                   v.Enrollment.ProtocolSite.ProtocolId == protocolId &&
                   (v.VisitStatus == "SCHEDULED" || v.VisitStatus == "RESCHEDULED"))
               .OrderBy(v => v.VisitDate)
               .ToListAsync();
}