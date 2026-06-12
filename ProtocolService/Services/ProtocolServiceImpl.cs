using ProtocolService.Data.Entities;
using ProtocolService.Repositories;
using ProtocolService.Enums;
using Shared.CL.DTOs;
using AssignmentStatus = ProtocolService.Enums.AssignmentStatus;
using ProtocolStatus   = ProtocolService.Enums.ProtocolStatus;

namespace ProtocolService.Services;

public class ProtocolServiceImpl : IProtocolService
{
    private readonly IProtocolRepository _repo;

    private static readonly string[] ValidPhases =
        ["Phase 1", "Phase 2", "Phase 3", "Phase 4"];

    private static readonly HashSet<string> BlockedValues =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "string", "test", "abc", "xyz", "foo", "bar", "null", "none",
            "na", "n/a", "sample", "example", "demo", "dummy", "placeholder"
        };

    public ProtocolServiceImpl(IProtocolRepository repo)
    {
        _repo = repo;
    }

    // â”€â”€ Create â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    public async Task<ProtocolResponseDto> CreateAsync(CreateProtocolDto dto, Guid createdByUserId)
    {
        var title       = dto.Title?.Trim() ?? "";
        var phase       = dto.Phase?.Trim() ?? "";
        var description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim();

        // â”€â”€ Title â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title is required.");

        if (title.Length < 3)
            throw new ArgumentException("Title must be at least 3 characters.");

        if (title.Length > 200)
            throw new ArgumentException("Title cannot exceed 200 characters.");

        if (BlockedValues.Contains(title))
            throw new ArgumentException($"'{title}' is not a valid title. Please provide a real protocol title.");

        // â”€â”€ Phase â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        if (string.IsNullOrWhiteSpace(phase))
            throw new ArgumentException("Phase is required.");

        if (!ValidPhases.Contains(phase, StringComparer.OrdinalIgnoreCase))
            throw new ArgumentException(
                $"Invalid phase. Allowed values: {string.Join(", ", ValidPhases)}.");

        phase = ValidPhases.First(p => p.Equals(phase, StringComparison.OrdinalIgnoreCase));

        // â”€â”€ Dates â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        if (dto.StartDate == default)
            throw new ArgumentException("Start date is required.");

        if (dto.EndDate == default)
            throw new ArgumentException("End date is required.");

        if (dto.EndDate.Date < dto.StartDate.Date)
            throw new ArgumentException("End date cannot be earlier than start date.");

        // â”€â”€ Description â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        if (description != null && description.Length < 3)
            throw new ArgumentException("Description must be at least 3 characters if provided.");

        if (description != null && BlockedValues.Contains(description))
            throw new ArgumentException($"'{description}' is not a valid description. Please provide a real description.");

        // â”€â”€ Duplicate check â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        if (await _repo.TitlePhaseExistsAsync(title, phase))
            throw new InvalidOperationException(
                "A protocol with the same title and phase already exists.");

        var protocol = new Protocol
        {
            Title           = title,
            Phase           = phase,
            // Status is always auto-computed from dates â€” never taken from DTO
            Status          = ComputeStatus(dto.StartDate, dto.EndDate),
            Description     = description,
            StartDate       = dto.StartDate,
            EndDate         = dto.EndDate,
            CreatedByUserId = createdByUserId
        };

        await _repo.AddAsync(protocol);
        await _repo.SaveChangesAsync();

        return MapToResponse(protocol);
    }

    // â”€â”€ Read â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    public async Task<ProtocolResponseDto?> GetByIdAsync(Guid id)
    {
        var protocol = await _repo.GetByIdAsync(id);
        if (protocol == null) return null;

        // Auto-sync status from dates and persist if changed
        await SyncStatusAsync(protocol);

        return MapToResponse(protocol);
    }

    public async Task<List<ProtocolResponseDto>> GetAllAsync(string? status, string? phase, string? title)
    {
        var list = await _repo.GetAllAsync(status?.ToUpperInvariant(), phase, title);

        // Auto-sync all statuses from dates, persist any that changed
        bool anyChanged = false;
        foreach (var p in list)
        {
            var computed = ComputeStatus(p.StartDate, p.EndDate, p.Status);
            if (p.Status != computed)
            {
                p.Status   = computed;
                anyChanged = true;
                // Auto-close site assignments when protocol completes
                if (computed == ProtocolStatus.Completed)
                {
                    foreach (var ps in p.ProtocolSites)
                        if (ps.Status != AssignmentStatus.Closed)
                            ps.Status = AssignmentStatus.Closed;
                }
            }
        }

        if (anyChanged) await _repo.SaveChangesAsync();

        return list.Select(MapToResponse).ToList();
    }

    // â”€â”€ Update â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    public async Task<ProtocolResponseDto> UpdateAsync(Guid id, UpdateProtocolDto dto)
    {
        var protocol = await _repo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException("Protocol not found.");

        var today = DateTime.UtcNow.Date;

        // â”€â”€ End-date extension rule â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        // Can only modify the protocol while the current end date has not passed.
        // Once the end date is in the past, the protocol is locked.
        if (protocol.EndDate.HasValue && today > protocol.EndDate.Value.Date)
            throw new InvalidOperationException(
                $"This protocol's end date ({protocol.EndDate.Value:dd MMM yyyy}) has already passed. " +
                $"It can no longer be modified.");

        var title       = dto.Title?.Trim() ?? "";
        var phase       = dto.Phase?.Trim() ?? "";
        var description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim();

        // â”€â”€ Title â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title is required.");

        if (title.Length < 3)
            throw new ArgumentException("Title must be at least 3 characters.");

        if (title.Length > 200)
            throw new ArgumentException("Title cannot exceed 200 characters.");

        if (BlockedValues.Contains(title))
            throw new ArgumentException($"'{title}' is not a valid title. Please provide a real protocol title.");

        // â”€â”€ Phase â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        if (string.IsNullOrWhiteSpace(phase))
            throw new ArgumentException("Phase is required.");

        if (!ValidPhases.Contains(phase, StringComparer.OrdinalIgnoreCase))
            throw new ArgumentException(
                $"Invalid phase. Allowed values: {string.Join(", ", ValidPhases)}.");

        phase = ValidPhases.First(p => p.Equals(phase, StringComparison.OrdinalIgnoreCase));

        // â”€â”€ Dates â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        if (dto.StartDate == default)
            throw new ArgumentException("Start date is required.");

        if (dto.EndDate == default)
            throw new ArgumentException("End date is required.");

        if (dto.EndDate.Date < dto.StartDate.Date)
            throw new ArgumentException("End date cannot be earlier than start date.");

        // â”€â”€ Description â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        if (description != null && description.Length < 3)
            throw new ArgumentException("Description must be at least 3 characters if provided.");

        if (description != null && BlockedValues.Contains(description))
            throw new ArgumentException($"'{description}' is not a valid description. Please provide a real description.");

        // â”€â”€ Duplicate check â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        if (await _repo.TitlePhaseExistsAsync(title, phase, id))
            throw new InvalidOperationException(
                "A protocol with the same title and phase already exists.");

        protocol.Title       = title;
        protocol.Phase       = phase;
        protocol.Description = description;
        protocol.StartDate   = dto.StartDate;
        protocol.EndDate     = dto.EndDate;

        // Recompute status from the updated dates
        protocol.Status = ComputeStatus(dto.StartDate, dto.EndDate, protocol.Status);

        await _repo.SaveChangesAsync();
        return MapToResponse(protocol);
    }

    // â”€â”€ Status (manual â€” only used for soft-delete flow now) â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    public async Task<ProtocolResponseDto> UpdateStatusAsync(Guid id, UpdateProtocolStatusDto dto)
    {
        // Manual status changes are disabled â€” status is now driven by dates.
        // This endpoint is kept only for the soft-delete pipeline.
        throw new InvalidOperationException(
            "Manual status changes are not allowed. Protocol status is automatically " +
            "managed based on start and end dates.");
    }

    // â”€â”€ Delete â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    public async Task<ProtocolResponseDto> SoftDeleteAsync(Guid id)
    {
        var protocol = await _repo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException("Protocol not found.");

        protocol.Status = ProtocolStatus.Deleted;

        foreach (var ps in protocol.ProtocolSites)
            if (ps.Status != AssignmentStatus.Closed)
                ps.Status = AssignmentStatus.Closed;

        await _repo.CancelEnrollmentsForProtocolAsync(id);
        await _repo.SaveChangesAsync();
        return MapToResponse(protocol);
    }

    // â”€â”€ Private Helpers â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    /// <summary>
    /// Computes the correct status purely from dates.
    /// DELETED status is preserved as-is.
    /// </summary>
    private static string ComputeStatus(
        DateTime? startDate,
        DateTime? endDate,
        string    currentStatus = "")
    {
        // Never override a soft-deleted protocol
        if (currentStatus == ProtocolStatus.Deleted) return ProtocolStatus.Deleted;

        if (!startDate.HasValue || !endDate.HasValue) return ProtocolStatus.Upcoming;

        var today = DateTime.UtcNow.Date;
        var start = startDate.Value.Date;
        var end   = endDate.Value.Date;

        if (today < start)  return ProtocolStatus.Upcoming;
        if (today <= end)   return ProtocolStatus.Ongoing;
        return ProtocolStatus.Completed;
    }

    /// <summary>
    /// Syncs a single protocol's status from its dates and saves if changed.
    /// </summary>
    private async Task SyncStatusAsync(Protocol p)
    {
        var computed = ComputeStatus(p.StartDate, p.EndDate, p.Status);
        if (p.Status == computed) return;

        p.Status = computed;

        if (computed == ProtocolStatus.Completed)
        {
            foreach (var ps in p.ProtocolSites)
                if (ps.Status != AssignmentStatus.Closed)
                    ps.Status = AssignmentStatus.Closed;
        }

        await _repo.SaveChangesAsync();
    }

    private static ProtocolResponseDto MapToResponse(Protocol p)
    {
        int siteCount = p.ProtocolSites.Count(ps => ps.Status == AssignmentStatus.Active);

        return new ProtocolResponseDto
        {
            ProtocolId      = p.ProtocolId,
            Title           = p.Title,
            Phase           = p.Phase,
            Status          = p.Status,
            SuggestedStatus = null,          // Removed â€” status is always accurate now
            Description     = p.Description,
            StartDate       = p.StartDate,
            EndDate         = p.EndDate,
            CreatedByUserId = p.CreatedByUserId,
            SiteCount       = siteCount
        };
    }
}

