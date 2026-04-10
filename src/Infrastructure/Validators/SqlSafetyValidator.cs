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
        var tableReferences = SqlInspectionGuard.ExtractTableReferences(normalizedSql);

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
            ValidateTables(tableReferences, allowedSchemaMetadata, errors);
        }

        if (_options.AllowedColumnWhitelistEnabled && errors.Count == 0)
        {
            ValidateColumns(normalizedSql, allowedSchemaMetadata, tableReferences, errors);
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

    private void ValidateTables(IReadOnlyCollection<SqlTableReference> tableReferences, AllowedSchemaMetadata allowedSchemaMetadata, List<string> errors)
    {
        if (tableReferences.Count == 0)
        {
            errors.Add("Query must reference at least one allowed table.");
            return;
        }

        var allowedTableLookup = BuildAllowedTableLookup(allowedSchemaMetadata);

        foreach (var tableReference in tableReferences)
        {
            if (TryResolveTable(tableReference.Table, allowedTableLookup, out _))
            {
                continue;
            }

            errors.Add($"Query references a table outside the allowed schema: {tableReference.Table}.");
        }
    }

    private void ValidateColumns(
        string sql,
        AllowedSchemaMetadata allowedSchemaMetadata,
        IReadOnlyCollection<SqlTableReference> tableReferences,
        List<string> errors)
    {
        if (!_options.AllowSelectStar && SqlInspectionGuard.ContainsSelectWildcard(sql))
        {
            errors.Add("SELECT * projections are not allowed.");
        }

        var allowedTableLookup = BuildAllowedTableLookup(allowedSchemaMetadata);
        var referencedTables = tableReferences
            .Select(reference => TryResolveTable(reference.Table, allowedTableLookup, out var table) ? table : null)
            .Where(table => table is not null)
            .Cast<TableMetadata>()
            .DistinctBy(table => $"{table.Schema}.{table.Name}", StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (referencedTables.Count == 0)
        {
            return;
        }

        var tableAliasLookup = BuildTableAliasLookup(tableReferences, allowedTableLookup);
        var selectExpressions = SqlInspectionGuard.ExtractSelectExpressions(sql);
        var projectionAliases = SqlInspectionGuard.ExtractProjectionAliases(selectExpressions)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        ValidateColumnExpressions(selectExpressions, referencedTables, tableAliasLookup, projectionAliases, allowProjectionAliases: false, errors);
        ValidateColumnExpressions(SqlInspectionGuard.ExtractPredicateExpressions(sql), referencedTables, tableAliasLookup, projectionAliases, allowProjectionAliases: false, errors);
        ValidateColumnExpressions(SqlInspectionGuard.ExtractGroupByExpressions(sql), referencedTables, tableAliasLookup, projectionAliases, allowProjectionAliases: false, errors);
        ValidateColumnExpressions(SqlInspectionGuard.ExtractOrderByExpressions(sql), referencedTables, tableAliasLookup, projectionAliases, allowProjectionAliases: true, errors);
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

    private static Dictionary<string, TableMetadata> BuildAllowedTableLookup(AllowedSchemaMetadata allowedSchemaMetadata)
    {
        return allowedSchemaMetadata.Tables
            .SelectMany(table => new[]
            {
                new KeyValuePair<string, TableMetadata>($"{table.Schema}.{table.Name}", table),
                new KeyValuePair<string, TableMetadata>(table.Name, table)
            })
            .GroupBy(entry => entry.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First().Value, StringComparer.OrdinalIgnoreCase);
    }

    private static Dictionary<string, TableMetadata> BuildTableAliasLookup(
        IReadOnlyCollection<SqlTableReference> tableReferences,
        IReadOnlyDictionary<string, TableMetadata> allowedTableLookup)
    {
        var lookup = new Dictionary<string, TableMetadata>(StringComparer.OrdinalIgnoreCase);

        foreach (var tableReference in tableReferences)
        {
            if (!TryResolveTable(tableReference.Table, allowedTableLookup, out var table))
            {
                continue;
            }

            lookup[table.Name] = table;
            lookup[$"{table.Schema}.{table.Name}"] = table;

            if (!string.IsNullOrWhiteSpace(tableReference.Alias))
            {
                lookup[tableReference.Alias] = table;
            }
        }

        return lookup;
    }

    private static bool TryResolveTable(
        string source,
        IReadOnlyDictionary<string, TableMetadata> allowedTableLookup,
        out TableMetadata table)
    {
        var normalizedSource = SqlInspectionGuard.NormalizeIdentifier(source);

        if (allowedTableLookup.TryGetValue(normalizedSource, out table!))
        {
            return true;
        }

        var shortName = normalizedSource.Split('.', StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
        return !string.IsNullOrWhiteSpace(shortName) && allowedTableLookup.TryGetValue(shortName, out table!);
    }

    private static void ValidateColumnExpressions(
        IReadOnlyCollection<string> expressions,
        IReadOnlyCollection<TableMetadata> referencedTables,
        IReadOnlyDictionary<string, TableMetadata> tableAliasLookup,
        IReadOnlySet<string> projectionAliases,
        bool allowProjectionAliases,
        List<string> errors)
    {
        var seenErrors = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var expression in expressions)
        {
            foreach (var columnReference in SqlInspectionGuard.ExtractColumnReferences(expression))
            {
                if (allowProjectionAliases &&
                    string.IsNullOrWhiteSpace(columnReference.Source) &&
                    projectionAliases.Contains(columnReference.Column))
                {
                    continue;
                }

                var validationError = ValidateColumnReference(columnReference, referencedTables, tableAliasLookup);
                if (validationError is null || !seenErrors.Add(validationError))
                {
                    continue;
                }

                errors.Add(validationError);
            }
        }
    }

    private static string? ValidateColumnReference(
        SqlColumnReference columnReference,
        IReadOnlyCollection<TableMetadata> referencedTables,
        IReadOnlyDictionary<string, TableMetadata> tableAliasLookup)
    {
        if (!string.IsNullOrWhiteSpace(columnReference.Source))
        {
            if (!tableAliasLookup.TryGetValue(columnReference.Source, out var referencedTable))
            {
                return $"Column source is not allowed or cannot be resolved: {columnReference.Source}.";
            }

            return referencedTable.Columns.Any(column => string.Equals(column.Name, columnReference.Column, StringComparison.OrdinalIgnoreCase))
                ? null
                : $"Column is not allowed on table {referencedTable.Schema}.{referencedTable.Name}: {columnReference.Column}.";
        }

        var matchingTables = referencedTables
            .Where(table => table.Columns.Any(column => string.Equals(column.Name, columnReference.Column, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        if (matchingTables.Count == 1)
        {
            return null;
        }

        if (matchingTables.Count > 1)
        {
            return $"Column must be qualified with a table or alias because it exists on multiple allowed tables: {columnReference.Column}.";
        }

        return $"Column is outside the allowed schema metadata: {columnReference.Column}.";
    }
}
