using System.Text.RegularExpressions;

namespace TTS.Infrastructure.Guards;

public static class SqlInspectionGuard
{
    private static readonly Regex TableReferenceRegex = new(@"\b(?:FROM|JOIN)\s+(?<table>(?:\[[^\]]+\]|\w+)(?:\s*\.\s*(?:\[[^\]]+\]|\w+))?)(?:\s+(?:AS\s+)?(?<alias>\[[^\]]+\]|\w+))?", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex QualifiedIdentifierRegex = new(@"(?<source>\[[^\]]+\]|\b[A-Za-z_][A-Za-z0-9_]*\b)\s*\.\s*(?<column>\[[^\]]+\]|\b[A-Za-z_][A-Za-z0-9_]*\b)", RegexOptions.Compiled);
    private static readonly Regex BracketedIdentifierRegex = new(@"\[(?<identifier>[^\]]+)\]", RegexOptions.Compiled);
    private static readonly Regex BareIdentifierRegex = new(@"\b(?<identifier>[A-Za-z_][A-Za-z0-9_]*)\b", RegexOptions.Compiled);
    private static readonly Regex StringLiteralRegex = new(@"'(?:''|[^'])*'", RegexOptions.Compiled);
    private static readonly Regex WhitespaceRegex = new(@"\s+", RegexOptions.Compiled);
    private static readonly HashSet<string> ReservedKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "SELECT",
        "TOP",
        "DISTINCT",
        "FROM",
        "JOIN",
        "INNER",
        "LEFT",
        "RIGHT",
        "FULL",
        "OUTER",
        "CROSS",
        "ON",
        "WHERE",
        "AND",
        "OR",
        "NOT",
        "GROUP",
        "BY",
        "ORDER",
        "ASC",
        "DESC",
        "AS",
        "CASE",
        "WHEN",
        "THEN",
        "ELSE",
        "END",
        "NULL",
        "IS",
        "IN",
        "LIKE",
        "BETWEEN",
        "HAVING",
        "OFFSET",
        "FETCH",
        "NEXT",
        "ROWS",
        "ONLY",
        "OVER",
        "PARTITION",
        "COUNT",
        "SUM",
        "AVG",
        "MIN",
        "MAX",
        "CAST",
        "CONVERT"
    };

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

    public static IReadOnlyCollection<SqlTableReference> ExtractTableReferences(string sql)
    {
        return TableReferenceRegex.Matches(sql)
            .Select(match =>
            {
                var table = NormalizeIdentifier(match.Groups["table"].Value);
                var alias = NormalizeIdentifier(match.Groups["alias"].Value);

                return new SqlTableReference(table, IsReservedKeyword(alias) ? null : alias);
            })
            .Where(reference => !string.IsNullOrWhiteSpace(reference.Table))
            .DistinctBy(reference => $"{reference.Table}|{reference.Alias}", StringComparer.OrdinalIgnoreCase)
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

    public static IReadOnlyCollection<string> ExtractSelectExpressions(string sql)
    {
        var selectBody = ExtractSelectBody(sql);
        return string.IsNullOrWhiteSpace(selectBody) ? [] : SplitSqlList(selectBody);
    }

    public static IReadOnlyCollection<string> ExtractOrderByExpressions(string sql)
    {
        var match = Regex.Match(
            sql,
            @"\bORDER\s+BY\b(?<body>.*?)(?=\bOFFSET\b|\bFETCH\b|\bOPTION\b|$)",
            RegexOptions.IgnoreCase | RegexOptions.Singleline);

        return match.Success ? SplitSqlList(match.Groups["body"].Value) : [];
    }

    public static IReadOnlyCollection<string> ExtractGroupByExpressions(string sql)
    {
        var match = Regex.Match(
            sql,
            @"\bGROUP\s+BY\b(?<body>.*?)(?=\bHAVING\b|\bORDER\s+BY\b|\bOFFSET\b|\bFETCH\b|\bOPTION\b|$)",
            RegexOptions.IgnoreCase | RegexOptions.Singleline);

        return match.Success ? SplitSqlList(match.Groups["body"].Value) : [];
    }

    public static IReadOnlyCollection<string> ExtractPredicateExpressions(string sql)
    {
        var expressions = new List<string>();

        var whereMatch = Regex.Match(
            sql,
            @"\bWHERE\b(?<body>.*?)(?=\bGROUP\s+BY\b|\bORDER\s+BY\b|\bHAVING\b|\bOFFSET\b|\bFETCH\b|\bOPTION\b|$)",
            RegexOptions.IgnoreCase | RegexOptions.Singleline);

        if (whereMatch.Success && !string.IsNullOrWhiteSpace(whereMatch.Groups["body"].Value))
        {
            expressions.Add(whereMatch.Groups["body"].Value.Trim());
        }

        var onMatches = Regex.Matches(
            sql,
            @"\bON\b(?<body>.*?)(?=\b(?:WHERE|GROUP\s+BY|ORDER\s+BY|HAVING|OFFSET|FETCH|OPTION|INNER|LEFT|RIGHT|FULL|CROSS|JOIN)\b|$)",
            RegexOptions.IgnoreCase | RegexOptions.Singleline);

        foreach (Match match in onMatches)
        {
            if (!string.IsNullOrWhiteSpace(match.Groups["body"].Value))
            {
                expressions.Add(match.Groups["body"].Value.Trim());
            }
        }

        return expressions;
    }

    public static IReadOnlyCollection<string> ExtractProjectionAliases(IEnumerable<string> selectExpressions)
    {
        return selectExpressions
            .Select(expression => Regex.Match(expression, @"\bAS\s+(?<alias>\[[^\]]+\]|\w+)\s*$", RegexOptions.IgnoreCase))
            .Where(match => match.Success)
            .Select(match => NormalizeIdentifier(match.Groups["alias"].Value))
            .Where(alias => !string.IsNullOrWhiteSpace(alias))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static bool ContainsSelectWildcard(string sql)
    {
        foreach (var expression in ExtractSelectExpressions(sql))
        {
            var cleanedExpression = StripTrailingAlias(expression).Trim();

            if (cleanedExpression == "*" ||
                Regex.IsMatch(cleanedExpression, @"^(?:\[[^\]]+\]|\w+)\s*\.\s*\*$", RegexOptions.IgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    public static IReadOnlyCollection<SqlColumnReference> ExtractColumnReferences(string expression)
    {
        var cleanedExpression = StripTrailingAlias(StringLiteralRegex.Replace(expression, " "));
        var references = new List<SqlColumnReference>();

        foreach (Match match in QualifiedIdentifierRegex.Matches(cleanedExpression))
        {
            var source = NormalizeIdentifier(match.Groups["source"].Value);
            var column = NormalizeIdentifier(match.Groups["column"].Value);

            if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(column) || IsReservedKeyword(column))
            {
                continue;
            }

            references.Add(new SqlColumnReference(source, column));
        }

        cleanedExpression = QualifiedIdentifierRegex.Replace(cleanedExpression, " ");

        foreach (Match match in BracketedIdentifierRegex.Matches(cleanedExpression))
        {
            var identifier = NormalizeIdentifier(match.Groups["identifier"].Value);

            if (string.IsNullOrWhiteSpace(identifier) || IsReservedKeyword(identifier))
            {
                continue;
            }

            references.Add(new SqlColumnReference(null, identifier));
        }

        cleanedExpression = BracketedIdentifierRegex.Replace(cleanedExpression, " ");

        foreach (Match match in BareIdentifierRegex.Matches(cleanedExpression))
        {
            var identifier = match.Groups["identifier"].Value;

            if (string.IsNullOrWhiteSpace(identifier) || IsReservedKeyword(identifier))
            {
                continue;
            }

            var suffix = cleanedExpression[(match.Index + match.Length)..].TrimStart();
            if (suffix.StartsWith("(", StringComparison.Ordinal))
            {
                continue;
            }

            var prefix = cleanedExpression[..match.Index].TrimEnd();
            if (prefix.EndsWith("AS", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            references.Add(new SqlColumnReference(null, identifier));
        }

        return references
            .DistinctBy(reference => $"{reference.Source}|{reference.Column}", StringComparer.OrdinalIgnoreCase)
            .ToList();
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

    public static bool IsReservedKeyword(string? value)
    {
        return !string.IsNullOrWhiteSpace(value) && ReservedKeywords.Contains(value);
    }

    private static string ExtractSelectBody(string sql)
    {
        var match = Regex.Match(sql, @"^\s*SELECT\b(?<body>.+?)\bFROM\b", RegexOptions.IgnoreCase | RegexOptions.Singleline);

        if (!match.Success)
        {
            return string.Empty;
        }

        var body = match.Groups["body"].Value.Trim();

        while (true)
        {
            var updatedBody = Regex.Replace(body, @"^\s*DISTINCT\s+", string.Empty, RegexOptions.IgnoreCase);
            if (!string.Equals(updatedBody, body, StringComparison.Ordinal))
            {
                body = updatedBody.TrimStart();
                continue;
            }

            updatedBody = Regex.Replace(body, @"^\s*TOP\s*(?:\(\s*\d+\s*\)|\d+)\s+", string.Empty, RegexOptions.IgnoreCase);
            if (!string.Equals(updatedBody, body, StringComparison.Ordinal))
            {
                body = updatedBody.TrimStart();
                continue;
            }

            break;
        }

        return body.Trim();
    }

    private static IReadOnlyCollection<string> SplitSqlList(string value)
    {
        var parts = new List<string>();
        var current = new System.Text.StringBuilder();
        var parenthesisDepth = 0;
        var inSingleQuotes = false;

        foreach (var character in value)
        {
            switch (character)
            {
                case '\'':
                    inSingleQuotes = !inSingleQuotes;
                    current.Append(character);
                    break;
                case '(' when !inSingleQuotes:
                    parenthesisDepth++;
                    current.Append(character);
                    break;
                case ')' when !inSingleQuotes && parenthesisDepth > 0:
                    parenthesisDepth--;
                    current.Append(character);
                    break;
                case ',' when !inSingleQuotes && parenthesisDepth == 0:
                    var segment = current.ToString().Trim();
                    if (!string.IsNullOrWhiteSpace(segment))
                    {
                        parts.Add(segment);
                    }

                    current.Clear();
                    break;
                default:
                    current.Append(character);
                    break;
            }
        }

        var trailingSegment = current.ToString().Trim();
        if (!string.IsNullOrWhiteSpace(trailingSegment))
        {
            parts.Add(trailingSegment);
        }

        return parts;
    }

    private static string StripTrailingAlias(string expression)
    {
        return Regex.Replace(expression, @"\s+AS\s+(?:\[[^\]]+\]|\w+)\s*$", string.Empty, RegexOptions.IgnoreCase);
    }
}
