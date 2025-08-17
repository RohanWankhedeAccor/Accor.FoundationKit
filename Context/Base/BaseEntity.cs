namespace Entities.Base;

public abstract class BaseEntity
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public string CreatedBy { get; set; } = string.Empty;

    [Required]
    public DateTime CreatedDate { get; set; }

    public string? UpdatedBy { get; set; }

    public DateTime? UpdatedDate { get; set; }

}