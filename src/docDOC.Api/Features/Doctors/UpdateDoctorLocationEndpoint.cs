using docDOC.Application.Features.Doctors.Commands;
using FastEndpoints;
using MediatR;

namespace docDOC.Api.Features.Doctors;

public class UpdateDoctorLocationEndpoint : Endpoint<UpdateDoctorLocationCommand>
{
    private readonly IMediator _mediator;

    public UpdateDoctorLocationEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Put("api/doctors/location");
        Roles("Doctor");
    }

    public override async Task HandleAsync(UpdateDoctorLocationCommand req, CancellationToken ct)
    {
        await _mediator.Send(req, ct);
        await Send.NoContentAsync(ct);

    }
}
