namespace Shared.DTOs;

public class EnrollRequestDto
{
    public Guid PatientId { get; set; }
    public Guid ProtocolSiteId { get; set; }
}
