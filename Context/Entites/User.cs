

using Entities.Base;

namespace Entities.Entites;


public class User : BaseEntity
{
    [Required]
    public string FirstName { get; set; } = string.Empty;
    [Required]
    public string LastName { get; set; } = string.Empty;
    [Required]
    public string Email { get; set; } = string.Empty;
    [Required]
    public bool Active { get; set; }

    public bool IsDeleted { get; set; } = false; // <-- Soft delete flag

    // Navigation property
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();

}
