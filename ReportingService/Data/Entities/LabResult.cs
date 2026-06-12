using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ReportingService.Data.Entities;

[Index("SampleId", Name = "IX_LabResults_SampleId")]
public partial class LabResult
{
    [Key]
    public Guid ResultId { get; set; }

    public Guid SampleId { get; set; }

    public Guid RecordedByUserId { get; set; }

    [StringLength(100)]
    public string TestType { get; set; } = null!;

    [StringLength(255)]
    public string ResultValue { get; set; } = null!;

    [Precision(0)]
    public DateTime ResultDate { get; set; }

    [ForeignKey("RecordedByUserId")]
    [InverseProperty("LabResults")]
    public virtual User RecordedByUser { get; set; } = null!;

    [ForeignKey("SampleId")]
    [InverseProperty("LabResults")]
    public virtual Sample Sample { get; set; } = null!;
}
