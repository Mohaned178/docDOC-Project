using docDOC.Application.Interfaces;
using docDOC.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace docDOC.Application.Features.Notifications.Commands;

public sealed record MarkAllNotificationsReadCommand() : IRequest;

public sealed class MarkAllNotificationsReadCommandHandler : IRequestHandler<MarkAllNotificationsReadCommand>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<MarkAllNotificationsReadCommandHandler> _logger;

    public MarkAllNotificationsReadCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        ILogger<MarkAllNotificationsReadCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task Handle(MarkAllNotificationsReadCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        _logger.LogInformation("Marking all notifications as read for user {UserId}", userId);

        await _unitOfWork.Notifications.MarkAllAsReadAsync(userId, cancellationToken);

        _logger.LogInformation("Successfully marked all notifications as read for user {UserId}", userId);
    }
}
