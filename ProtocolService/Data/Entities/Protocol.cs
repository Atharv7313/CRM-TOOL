using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ProtocolService.Data.Entities;

[Index("CreatedByUserId", Name = "IX_Protocols_CreatedBy")]
[Index("Status", Name = "IX_Protocols_Status")]
public partial class Protocol
{
    [Key]
    public Guid ProtocolId { get; set; }

    [StringLength(200)]
    public string Title { get; set; } = null!;

    [StringLength(50)]
    public string Phase { get; set; } = null!;

    [StringLength(50)]
    public string Status { get; set; } = null!;

    public string? Description { get; set; }

    [Precision(0)]
    public DateTime? StartDate { get; set; }

    [Precision(0)]
    public DateTime? EndDate { get; set; }

    public Guid CreatedByUserId { get; set; }

    [Precision(0)]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [InverseProperty("Protocol")]
    public virtual ICollection<ComplianceReport> ComplianceReports { get; set; } = new List<ComplianceReport>();

    [ForeignKey("CreatedByUserId")]
    [InverseProperty("Protocols")]
    public virtual User CreatedByUser { get; set; } = null!;

    [InverseProperty("Protocol")]
    public virtual ICollection<KpiReport> KpiReports { get; set; } = new List<KpiReport>();

    [InverseProperty("Protocol")]
    public virtual ICollection<ProtocolSite> ProtocolSites { get; set; } = new List<ProtocolSite>();
}