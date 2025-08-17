// Data/Repositories/IBaseRepository.cs
namespace Data.Repositories;

public interface IBaseRepository<TEntity, TId>
{
    // Expose IQueryable so services can compose paging/sorting/includes.
    IQueryable<TEntity> Queryable { get; }

    Task<IReadOnlyList<TEntity>> ListAsync(CancellationToken ct = default);
    Task<TEntity?> GetByIdAsync(TId id, CancellationToken ct = default);
    Task<TEntity> AddAsync(TEntity entity, CancellationToken ct = default);
    Task<TEntity?> UpdateAsync(TEntity entity, CancellationToken ct = default);
    Task<bool> DeleteAsync(TId id, CancellationToken ct = default);
}
