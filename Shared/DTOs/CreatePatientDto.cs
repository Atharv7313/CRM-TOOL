namespace Shared.DTOs;

public class CreatePatientDto
{
    public string Name { get; set; } = string.Empty;
    public DateOnly DateOfBirth { get; set; }
    public string? ContactInfo { get; set; }
}
