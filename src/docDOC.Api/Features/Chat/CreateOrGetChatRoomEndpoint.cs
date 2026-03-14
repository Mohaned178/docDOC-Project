using docDOC.Application.Features.Chat.Commands;
using docDOC.Application.Features.Chat.Queries;
using FastEndpoints;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace docDOC.Api.Features.Chat;

public class CreateOrGetChatRoomEndpoint : Endpoint<CreateOrGetChatRoomCommand, ChatRoomResult>
{
    private readonly IMediator _mediator;

    public CreateOrGetChatRoomEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Post("api/chat");
        // Authorized by default
    }

    public override async Task HandleAsync(CreateOrGetChatRoomCommand req, CancellationToken ct)
    {
        var result = await _mediator.Send(req, ct);
        if (result.IsNew)
            await Send.CreatedAtAsync<GetMyChatRoomsEndpoint>(new { }, result, cancellation: ct);

        else
            await Send.OkAsync(result, ct);

    }
}
