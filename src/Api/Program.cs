using Microsoft.AspNetCore.Mvc;
using TTS.Api.DTOs;
using TTS.Api.Middleware;
using TTS.Application.Extensions;
using TTS.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.Configure<ApiBehaviorOptions>(options =>
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

builder.Services.AddTransient<GlobalExceptionHandlingMiddleware>();
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

var app = builder.Build();

app.UseMiddleware<GlobalExceptionHandlingMiddleware>();
app.UseHttpsRedirection();
app.MapControllers();

app.Run();

public partial class Program;
