using Microsoft.AspNetCore.Mvc;
using TTS.Api.DTOs;
using TTS.Api.Extensions;
using TTS.Api.Middleware;
using TTS.Application.Extensions;
using TTS.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApiServices();
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

var app = builder.Build();

app.UseMiddleware<GlobalExceptionHandlingMiddleware>();
app.UseHttpsRedirection();
app.MapControllers();

app.Run();

public partial class Program;
