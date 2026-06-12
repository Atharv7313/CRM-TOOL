using Microsoft.EntityFrameworkCore;
using SampleService.Data;
using SampleService.Data.Entities;
using Shared.CL.DTOs;

namespace SampleService.Repositories;

public class SampleRepository : ISampleRepository
{
    private readonly ServicesDbContext _db;

    public SampleRepository(ServicesDbContext db) => _db = db;

    public async Task<IList<SampleListDto>> GetAllAsync()
    {
        var samples = await _db.Samples
            .AsNoTracking()
            .Include(s => s.CollectedByUser).ThenInclude(u => u.Role)
            .Include(s => s.Enrollment).ThenInclude(e => e.Patient)
            .Include(s => s.Enrollment).ThenInclude(e => e.ProtocolSite).ThenInclude(ps => ps.Protocol)
            .AsSplitQuery()
            .OrderByDescending(s => s.CollectedDate)
            .ToListAsync();

        return samples.Select(ToDto).ToList();
    }

    public async Task<SampleListDto?> GetByIdAsync(Guid id)
    {
        Sample? sample = await _db.Samples
            .AsNoTracking()
            .Include(s => s.CollectedByUser).ThenInclude(u => u.Role)
            .Include(s => s.Enrollment).ThenInclude(e => e.Patient)
            .Include(s => s.Enrollment).ThenInclude(e => e.ProtocolSite).ThenInclude(ps => ps.Protocol)
            .AsSplitQuery()
            .FirstOrDefaultAsync(s => s.SampleId == id);

        return sample == null ? null : ToDto(sample);
    }

    public async Task<IList<SampleListDto>> GetByEnrollmentAsync(Guid enrollmentId)
    {
        var samples = await _db.Samples
            .AsNoTracking()
            .Include(s => s.CollectedByUser).ThenInclude(u => u.Role)
            .Include(s => s.Enrollment).ThenInclude(e => e.Patient)
            .Include(s => s.Enrollment).ThenInclude(e => e.ProtocolSite).ThenInclude(ps => ps.Protocol)
            .AsSplitQuery()
            .Where(s => s.EnrollmentId == enrollmentId)
            .OrderByDescending(s => s.CollectedDate)
            .ToListAsync();

        return samples.Select(ToDto).ToList();
    }

    public async Task<SampleListDto> CreateAsync(SampleCreateDto dto)
    {
        Sample sample = new()
        {
            EnrollmentId = dto.EnrollmentId,
            CollectedByUserId = dto.CollectedByUserId,
            SampleType = dto.SampleType,
            CollectedDate = dto.CollectedDate,
            Status = "COLLECTED"
        };

        _db.Samples.Add(sample);
        await _db.SaveChangesAsync();

        return (await GetByIdAsync(sample.SampleId))!;
    }

    public async Task<SampleListDto?> UpdateAsync(Guid id, SampleUpdateDto dto)
    {
        Sample? sample = await _db.Samples.FindAsync(id);
        if (sample == null) return null;

        if (dto.SampleType != null) sample.SampleType = dto.SampleType;
        if (dto.Status     != null) sample.Status     = dto.Status;
        if (dto.Notes      != null) sample.Notes      = dto.Notes;

        await _db.SaveChangesAsync();
        return await GetByIdAsync(id);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        Sample? sample = await _db.Samples
            .Include(s => s.LabResults)  // load related lab results
            .FirstOrDefaultAsync(s => s.SampleId == id);
        if (sample == null) return false;

        // Delete lab results first — FK constraint prevents deleting sample directly
        if (sample.LabResults.Any())
            _db.LabResults.RemoveRange(sample.LabResults);

        _db.Samples.Remove(sample);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateStatusAsync(Guid id, string status)
    {
        Sample? sample = await _db.Samples
            .Include(s => s.LabResults)
            .FirstOrDefaultAsync(s => s.SampleId == id);
        if (sample == null) return false;

        // Business rule: TESTED or ANALYZED require at least one lab result
        if ((status == "TESTED" || status == "ANALYZED") && !sample.LabResults.Any())
            throw new InvalidOperationException(
                $"Cannot mark sample as {status} — no lab results recorded yet. Add at least one lab result first.");

        sample.Status = status;
        await _db.SaveChangesAsync();
        return true;
    }

    private static SampleListDto ToDto(Sample s) => new()
    {
        SampleId            = s.SampleId,
        EnrollmentId        = s.EnrollmentId,
        CollectedByUserId   = s.CollectedByUserId,
        CollectedByUserName = s.CollectedByUser?.Name ?? string.Empty,
        CollectedByUserRole = FormatRole(s.CollectedByUser?.Role?.RoleName ?? string.Empty),
        SampleType          = s.SampleType,
        CollectedDate       = s.CollectedDate,
        Status              = s.Status,
        Notes               = s.Notes,
        PatientName         = s.Enrollment?.Patient?.Name,
        PatientEmail        = s.Enrollment?.Patient?.ContactInfo,
        ProtocolTitle       = s.Enrollment?.ProtocolSite?.Protocol?.Title
    };

    // Converts LAB_TECHNICIAN → Lab Technician
    private static string FormatRole(string role) =>
        string.Join(" ", role.Split('_')
            .Select(w => w.Length > 0
                ? char.ToUpper(w[0]) + w[1..].ToLower()
                : string.Empty));
}
