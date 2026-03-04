using docDOC.Application.Features.Doctors.Commands;
using docDOC.Application.Features.Doctors.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace docDOC.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DoctorsController : ControllerBase
{
    private readonly IMediator _mediator;

    public DoctorsController(IMediator mediator)
    {
        _mediator = mediator;
    }

[Authorize(Roles = "Patient")]
    [HttpGet("nearby")]
    [ProducesResponseType(typeof(GetNearbyDoctorsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetNearby([FromQuery] double lat, [FromQuery] double lon, [FromQuery] double radiusKm = 10, [FromQuery] int? specialityId = null, CancellationToken cancellationToken = default)
    {
        var response = await _mediator.Send(new GetNearbyDoctorsQuery(lat, lon, radiusKm, specialityId), cancellationToken);
        return Ok(response);
    }

[Authorize(Roles = "Patient")]
    [HttpGet("{id}/availability")]
    [ProducesResponseType(typeof(GetDoctorAvailabilityResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAvailability(int id, [FromQuery] DateOnly date, CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(new GetDoctorAvailabilityQuery(id, date), cancellationToken);
        return Ok(response);
    }

[Authorize(Roles = "Doctor")]
    [HttpPut("location")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateLocation([FromBody] UpdateDoctorLocationCommand command, CancellationToken cancellationToken)
    {
        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }
}
