using TTS.Application.Interfaces;

namespace TTS.Infrastructure.Services;

public sealed class JsonResultFormatter : IResultFormatter
{
    public List<Dictionary<string, object?>> Format(IReadOnlyCollection<Dictionary<string, object?>> rows)
    {
        return rows
            .Select(row => row.ToDictionary(
                entry => entry.Key,
                entry => entry.Value is DBNull ? null : entry.Value,
                StringComparer.OrdinalIgnoreCase))
            .ToList();
    }
}
