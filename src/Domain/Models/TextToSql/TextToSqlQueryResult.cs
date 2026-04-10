namespace TTS.Domain.Models.TextToSql;

public sealed class TextToSqlQueryResult
{
    public bool Success { get; set; } = true;

    public string Question { get; set; } = string.Empty;

    public string GeneratedSql { get; set; } = string.Empty;

    public List<Dictionary<string, object?>> Rows { get; set; } = [];

    public List<string> Warnings { get; set; } = [];

    public double Confidence { get; set; }
}
