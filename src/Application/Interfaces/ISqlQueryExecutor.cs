using TTS.Domain.Models.Execution;

namespace TTS.Application.Interfaces;

public interface ISqlQueryExecutor
{
    Task<SqlExecutionResult> ExecuteAsync(string sql, CancellationToken cancellationToken);
}
