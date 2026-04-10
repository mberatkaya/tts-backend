namespace TTS.Domain.Models.Schema;

public sealed class TableMetadata
{
    public string Schema { get; set; } = "dbo";

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public List<ColumnMetadata> Columns { get; set; } = [];
}
