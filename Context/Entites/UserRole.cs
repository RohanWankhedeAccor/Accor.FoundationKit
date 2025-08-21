namespace Entities.Entites;
public class UserRole
{
    [Required]
    public Guid UserId { get; set; }

    [Required]
    public Guid RoleId { get; set; }

    // Add navigation properties
    public User User { get; set; } = default!;
    public Role Role { get; set; } = default!;

}
