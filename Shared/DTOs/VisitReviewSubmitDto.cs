namespace Shared.DTOs;

public class VisitReviewSubmitDto
{
    public DateTime Date { get; set; }
    public Guid? ProtocolSiteId { get; set; }
    public List<Guid> AttendedVisitIds { get; set; } = new();
}
