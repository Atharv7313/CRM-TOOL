using Microsoft.EntityFrameworkCore;
using SampleService.Data;
using SampleService.Data.Entities;
using Shared.CL.DTOs;

namespace SampleService.Repositories;

public class LabResultRepository : ILabResultRepository
{
    private readonly ServicesDbContext _db;

    public LabResultRepository(ServicesDbContext db) => _db = db;

    public async Task<IList<LabResultListDto>> GetBySampleAsync(Guid sampleId)
    {
        return await _db.LabResults
            .AsNoTracking()
            .Include(r => r.RecordedByUser)
            .Where(r => r.SampleId == sampleId)
            .OrderByDescending(r => r.ResultDate)
            .Select(r => ToDto(r))
            .ToListAsync();
    }

    public async Task<LabResultListDto?> GetByIdAsync(Guid id)
    {
        LabResult? result = await _db.LabResults
            .AsNoTracking()
            .Include(r => r.RecordedByUser)
            .FirstOrDefaultAsync(r => r.ResultId == id);

        return result == null ? null : ToDto(result);
    }

    public async Task<LabResultListDto> CreateAsync(LabResultCreateDto dto)
    {
        LabResult result = new()
        {
            SampleId         = dto.SampleId,
            RecordedByUserId = dto.RecordedByUserId,
            TestType         = dto.TestType,
            ResultValue      = dto.ResultValue,
            ResultStatus     = dto.ResultStatus,
            ResultDate       = dto.ResultDate
        };

        _db.LabResults.Add(result);
        await _db.SaveChangesAsync();

        return (await GetByIdAsync(result.ResultId))!;
    }

    public async Task<LabResultListDto?> UpdateAsync(Guid id, LabResultUpdateDto dto)
    {
        LabResult? result = await _db.LabResults.FindAsync(id);
        if (result == null) return null;

        if (dto.TestType     != null)        result.TestType     = dto.TestType;
        if (dto.ResultValue  != null)        result.ResultValue  = dto.ResultValue;
        if (dto.ResultStatus != null)        result.ResultStatus = dto.ResultStatus;
        if (dto.ResultDate.HasValue)         result.ResultDate   = dto.ResultDate.Value;

        await _db.SaveChangesAsync();
        return await GetByIdAsync(id);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        LabResult? result = await _db.LabResults.FindAsync(id);
        if (result == null) return false;

        _db.LabResults.Remove(result);
        await _db.SaveChangesAsync();
        return true;
    }

    private static LabResultListDto ToDto(LabResult r) => new()
    {
        ResultId           = r.ResultId,
        SampleId           = r.SampleId,
        RecordedByUserId   = r.RecordedByUserId,
        RecordedByUserName = r.RecordedByUser?.Name ?? string.Empty,
        TestType           = r.TestType,
        ResultValue        = r.ResultValue,
        ResultStatus       = r.ResultStatus,
        ResultDate         = r.ResultDate
    };
}
