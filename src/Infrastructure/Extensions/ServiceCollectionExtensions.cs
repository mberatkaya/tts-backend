using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TTS.Application.Configuration;
using TTS.Application.Interfaces;
using TTS.Infrastructure.Ai;
using TTS.Infrastructure.Configuration;
using TTS.Infrastructure.Repositories;
using TTS.Infrastructure.Schema;
using TTS.Infrastructure.Services;
using TTS.Infrastructure.Validators;

namespace TTS.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<TextToSqlOptions>(configuration.GetSection(TextToSqlOptions.SectionName));
        services.Configure<AiProviderOptions>(configuration.GetSection(AiProviderOptions.SectionName));
        services.Configure<QuerySafetyOptions>(configuration.GetSection(QuerySafetyOptions.SectionName));
        services.Configure<SqlConnectionOptions>(configuration.GetSection(SqlConnectionOptions.SectionName));

        services.AddScoped<IAiQueryPlanner, FakeAiQueryPlanner>();
        services.AddSingleton<ISchemaMetadataProvider, ConfiguredSchemaMetadataProvider>();
        services.AddSingleton<ISqlSafetyValidator, SqlSafetyValidator>();
        services.AddScoped<ISqlQueryExecutor, SqlQueryExecutor>();
        services.AddSingleton<IResultFormatter, JsonResultFormatter>();

        return services;
    }
}
