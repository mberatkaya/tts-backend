namespace TTS.Domain.Models.Ai;

public sealed class QueryFilter
{
    public string Column { get; set; } = string.Empty;

    public string Operator { get; set; } = "=";

    public string? Value { get; set; }
}
