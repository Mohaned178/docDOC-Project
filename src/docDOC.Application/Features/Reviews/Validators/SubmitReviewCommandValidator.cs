using FluentValidation;
using docDOC.Application.Features.Reviews.Commands;

namespace docDOC.Application.Features.Reviews.Validators;

public class SubmitReviewCommandValidator : AbstractValidator<SubmitReviewCommand>
{
    public SubmitReviewCommandValidator()
    {
        RuleFor(x => x.AppointmentId)
            .NotEmpty();

        RuleFor(x => x.Rating)
            .InclusiveBetween(1, 5)
            .WithMessage("Rating must be between 1 and 5.");

        RuleFor(x => x.Comment)
            .MaximumLength(1000)
            .WithMessage("Comment cannot exceed 1000 characters.");
    }
}
