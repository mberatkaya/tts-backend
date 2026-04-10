namespace TTS.Infrastructure.Configuration;

public sealed class AiProviderOptions
{
    public const string SectionName = "AiProvider";

    public string ProviderName { get; set; } = "Fake";

    public string ModelName { get; set; } = "mock-text-to-sql-planner";

    public bool UseMock { get; set; } = true;

    public string Endpoint { get; set; } = string.Empty;

    public string ApiKey { get; set; } = string.Empty;
}
