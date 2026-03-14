using docDOC.Application.Features.Appointments.Commands;
using FastEndpoints;
using MediatR;

namespace docDOC.Api.Features.Appointments;

public class BookAppointmentEndpoint : Endpoint<BookAppointmentCommand, object>
{
    private readonly IMediator _mediator;

    public BookAppointmentEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Post("api/appointments");
        Roles("Patient");
    }

    public override async Task HandleAsync(BookAppointmentCommand req, CancellationToken ct)
    {
        var response = await _mediator.Send(req, ct);
        // CreatedAtAsync is typically handled by mapping the location header, but for now we can just send SendCreatedAtAsync or SendOkAsync
        await Send.CreatedAtAsync<GetMyAppointmentsEndpoint>(new { }, response, cancellation: ct);

    }
}
