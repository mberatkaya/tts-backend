using TTS.Domain.Models.Schema;

namespace TTS.Application.Configuration;

public sealed class TextToSqlOptions
{
    public const string SectionName = "TextToSql";

    public bool EnableQueryExecution { get; set; }

    public bool UseSampleSchemaMetadata { get; set; } = true;

    public string DefaultSchema { get; set; } = "dbo";

    public int CommandTimeoutSeconds { get; set; } = 30;

    public int MaxQuestionLength { get; set; } = 500;

    public int DefaultResultLimit { get; set; } = 25;

    public List<TableMetadata> AllowedTables { get; set; } = [];

    public List<RelationshipMetadata> Relationships { get; set; } = [];
}
