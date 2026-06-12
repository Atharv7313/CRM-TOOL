using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ReportingService.Data.Entities;

[Index("CreatedAt", Name = "IX_Notifications_CreatedAt", AllDescending = true)]
[Index("Status", Name = "IX_Notifications_Status")]
[Index("UserId", Name = "IX_Notifications_UserId")]
public partial class Notification
{
    [Key]
    public Guid NotificationId { get; set; }

    public Guid UserId { get; set; }

    [StringLength(100)]
    public string Category { get; set; } = null!;

    public string Message { get; set; } = null!;

    [StringLength(50)]
    public string Channel { get; set; } = null!;

    [StringLength(50)]
    public string Status { get; set; } = null!;

    [Precision(0)]
    public DateTime CreatedAt { get; set; }

    [Precision(0)]
    public DateTime? ReadAt { get; set; }

    [ForeignKey("UserId")]
    [InverseProperty("Notifications")]
    public virtual User User { get; set; } = null!;
}
