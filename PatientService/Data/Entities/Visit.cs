using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace PatientService.Data.Entities;

[Index("EnrollmentId", Name = "IX_Visits_EnrollmentId")]
[Index("VisitDate", Name = "IX_Visits_VisitDate", AllDescending = true)]
[Index("VisitStatus", Name = "IX_Visits_VisitStatus")]
public partial class Visit
{
    [Key]
    public Guid VisitId { get; set; }

    public Guid EnrollmentId { get; set; }

    [StringLength(200)]
    public string VisitName { get; set; } = null!;

    [Precision(0)]
    public DateTime VisitDate { get; set; }

    [StringLength(50)]
    public string VisitStatus { get; set; } = null!;

    [ForeignKey("EnrollmentId")]
    [InverseProperty("Visits")]
    public virtual PatientEnrollment Enrollment { get; set; } = null!;
}
