using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TTS.Application.Configuration;
using TTS.Application.Interfaces;
using TTS.Domain.Models.Ai;
using TTS.Domain.Models.Schema;
using TTS.Infrastructure.Configuration;

namespace TTS.Infrastructure.Ai;

public sealed class FakeAiQueryPlanner : IAiQueryPlanner
{
    private readonly TextToSqlOptions _textToSqlOptions;
    private readonly QuerySafetyOptions _querySafetyOptions;
    private readonly ILogger<FakeAiQueryPlanner> _logger;

    public FakeAiQueryPlanner(
        IOptions<TextToSqlOptions> textToSqlOptions,
        IOptions<QuerySafetyOptions> querySafetyOptions,
        ILogger<FakeAiQueryPlanner> logger)
    {
        _textToSqlOptions = textToSqlOptions.Value;
        _querySafetyOptions = querySafetyOptions.Value;
        _logger = logger;
    }

    public Task<AiQueryPlan> PlanAsync(string question, AllowedSchemaMetadata allowedSchemaMetadata, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var plan = new AiQueryPlan
        {
            Confidence = 0.35,
            Warnings =
            [
                "Fake planner is enabled. Replace IAiQueryPlanner with a real LLM-backed implementation in the next phase."
            ]
        };

        if (allowedSchemaMetadata.Tables.Count == 0)
        {
            plan.Warnings.Add("No schema metadata is available for planning.");
            return Task.FromResult(plan);
        }

        var normalizedQuestion = question.Trim();
        var questionLower = normalizedQuestion.ToLowerInvariant();
        var maxLimit = Math.Min(_textToSqlOptions.DefaultResultLimit, _querySafetyOptions.MaxRowLimit);
        var targetTable = ResolveTargetTable(questionLower, allowedSchemaMetadata.Tables);
        var selectableColumns = targetTable.Columns.Take(3).ToList();

        _logger.LogInformation("Fake planner selected table {Schema}.{Table}", targetTable.Schema, targetTable.Name);

        plan.TargetTables.Add($"{targetTable.Schema}.{targetTable.Name}");

        if (questionLower.Contains("count", StringComparison.Ordinal) || questionLower.Contains("how many", StringComparison.Ordinal))
        {
            plan.Intent = QueryIntent.Aggregation;
            plan.Aggregations.Add(new AggregationDefinition
            {
                Function = "COUNT",
                Column = "*",
                Alias = "TotalCount"
            });
            plan.TargetColumns.Add("TotalCount");
            plan.Limit = 1;
            plan.Confidence = 0.42;
            plan.GeneratedSql = $"SELECT TOP 1 COUNT(*) AS [TotalCount] FROM {FormatTable(targetTable)}";

            return Task.FromResult(plan);
        }

        if (selectableColumns.Count == 0)
        {
            plan.Warnings.Add($"No selectable columns were configured for table {targetTable.Name}.");
            return Task.FromResult(plan);
        }

        plan.Intent = QueryIntent.Lookup;
        plan.TargetColumns.AddRange(selectableColumns.Select(column => column.Name));
        plan.Sort.Add(new SortDefinition
        {
            Column = selectableColumns[0].Name,
            Direction = "ASC"
        });
        plan.Limit = maxLimit;
        plan.GeneratedSql =
            $"SELECT TOP {maxLimit} {string.Join(", ", selectableColumns.Select(column => FormatColumn(column.Name)))} " +
            $"FROM {FormatTable(targetTable)} " +
            $"ORDER BY {FormatColumn(selectableColumns[0].Name)} ASC";

        return Task.FromResult(plan);
    }

    private static TableMetadata ResolveTargetTable(string normalizedQuestion, IEnumerable<TableMetadata> availableTables)
    {
        return availableTables.FirstOrDefault(table =>
                   normalizedQuestion.Contains(table.Name.ToLowerInvariant(), StringComparison.Ordinal)) ??
               availableTables.First();
    }

    private static string FormatTable(TableMetadata table)
    {
        return $"[{table.Schema}].[{table.Name}]";
    }

    private static string FormatColumn(string columnName)
    {
        return $"[{columnName}]";
    }
}
