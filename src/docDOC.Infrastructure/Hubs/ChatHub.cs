using docDOC.Application.Features.Chat.Commands;
using docDOC.Application.Interfaces;
using docDOC.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace docDOC.Infrastructure.Hubs;

[Authorize]
public sealed class ChatHub : Hub
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUserService;
    private readonly IRedisService _redisService;
    private readonly ILogger<ChatHub> _logger;

    public ChatHub(
        IMediator mediator,
        ICurrentUserService currentUserService,
        IRedisService redisService,
        ILogger<ChatHub> logger)
    {
        _mediator = mediator;
        _currentUserService = currentUserService;
        _redisService = redisService;
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = _currentUserService.UserId;
        if (userId != 0)
        {
            _logger.LogInformation("User {UserId} connected to ChatHub", userId);
            await Clients.Others.SendAsync("OnUserOnline", new { UserId = userId });
        }
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = _currentUserService.UserId;
        if (userId != 0)
        {
            _logger.LogInformation("User {UserId} disconnected from ChatHub", userId);
            await Clients.Others.SendAsync("OnUserOffline", new { UserId = userId });
        }
        await base.OnDisconnectedAsync(exception);
    }

    public async Task JoinRoom(string roomId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
        _logger.LogInformation("Connection {ConnectionId} joined room {RoomId}", Context.ConnectionId, roomId);
    }

    public async Task LeaveRoom(string roomId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomId);
        _logger.LogInformation("Connection {ConnectionId} left room {RoomId}", Context.ConnectionId, roomId);
    }

    public async Task SendMessage(int roomId, string content)
    {
        var command = new SendMessageCommand(roomId, content);
        var result = await _mediator.Send(command);
        await Clients.Group(roomId.ToString()).SendAsync("OnReceiveMessage", result);
    }

    public async Task Typing(int roomId)
    {
        var userId = _currentUserService.UserId;
        if (userId == 0) return;

        var typingKey = $"typing:{roomId}:{userId}";
        await _redisService.SetAsync(typingKey, "true", TimeSpan.FromSeconds(3));

        await Clients.OthersInGroup(roomId.ToString()).SendAsync("OnTypingIndicator", new
        {
            UserId = userId,
            SenderType = _currentUserService.UserType
        });
    }

    public async Task MarkRead(int roomId)
    {
        var command = new MarkMessagesReadCommand(roomId);
        await _mediator.Send(command);

        await Clients.OthersInGroup(roomId.ToString()).SendAsync("OnMessagesRead", new
        {
            UserId = _currentUserService.UserId,
            RoomId = roomId
        });
    }
}
