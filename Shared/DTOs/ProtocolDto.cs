namespace Shared.DTOs;

public class ProtocolDto
{
    public Guid ProtocolId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Phase { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}
