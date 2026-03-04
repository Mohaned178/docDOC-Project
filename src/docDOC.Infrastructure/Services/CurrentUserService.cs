using System.Security.Claims;
using docDOC.Application.Interfaces;
using Microsoft.AspNetCore.Http;

namespace docDOC.Infrastructure.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public int UserId
    {
        get
        {
            var sub = _httpContextAccessor.HttpContext?.User?.FindFirstValue("sub") ?? 
                      _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(sub, out var userId) ? userId : 0;
        }
    }

    public string? Role => _httpContextAccessor.HttpContext?.User?.FindFirstValue("role") ??
                           _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Role);
    
    public string? UserType => _httpContextAccessor.HttpContext?.User?.FindFirstValue("userType");
    
    public string? Jti => _httpContextAccessor.HttpContext?.User?.FindFirstValue("jti");

    public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;
}
