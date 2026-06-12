namespace Shared.CL.DTOs;

public class LabResultUpdateDto
{
    public string?   TestType     { get; set; }
    public string?   ResultValue  { get; set; }
    public string?   ResultStatus { get; set; }
    public DateTime? ResultDate   { get; set; }
}
