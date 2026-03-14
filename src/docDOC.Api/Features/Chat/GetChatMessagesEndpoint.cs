using docDOC.Application.Features.Chat.Queries;
using FastEndpoints;
using MediatR;

namespace docDOC.Api.Features.Chat;

public class GetChatMessagesRequest
{
    public int Id { get; set; }
    [QueryParam] public int? Cursor { get; set; }
    [QueryParam] public int Limit { get; set; } = 20;
}

public class GetChatMessagesEndpoint : Endpoint<GetChatMessagesRequest, ChatMessagesResponse>
{
    private readonly IMediator _mediator;

    public GetChatMessagesEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Get("api/chat/{id}/messages");
    }

    public override async Task HandleAsync(GetChatMessagesRequest req, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetChatMessagesQuery(req.Id, req.Cursor, req.Limit), ct);
        await Send.OkAsync(result, ct);

    }
}
