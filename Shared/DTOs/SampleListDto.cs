namespace Shared.CL.DTOs;

public class SampleListDto
{
    public Guid SampleId { get; set; }
    public Guid EnrollmentId { get; set; }
    public Guid CollectedByUserId { get; set; }
    public string CollectedByUserName { get; set; } = string.Empty;
    public string CollectedByUserRole { get; set; } = string.Empty;
    public string SampleType { get; set; } = string.Empty;
    public DateTime CollectedDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public string? PatientName { get; set; }
    public string? PatientEmail { get; set; }
    public string? ProtocolTitle { get; set; }
}
