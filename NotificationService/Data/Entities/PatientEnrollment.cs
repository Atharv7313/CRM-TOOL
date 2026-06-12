using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace NotificationService.Data.Entities;

[Index("PatientId", Name = "IX_Enrollments_PatientId")]
[Index("ProtocolSiteId", Name = "IX_Enrollments_ProtocolSite")]
[Index("PatientId", "ProtocolSiteId", Name = "UQ_PatientEnrollments", IsUnique = true)]
public partial class PatientEnrollment
{
    [Key]
    public Guid EnrollmentId { get; set; }

    public Guid PatientId { get; set; }

    public Guid ProtocolSiteId { get; set; }

    [StringLength(50)]
    public string EnrollmentStatus { get; set; } = null!;

    [Precision(0)]
    public DateTime EnrolledAt { get; set; }

    [ForeignKey("PatientId")]
    [InverseProperty("PatientEnrollments")]
    public virtual Patient Patient { get; set; } = null!;

    [ForeignKey("ProtocolSiteId")]
    [InverseProperty("PatientEnrollments")]
    public virtual ProtocolSite ProtocolSite { get; set; } = null!;

    [InverseProperty("Enrollment")]
    public virtual ICollection<Sample> Samples { get; set; } = new List<Sample>();

    [InverseProperty("Enrollment")]
    public virtual ICollection<Visit> Visits { get; set; } = new List<Visit>();
}
