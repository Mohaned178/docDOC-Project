namespace docDOC.Application.Interfaces;

public interface ICurrentUserService
{
    int UserId { get; }
    string? Role { get; }
    string? UserType { get; }
    string? Jti { get; }
    bool IsAuthenticated { get; }
}
