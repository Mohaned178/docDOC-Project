using docDOC.Application.Features.Appointments.Queries;
using docDOC.Domain.Enums;
using FastEndpoints;
using MediatR;

namespace docDOC.Api.Features.Appointments;

public class GetMyAppointmentsRequest
{
    [QueryParam] public AppointmentStatus? Status { get; set; }
    [QueryParam] public int Page { get; set; } = 1;
    [QueryParam] public int PageSize { get; set; } = 20;
}

public class GetMyAppointmentsEndpoint : Endpoint<GetMyAppointmentsRequest, object>
{
    private readonly IMediator _mediator;

    public GetMyAppointmentsEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Get("api/appointments/mine");
        Summary(s => {
            s.Summary = "Get current user's appointments";
            s.Description = "Retrieves a list of appointments for the authenticated patient or doctor.";
        });
    }

    public override async Task HandleAsync(GetMyAppointmentsRequest req, CancellationToken ct)
    {
        var response = await _mediator.Send(new GetMyAppointmentsQuery(req.Status, req.Page, req.PageSize), ct);
        await Send.OkAsync(response, ct);

    }
}
