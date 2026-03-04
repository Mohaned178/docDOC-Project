using docDOC.Application.Interfaces;
using docDOC.Domain.Exceptions;
using docDOC.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace docDOC.Application.Features.Chat.Queries;

public sealed record GetChatMessagesQuery(int ChatRoomId, int? Cursor = null, int Limit = 20) : IRequest<ChatMessagesResponse>;

public sealed record MessageResponse(
    int Id, int SenderId, string SenderType,
    string Content, string Status, DateTimeOffset SentAt);

public sealed record ChatMessagesResponse(
    IEnumerable<MessageResponse> Messages, int? NextCursor, bool HasMore);

public sealed class GetChatMessagesQueryHandler : IRequestHandler<GetChatMessagesQuery, ChatMessagesResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<GetChatMessagesQueryHandler> _logger;

    public GetChatMessagesQueryHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        ILogger<GetChatMessagesQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<ChatMessagesResponse> Handle(GetChatMessagesQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        _logger.LogInformation("Fetching messages for room {RoomId}, user {UserId}, cursor {Cursor}", request.ChatRoomId, userId, request.Cursor);

        var chatRoom = await _unitOfWork.ChatRooms.GetByIdAsync(request.ChatRoomId, cancellationToken)
            ?? throw new NotFoundException("Chat room not found");

        if (chatRoom.PatientId != userId && chatRoom.DoctorId != userId)
        {
            throw new ForbiddenException("You are not a participant in this chat room.");
        }

        var messages = await _unitOfWork.Messages.GetPagedByCursorAsync(chatRoom.Id, request.Cursor, request.Limit + 1, cancellationToken);
        var messageList = messages.ToList();

        bool hasMore = messageList.Count > request.Limit;
        if (hasMore) messageList = messageList.Take(request.Limit).ToList();

        int? nextCursor = hasMore && messageList.Any() ? messageList.Last().Id : null;

        var items = messageList.Select(m => new MessageResponse(
            m.Id, m.SenderId, m.SenderType.ToString(),
            m.Content, m.Status.ToString(), m.SentAt));

        return new ChatMessagesResponse(items, nextCursor, hasMore);
    }
}
