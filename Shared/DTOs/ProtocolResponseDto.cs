namespace Shared.CL.DTOs;

public class ProtocolResponseDto
{
    public Guid ProtocolId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Phase { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? SuggestedStatus { get; set; }
    public string? Description { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public Guid CreatedByUserId { get; set; }
    public int SiteCount { get; set; }
}
