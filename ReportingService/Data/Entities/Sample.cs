using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ReportingService.Data.Entities;

[Index("CollectedDate", Name = "IX_Samples_CollectedDate", AllDescending = true)]
[Index("EnrollmentId", Name = "IX_Samples_EnrollmentId")]
[Index("Status", Name = "IX_Samples_Status")]
public partial class Sample
{
    [Key]
    public Guid SampleId { get; set; }

    public Guid EnrollmentId { get; set; }

    public Guid CollectedByUserId { get; set; }

    [StringLength(100)]
    public string SampleType { get; set; } = null!;

    [Precision(0)]
    public DateTime CollectedDate { get; set; }

    [StringLength(50)]
    public string Status { get; set; } = null!;

    [ForeignKey("CollectedByUserId")]
    [InverseProperty("Samples")]
    public virtual User CollectedByUser { get; set; } = null!;

    [ForeignKey("EnrollmentId")]
    [InverseProperty("Samples")]
    public virtual PatientEnrollment Enrollment { get; set; } = null!;

    [InverseProperty("Sample")]
    public virtual ICollection<LabResult> LabResults { get; set; } = new List<LabResult>();
}
