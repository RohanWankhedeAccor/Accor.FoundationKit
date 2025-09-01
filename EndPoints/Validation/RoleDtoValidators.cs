// Web/Endpoints/Validation/RoleDtoValidators.cs
using FluentValidation;

namespace Web.Endpoints.Validation;

public sealed class RoleCreateDtoValidator : AbstractValidator<RoleCreateDto>
{
    public RoleCreateDtoValidator()
    {
        RuleFor(x => x.Name)
            .Cascade(CascadeMode.Stop)
            .Must(s => !string.IsNullOrWhiteSpace(s)).WithMessage("Name is required.")
            .MaximumLength(50);
    }
}

public sealed class RoleUpdateDtoValidator : AbstractValidator<RoleUpdateDto>
{
    public RoleUpdateDtoValidator()
    {
        RuleFor(x => x.Name)
            .Cascade(CascadeMode.Stop)
            .Must(s => !string.IsNullOrWhiteSpace(s)).WithMessage("Name is required.")
            .MaximumLength(50);
    }
}
