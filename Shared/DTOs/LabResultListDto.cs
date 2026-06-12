namespace Shared.CL.DTOs;

public class LabResultListDto
{
    public Guid ResultId { get; set; }
    public Guid SampleId { get; set; }
    public Guid RecordedByUserId { get; set; }
    public string RecordedByUserName { get; set; } = string.Empty;
    public string  TestType     { get; set; } = string.Empty;
    public string  ResultValue  { get; set; } = string.Empty;
    public string? ResultStatus { get; set; }
    public DateTime ResultDate  { get; set; }
}
