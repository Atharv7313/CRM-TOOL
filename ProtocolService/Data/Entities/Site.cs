using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ProtocolService.Data.Entities;

public partial class Site
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid SiteId { get; set; }

    [StringLength(150)]
    public string Name { get; set; } = null!;

    [StringLength(255)]
    public string? Location { get; set; }

    // Soft delete flag — column exists in DB
    public bool IsActive { get; set; } = true;

    [InverseProperty("Site")]
    public virtual ICollection<ProtocolSite> ProtocolSites { get; set; } = new List<ProtocolSite>();
}