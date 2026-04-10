namespace TTS.Domain.Models.Schema;

public sealed class AllowedSchemaMetadata
{
    public List<TableMetadata> Tables { get; set; } = [];

    public List<RelationshipMetadata> Relationships { get; set; } = [];
}
