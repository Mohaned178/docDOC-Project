using docDOC.Application.Features.Chat.Commands;
using FastEndpoints;
using MediatR;

namespace docDOC.Api.Features.Chat;

public class MarkMessagesReadRequest
{
    public int Id { get; set; }
}

public class MarkMessagesReadEndpoint : Endpoint<MarkMessagesReadRequest>
{
    private readonly IMediator _mediator;

    public MarkMessagesReadEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Put("api/chat/{id}/messages/read");
    }

    public override async Task HandleAsync(MarkMessagesReadRequest req, CancellationToken ct)
    {
        await _mediator.Send(new MarkMessagesReadCommand(req.Id), ct);
        await Send.NoContentAsync(ct);

    }
}
