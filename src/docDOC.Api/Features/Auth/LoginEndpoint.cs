using docDOC.Application.Features.Auth.Commands;
using FastEndpoints;
using MediatR;

namespace docDOC.Api.Features.Auth;

public class LoginEndpoint : Endpoint<LoginUserCommand, object>
{
    private readonly IMediator _mediator;

    public LoginEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Post("api/auth/login");
        AllowAnonymous();
    }

    public override async Task HandleAsync(LoginUserCommand req, CancellationToken ct)
    {
        var result = await _mediator.Send(req, ct);
        await Send.OkAsync(result, ct);

    }
}
