// Data/Repositories/EfBaseRepository.cs
using AppContext.Context;
using Microsoft.EntityFrameworkCore;

namespace Data.Repositories;

public class EfBaseRepository<TEntity, TId> : IBaseRepository<TEntity, TId>
    where TEntity : class
{
    protected readonly AppDbContext _db;
    protected readonly DbSet<TEntity> _set;

    public EfBaseRepository(AppDbContext db)
    {
        _db = db;
        _set = db.Set<TEntity>();
    }

    public IQueryable<TEntity> Queryable => _set.AsQueryable();

    public virtual async Task<IReadOnlyList<TEntity>> ListAsync(CancellationToken ct = default)
        => await _set.ToListAsync(ct);

    public virtual async Task<TEntity?> GetByIdAsync(TId id, CancellationToken ct = default)
        => await _set.FindAsync([id], ct);

    public virtual async Task<TEntity> AddAsync(TEntity entity, CancellationToken ct = default)
    {
        await _set.AddAsync(entity, ct);
        await _db.SaveChangesAsync(ct);
        return entity;
    }

    public virtual async Task<TEntity?> UpdateAsync(TEntity entity, CancellationToken ct = default)
    {
        _set.Update(entity);
        await _db.SaveChangesAsync(ct);
        return entity;
    }

    public virtual async Task<bool> DeleteAsync(TId id, CancellationToken ct = default)
    {
        var entity = await GetByIdAsync(id, ct);
        if (entity is null) return false;
        _set.Remove(entity);
        await _db.SaveChangesAsync(ct);
        return true;
    }
}
