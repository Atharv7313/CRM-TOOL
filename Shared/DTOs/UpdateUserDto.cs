using System.ComponentModel.DataAnnotations;

namespace Shared.DTOs;

public class UpdateUserDto
{
    [Required, StringLength(100, MinimumLength = 2)]
    public string Name { get; set; } = null!;

    [StringLength(20)]
    public string? Phone { get; set; }

    [Required]
    public string RoleName { get; set; } = null!;

    public bool IsActive { get; set; }
}
