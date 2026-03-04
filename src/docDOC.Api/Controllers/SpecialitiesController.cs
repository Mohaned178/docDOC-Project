using docDOC.Application.Features.Specialities.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace docDOC.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SpecialitiesController : ControllerBase
{
    private readonly IMediator _mediator;

    public SpecialitiesController(IMediator mediator)
    {
        _mediator = mediator;
    }

[HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSpecialities(CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(new GetSpecialitiesQuery(), cancellationToken);
        return Ok(response);
    }
}
