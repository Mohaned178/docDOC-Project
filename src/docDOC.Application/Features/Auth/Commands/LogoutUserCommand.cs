using docDOC.Domain.Interfaces;
using docDOC.Application.Interfaces;
using MediatR;
using docDOC.Domain.Exceptions;

namespace docDOC.Application.Features.Auth.Commands;

public record LogoutUserCommand() : IRequest<bool>;

public class LogoutUserCommandHandler : IRequestHandler<LogoutUserCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IRedisService _redisService;

    public LogoutUserCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, IRedisService redisService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _redisService = redisService;
    }

    public async Task<bool> Handle(LogoutUserCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (userId == 0) throw new ForbiddenException("Not authenticated");
        var jti = _currentUserService.Jti ?? throw new ForbiddenException("Invalid token");

        await _unitOfWork.RefreshTokens.RevokeAllForUserAsync(userId, cancellationToken);

await _redisService.SetAsync($"blacklist:{jti}", "true", TimeSpan.FromMinutes(15));

        return true;
    }
}
