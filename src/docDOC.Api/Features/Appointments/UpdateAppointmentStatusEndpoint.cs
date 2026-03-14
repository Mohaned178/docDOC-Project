using docDOC.Application.Features.Appointments.Commands;
using docDOC.Domain.Enums;
using FastEndpoints;
using MediatR;

namespace docDOC.Api.Features.Appointments;

public class UpdateAppointmentStatusRequest
{
    public int Id { get; set; }
    public AppointmentStatus Status { get; set; }
}

public class UpdateAppointmentStatusEndpoint : Endpoint<UpdateAppointmentStatusRequest>
{
    private readonly IMediator _mediator;

    public UpdateAppointmentStatusEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Patch("api/appointments/{id}/status");
    }

    public override async Task HandleAsync(UpdateAppointmentStatusRequest req, CancellationToken ct)
    {
        var command = new UpdateAppointmentStatusCommand(req.Id, req.Status);
        await _mediator.Send(command, ct);
        await Send.NoContentAsync(ct);

    }
}
