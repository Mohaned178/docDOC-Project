using docDOC.Application.Interfaces;
using docDOC.Domain.Entities;
using docDOC.Domain.Enums;
using docDOC.Domain.Exceptions;
using docDOC.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using docDOC.Application.Features.Chat.Queries; 

namespace docDOC.Application.Features.Chat.Commands;

public sealed record SendMessageCommand(int ChatRoomId, string Content) : IRequest<MessageResponse>;

public sealed class SendMessageCommandHandler : IRequestHandler<SendMessageCommand, MessageResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IRedisService _redisService;
    private readonly INotificationDispatcher _notificationDispatcher;
    private readonly ILogger<SendMessageCommandHandler> _logger;

    public SendMessageCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IRedisService redisService,
        INotificationDispatcher notificationDispatcher,
        ILogger<SendMessageCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _redisService = redisService;
        _notificationDispatcher = notificationDispatcher;
        _logger = logger;
    }

    public async Task<MessageResponse> Handle(SendMessageCommand request, CancellationToken cancellationToken)
    {
        var senderId = _currentUserService.UserId;
        var userType = _currentUserService.UserType switch
        {
            "Patient" => UserType.Patient,
            "Doctor" => UserType.Doctor,
            _ => throw new ForbiddenException("Invalid user type")
        };

        _logger.LogInformation("User {SenderId} ({UserType}) sending message to room {RoomId}", senderId, userType, request.ChatRoomId);

        var chatRoom = await _unitOfWork.ChatRooms.GetByIdAsync(request.ChatRoomId, cancellationToken)
            ?? throw new NotFoundException("Chat room not found");

        if (!chatRoom.IsActive)
        {
            _logger.LogWarning("Message rejected: Chat room {RoomId} is soft-closed", chatRoom.Id);
            throw new ForbiddenException("This chat room is closed.");
        }

if (chatRoom.PatientId != senderId && chatRoom.DoctorId != senderId)
        {
            throw new ForbiddenException("You are not a participant in this chat room.");
        }

        var recipientId = chatRoom.PatientId == senderId ? chatRoom.DoctorId : chatRoom.PatientId;
        var recipientType = chatRoom.PatientId == senderId ? UserType.Doctor : UserType.Patient;

var message = new Message
        {
            ChatRoomId = chatRoom.Id,
            SenderId = senderId,
            SenderType = userType == UserType.Patient ? SenderType.Patient : SenderType.Doctor,
            Content = request.Content,
            Status = MessageStatus.Sent,
            SentAt = DateTimeOffset.UtcNow
        };

        await _unitOfWork.Messages.AddAsync(message, cancellationToken);

chatRoom.UpdatedAt = DateTimeOffset.UtcNow;
        _unitOfWork.ChatRooms.Update(chatRoom);

var unreadKey = $"unread:{chatRoom.Id}:{recipientId}";
        await _redisService.IncrementAsync(unreadKey);

await _notificationDispatcher.SendAsync(
            recipientId,
            recipientType,
            "new_message",
            request.Content.Length > 50 ? request.Content[..47] + "..." : request.Content,
            chatRoom.Id);

        return new MessageResponse(
            message.Id,
            message.SenderId,
            message.SenderType.ToString(),
            message.Content,
            message.Status.ToString(),
            message.SentAt);
    }
}
