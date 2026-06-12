using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ReportingService.Data.Entities;

[Index("ProtocolId", Name = "IX_ProtocolSites_Protocol")]
[Index("SiteId", Name = "IX_ProtocolSites_Site")]
[Index("ProtocolId", "SiteId", Name = "UQ_ProtocolSites", IsUnique = true)]
public partial class ProtocolSite
{
    [Key]
    public Guid ProtocolSiteId { get; set; }

    public Guid ProtocolId { get; set; }

    public Guid SiteId { get; set; }

    public Guid InvestigatorUserId { get; set; }

    [StringLength(50)]
    public string Status { get; set; } = null!;

    [ForeignKey("InvestigatorUserId")]
    [InverseProperty("ProtocolSites")]
    public virtual User InvestigatorUser { get; set; } = null!;

    [InverseProperty("ProtocolSite")]
    public virtual ICollection<PatientEnrollment> PatientEnrollments { get; set; } = new List<PatientEnrollment>();

    [ForeignKey("ProtocolId")]
    [InverseProperty("ProtocolSites")]
    public virtual Protocol Protocol { get; set; } = null!;

    [ForeignKey("SiteId")]
    [InverseProperty("ProtocolSites")]
    public virtual Site Site { get; set; } = null!;
}
