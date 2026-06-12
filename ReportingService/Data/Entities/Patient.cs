using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ReportingService.Data.Entities;

[Index("PatientStatus", Name = "IX_Patients_PatientStatus")]
public partial class Patient
{
    [Key]
    public Guid PatientId { get; set; }

    [StringLength(100)]
    public string Name { get; set; } = null!;

    public DateOnly DateOfBirth { get; set; }

    [StringLength(255)]
    public string? ContactInfo { get; set; }

    [StringLength(50)]
    public string PatientStatus { get; set; } = null!;

    [Precision(0)]
    public DateTime CreatedAt { get; set; }

    [InverseProperty("Patient")]
    public virtual ICollection<PatientEnrollment> PatientEnrollments { get; set; } = new List<PatientEnrollment>();
}
