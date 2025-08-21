
namespace Business.Interfaces;

public interface IBaseService<TListDto, TDetailDto, TCreateDto, TUpdateDto, TId>
{
    Task<IReadOnlyList<TListDto>> ListAsync(CancellationToken ct = default);
    Task<PagedResult<TListDto>> ListPagedAsync(PagingRequest request, CancellationToken ct = default);
    Task<TDetailDto?> GetAsync(TId id, CancellationToken ct = default);
    Task<TDetailDto> CreateAsync(TCreateDto dto, CancellationToken ct = default);
    Task<TDetailDto?> UpdateAsync(TId id, TUpdateDto dto, CancellationToken ct = default);
    Task<bool> DeleteAsync(TId id, CancellationToken ct = default);
}
