using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TTS.Application.Configuration;
using TTS.Application.Exceptions;
using TTS.Application.Interfaces;
using TTS.Application.Policies;
using TTS.Application.Validators;
using TTS.Domain.Models.TextToSql;

namespace TTS.Application.Services;

public sealed class TextToSqlService : ITextToSqlService
{
    private readonly TextToSqlQuestionValidator _questionValidator;
    private readonly ISchemaMetadataProvider _schemaMetadataProvider;
    private readonly IAiQueryPlanner _aiQueryPlanner;
    private readonly IRoleBasedSqlPolicy _roleBasedSqlPolicy;
    private readonly ISqlSafetyValidator _sqlSafetyValidator;
    private readonly ISqlQueryExecutor _sqlQueryExecutor;
    private readonly IResultFormatter _resultFormatter;
    private readonly TextToSqlOptions _options;
    private readonly ILogger<TextToSqlService> _logger;

    public TextToSqlService(
        TextToSqlQuestionValidator questionValidator,
        ISchemaMetadataProvider schemaMetadataProvider,
        IAiQueryPlanner aiQueryPlanner,
        IRoleBasedSqlPolicy roleBasedSqlPolicy,
        ISqlSafetyValidator sqlSafetyValidator,
        ISqlQueryExecutor sqlQueryExecutor,
        IResultFormatter resultFormatter,
        IOptions<TextToSqlOptions> options,
        ILogger<TextToSqlService> logger)
    {
        _questionValidator = questionValidator;
        _schemaMetadataProvider = schemaMetadataProvider;
        _aiQueryPlanner = aiQueryPlanner;
        _roleBasedSqlPolicy = roleBasedSqlPolicy;
        _sqlSafetyValidator = sqlSafetyValidator;
        _sqlQueryExecutor = sqlQueryExecutor;
        _resultFormatter = resultFormatter;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<TextToSqlQueryResult> QueryAsync(string question, CancellationToken cancellationToken)
    {
        var normalizedQuestion = question?.Trim() ?? string.Empty;
        var questionErrors = _questionValidator.Validate(normalizedQuestion, _options.MaxQuestionLength);

        if (questionErrors.Count > 0)
        {
            throw new TextToSqlValidationException(questionErrors);
        }

        var allowedSchemaMetadata = await _schemaMetadataProvider.GetAllowedSchemaAsync(cancellationToken);

        if (allowedSchemaMetadata.Tables.Count == 0)
        {
            throw new TextToSqlValidationException(["No allowed schema metadata has been configured."]);
        }

        _logger.LogInformation("Planning SQL for question: {Question}", normalizedQuestion);

        var queryPlan = await _aiQueryPlanner.PlanAsync(normalizedQuestion, allowedSchemaMetadata, cancellationToken);

        if (string.IsNullOrWhiteSpace(queryPlan.GeneratedSql))
        {
            throw new TextToSqlValidationException(["The AI query planner did not return SQL."]);
        }

        var policyAdjustedSql = await _roleBasedSqlPolicy.ApplyAsync(
            queryPlan.GeneratedSql,
            new RoleBasedSqlPolicyContext
            {
                Question = normalizedQuestion,
                QueryPlan = queryPlan,
                AllowedSchemaMetadata = allowedSchemaMetadata
            },
            cancellationToken);

        var validationResult = _sqlSafetyValidator.Validate(policyAdjustedSql, allowedSchemaMetadata);

        if (!validationResult.IsValid)
        {
            throw new TextToSqlValidationException(validationResult.Errors);
        }

        var executionResult = await _sqlQueryExecutor.ExecuteAsync(validationResult.NormalizedSql, cancellationToken);
        var formattedRows = _resultFormatter.Format(executionResult.Rows);
        var warnings = queryPlan.Warnings
            .Concat(executionResult.Warnings)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new TextToSqlQueryResult
        {
            Success = true,
            Question = normalizedQuestion,
            GeneratedSql = validationResult.NormalizedSql,
            Rows = formattedRows,
            Warnings = warnings,
            Confidence = Math.Clamp(queryPlan.Confidence, 0d, 1d)
        };
    }
}
