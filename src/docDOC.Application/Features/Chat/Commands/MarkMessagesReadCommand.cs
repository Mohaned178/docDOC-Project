using docDOC.Application.Interfaces;
using docDOC.Domain.Exceptions;
using docDOC.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace docDOC.Application.Features.Chat.Commands;

public sealed record MarkMessagesReadCommand(int ChatRoomId) : IRequest;

public sealed class MarkMessagesReadCommandHandler : IRequestHandler<MarkMessagesReadCommand>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IRedisService _redisService;
    private readonly ILogger<MarkMessagesReadCommandHandler> _logger;

    public MarkMessagesReadCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IRedisService redisService,
        ILogger<MarkMessagesReadCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _redisService = redisService;
        _logger = logger;
    }

    public async Task Handle(MarkMessagesReadCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;

        _logger.LogInformation("Marking messages as read in room {RoomId} for user {UserId}", request.ChatRoomId, userId);

        var chatRoom = await _unitOfWork.ChatRooms.GetByIdAsync(request.ChatRoomId, cancellationToken)
            ?? throw new NotFoundException("Chat room not found");

if (chatRoom.PatientId != userId && chatRoom.DoctorId != userId)
        {
            throw new ForbiddenException("You are not a participant in this chat room.");
        }

await _unitOfWork.Messages.MarkAsReadAsync(chatRoom.Id, userId, cancellationToken);

var unreadKey = $"unread:{chatRoom.Id}:{userId}";
        await _redisService.RemoveAsync(unreadKey);

        _logger.LogInformation("Successfully marked messages as read in room {RoomId}", request.ChatRoomId);
    }
}
