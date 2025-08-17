using Business.Interfaces;
using Common.DTOs.Users;
using Data.Repositories;
using Entities.Entites;

namespace Business.Services;

public class RoleService : BaseService<Role,
    Guid, RoleItemDto, RoleDetailDto,
    RoleCreateDto, RoleUpdateDto>, IRoleService
{
    private readonly IRoleRepository _roleRepo;

    public RoleService(IRoleRepository roleRepo)
        : base(
            roleRepo,
            toList: MapToList,
            toDetail: MapToDetail,
            fromCreate: MapFromCreate,
            applyUpdate: ApplyUpdate)
    {
        _roleRepo = roleRepo;
    }

    // ----- Read operations -----

    // Optional: ensure DB-side ordering by name if your repo supports it
    public override async Task<IReadOnlyList<RoleItemDto>> ListAsync(CancellationToken ct = default)
    {
        var roles = await _roleRepo.ListAsync(ct);
        return roles.Select(MapToList).ToList();
    }

    // ----- Mutations are blocked for Roles -----

    public override Task<RoleDetailDto> CreateAsync(RoleCreateDto dto, CancellationToken ct = default)
        => Task.FromException<RoleDetailDto>(
            new InvalidOperationException("Roles are system-managed and cannot be created via API."));

    public override Task<RoleDetailDto?> UpdateAsync(Guid id, RoleUpdateDto dto, CancellationToken ct = default)
        => Task.FromException<RoleDetailDto?>(
            new InvalidOperationException("Roles are system-managed and cannot be updated via API."));

    public override Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
        => Task.FromException<bool>(
            new InvalidOperationException("Roles are system-managed and cannot be deleted via API."));

    // ----- Mappers used by BaseService -----

    private static RoleItemDto MapToList(Role r) => new(r.Id, r.Name);

    private static RoleDetailDto MapToDetail(Role r) => new(r.Id, r.Name);

    // Not used (mutations blocked) but required by BaseService
    private static Role MapFromCreate(RoleCreateDto dto) => new()
    {
        Name = dto.Name.Trim()
    };

    // Not used (mutations blocked) but required by BaseService
    private static void ApplyUpdate(Role entity, RoleUpdateDto dto)
    {
        entity.Name = dto.Name.Trim();
    }
}
