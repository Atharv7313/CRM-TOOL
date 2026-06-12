using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ReportingService.Data.Entities;

[Index("Email", Name = "IX_Users_Email")]
[Index("RoleId", Name = "IX_Users_RoleId")]
[Index("Email", Name = "UQ_Users_Email", IsUnique = true)]
public partial class User
{
    [Key]
    public Guid UserId { get; set; }

    [StringLength(100)]
    public string Name { get; set; } = null!;

    [StringLength(150)]
    public string Email { get; set; } = null!;

    [StringLength(20)]
    public string? Phone { get; set; }

    [StringLength(255)]
    public string PasswordHash { get; set; } = null!;

    public Guid RoleId { get; set; }

    public bool IsActive { get; set; }

    [Precision(0)]
    public DateTime CreatedAt { get; set; }

    [InverseProperty("GeneratedByUser")]
    public virtual ICollection<ComplianceReport> ComplianceReports { get; set; } = new List<ComplianceReport>();

    [InverseProperty("GeneratedByUser")]
    public virtual ICollection<KpiReport> KpiReports { get; set; } = new List<KpiReport>();

    [InverseProperty("RecordedByUser")]
    public virtual ICollection<LabResult> LabResults { get; set; } = new List<LabResult>();

    [InverseProperty("User")]
    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    [InverseProperty("InvestigatorUser")]
    public virtual ICollection<ProtocolSite> ProtocolSites { get; set; } = new List<ProtocolSite>();

    [InverseProperty("CreatedByUser")]
    public virtual ICollection<Protocol> Protocols { get; set; } = new List<Protocol>();

    [ForeignKey("RoleId")]
    [InverseProperty("Users")]
    public virtual Role Role { get; set; } = null!;

    [InverseProperty("CollectedByUser")]
    public virtual ICollection<Sample> Samples { get; set; } = new List<Sample>();

    [InverseProperty("User")]
    public virtual ICollection<UserSession> UserSessions { get; set; } = new List<UserSession>();
}
