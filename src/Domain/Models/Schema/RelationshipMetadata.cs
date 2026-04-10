namespace TTS.Domain.Models.Schema;

public sealed class RelationshipMetadata
{
    public string FromSchema { get; set; } = "dbo";

    public string FromTable { get; set; } = string.Empty;

    public string FromColumn { get; set; } = string.Empty;

    public string ToSchema { get; set; } = "dbo";

    public string ToTable { get; set; } = string.Empty;

    public string ToColumn { get; set; } = string.Empty;

    public string RelationshipType { get; set; } = string.Empty;
}
