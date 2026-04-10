namespace TTS.Domain.Models.Ai;

public sealed class AggregationDefinition
{
    public string Function { get; set; } = string.Empty;

    public string Column { get; set; } = string.Empty;

    public string? Alias { get; set; }
}
