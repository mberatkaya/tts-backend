namespace TTS.Infrastructure.Configuration;

public sealed class AiProviderOptions
{
    public const string SectionName = "AiProvider";

    public string ProviderName { get; set; } = "Fake";

    public string ModelName { get; set; } = "mock-text-to-sql-planner";

    public bool UseMock { get; set; } = true;
}
