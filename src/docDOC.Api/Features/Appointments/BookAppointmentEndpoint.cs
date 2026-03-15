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
        Post("api/appointments/book");
        Roles("Patient");
        Summary(s => {
            s.Summary = "Book a new appointment";
            s.Description = "Allows a patient to book an appointment with a doctor.";
        });
    }

    public override async Task HandleAsync(BookAppointmentCommand req, CancellationToken ct)
    {
        var response = await _mediator.Send(req, ct);
        await Send.CreatedAtAsync<GetMyAppointmentsEndpoint>(new { }, response, cancellation: ct);

    }
}
