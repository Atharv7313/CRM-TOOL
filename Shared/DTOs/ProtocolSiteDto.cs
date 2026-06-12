namespace Shared.DTOs;

public class ProtocolSiteDto
{
    public Guid ProtocolSiteId { get; set; }
    public Guid SiteId { get; set; }
    public string SiteName { get; set; } = string.Empty;
    public string SiteLocation { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}
