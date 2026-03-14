using docDOC.Application.Features.Reviews.Commands;
using FastEndpoints;
using MediatR;

namespace docDOC.Api.Features.Reviews;

public class SubmitReviewEndpoint : Endpoint<SubmitReviewCommand, SubmitReviewResponse>
{
    private readonly IMediator _mediator;

    public SubmitReviewEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Post("api/reviews");
        Roles("Patient");
    }

    public override async Task HandleAsync(SubmitReviewCommand req, CancellationToken ct)
    {
        var result = await _mediator.Send(req, ct);
        await Send.CreatedAtAsync<SubmitReviewEndpoint>(new { }, result, cancellation: ct);
 // Actually just sending 201 Created is fine
    }
}
