using TTS.Domain.Models.TextToSql;

namespace TTS.Application.Interfaces;

public interface ITextToSqlService
{
    Task<TextToSqlQueryResult> QueryAsync(string question, CancellationToken cancellationToken);
}
