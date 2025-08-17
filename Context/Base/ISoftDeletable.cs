namespace Entities;

public interface ISoftDeletable
{
    bool IsDeleted { get; set; }
}