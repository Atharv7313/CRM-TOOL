using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SampleService.Data.Entities;

[Index("ExpiresAt", Name = "IX_UserSessions_ExpiresAt")]
[Index("TokenJti", Name = "IX_UserSessions_TokenJti")]
[Index("UserId", Name = "IX_UserSessions_UserId")]
[Index("TokenJti", Name = "UQ_UserSessions_TokenJti", IsUnique = true)]
public partial class UserSession
{
    [Key]
    public Guid SessionId { get; set; }

    public Guid UserId { get; set; }

    [StringLength(100)]
    public string TokenJti { get; set; } = null!;

    [StringLength(45)]
    public string? IpAddress { get; set; }

    [StringLength(500)]
    public string? UserAgent { get; set; }

    public bool IsRevoked { get; set; }

    [Precision(0)]
    public DateTime ExpiresAt { get; set; }

    [Precision(0)]
    public DateTime CreatedAt { get; set; }

    [ForeignKey("UserId")]
    [InverseProperty("UserSessions")]
    public virtual User User { get; set; } = null!;
}
