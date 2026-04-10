using TTS.Domain.Models.Ai;
using TTS.Domain.Models.Schema;

namespace TTS.Application.Policies;

public sealed class RoleBasedSqlPolicyContext
{
    public string Question { get; init; } = string.Empty;

    public AiQueryPlan QueryPlan { get; init; } = new();

    public AllowedSchemaMetadata AllowedSchemaMetadata { get; init; } = new();
}
