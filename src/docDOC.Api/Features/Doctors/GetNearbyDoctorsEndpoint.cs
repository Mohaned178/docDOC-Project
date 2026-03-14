using docDOC.Application.Features.Doctors.Queries;
using FastEndpoints;
using MediatR;

namespace docDOC.Api.Features.Doctors;

public class GetNearbyDoctorsRequest
{
    [QueryParam] public double Lat { get; set; }
    [QueryParam] public double Lon { get; set; }
    [QueryParam] public double RadiusKm { get; set; } = 10;
    [QueryParam] public int? SpecialityId { get; set; }
}

public class GetNearbyDoctorsEndpoint : Endpoint<GetNearbyDoctorsRequest, GetNearbyDoctorsResponse>
{
    private readonly IMediator _mediator;

    public GetNearbyDoctorsEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Get("api/doctors/nearby");
        Roles("Patient");
    }

    public override async Task HandleAsync(GetNearbyDoctorsRequest req, CancellationToken ct)
    {
        var response = await _mediator.Send(new GetNearbyDoctorsQuery(req.Lat, req.Lon, req.RadiusKm, req.SpecialityId), ct);
        await Send.OkAsync(response, ct);

    }
}
