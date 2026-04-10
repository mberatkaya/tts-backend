namespace TTS.Domain.Models.Validation;

public sealed class SqlValidationResult
{
    public bool IsValid { get; set; }

    public List<string> Errors { get; set; } = [];

    public string NormalizedSql { get; set; } = string.Empty;
}
