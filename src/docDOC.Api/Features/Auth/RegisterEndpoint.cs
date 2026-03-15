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
        Summary(s => {
            s.Summary = "User registration";
            s.Description = "Registers a new patient or doctor in the system.";
        });
    }

    public override async Task HandleAsync(RegisterUserCommand req, CancellationToken ct)
    {
        var result = await _mediator.Send(req, ct);
        
        await HttpContext.Response.SendAsync(result, 201, cancellation: ct);


    }
}
