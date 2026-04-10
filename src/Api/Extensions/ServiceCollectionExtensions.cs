using Microsoft.AspNetCore.Mvc;
using TTS.Api.DTOs;
using TTS.Api.Middleware;

namespace TTS.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApiServices(this IServiceCollection services)
    {
        services.AddControllers();
        services.AddProblemDetails();
        services.Configure<ApiBehaviorOptions>(options =>
        {
            options.InvalidModelStateResponseFactory = context =>
            {
                var errors = context.ModelState
                    .Values
                    .SelectMany(value => value.Errors)
                    .Select(error => string.IsNullOrWhiteSpace(error.ErrorMessage) ? "The request payload is invalid." : error.ErrorMessage)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                return new BadRequestObjectResult(new ApiErrorResponseDto
                {
                    Message = "Request validation failed.",
                    Errors = errors,
                    TraceId = context.HttpContext.TraceIdentifier
                });
            };
        });

        services.AddTransient<GlobalExceptionHandlingMiddleware>();

        return services;
    }
}
