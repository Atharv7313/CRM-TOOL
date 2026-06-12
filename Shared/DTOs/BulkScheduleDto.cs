namespace Shared.DTOs;

public class BulkScheduleDto
{
    public Guid ProtocolId { get; set; }
    public List<BulkVisitItem> Visits { get; set; } = new();
}
