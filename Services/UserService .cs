// Business/Services/UserService.cs
using Business.Interfaces;
using Common.DTOs.Paging;
using Common.DTOs.Users;
using Data.Repositories;
using Entities.Entites;
using Microsoft.EntityFrameworkCore;

namespace Business.Services;

public class UserService : BaseService<User,
    Guid, UserListItemDto, UserDetailDto,
    UserCreateDto, UserUpdateDto>, IUserService
{
    private readonly IUserRepository _userRepo;

    public UserService(IUserRepository userRepo)
        : base(
            userRepo,
            toList: MapToList,
            toDetail: MapToDetail,
            fromCreate: MapFromCreate,
            applyUpdate: ApplyUpdate)
    {
        _userRepo = userRepo;
    }

    // ---- Overrides to handle uniqueness, includes, and role writes ----

    public override async Task<UserDetailDto> CreateAsync(UserCreateDto dto, CancellationToken ct = default)
    {
        // Email uniqueness
        if (await _userRepo.EmailExistsAsync(dto.Email, null, ct))
            throw new InvalidOperationException("Email already exists.");

        // Create base entity
        var created = await base.CreateAsync(dto, ct);

        // Set roles if provided
        if (dto.RoleIds is not null && dto.RoleIds.Count > 0)
        {
            await _userRepo.SetRolesAsync(created.Id, dto.RoleIds, ct);
        }

        // Return with roles populated
        var full = await _userRepo.GetWithRolesAsync(created.Id, ct);
        return MapToDetail(full!);
    }

    public override async Task<UserDetailDto?> UpdateAsync(Guid id, UserUpdateDto dto, CancellationToken ct = default)
    {
        // Uniqueness (exclude current id)
        if (await _userRepo.EmailExistsAsync(dto.Email, id, ct))
            throw new InvalidOperationException("Email already exists.");

        // Update base
        var updated = await base.UpdateAsync(id, dto, ct);
        if (updated is null) return null;

        // Replace roles
        await _userRepo.SetRolesAsync(id, dto.RoleIds ?? Array.Empty<Guid>(), ct);

        // Return with roles populated
        var full = await _userRepo.GetWithRolesAsync(id, ct);
        return full is null ? null : MapToDetail(full);
    }

    public override async Task<UserDetailDto?> GetAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await _userRepo.GetWithRolesAsync(id, ct);
        return entity is null ? null : MapToDetail(entity);
    }

    /// <summary>
    /// Include roles and apply search/sort for paged list.
    /// </summary>
    protected override IQueryable<User> BuildListQuery(IQueryable<User> query, PagingRequest request)
    {
        // 1) Keep the variable typed as IQueryable<User> from the very beginning
        IQueryable<User> q = query.Where(u => !u.IsDeleted);

        // 2) Includes (these return IIncludableQueryable<...>, but can be assigned to IQueryable<User>)
        q = q
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role);

        // 3) Search
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var s = request.Search.Trim().ToLower();
            q = q.Where(u =>
                u.FirstName.ToLower().Contains(s) ||
                u.LastName.ToLower().Contains(s) ||
                u.Email.ToLower().Contains(s));
        }

        // 4) Sort (IOrderedQueryable<User> is still assignable to IQueryable<User>)
        var sort = request.Sort?.Trim();
        q = sort switch
        {
            "firstName" => q.OrderBy(u => u.FirstName).ThenBy(u => u.LastName),
            "-firstName" => q.OrderByDescending(u => u.FirstName).ThenByDescending(u => u.LastName),
            "lastName" => q.OrderBy(u => u.LastName).ThenBy(u => u.FirstName),
            "-lastName" => q.OrderByDescending(u => u.LastName).ThenByDescending(u => u.FirstName),
            "email" => q.OrderBy(u => u.Email),
            "-email" => q.OrderByDescending(u => u.Email),
            "createdDate" => q.OrderBy(u => u.CreatedDate),
            "-createdDate" => q.OrderByDescending(u => u.CreatedDate),
            _ => q.OrderByDescending(u => u.CreatedDate)
        };

        return q;
    }

    // ----------------- Mapping helpers -----------------

    private static UserListItemDto MapToList(User u)
        => new(
            u.Id,
            u.FirstName,
            u.LastName,
            u.Email,
            u.Active,
            (u.UserRoles ?? new List<UserRole>())
                .Where(ur => ur.Role != null)
                .Select(ur => new RoleItemDto(ur.RoleId, ur.Role!.Name))
                .ToList()
        );

    private static UserDetailDto MapToDetail(User u)
        => new(
            u.Id,
            u.FirstName,
            u.LastName,
            u.Email,
            u.Active,
            u.CreatedDate,
            u.UpdatedDate,
            (u.UserRoles ?? new List<UserRole>())
                .Where(ur => ur.Role != null)
                .Select(ur => new RoleItemDto(ur.RoleId, ur.Role!.Name))
                .ToList()
        );

    private static User MapFromCreate(UserCreateDto dto)
        => new()
        {
            FirstName = dto.FirstName.Trim(),
            LastName = dto.LastName.Trim(),
            Email = dto.Email.Trim(),
            Active = dto.Active,
            IsDeleted = false
        };

    private static void ApplyUpdate(User entity, UserUpdateDto dto)
    {
        entity.FirstName = dto.FirstName.Trim();
        entity.LastName = dto.LastName.Trim();
        entity.Email = dto.Email.Trim();
        entity.Active = dto.Active;
    }
}
