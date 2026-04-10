using System.ComponentModel.DataAnnotations;

namespace TTS.Api.DTOs;

public sealed record TextToSqlQueryRequestDto
{
    [Required(AllowEmptyStrings = false)]
    [MinLength(3)]
    [MaxLength(500)]
    public string Question { get; init; } = string.Empty;
}
