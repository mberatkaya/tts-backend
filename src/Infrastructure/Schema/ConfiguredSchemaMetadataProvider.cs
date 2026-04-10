using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TTS.Application.Configuration;
using TTS.Application.Interfaces;
using TTS.Domain.Models.Schema;

namespace TTS.Infrastructure.Schema;

public sealed class ConfiguredSchemaMetadataProvider : ISchemaMetadataProvider
{
    private readonly TextToSqlOptions _options;
    private readonly ILogger<ConfiguredSchemaMetadataProvider> _logger;

    public ConfiguredSchemaMetadataProvider(IOptions<TextToSqlOptions> options, ILogger<ConfiguredSchemaMetadataProvider> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public Task<AllowedSchemaMetadata> GetAllowedSchemaAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var tables = _options.AllowedTables
            .Where(table => !string.IsNullOrWhiteSpace(table.Name))
            .Select(table => new TableMetadata
            {
                Schema = string.IsNullOrWhiteSpace(table.Schema) ? "dbo" : table.Schema,
                Name = table.Name,
                Description = table.Description,
                Columns = table.Columns
                    .Where(column => !column.IsSensitive)
                    .Where(column => !string.IsNullOrWhiteSpace(column.Name))
                    .Select(column => new ColumnMetadata
                    {
                        Name = column.Name,
                        DataType = column.DataType,
                        Description = column.Description,
                        IsNullable = column.IsNullable,
                        IsSensitive = false,
                        IsFilterable = column.IsFilterable,
                        IsSortable = column.IsSortable
                    })
                    .ToList()
            })
            .Where(table => table.Columns.Count > 0)
            .ToList();

        if (tables.Count == 0)
        {
            _logger.LogWarning("No allowed tables were resolved from configuration.");
        }

        var allowedTableKeys = tables
            .SelectMany(table => new[]
            {
                table.Name,
                $"{table.Schema}.{table.Name}"
            })
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var relationships = _options.Relationships
            .Where(relationship =>
                allowedTableKeys.Contains(relationship.FromTable) ||
                allowedTableKeys.Contains($"{relationship.FromSchema}.{relationship.FromTable}"))
            .Where(relationship =>
                allowedTableKeys.Contains(relationship.ToTable) ||
                allowedTableKeys.Contains($"{relationship.ToSchema}.{relationship.ToTable}"))
            .Select(relationship => new RelationshipMetadata
            {
                FromSchema = string.IsNullOrWhiteSpace(relationship.FromSchema) ? "dbo" : relationship.FromSchema,
                FromTable = relationship.FromTable,
                FromColumn = relationship.FromColumn,
                ToSchema = string.IsNullOrWhiteSpace(relationship.ToSchema) ? "dbo" : relationship.ToSchema,
                ToTable = relationship.ToTable,
                ToColumn = relationship.ToColumn,
                RelationshipType = relationship.RelationshipType
            })
            .ToList();

        return Task.FromResult(new AllowedSchemaMetadata
        {
            Tables = tables,
            Relationships = relationships
        });
    }
}
