using Entities.Base;

namespace Entities.Entites;

public class Role : BaseEntity
{
    [Required]
    public string Name { get; set; } = string.Empty;

    // Navigation property
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();

}
