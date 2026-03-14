using docDOC.Application.Features.Auth.Commands;
using FastEndpoints;
using MediatR;

namespace docDOC.Api.Features.Auth;

public class LogoutEndpoint : EndpointWithoutRequest
{
    private readonly IMediator _mediator;

    public LogoutEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Post("api/auth/logout");
        // Authorized by default
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        await _mediator.Send(new LogoutUserCommand(), ct);
        await Send.NoContentAsync(ct);

    }
}
