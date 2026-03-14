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
        // Authorized by default if we don't say AllowAnonymous(), but we can explicitly call Policies if needed or just leave it depending on global auth rules.
    }

    public override async Task HandleAsync(GetMyAppointmentsRequest req, CancellationToken ct)
    {
        var response = await _mediator.Send(new GetMyAppointmentsQuery(req.Status, req.Page, req.PageSize), ct);
        await Send.OkAsync(response, ct);

    }
}
