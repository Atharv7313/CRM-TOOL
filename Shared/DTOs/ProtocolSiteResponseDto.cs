namespace Shared.CL.DTOs;

public class ProtocolSiteResponseDto
{
    public Guid ProtocolSiteId { get; set; }
    public Guid ProtocolId { get; set; }
    public string ProtocolTitle { get; set; } = string.Empty;
    public Guid SiteId { get; set; }
    public string SiteName { get; set; } = string.Empty;
    public string? SiteLocation { get; set; }
    public Guid InvestigatorUserId { get; set; }
    public string InvestigatorName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}
