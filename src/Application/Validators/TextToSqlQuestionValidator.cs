namespace TTS.Application.Validators;

public sealed class TextToSqlQuestionValidator
{
    public IReadOnlyCollection<string> Validate(string? question, int maxLength)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(question))
        {
            errors.Add("Question is required.");
            return errors;
        }

        if (question.Trim().Length < 3)
        {
            errors.Add("Question must be at least 3 characters long.");
        }

        if (question.Length > maxLength)
        {
            errors.Add($"Question cannot exceed {maxLength} characters.");
        }

        return errors;
    }
}
