using docDOC.Domain.Entities;
using docDOC.Domain.Enums;
using docDOC.Domain.Interfaces;
using docDOC.Domain.Exceptions;
using docDOC.Application.Interfaces;
using MediatR;
using System.Security.Cryptography;

namespace docDOC.Application.Features.Auth.Commands;

public record RefreshTokenCommand(string RefreshToken) : IRequest<AuthResultDto>;

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, AuthResultDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJwtService _jwtService;

    public RefreshTokenCommandHandler(IUnitOfWork unitOfWork, IJwtService jwtService)
    {
        _unitOfWork = unitOfWork;
        _jwtService = jwtService;
    }

    public async Task<AuthResultDto> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var tokenHash = HashToken(request.RefreshToken);
        var existingToken = await _unitOfWork.RefreshTokens.GetByHashAsync(tokenHash, cancellationToken);

        if (existingToken == null || existingToken.IsRevoked || existingToken.ExpiresAt <= DateTimeOffset.UtcNow)
            throw new UnauthorizedException("Invalid or expired refresh token");

existingToken.IsRevoked = true;
        _unitOfWork.RefreshTokens.Update(existingToken);

string email;
        string firstName;
        string lastName;
        string roleStr = existingToken.UserType.ToString();

        if (existingToken.UserType == UserType.Patient)
        {
            var user = await _unitOfWork.Patients.GetByIdAsync(existingToken.UserId, cancellationToken)
                ?? throw new NotFoundException("User not found");
            email = user.Email;
            firstName = user.FirstName;
            lastName = user.LastName;
        }
        else
        {
            var user = await _unitOfWork.Doctors.GetByIdAsync(existingToken.UserId, cancellationToken)
                ?? throw new NotFoundException("User not found");
            email = user.Email;
            firstName = user.FirstName;
            lastName = user.LastName;
        }

        var jti = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
        var accessToken = _jwtService.GenerateToken(existingToken.UserId, email, roleStr, roleStr, jti);

        var newRawRefreshToken = GenerateRefreshToken();
        var newTokenHash = HashToken(newRawRefreshToken);

        var newRefreshTokenEntity = new RefreshToken
        {
            UserId = existingToken.UserId,
            UserType = existingToken.UserType,
            TokenHash = newTokenHash,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(7),
            CreatedAt = DateTimeOffset.UtcNow,
            IsRevoked = false
        };

        await _unitOfWork.RefreshTokens.AddAsync(newRefreshTokenEntity, cancellationToken);

var authUser = new AuthUserDto(existingToken.UserId, firstName, lastName, email, roleStr);
        return new AuthResultDto(accessToken, newRawRefreshToken, 15 * 60, authUser);
    }

    private string GenerateRefreshToken()
    {
        var randomBytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    private string HashToken(string token)
    {
        using var sha256 = SHA256.Create();
        var bytes = System.Text.Encoding.UTF8.GetBytes(token);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }
}
