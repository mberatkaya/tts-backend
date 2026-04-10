using System.Text.RegularExpressions;

namespace TTS.Application.SqlBuilders;

public sealed class SqlServerTopClauseBuilder : ISqlTopClauseBuilder
{
    public string EnsureTopClause(string sql, int maxRowLimit)
    {
        if (maxRowLimit <= 0)
        {
            return sql;
        }

        if (Regex.IsMatch(sql, @"^\s*SELECT\s+DISTINCT\b", RegexOptions.IgnoreCase))
        {
            return Regex.Replace(sql, @"^\s*SELECT\s+DISTINCT\s+", $"SELECT DISTINCT TOP {maxRowLimit} ", RegexOptions.IgnoreCase);
        }

        return Regex.Replace(sql, @"^\s*SELECT\s+", $"SELECT TOP {maxRowLimit} ", RegexOptions.IgnoreCase);
    }
}
