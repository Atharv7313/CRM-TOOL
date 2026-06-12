namespace Shared.DTOs;

public class PatientDto
{
    public Guid PatientId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateOnly DateOfBirth { get; set; }
    public string? ContactInfo { get; set; }
    public string PatientStatus { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    // Enrollment info
    public string EnrollmentStatus { get; set; } = "Not enrolled";
    public Guid? EnrollmentId { get; set; }
    public string? ProtocolTitle { get; set; }
    public string? SiteName { get; set; }
    public DateTime? EnrolledAt { get; set; }

    public List<string> PreviousProtocols { get; set; } = new();
}