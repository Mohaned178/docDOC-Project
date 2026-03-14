using docDOC.Application.Features.Notifications.Commands;
using FastEndpoints;
using MediatR;

namespace docDOC.Api.Features.Notifications;

public class MarkNotificationReadRequest
{
    public int Id { get; set; }
}

public class MarkNotificationReadEndpoint : Endpoint<MarkNotificationReadRequest>
{
    private readonly IMediator _mediator;

    public MarkNotificationReadEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Put("api/notifications/{id}/read");
    }

    public override async Task HandleAsync(MarkNotificationReadRequest req, CancellationToken ct)
    {
        await _mediator.Send(new MarkNotificationReadCommand(req.Id), ct);
        await Send.NoContentAsync(ct);

    }
}
