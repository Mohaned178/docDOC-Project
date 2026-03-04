using docDOC.Application.Interfaces;
using docDOC.Domain.Exceptions;
using docDOC.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace docDOC.Application.Features.Notifications.Commands;

public sealed record MarkNotificationReadCommand(int Id) : IRequest;

public sealed class MarkNotificationReadCommandHandler : IRequestHandler<MarkNotificationReadCommand>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<MarkNotificationReadCommandHandler> _logger;

    public MarkNotificationReadCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        ILogger<MarkNotificationReadCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task Handle(MarkNotificationReadCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        _logger.LogInformation("Marking notification {Id} as read for user {UserId}", request.Id, userId);

        var notification = await _unitOfWork.Notifications.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("Notification not found");

        if (notification.UserId != userId)
        {
            _logger.LogWarning("Forbidden: User {UserId} attempted to mark notification {Id} of User {OwnerId} as read", 
                userId, request.Id, notification.UserId);
            throw new ForbiddenException("You can only mark your own notifications as read.");
        }

        notification.IsRead = true;
        _unitOfWork.Notifications.Update(notification);

_logger.LogInformation("Notification {Id} marked as read", request.Id);
    }
}
