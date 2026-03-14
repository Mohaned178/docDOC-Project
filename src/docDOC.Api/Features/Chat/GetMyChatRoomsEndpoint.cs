using docDOC.Application.Features.Chat.Queries;
using FastEndpoints;
using MediatR;
using System.Collections.Generic;

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
        // Authorized by default
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetMyChatRoomsQuery(), ct);
        await Send.OkAsync(result, ct);

    }
}
