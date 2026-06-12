namespace Shared.Models;

public class KPIReport
{
    public int ReportID { get; set; }
    public string Scope { get; set; } = string.Empty;
    public string Metrics { get; set; } = string.Empty; // stored as JSON string
    public DateTime GeneratedDate { get; set; } = DateTime.UtcNow;
}