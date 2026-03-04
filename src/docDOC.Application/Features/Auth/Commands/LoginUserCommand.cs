using docDOC.Domain.Entities;
using docDOC.Domain.Enums;
using docDOC.Domain.Interfaces;
using docDOC.Domain.Exceptions;
using docDOC.Application.Interfaces;
using MediatR;
using System.Security.Cryptography;

namespace docDOC.Application.Features.Auth.Commands;

public record LoginUserCommand(string Email, string Password, string Role) : IRequest<AuthResultDto>;

public record AuthUserDto(int Id, string FirstName, string LastName, string Email, string Role);
public record AuthResultDto(string AccessToken, string RefreshToken, int ExpiresIn, AuthUserDto? User = null);

public class LoginUserCommandHandler : IRequestHandler<LoginUserCommand, AuthResultDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJwtService _jwtService;

    public LoginUserCommandHandler(IUnitOfWork unitOfWork, IJwtService jwtService)
    {
        _unitOfWork = unitOfWork;
        _jwtService = jwtService;
    }

    public async Task<AuthResultDto> Handle(LoginUserCommand request, CancellationToken cancellationToken)
    {
        bool isDoctor = request.Role.Equals("Doctor", StringComparison.OrdinalIgnoreCase);
        bool isPatient = request.Role.Equals("Patient", StringComparison.OrdinalIgnoreCase);

        if (!isDoctor && !isPatient)
            throw new ArgumentException("Invalid role");

        int userId;
        string email;
        string firstName;
        string lastName;
        string passwordHash;
        string userTypeStr = isDoctor ? "Doctor" : "Patient";

        if (isPatient)
        {
            var user = await _unitOfWork.Patients.GetByEmailAsync(request.Email, cancellationToken);
            if (user == null) throw new NotFoundException("User not found");
            userId = user.Id;
            email = user.Email;
            firstName = user.FirstName;
            lastName = user.LastName;
            passwordHash = user.PasswordHash;
        }
        else
        {
            var user = await _unitOfWork.Doctors.GetByEmailAsync(request.Email, cancellationToken);
            if (user == null) throw new NotFoundException("User not found");
            userId = user.Id;
            email = user.Email;
            firstName = user.FirstName;
            lastName = user.LastName;
            passwordHash = user.PasswordHash;
        }

        if (!BCrypt.Net.BCrypt.Verify(request.Password, passwordHash))
            throw new UnauthorizedException("Invalid credentials");

        var jti = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
        var userType = Enum.Parse<UserType>(userTypeStr);
        var accessToken = _jwtService.GenerateToken(userId, email, userTypeStr, userTypeStr, jti);

        var rawRefreshToken = GenerateRefreshToken();
        var tokenHash = HashToken(rawRefreshToken);

        var refreshTokenEntity = new RefreshToken
        {
            UserId = userId,
            UserType = userType,
            TokenHash = tokenHash,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(7),
            CreatedAt = DateTimeOffset.UtcNow,
            IsRevoked = false
        };
        await _unitOfWork.RefreshTokens.AddAsync(refreshTokenEntity, cancellationToken);
        var authUser = new AuthUserDto(userId, firstName, lastName, email, userTypeStr);
        return new AuthResultDto(accessToken, rawRefreshToken, 15 * 60, authUser);
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
