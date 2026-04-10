using System.Net;
using TTS.Api.DTOs;
using TTS.Application.Exceptions;

namespace TTS.Api.Middleware;

public sealed class GlobalExceptionHandlingMiddleware : IMiddleware
{
    private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;

    public GlobalExceptionHandlingMiddleware(ILogger<GlobalExceptionHandlingMiddleware> logger)
    {
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (TextToSqlValidationException exception)
        {
            _logger.LogWarning(exception, "A validation error occurred while handling a text-to-sql request.");
            await WriteErrorAsync(context, HttpStatusCode.BadRequest, "The request could not be processed.", exception.Errors);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "An unhandled exception occurred while processing the request.");
            await WriteErrorAsync(context, HttpStatusCode.InternalServerError, "An unexpected error occurred.", []);
        }
    }

    private static Task WriteErrorAsync(HttpContext context, HttpStatusCode statusCode, string message, IReadOnlyCollection<string> errors)
    {
        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json";

        return context.Response.WriteAsJsonAsync(new ApiErrorResponseDto
        {
            Message = message,
            Errors = errors.ToList(),
            TraceId = context.TraceIdentifier
        });
    }
}
