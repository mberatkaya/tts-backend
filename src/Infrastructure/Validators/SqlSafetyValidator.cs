using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using TTS.Application.Interfaces;
using TTS.Application.SqlBuilders;
using TTS.Domain.Models.Schema;
using TTS.Domain.Models.Validation;
using TTS.Infrastructure.Configuration;
using TTS.Infrastructure.Guards;

namespace TTS.Infrastructure.Validators;

public sealed class SqlSafetyValidator : ISqlSafetyValidator
{
    private static readonly string[] ForbiddenKeywords =
    [
        "UPDATE",
        "DELETE",
        "INSERT",
        "DROP",
        "ALTER",
        "TRUNCATE",
        "EXEC",
        "MERGE",
        "INTO"
    ];

    private readonly QuerySafetyOptions _options;
    private readonly ISqlTopClauseBuilder _sqlTopClauseBuilder;

    public SqlSafetyValidator(IOptions<QuerySafetyOptions> options, ISqlTopClauseBuilder sqlTopClauseBuilder)
    {
        _options = options.Value;
        _sqlTopClauseBuilder = sqlTopClauseBuilder;
    }

    public SqlValidationResult Validate(string sql, AllowedSchemaMetadata allowedSchemaMetadata)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(sql))
        {
            errors.Add("Generated SQL is empty.");

            return new SqlValidationResult
            {
                IsValid = false,
                Errors = errors
            };
        }

        var normalizedSql = SqlInspectionGuard.Normalize(sql);

        if (!SqlInspectionGuard.StartsWithSelect(normalizedSql))
        {
            errors.Add("Only SELECT queries are allowed.");
        }

        if (SqlInspectionGuard.ContainsMultipleStatements(normalizedSql))
        {
            errors.Add("Multiple SQL statements are not allowed.");
        }

        if (_options.RejectComments && SqlInspectionGuard.ContainsComment(normalizedSql))
        {
            errors.Add("SQL comments are not allowed.");
        }

        foreach (var keyword in ForbiddenKeywords)
        {
            if (SqlInspectionGuard.ContainsKeyword(normalizedSql, keyword))
            {
                errors.Add($"Forbidden SQL keyword detected: {keyword}.");
            }
        }

        if (_options.AllowedTableWhitelistEnabled)
        {
            ValidateTables(normalizedSql, allowedSchemaMetadata, errors);
        }

        if (_options.BlockedColumns.Count > 0 && SqlInspectionGuard.ContainsBlockedColumnReference(normalizedSql, _options.BlockedColumns))
        {
            errors.Add("The query references a blocked or sensitive column.");
        }

        if (errors.Count == 0)
        {
            normalizedSql = EnforceTopClause(normalizedSql, errors);
        }

        return new SqlValidationResult
        {
            IsValid = errors.Count == 0,
            Errors = errors,
            NormalizedSql = errors.Count == 0 ? normalizedSql : string.Empty
        };
    }

    private void ValidateTables(string sql, AllowedSchemaMetadata allowedSchemaMetadata, List<string> errors)
    {
        var allowedTables = allowedSchemaMetadata.Tables
            .SelectMany(table => new[]
            {
                table.Name,
                $"{table.Schema}.{table.Name}"
            })
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var referencedTable in SqlInspectionGuard.ExtractReferencedTables(sql))
        {
            if (allowedTables.Contains(referencedTable))
            {
                continue;
            }

            var shortName = referencedTable.Split('.', StringSplitOptions.RemoveEmptyEntries).LastOrDefault();

            if (!string.IsNullOrWhiteSpace(shortName) && allowedTables.Contains(shortName))
            {
                continue;
            }

            errors.Add($"Query references a table outside the allowed schema: {referencedTable}.");
        }
    }

    private string EnforceTopClause(string sql, List<string> errors)
    {
        var topMatch = Regex.Match(sql, @"\bTOP\s*(?:\(\s*(?<limit>\d+)\s*\)|(?<limit>\d+))", RegexOptions.IgnoreCase);

        if (!topMatch.Success)
        {
            return _sqlTopClauseBuilder.EnsureTopClause(sql, _options.MaxRowLimit);
        }

        if (!int.TryParse(topMatch.Groups["limit"].Value, out var requestedLimit) || requestedLimit < 1)
        {
            errors.Add("TOP clause must contain a positive integer.");
            return sql;
        }

        if (requestedLimit > _options.MaxRowLimit)
        {
            errors.Add($"TOP clause exceeds the maximum allowed row limit of {_options.MaxRowLimit}.");
        }

        return sql;
    }
}
