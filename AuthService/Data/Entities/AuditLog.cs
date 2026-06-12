using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AuthService.Data.Entities;

public class AuditLog
{
    [Key]
    public int LogId { get; set; }

    public Guid? ActorUserId { get; set; }

    [StringLength(100)]
    public string ActorName { get; set; } = null!;

    [StringLength(150)]
    public string ActorEmail { get; set; } = null!;

    [StringLength(50)]
    public string Action { get; set; } = null!;

    [StringLength(500)]
    public string Description { get; set; } = null!;

    public Guid? TargetUserId { get; set; }

    [StringLength(100)]
    public string? TargetUserName { get; set; }

    [StringLength(45)]
    public string? IpAddress { get; set; }

    public bool IsSuccess { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey("ActorUserId")]
    public virtual User? Actor { get; set; }
}
