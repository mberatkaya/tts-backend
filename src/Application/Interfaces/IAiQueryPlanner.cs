using TTS.Domain.Models.Ai;
using TTS.Domain.Models.Schema;

namespace TTS.Application.Interfaces;

public interface IAiQueryPlanner
{
    Task<AiQueryPlan> PlanAsync(string question, AllowedSchemaMetadata allowedSchemaMetadata, CancellationToken cancellationToken);
}
