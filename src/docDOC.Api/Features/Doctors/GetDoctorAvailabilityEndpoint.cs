using docDOC.Application.Features.Doctors.Queries;
using FastEndpoints;
using MediatR;

namespace docDOC.Api.Features.Doctors;

public class GetDoctorAvailabilityRequest
{
    public int Id { get; set; }
    [QueryParam] public DateOnly Date { get; set; }
}

public class GetDoctorAvailabilityEndpoint : Endpoint<GetDoctorAvailabilityRequest, GetDoctorAvailabilityResponse>
{
    private readonly IMediator _mediator;

    public GetDoctorAvailabilityEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Get("api/doctors/{id}/availability");
        Roles("Patient");
    }

    public override async Task HandleAsync(GetDoctorAvailabilityRequest req, CancellationToken ct)
    {
        var response = await _mediator.Send(new GetDoctorAvailabilityQuery(req.Id, req.Date), ct);
        await Send.OkAsync(response, ct);

    }
}
