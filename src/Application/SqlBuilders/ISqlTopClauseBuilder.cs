namespace TTS.Application.SqlBuilders;

public interface ISqlTopClauseBuilder
{
    string EnsureTopClause(string sql, int maxRowLimit);
}
