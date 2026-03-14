using docDOC.Application.Features.Notifications.Queries;
using FastEndpoints;
using MediatR;

namespace docDOC.Api.Features.Notifications;

public class GetNotificationsRequest
{
    [QueryParam] public bool UnreadOnly { get; set; } = false;
    [QueryParam] public int Page { get; set; } = 1;
    [QueryParam] public int PageSize { get; set; } = 20;
}

public class GetNotificationsEndpoint : Endpoint<GetNotificationsRequest, NotificationsResponse>
{
    private readonly IMediator _mediator;

    public GetNotificationsEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Get("api/notifications");
    }

    public override async Task HandleAsync(GetNotificationsRequest req, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetNotificationsQuery(req.UnreadOnly, req.Page, req.PageSize), ct);
        await Send.OkAsync(result, ct);

    }
}
