using System.Text.Json.Serialization;

namespace TTS.Api.DTOs;

public sealed record ApiErrorResponseDto
{
    public bool Success { get; init; } = false;

    public string Message { get; init; } = string.Empty;

    public IReadOnlyList<string> Errors { get; init; } = [];

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? TraceId { get; init; }
}
