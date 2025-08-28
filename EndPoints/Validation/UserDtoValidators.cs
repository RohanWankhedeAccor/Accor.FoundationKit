// Web/Endpoints/Validation/UserDtoValidators.cs
using FluentValidation;

namespace Web.Endpoints.Validation;

public sealed class UserCreateDtoValidator : AbstractValidator<UserCreateDto>
{
    public UserCreateDtoValidator()
    {
        RuleFor(x => x.FirstName)
            .Cascade(CascadeMode.Stop)
            .Must(s => !string.IsNullOrWhiteSpace(s)).WithMessage("FirstName is required.")
            .MaximumLength(100);

        RuleFor(x => x.LastName)
            .Cascade(CascadeMode.Stop)
            .Must(s => !string.IsNullOrWhiteSpace(s)).WithMessage("LastName is required.")
            .MaximumLength(100);

        RuleFor(x => x.Email)
            .Cascade(CascadeMode.Stop)
            .Must(s => !string.IsNullOrWhiteSpace(s)).WithMessage("Email is required.")
            .EmailAddress()
            .MaximumLength(256);

        // Create: RoleIds optional; if supplied, ensure valid
        When(x => x.RoleIds is not null, () =>
        {
            RuleFor(x => x.RoleIds!)
                .Must(list => list.Distinct().Count() == list.Count)
                    .WithMessage("RoleIds must be unique.")
                .Must(list => list.All(id => id != Guid.Empty))
                    .WithMessage("RoleIds must not contain empty GUIDs.");
        });
    }
}

public sealed class UserUpdateDtoValidator : AbstractValidator<UserUpdateDto>
{
    public UserUpdateDtoValidator()
    {
        RuleFor(x => x.FirstName)
            .Cascade(CascadeMode.Stop)
            .Must(s => !string.IsNullOrWhiteSpace(s)).WithMessage("FirstName is required.")
            .MaximumLength(100);

        RuleFor(x => x.LastName)
            .Cascade(CascadeMode.Stop)
            .Must(s => !string.IsNullOrWhiteSpace(s)).WithMessage("LastName is required.")
            .MaximumLength(100);

        RuleFor(x => x.Email)
            .Cascade(CascadeMode.Stop)
            .Must(s => !string.IsNullOrWhiteSpace(s)).WithMessage("Email is required.")
            .EmailAddress()
            .MaximumLength(256);

        // Update: RoleIds null = keep existing; [] = remove all; if supplied, ensure valid
        When(x => x.RoleIds is not null, () =>
        {
            RuleFor(x => x.RoleIds!)
                .Must(list => list.Distinct().Count() == list.Count)
                    .WithMessage("RoleIds must be unique.")
                .Must(list => list.All(id => id != Guid.Empty))
                    .WithMessage("RoleIds must not contain empty GUIDs.");
        });
    }
}
