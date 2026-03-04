using System.Net;
using System.Text.Json;
using docDOC.Domain.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace docDOC.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        int statusCode;
        string result;

        switch (exception)
        {
            case NotFoundException notFoundException:
                statusCode = (int)HttpStatusCode.NotFound;
                result = JsonSerializer.Serialize(new { error = notFoundException.Message });
                break;
            case ForbiddenException forbiddenException:
                statusCode = (int)HttpStatusCode.Forbidden;
                result = JsonSerializer.Serialize(new { error = forbiddenException.Message });
                break;
            case UnauthorizedException unauthorizedException:
                statusCode = (int)HttpStatusCode.Unauthorized;
                result = JsonSerializer.Serialize(new { error = unauthorizedException.Message });
                break;
            case ConflictException conflictException:
                statusCode = (int)HttpStatusCode.Conflict;
                result = JsonSerializer.Serialize(new { error = conflictException.Message });
                break;
            case ValidationException validationException:
                statusCode = (int)HttpStatusCode.BadRequest;
                result = JsonSerializer.Serialize(new { error = "Validation failed", details = validationException.Errors.Select(e => e.ErrorMessage) });
                break;
            case DomainException domainException:
                statusCode = (int)HttpStatusCode.BadRequest;
                result = JsonSerializer.Serialize(new { error = domainException.Message });
                break;
            default:
                statusCode = (int)HttpStatusCode.InternalServerError;
                result = JsonSerializer.Serialize(new { error = "An unexpected error occurred", details = exception.ToString() });
                break;
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;

        return context.Response.WriteAsync(result);
    }
}
