using System.Security.Claims;

namespace docDOC.Application.Interfaces;

public interface IJwtService
{
    string GenerateToken(int userId, string email, string role, string userType, string jti);
}
