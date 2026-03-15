using docDOC.Application.Features.Chat.Commands;
using FastEndpoints;
using MediatR;

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
        Summary(s => {
            s.Summary = "Create or get chat room";
            s.Description = "Starts a new chat or retrieves an existing one between a patient and doctor.";
        });
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
