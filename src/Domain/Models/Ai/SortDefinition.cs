namespace TTS.Domain.Models.Ai;

public sealed class SortDefinition
{
    public string Column { get; set; } = string.Empty;

    public string Direction { get; set; } = "ASC";
}
