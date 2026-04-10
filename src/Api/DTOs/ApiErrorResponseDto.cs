namespace TTS.Api.DTOs;

public sealed class ApiErrorResponseDto
{
    public bool Success { get; init; } = false;

    public string Message { get; init; } = string.Empty;

    public IReadOnlyList<string> Errors { get; init; } = [];

    public string? TraceId { get; init; }
}
