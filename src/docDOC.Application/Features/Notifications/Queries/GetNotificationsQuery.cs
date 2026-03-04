using docDOC.Application.Interfaces;
using docDOC.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace docDOC.Application.Features.Notifications.Queries;

public sealed record GetNotificationsQuery(bool UnreadOnly = false, int Page = 1, int PageSize = 20) : IRequest<NotificationsResponse>;

public sealed class GetNotificationsQueryHandler : IRequestHandler<GetNotificationsQuery, NotificationsResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<GetNotificationsQueryHandler> _logger;

    public GetNotificationsQueryHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        ILogger<GetNotificationsQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<NotificationsResponse> Handle(GetNotificationsQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        _logger.LogInformation("Fetching notifications for user {UserId} (UnreadOnly: {UnreadOnly})", userId, request.UnreadOnly);

        var notifications = await _unitOfWork.Notifications.GetPagedAsync(
            userId, request.UnreadOnly, request.Page, request.PageSize, cancellationToken);

        var totalCount = await _unitOfWork.Notifications.GetTotalCountAsync(userId, cancellationToken);
        var unreadCount = await _unitOfWork.Notifications.GetUnreadCountAsync(userId, cancellationToken);

        var items = notifications.Select(n => new NotificationResponse(
            n.Id, n.EventType, n.Content, n.ReferenceId, n.IsRead, n.CreatedAt));

        return new NotificationsResponse(items, totalCount, unreadCount);
    }
}

public sealed record NotificationResponse(
    int Id, string EventType, string Content,
    int? ReferenceId, bool IsRead, DateTimeOffset CreatedAt);

public sealed record NotificationsResponse(
    IEnumerable<NotificationResponse> Items,
    int TotalCount, int UnreadCount);
