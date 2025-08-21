

namespace Business.Services;

public class BaseService<TEntity, TId, TListDto, TDetailDto, TCreateDto, TUpdateDto>
    : IBaseService<TListDto, TDetailDto, TCreateDto, TUpdateDto, TId>
    where TEntity : BaseEntity
{
    protected readonly IBaseRepository<TEntity, TId> _repo;

    private readonly Func<TEntity, TListDto> _toList;
    private readonly Func<TEntity, TDetailDto> _toDetail;
    private readonly Func<TCreateDto, TEntity> _fromCreate;
    private readonly Action<TEntity, TUpdateDto> _applyUpdate;

    public BaseService(
        IBaseRepository<TEntity, TId> repo,
        Func<TEntity, TListDto> toList,
        Func<TEntity, TDetailDto> toDetail,
        Func<TCreateDto, TEntity> fromCreate,
        Action<TEntity, TUpdateDto> applyUpdate)
    {
        _repo = repo;
        _toList = toList;
        _toDetail = toDetail;
        _fromCreate = fromCreate;
        _applyUpdate = applyUpdate;
    }

    public virtual async Task<IReadOnlyList<TListDto>> ListAsync(CancellationToken ct = default)
    {
        var all = await _repo.ListAsync(ct);
        return all.Select(_toList).ToList();
    }

    public virtual async Task<PagedResult<TListDto>> ListPagedAsync(PagingRequest request, CancellationToken ct = default)
    {
        var page = request.Page <= 0 ? 1 : request.Page;
        var pageSize = request.PageSize <= 0 ? 20 : request.PageSize;

        // Start from repository IQueryable
        IQueryable<TEntity> q = _repo.Queryable;

        // Let derived services customize (includes/search/sort)
        q = BuildListQuery(q, request);

        var total = await q.CountAsync(ct);
        var items = await q.Skip((page - 1) * pageSize)
                           .Take(pageSize)
                           .ToListAsync(ct);

        var listDtos = items.Select(_toList).ToList();
        return new PagedResult<TListDto>(listDtos, page, pageSize, total);
    }

    public virtual async Task<TDetailDto?> GetAsync(TId id, CancellationToken ct = default)
    {
        var entity = await _repo.GetByIdAsync(id, ct);
        return entity is null ? default : _toDetail(entity);
    }

    public virtual async Task<TDetailDto> CreateAsync(TCreateDto dto, CancellationToken ct = default)
    {
        var entity = _fromCreate(dto);
        entity = await _repo.AddAsync(entity, ct);
        return _toDetail(entity);
    }

    public virtual async Task<TDetailDto?> UpdateAsync(TId id, TUpdateDto dto, CancellationToken ct = default)
    {
        var entity = await _repo.GetByIdAsync(id, ct);
        if (entity is null) return default;

        _applyUpdate(entity, dto);
        var updated = await _repo.UpdateAsync(entity, ct);
        return updated is null ? default : _toDetail(updated);
    }

    public virtual Task<bool> DeleteAsync(TId id, CancellationToken ct = default)
        => _repo.DeleteAsync(id, ct);

    /// <summary>
    /// Override in derived services to apply includes/search/sort.
    /// Default: order by CreatedDate (if available) then Id for stable results.
    /// </summary>
    protected virtual IQueryable<TEntity> BuildListQuery(IQueryable<TEntity> query, PagingRequest request)
    {
        // basic default sort
        return query.OrderBy(e => e.CreatedDate);
    }
}
