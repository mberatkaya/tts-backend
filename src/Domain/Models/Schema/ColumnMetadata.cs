namespace TTS.Domain.Models.Schema;

public sealed class ColumnMetadata
{
    public string Name { get; set; } = string.Empty;

    public string DataType { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public bool IsNullable { get; set; }

    public bool IsSensitive { get; set; }

    public bool IsFilterable { get; set; } = true;

    public bool IsSortable { get; set; } = true;
}
