namespace TTS.Api.DTOs;

public sealed record TextToSqlQueryResponseDto
{
    public bool Success { get; init; }

    public string Question { get; init; } = string.Empty;

    public string GeneratedSql { get; init; } = string.Empty;

    public IReadOnlyList<Dictionary<string, object?>> Rows { get; init; } = [];

    public IReadOnlyList<string> Warnings { get; init; } = [];

    public double Confidence { get; init; }
}
