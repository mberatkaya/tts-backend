using Microsoft.Extensions.DependencyInjection;
using TTS.Application.Interfaces;
using TTS.Application.Policies;
using TTS.Application.Services;
using TTS.Application.SqlBuilders;
using TTS.Application.Validators;

namespace TTS.Application.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<ITextToSqlService, TextToSqlService>();
        services.AddSingleton<TextToSqlQuestionValidator>();
        services.AddSingleton<IRoleBasedSqlPolicy, NoOpRoleBasedSqlPolicy>();
        services.AddSingleton<ISqlTopClauseBuilder, SqlServerTopClauseBuilder>();

        return services;
    }
}
