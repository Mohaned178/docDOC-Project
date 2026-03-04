using FluentValidation;

namespace docDOC.Application.Features.Auth.Validators;

public class RegisterUserCommandValidator : AbstractValidator<Commands.RegisterUserCommand>
{
    public RegisterUserCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6);
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(50);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Role)
            .NotEmpty()
            .Must(role => role.Equals("Doctor", StringComparison.OrdinalIgnoreCase) || role.Equals("Patient", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Role must be 'Doctor' or 'Patient'");

        When(x => x.Role.Equals("Doctor", StringComparison.OrdinalIgnoreCase), () =>
        {
            RuleFor(x => x.SpecialityId).NotNull().WithMessage("SpecialityId is required for Doctors");
        });

        When(x => x.Role.Equals("Patient", StringComparison.OrdinalIgnoreCase), () =>
        {
            RuleFor(x => x.DateOfBirth).NotNull().WithMessage("DateOfBirth is required for Patients");
            RuleFor(x => x.Gender).NotEmpty().WithMessage("Gender is required for Patients");
        });
    }
}
