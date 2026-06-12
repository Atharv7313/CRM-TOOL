namespace Shared.CL.DTOs;

public class LabResultCreateDto
{
    public Guid SampleId { get; set; }
    public Guid RecordedByUserId { get; set; }
    public string  TestType     { get; set; } = string.Empty;
    public string  ResultValue  { get; set; } = string.Empty;
    public string? ResultStatus { get; set; }
    public DateTime ResultDate  { get; set; }
}
