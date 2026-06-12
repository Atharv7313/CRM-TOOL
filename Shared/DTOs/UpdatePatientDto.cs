namespace Shared.DTOs;

public class UpdatePatientDto
{
    public string Name { get; set; } = string.Empty;
    public DateOnly DateOfBirth { get; set; }
    public string? ContactInfo { get; set; }
}
