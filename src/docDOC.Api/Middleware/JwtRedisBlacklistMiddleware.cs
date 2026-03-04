using System.Security.Claims;
using docDOC.Application.Interfaces;
using Microsoft.AspNetCore.Http;

namespace docDOC.Api.Middleware;

public class JwtRedisBlacklistMiddleware
{
    private readonly RequestDelegate _next;

    public JwtRedisBlacklistMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IRedisService redisService)
    {
        var jti = context.User?.FindFirstValue("jti");

        if (!string.IsNullOrEmpty(jti))
        {
            var isBlacklisted = await redisService.ExistsAsync($"blacklist:{jti}");
            if (isBlacklisted)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }
        }

        await _next(context);
    }
}
