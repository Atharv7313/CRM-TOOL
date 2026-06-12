using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace NotificationService.Data.Entities;

[Index("ProtocolId", Name = "IX_ComplianceReports_Protocol")]
public partial class ComplianceReport
{
    [Key]
    public Guid ReportId { get; set; }

    public Guid ProtocolId { get; set; }

    public Guid GeneratedByUserId { get; set; }

    [StringLength(100)]
    public string Scope { get; set; } = null!;

    public string? Metrics { get; set; }

    [StringLength(50)]
    public string Status { get; set; } = null!;

    [Precision(0)]
    public DateTime GeneratedAt { get; set; }

    [ForeignKey("GeneratedByUserId")]
    [InverseProperty("ComplianceReports")]
    public virtual User GeneratedByUser { get; set; } = null!;

    [ForeignKey("ProtocolId")]
    [InverseProperty("ComplianceReports")]
    public virtual Protocol Protocol { get; set; } = null!;
}
