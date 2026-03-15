using docDOC.Application.Features.Chat.Queries;
using FastEndpoints;
using MediatR;

namespace docDOC.Api.Features.Chat;

public class GetMyChatRoomsEndpoint : EndpointWithoutRequest<IEnumerable<ChatRoomListItem>>
{
    private readonly IMediator _mediator;

    public GetMyChatRoomsEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Get("api/chat");
        Summary(s => {
            s.Summary = "Get my chat rooms";
            s.Description = "Retrieves all chat rooms associated with the authenticated user.";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetMyChatRoomsQuery(), ct);
        await Send.OkAsync(result, ct);

    }
}
