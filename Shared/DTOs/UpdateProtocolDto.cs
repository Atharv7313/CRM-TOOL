namespace Shared.CL.DTOs;

public class UpdateProtocolDto
{
    public string? Title { get; set; }
    public string? Phase { get; set; }
    public string? Description { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}