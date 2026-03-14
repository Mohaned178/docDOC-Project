using docDOC.Application.Features.Specialities.Queries;
using FastEndpoints;
using MediatR;

namespace docDOC.Api.Features.Specialities;

public class GetSpecialitiesEndpoint : EndpointWithoutRequest<object>
{
    private readonly IMediator _mediator;

    public GetSpecialitiesEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Get("api/specialities");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var response = await _mediator.Send(new GetSpecialitiesQuery(), ct);
        await Send.OkAsync(response, ct);

    }
}
