namespace TTS.Domain.Models.Execution;

public sealed class SqlExecutionResult
{
    public List<Dictionary<string, object?>> Rows { get; set; } = [];

    public List<string> Warnings { get; set; } = [];
}
