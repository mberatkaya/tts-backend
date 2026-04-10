namespace TTS.Infrastructure.Configuration;

public sealed class SqlConnectionOptions
{
    public const string SectionName = "ConnectionStrings";

    public string SqlServer { get; set; } = string.Empty;
}
