namespace Shared.CL.DTOs;

public class SiteResponseDto
{
    public Guid SiteId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Location { get; set; }
    public int ProtocolCount { get; set; }
}
