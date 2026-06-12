namespace ProtocolService.DTOs;

public class InvestigatorDto
{
    public Guid   UserId { get; set; }
    public string Name   { get; set; } = string.Empty;
    public string Email  { get; set; } = string.Empty;
    public string Role   { get; set; } = string.Empty;
}
