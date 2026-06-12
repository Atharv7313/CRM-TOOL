using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SampleService.Data.Entities;

[Index("ProtocolId", Name = "IX_KpiReports_Protocol")]
public partial class KpiReport
{
    [Key]
    public Guid ReportId { get; set; }

    public Guid? ProtocolId { get; set; }

    public Guid GeneratedByUserId { get; set; }

    [StringLength(100)]
    public string Scope { get; set; } = null!;

    public string? Metrics { get; set; }

    [Precision(0)]
    public DateTime GeneratedAt { get; set; }

    [ForeignKey("GeneratedByUserId")]
    [InverseProperty("KpiReports")]
    public virtual User GeneratedByUser { get; set; } = null!;

    [ForeignKey("ProtocolId")]
    [InverseProperty("KpiReports")]
    public virtual Protocol? Protocol { get; set; }
}
