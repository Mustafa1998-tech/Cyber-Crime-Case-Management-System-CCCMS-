using FluentValidation;
using Nciems.Application.Common.Exceptions;

namespace Nciems.Api.Middleware;

public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task Invoke(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        logger.LogError(exception, "Request failed: {Path}", context.Request.Path);

        var (statusCode, title, errors) = exception switch
        {
            ValidationException validationException => (
                StatusCodes.Status400BadRequest,
                "Validation error",
                validationException.Errors.Select(x => x.ErrorMessage).ToArray()),
            NotFoundException => (StatusCodes.Status404NotFound, "Not found", Array.Empty<string>()),
            ForbiddenException => (StatusCodes.Status403Forbidden, "Forbidden", Array.Empty<string>()),
            ConflictException => (StatusCodes.Status409Conflict, "Conflict", Array.Empty<string>()),
            _ => (StatusCodes.Status500InternalServerError, "Server error", Array.Empty<string>())
        };

        var detail = statusCode == StatusCodes.Status500InternalServerError
            ? "An unexpected error occurred."
            : exception.Message;

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = statusCode;

        await context.Response.WriteAsJsonAsync(new
        {
            title,
            status = statusCode,
            detail,
            errors,
            traceId = context.TraceIdentifier
        });
    }
}
