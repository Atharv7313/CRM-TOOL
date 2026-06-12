namespace Shared.CL.DTOs;

public class SampleCreateDto
{
    public Guid EnrollmentId { get; set; }
    public Guid CollectedByUserId { get; set; }
    public string SampleType { get; set; } = string.Empty;
    public DateTime CollectedDate { get; set; }
}
