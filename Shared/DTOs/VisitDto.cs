namespace Shared.DTOs;

public class VisitDto
{
    public Guid VisitId { get; set; }
    public Guid EnrollmentId { get; set; }
    public string VisitName { get; set; } = string.Empty;
    public DateTime VisitDate { get; set; }
    public string VisitStatus { get; set; } = string.Empty;
    public string? PrevVisit { get; set; }
    public string? NextVisit { get; set; }
    public string? PatientName { get; set; }
    public string? ProtocolTitle { get; set; }
    public string? SiteName { get; set; }
    public string? EnrollmentStatus { get; set; }

    /// Position of this visit within the enrollment (1-based)
    public int VisitNumber { get; set; }

    /// Total number of visits for this enrollment
    public int TotalVisits { get; set; }

    /// Protocol date boundaries — used by frontend to restrict date pickers
    public DateTime? ProtocolStartDate { get; set; }
    public DateTime? ProtocolEndDate { get; set; }
    public DateTime? EnrollmentWindowEnd { get; set; }
}

public record BulkVisitItem(string VisitName, DateTime VisitDate);