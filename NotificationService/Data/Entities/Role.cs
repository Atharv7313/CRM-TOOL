using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace NotificationService.Data.Entities;

[Index("RoleName", Name = "UQ_Roles_RoleName", IsUnique = true)]
public partial class Role
{
    [Key]
    public Guid RoleId { get; set; }

    [StringLength(50)]
    public string RoleName { get; set; } = null!;

    public bool IsActive { get; set; }

    [InverseProperty("Role")]
    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
