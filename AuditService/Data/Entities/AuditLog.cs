using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AuditService.Data.Entities;

[Index("Action", Name = "IX_AuditLogs_Action")]
[Index("Timestamp", Name = "IX_AuditLogs_Timestamp", AllDescending = true)]
[Index("UserId", Name = "IX_AuditLogs_UserId")]
public partial class AuditLog
{
    [Key]
    public Guid AuditId { get; set; }

    [StringLength(100)]
    public string UserId { get; set; } = null!;

    [StringLength(100)]
    public string Action { get; set; } = null!;

    [Precision(0)]
    public DateTime Timestamp { get; set; }
}
