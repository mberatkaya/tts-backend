namespace TTS.Application.Interfaces;

public interface IResultFormatter
{
    List<Dictionary<string, object?>> Format(IReadOnlyCollection<Dictionary<string, object?>> rows);
}
