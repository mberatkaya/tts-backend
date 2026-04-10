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
        services.AddApplicationCoreServices();
        services.AddApplicationPolicies();

        return services;
    }

    public static IServiceCollection AddApplicationCoreServices(this IServiceCollection services)
    {
        services.AddScoped<ITextToSqlService, TextToSqlService>();
        services.AddSingleton<TextToSqlQuestionValidator>();
        services.AddSingleton<ISqlTopClauseBuilder, SqlServerTopClauseBuilder>();

        return services;
    }

    public static IServiceCollection AddApplicationPolicies(this IServiceCollection services)
    {
        services.AddSingleton<IRoleBasedSqlPolicy, NoOpRoleBasedSqlPolicy>();

        return services;
    }
}
