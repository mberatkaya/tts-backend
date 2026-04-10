namespace TTS.Infrastructure.Configuration;

public sealed class QuerySafetyOptions
{
    public const string SectionName = "QuerySafety";

    public int MaxRowLimit { get; set; } = 100;

    public bool RejectComments { get; set; } = true;

    public bool AllowSelectStar { get; set; }

    public bool AllowedTableWhitelistEnabled { get; set; } = true;

    public bool AllowedColumnWhitelistEnabled { get; set; } = true;

    public bool EnableRoleBasedFilteringPlaceholder { get; set; } = true;

    public List<string> BlockedColumns { get; set; } = [];
}
