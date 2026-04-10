namespace TTS.Application.Exceptions;

public sealed class TextToSqlValidationException : Exception
{
    public TextToSqlValidationException(IReadOnlyCollection<string> errors)
        : base("The text-to-sql request is invalid.")
    {
        Errors = errors;
    }

    public IReadOnlyCollection<string> Errors { get; }
}
