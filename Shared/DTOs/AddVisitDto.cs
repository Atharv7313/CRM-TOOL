namespace Shared.DTOs;

public class AddVisitDto
{
    public Guid EnrollmentId { get; set; }
    public string VisitName { get; set; } = string.Empty;
    public DateTime VisitDate { get; set; }
}
