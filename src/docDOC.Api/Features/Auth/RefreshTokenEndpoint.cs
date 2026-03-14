using docDOC.Application.Features.Auth.Commands;
using FastEndpoints;
using MediatR;

namespace docDOC.Api.Features.Auth;

public class RefreshTokenEndpoint : Endpoint<RefreshTokenCommand, object>
{
    private readonly IMediator _mediator;

    public RefreshTokenEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Post("api/auth/refresh-token");
        AllowAnonymous();
    }

    public override async Task HandleAsync(RefreshTokenCommand req, CancellationToken ct)
    {
        var result = await _mediator.Send(req, ct);
        await Send.OkAsync(result, ct);

    }
}
