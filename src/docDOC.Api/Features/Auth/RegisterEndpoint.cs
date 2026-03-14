using docDOC.Application.Features.Auth.Commands;
using FastEndpoints;
using MediatR;

namespace docDOC.Api.Features.Auth;

public class RegisterEndpoint : Endpoint<RegisterUserCommand, object>
{
    private readonly IMediator _mediator;

    public RegisterEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Post("api/auth/register");
        AllowAnonymous();
    }

    public override async Task HandleAsync(RegisterUserCommand req, CancellationToken ct)
    {
        var result = await _mediator.Send(req, ct);
        // Created without URL
        await HttpContext.Response.SendAsync(result, 201, cancellation: ct);


    }
}
