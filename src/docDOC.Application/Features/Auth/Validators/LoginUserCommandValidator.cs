using FluentValidation;

namespace docDOC.Application.Features.Auth.Validators;

public class LoginUserCommandValidator : AbstractValidator<Commands.LoginUserCommand>
{
    public LoginUserCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
        RuleFor(x => x.Role)
            .NotEmpty()
            .Must(role => role.Equals("Doctor", StringComparison.OrdinalIgnoreCase) || role.Equals("Patient", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Role must be 'Doctor' or 'Patient'");
    }
}
