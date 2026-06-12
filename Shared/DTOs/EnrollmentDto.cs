namespace Shared.DTOs;

public class EnrollmentDto
{
    public Guid EnrollmentId { get; set; }
    public Guid PatientId { get; set; }
    public string PatientName  { get; set; } = string.Empty;
    public string PatientEmail { get; set; } = string.Empty;
    public Guid ProtocolSiteId { get; set; }
    public string ProtocolTitle { get; set; } = string.Empty;
    public string SiteName { get; set; } = string.Empty;
    public string EnrollmentStatus { get; set; } = string.Empty;
    public DateTime EnrolledAt { get; set; }
}