using System.Text.RegularExpressions;

namespace TTS.Infrastructure.Guards;

public static class SqlInspectionGuard
{
    private static readonly Regex TableReferenceRegex = new(@"\b(?:FROM|JOIN)\s+(?<table>(?:\[[^\]]+\]|\w+)(?:\.(?:\[[^\]]+\]|\w+))?)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex WhitespaceRegex = new(@"\s+", RegexOptions.Compiled);

    public static string Normalize(string sql)
    {
        return WhitespaceRegex.Replace(sql, " ").Trim();
    }

    public static bool StartsWithSelect(string sql)
    {
        return Regex.IsMatch(sql, @"^\s*SELECT\b", RegexOptions.IgnoreCase);
    }

    public static bool ContainsMultipleStatements(string sql)
    {
        return sql.Contains(';');
    }

    public static bool ContainsComment(string sql)
    {
        return sql.Contains("--", StringComparison.Ordinal) ||
               sql.Contains("/*", StringComparison.Ordinal) ||
               sql.Contains("*/", StringComparison.Ordinal);
    }

    public static bool ContainsKeyword(string sql, string keyword)
    {
        return Regex.IsMatch(sql, $@"\b{Regex.Escape(keyword)}\b", RegexOptions.IgnoreCase);
    }

    public static IReadOnlyCollection<string> ExtractReferencedTables(string sql)
    {
        return TableReferenceRegex.Matches(sql)
            .Select(match => NormalizeIdentifier(match.Groups["table"].Value))
            .Where(identifier => !string.IsNullOrWhiteSpace(identifier))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static bool ContainsBlockedColumnReference(string sql, IEnumerable<string> blockedColumns)
    {
        foreach (var blockedColumn in blockedColumns.Where(column => !string.IsNullOrWhiteSpace(column)))
        {
            if (Regex.IsMatch(sql, $@"\b{Regex.Escape(blockedColumn)}\b", RegexOptions.IgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    public static string NormalizeIdentifier(string identifier)
    {
        var parts = identifier
            .Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(part => part.Trim().Trim('[', ']', '"'))
            .Where(part => !string.IsNullOrWhiteSpace(part))
            .ToArray();

        return string.Join('.', parts);
    }
}
