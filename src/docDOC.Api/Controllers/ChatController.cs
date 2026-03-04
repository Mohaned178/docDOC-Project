using docDOC.Application.Features.Chat.Commands;
using docDOC.Application.Features.Chat.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace docDOC.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public sealed class ChatController : ControllerBase
{
    private readonly IMediator _mediator;

    public ChatController(IMediator mediator)
    {
        _mediator = mediator;
    }

[HttpPost]
    [ProducesResponseType(typeof(ChatRoomResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ChatRoomResult), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateOrGet([FromBody] CreateOrGetChatRoomCommand command)
    {
        var result = await _mediator.Send(command);
        if (result.IsNew)
            return CreatedAtAction(nameof(GetMyRooms), new { }, result);
        return Ok(result);
    }

[HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ChatRoomListItem>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyRooms()
    {
        var result = await _mediator.Send(new GetMyChatRoomsQuery());
        return Ok(result);
    }

[HttpGet("{id}/messages")]
    [ProducesResponseType(typeof(ChatMessagesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMessages(int id, [FromQuery] int? cursor = null, [FromQuery] int limit = 20)
    {
        var result = await _mediator.Send(new GetChatMessagesQuery(id, cursor, limit));
        return Ok(result);
    }

[HttpPut("{id}/messages/read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkRead(int id)
    {
        await _mediator.Send(new MarkMessagesReadCommand(id));
        return NoContent();
    }
}
