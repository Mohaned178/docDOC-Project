using docDOC.Application.Features.Notifications.Commands;
using FastEndpoints;
using MediatR;

namespace docDOC.Api.Features.Notifications;

public class MarkAllNotificationsReadEndpoint : EndpointWithoutRequest
{
    private readonly IMediator _mediator;

    public MarkAllNotificationsReadEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Put("api/notifications/read-all");
        Summary(s => {
            s.Summary = "Mark all as read";
            s.Description = "Marks all notifications for the current user as read.";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        await _mediator.Send(new MarkAllNotificationsReadCommand(), ct);
        await Send.NoContentAsync(ct);

    }
}
