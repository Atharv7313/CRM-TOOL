using System.Text.RegularExpressions;
using ProtocolService.Data.Entities;
using ProtocolService.Repositories;
using ProtocolService.Enums;
using Shared.CL.DTOs;
using AssignmentStatus = ProtocolService.Enums.AssignmentStatus;
using ProtocolStatus   = ProtocolService.Enums.ProtocolStatus;

namespace ProtocolService.Services;

public class SiteServiceImpl : ISiteService
{
    private readonly ISiteRepository _repo;

    private static readonly HashSet<string> BlockedValues = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "string", "test", "abc", "xyz", "foo", "bar", "null", "none",
        "na", "n/a", "sample", "example", "demo", "dummy", "placeholder"
    };

    public SiteServiceImpl(ISiteRepository repo)
    {
        _repo = repo;
    }

    public async Task<SiteResponseDto> CreateAsync(CreateSiteDto dto)
    {
        var name = dto.Name?.Trim() ?? string.Empty;
        var location = dto.Location?.Trim() ?? string.Empty;

        ValidateName(name);
        ValidateLocation(location);

        if (await _repo.NameLocationExistsAsync(name, location))
            throw new InvalidOperationException(
                "A site with the same name and location already exists.");

        var site = new Site { Name = name, Location = location, IsActive = true };

        await _repo.AddAsync(site);
        await _repo.SaveChangesAsync();
        return MapToResponse(site);
    }

    public async Task<SiteResponseDto?> GetByIdAsync(Guid id)
    {
        var site = await _repo.GetByIdAsync(id);
        return site == null ? null : MapToResponse(site);
    }

    public async Task<List<SiteResponseDto>> GetAllAsync(string? name, string? location)
    {
        var sites = await _repo.GetAllAsync(name, location);
        return sites.Select(MapToResponse).ToList();
    }

    public async Task<SiteResponseDto> UpdateAsync(Guid id, UpdateSiteDto dto)
    {
        var site = await _repo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException("Site not found.");

        var name = dto.Name?.Trim() ?? string.Empty;
        var location = dto.Location?.Trim() ?? string.Empty;

        ValidateName(name);
        ValidateLocation(location);

        if (await _repo.NameLocationExistsAsync(name, location, id))
            throw new InvalidOperationException(
                "A site with the same name and location already exists.");

        site.Name = name;
        site.Location = location;

        await _repo.SaveChangesAsync();
        return MapToResponse(site);
    }

    public async Task<SiteResponseDto> SoftDeleteAsync(Guid id)
    {
        var site = await _repo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException("Site not found.");

        site.IsActive = false;

        foreach (var ps in site.ProtocolSites)
            if (ps.Status != AssignmentStatus.Closed)
                ps.Status = AssignmentStatus.Closed;

        await _repo.SaveChangesAsync();
        return MapToResponse(site);
    }

    // â”€â”€ Private Helpers â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Site name is required and cannot be blank or whitespace.");

        if (name.Length < 3)
            throw new ArgumentException("Site name must be at least 3 characters.");

        if (!Regex.IsMatch(name, @"[a-zA-Z]"))
            throw new ArgumentException("Site name must contain at least one letter.");

        if (BlockedValues.Contains(name))
            throw new ArgumentException($"'{name}' is not a valid site name. Please provide a real site name.");
    }

    private static void ValidateLocation(string location)
    {
        if (string.IsNullOrWhiteSpace(location))
            throw new ArgumentException("Location is required and cannot be blank or whitespace.");

        if (location.Length < 3)
            throw new ArgumentException("Location must be at least 3 characters.");

        if (!Regex.IsMatch(location, @"[a-zA-Z]"))
            throw new ArgumentException("Location must contain at least one letter.");

        if (BlockedValues.Contains(location))
            throw new ArgumentException($"'{location}' is not a valid location. Please provide a real location.");
    }

    private static SiteResponseDto MapToResponse(Site s)
    {
        int count = s.ProtocolSites.Count(ps => ps.Status == AssignmentStatus.Active);

        return new SiteResponseDto
        {
            SiteId = s.SiteId,
            Name = s.Name,
            Location = s.Location,
            ProtocolCount = count
        };
    }
}
