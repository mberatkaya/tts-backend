using System.ComponentModel.DataAnnotations;

namespace TTS.Api.DTOs;

public sealed class TextToSqlQueryRequestDto
{
    [Required]
    [MaxLength(500)]
    public string Question { get; set; } = string.Empty;
}
