using ProtocolService.Data.Entities;
using ProtocolService.Repositories;
using ProtocolService.Enums;
using ProtocolService.DTOs;
using Shared.CL.DTOs;
using AssignmentStatus = ProtocolService.Enums.AssignmentStatus;
using ProtocolStatus   = ProtocolService.Enums.ProtocolStatus;

namespace ProtocolService.Services;

public class ProtocolSiteServiceImpl : IProtocolSiteService
{
    private readonly IProtocolSiteRepository _repo;

    public ProtocolSiteServiceImpl(IProtocolSiteRepository repo)
    {
        _repo = repo;
    }

    public async Task<ProtocolSiteResponseDto> AssignAsync(Guid protocolId, AssignSiteDto dto)
    {
        // â”€â”€ Input validation (moved here from DTO [Required] annotations) â”€â”€
        if (protocolId == Guid.Empty)
            throw new ArgumentException("Protocol ID is required.");

        if (dto.SiteId == Guid.Empty)
            throw new ArgumentException("Site ID is required.");

        if (dto.InvestigatorUserId == Guid.Empty)
            throw new ArgumentException("Investigator user ID is required.");

        var protocol = await _repo.GetProtocolAsync(protocolId)
            ?? throw new KeyNotFoundException("Protocol not found.");

        if (protocol.Status == ProtocolStatus.Completed)
            throw new InvalidOperationException(
                $"Cannot assign sites to a protocol with status '{protocol.Status}'.");

        var site = await _repo.GetSiteAsync(dto.SiteId)
            ?? throw new KeyNotFoundException("Site not found or has been deleted.");

        var investigator = await _repo.GetUserAsync(dto.InvestigatorUserId)
            ?? throw new KeyNotFoundException("Investigator not found or is inactive.");

        var existing = await _repo.GetExistingAssignmentAsync(protocolId, dto.SiteId);

        if (existing != null)
        {
            if (existing.Status != AssignmentStatus.Closed)
                throw new InvalidOperationException("This site is already assigned to the protocol.");

            // Re-open a previously closed assignment
            existing.Status             = AssignmentStatus.Active;
            existing.InvestigatorUserId = dto.InvestigatorUserId;
            await _repo.SaveChangesAsync();
            return MapToResponse(existing, investigator.Name);
        }

        var assignment = new ProtocolSite
        {
            ProtocolId          = protocolId,
            SiteId              = dto.SiteId,
            InvestigatorUserId  = dto.InvestigatorUserId,
            Status              = AssignmentStatus.Active,
            Protocol            = protocol,
            Site                = site
        };

        await _repo.AddAssignmentAsync(assignment);
        await _repo.SaveChangesAsync();
        return MapToResponse(assignment, investigator.Name);
    }

    public async Task<List<ProtocolSiteResponseDto>> GetByProtocolAsync(Guid protocolId)
    {
        var assignments = await _repo.GetByProtocolAsync(protocolId);
        return await BuildResponseListAsync(assignments);
    }

    public async Task<List<ProtocolSiteResponseDto>> GetBySiteAsync(Guid siteId)
    {
        var assignments = await _repo.GetBySiteAsync(siteId);
        return await BuildResponseListAsync(assignments);
    }

    public async Task<ProtocolSiteResponseDto> UpdateStatusAsync(
        Guid protocolId, Guid assignmentId, UpdateProtocolSiteStatusDto dto)
    {
        var assignment = await _repo.GetAssignmentByIdAsync(assignmentId, protocolId)
            ?? throw new KeyNotFoundException("Assignment not found.");

        if (string.IsNullOrWhiteSpace(dto.Status))
            throw new ArgumentException("Status is required.");

        var newStatus = dto.Status.ToUpperInvariant();

        if (!IsValidAssignmentTransition(assignment.Status, newStatus))
            throw new InvalidOperationException(
                $"Cannot transition from '{assignment.Status}' to '{newStatus}'.");

        assignment.Status = newStatus;
        await _repo.SaveChangesAsync();

        var name = ResolveInvestigatorName(
            await _repo.GetInvestigatorNameAsync(assignment.InvestigatorUserId),
            assignment.InvestigatorUserId);
        return MapToResponse(assignment, name);
    }

    public async Task<ProtocolSiteResponseDto> RemoveAsync(Guid protocolId, Guid assignmentId)
    {
        var assignment = await _repo.GetAssignmentByIdAsync(assignmentId, protocolId)
            ?? throw new KeyNotFoundException("Assignment not found.");

        if (assignment.Status == AssignmentStatus.Closed)
            throw new InvalidOperationException("This assignment is already closed.");

        assignment.Status = AssignmentStatus.Closed;
        await _repo.SaveChangesAsync();

        var name = ResolveInvestigatorName(
            await _repo.GetInvestigatorNameAsync(assignment.InvestigatorUserId),
            assignment.InvestigatorUserId);
        return MapToResponse(assignment, name);
    }

    public async Task<List<InvestigatorDto>> GetInvestigatorsAsync()
    {
        var users = await _repo.GetAllUsersAsync();
        return users
            .Select(u => new InvestigatorDto
            {
                UserId = u.UserId,
                Name   = u.Name,
                Email  = u.Email,
                Role   = u.Role?.RoleName ?? string.Empty
            }).ToList();
    }

    // â”€â”€ Private Helpers (pure business logic â€” no DB access) â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    private static bool IsValidAssignmentTransition(string current, string next)
    {
        return current switch
        {
            AssignmentStatus.Active   => next == AssignmentStatus.Inactive || next == AssignmentStatus.Closed,
            AssignmentStatus.Inactive => next == AssignmentStatus.Active   || next == AssignmentStatus.Closed,
            _                         => false
        };
    }

    private async Task<List<ProtocolSiteResponseDto>> BuildResponseListAsync(List<ProtocolSite> assignments)
    {
        if (assignments.Count == 0) return new List<ProtocolSiteResponseDto>();

        var userIds = assignments.Select(ps => ps.InvestigatorUserId).Distinct().ToList();
        var nameMap = await _repo.GetInvestigatorNamesAsync(userIds);

        return assignments.Select(ps =>
        {
            // Fallback display logic lives here in the service, not in the repository
            nameMap.TryGetValue(ps.InvestigatorUserId, out var rawName);
            var name = ResolveInvestigatorName(rawName, ps.InvestigatorUserId);
            return MapToResponse(ps, name);
        }).ToList();
    }

    /// <summary>
    /// Service-layer fallback: if the DB has no name for this user,
    /// show a truncated ID so the UI always has something to display.
    /// </summary>
    private static string ResolveInvestigatorName(string? name, Guid userId)
        => string.IsNullOrWhiteSpace(name) ? string.Empty : name;

    private static ProtocolSiteResponseDto MapToResponse(ProtocolSite ps, string investigatorName)
    {
        return new ProtocolSiteResponseDto
        {
            ProtocolSiteId     = ps.ProtocolSiteId,
            ProtocolId         = ps.ProtocolId,
            ProtocolTitle      = ps.Protocol.Title,
            SiteId             = ps.SiteId,
            SiteName           = ps.Site.Name,
            SiteLocation       = ps.Site.Location,
            InvestigatorUserId = ps.InvestigatorUserId,
            InvestigatorName   = investigatorName,
            Status             = ps.Status
        };
    }
}

