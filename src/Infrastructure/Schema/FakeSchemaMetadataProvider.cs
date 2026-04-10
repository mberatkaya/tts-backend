using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TTS.Application.Configuration;
using TTS.Application.Interfaces;
using TTS.Domain.Models.Schema;
using TTS.Infrastructure.Configuration;

namespace TTS.Infrastructure.Schema;

public sealed class FakeSchemaMetadataProvider : ISchemaMetadataProvider
{
    private readonly TextToSqlOptions _textToSqlOptions;
    private readonly QuerySafetyOptions _querySafetyOptions;
    private readonly ILogger<FakeSchemaMetadataProvider> _logger;

    public FakeSchemaMetadataProvider(
        IOptions<TextToSqlOptions> textToSqlOptions,
        IOptions<QuerySafetyOptions> querySafetyOptions,
        ILogger<FakeSchemaMetadataProvider> logger)
    {
        _textToSqlOptions = textToSqlOptions.Value;
        _querySafetyOptions = querySafetyOptions.Value;
        _logger = logger;
    }

    public Task<AllowedSchemaMetadata> GetAllowedSchemaAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var sourceSchema = ResolveSourceSchema();
        var sanitizedSchema = SanitizeSchema(sourceSchema);

        _logger.LogInformation(
            "Resolved {TableCount} allowed tables and {RelationshipCount} relationships for AI planning.",
            sanitizedSchema.Tables.Count,
            sanitizedSchema.Relationships.Count);

        return Task.FromResult(sanitizedSchema);
    }

    private AllowedSchemaMetadata ResolveSourceSchema()
    {
        if (_textToSqlOptions.AllowedTables.Count > 0)
        {
            return new AllowedSchemaMetadata
            {
                Tables = _textToSqlOptions.AllowedTables,
                Relationships = _textToSqlOptions.Relationships
            };
        }

        if (_textToSqlOptions.UseSampleSchemaMetadata)
        {
            _logger.LogInformation("No configured schema metadata found. Falling back to built-in sample schema metadata.");
            return BuildSampleSchema();
        }

        _logger.LogWarning("No configured schema metadata found and sample metadata is disabled.");

        return new AllowedSchemaMetadata();
    }

    private AllowedSchemaMetadata SanitizeSchema(AllowedSchemaMetadata sourceSchema)
    {
        var blockedColumns = _querySafetyOptions.BlockedColumns
            .Where(column => !string.IsNullOrWhiteSpace(column))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var tables = sourceSchema.Tables
            .Where(table => !string.IsNullOrWhiteSpace(table.Name))
            .Select(table => new TableMetadata
            {
                Schema = string.IsNullOrWhiteSpace(table.Schema) ? _textToSqlOptions.DefaultSchema : table.Schema.Trim(),
                Name = table.Name.Trim(),
                Description = table.Description.Trim(),
                Columns = table.Columns
                    .Where(column => !string.IsNullOrWhiteSpace(column.Name))
                    .Where(column => !column.IsSensitive)
                    .Where(column => !blockedColumns.Contains(column.Name))
                    .GroupBy(column => column.Name.Trim(), StringComparer.OrdinalIgnoreCase)
                    .Select(group => group.First())
                    .Select(column => new ColumnMetadata
                    {
                        Name = column.Name.Trim(),
                        DataType = column.DataType.Trim(),
                        Description = column.Description.Trim(),
                        IsNullable = column.IsNullable,
                        IsSensitive = false,
                        IsFilterable = column.IsFilterable,
                        IsSortable = column.IsSortable
                    })
                    .ToList()
            })
            .Where(table => table.Columns.Count > 0)
            .GroupBy(table => $"{table.Schema}.{table.Name}", StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToList();

        var tableLookup = tables.ToDictionary(
            table => $"{table.Schema}.{table.Name}",
            table => table,
            StringComparer.OrdinalIgnoreCase);

        var relationships = sourceSchema.Relationships
            .Where(relationship =>
                tableLookup.ContainsKey($"{NormalizeSchema(relationship.FromSchema)}.{relationship.FromTable}") &&
                tableLookup.ContainsKey($"{NormalizeSchema(relationship.ToSchema)}.{relationship.ToTable}"))
            .Where(relationship =>
                HasColumn(tableLookup, NormalizeSchema(relationship.FromSchema), relationship.FromTable, relationship.FromColumn) &&
                HasColumn(tableLookup, NormalizeSchema(relationship.ToSchema), relationship.ToTable, relationship.ToColumn))
            .Select(relationship => new RelationshipMetadata
            {
                FromSchema = NormalizeSchema(relationship.FromSchema),
                FromTable = relationship.FromTable.Trim(),
                FromColumn = relationship.FromColumn.Trim(),
                ToSchema = NormalizeSchema(relationship.ToSchema),
                ToTable = relationship.ToTable.Trim(),
                ToColumn = relationship.ToColumn.Trim(),
                RelationshipType = relationship.RelationshipType.Trim()
            })
            .ToList();

        return new AllowedSchemaMetadata
        {
            Tables = tables,
            Relationships = relationships
        };
    }

    private AllowedSchemaMetadata BuildSampleSchema()
    {
        var schema = _textToSqlOptions.DefaultSchema;

        return new AllowedSchemaMetadata
        {
            Tables =
            [
                new TableMetadata
                {
                    Schema = schema,
                    Name = "Customers",
                    Description = "Customer master records exposed for reporting.",
                    Columns =
                    [
                        new ColumnMetadata { Name = "CustomerId", DataType = "int", Description = "Primary key.", IsNullable = false, IsFilterable = true, IsSortable = true },
                        new ColumnMetadata { Name = "CustomerName", DataType = "nvarchar(200)", Description = "Customer display name.", IsNullable = false, IsFilterable = true, IsSortable = true },
                        new ColumnMetadata { Name = "EmailAddress", DataType = "nvarchar(320)", Description = "Customer email address.", IsNullable = true, IsFilterable = true, IsSortable = false },
                        new ColumnMetadata { Name = "Country", DataType = "nvarchar(100)", Description = "Customer country.", IsNullable = true, IsFilterable = true, IsSortable = true },
                        new ColumnMetadata { Name = "CreatedAt", DataType = "datetime2", Description = "Customer creation timestamp.", IsNullable = false, IsFilterable = true, IsSortable = true }
                    ]
                },
                new TableMetadata
                {
                    Schema = schema,
                    Name = "Orders",
                    Description = "Sales order headers.",
                    Columns =
                    [
                        new ColumnMetadata { Name = "OrderId", DataType = "int", Description = "Primary key.", IsNullable = false, IsFilterable = true, IsSortable = true },
                        new ColumnMetadata { Name = "CustomerId", DataType = "int", Description = "Foreign key to Customers.", IsNullable = false, IsFilterable = true, IsSortable = true },
                        new ColumnMetadata { Name = "OrderDate", DataType = "datetime2", Description = "Order creation timestamp.", IsNullable = false, IsFilterable = true, IsSortable = true },
                        new ColumnMetadata { Name = "Status", DataType = "nvarchar(50)", Description = "Order lifecycle status.", IsNullable = false, IsFilterable = true, IsSortable = true },
                        new ColumnMetadata { Name = "TotalAmount", DataType = "decimal(18,2)", Description = "Total order amount.", IsNullable = false, IsFilterable = true, IsSortable = true }
                    ]
                },
                new TableMetadata
                {
                    Schema = schema,
                    Name = "OrderItems",
                    Description = "Order line items.",
                    Columns =
                    [
                        new ColumnMetadata { Name = "OrderItemId", DataType = "int", Description = "Primary key.", IsNullable = false, IsFilterable = true, IsSortable = true },
                        new ColumnMetadata { Name = "OrderId", DataType = "int", Description = "Foreign key to Orders.", IsNullable = false, IsFilterable = true, IsSortable = true },
                        new ColumnMetadata { Name = "ProductId", DataType = "int", Description = "Foreign key to Products.", IsNullable = false, IsFilterable = true, IsSortable = true },
                        new ColumnMetadata { Name = "Quantity", DataType = "int", Description = "Item quantity.", IsNullable = false, IsFilterable = true, IsSortable = true },
                        new ColumnMetadata { Name = "LineTotal", DataType = "decimal(18,2)", Description = "Extended line total.", IsNullable = false, IsFilterable = true, IsSortable = true }
                    ]
                },
                new TableMetadata
                {
                    Schema = schema,
                    Name = "Products",
                    Description = "Product catalog records.",
                    Columns =
                    [
                        new ColumnMetadata { Name = "ProductId", DataType = "int", Description = "Primary key.", IsNullable = false, IsFilterable = true, IsSortable = true },
                        new ColumnMetadata { Name = "ProductName", DataType = "nvarchar(200)", Description = "Product display name.", IsNullable = false, IsFilterable = true, IsSortable = true },
                        new ColumnMetadata { Name = "CategoryName", DataType = "nvarchar(100)", Description = "Product category.", IsNullable = true, IsFilterable = true, IsSortable = true },
                        new ColumnMetadata { Name = "UnitPrice", DataType = "decimal(18,2)", Description = "Current unit price.", IsNullable = false, IsFilterable = true, IsSortable = true },
                        new ColumnMetadata { Name = "IsActive", DataType = "bit", Description = "Whether the product is active.", IsNullable = false, IsFilterable = true, IsSortable = true }
                    ]
                }
            ],
            Relationships =
            [
                new RelationshipMetadata
                {
                    FromSchema = schema,
                    FromTable = "Orders",
                    FromColumn = "CustomerId",
                    ToSchema = schema,
                    ToTable = "Customers",
                    ToColumn = "CustomerId",
                    RelationshipType = "ManyToOne"
                },
                new RelationshipMetadata
                {
                    FromSchema = schema,
                    FromTable = "OrderItems",
                    FromColumn = "OrderId",
                    ToSchema = schema,
                    ToTable = "Orders",
                    ToColumn = "OrderId",
                    RelationshipType = "ManyToOne"
                },
                new RelationshipMetadata
                {
                    FromSchema = schema,
                    FromTable = "OrderItems",
                    FromColumn = "ProductId",
                    ToSchema = schema,
                    ToTable = "Products",
                    ToColumn = "ProductId",
                    RelationshipType = "ManyToOne"
                }
            ]
        };
    }

    private bool HasColumn(IReadOnlyDictionary<string, TableMetadata> tableLookup, string schema, string tableName, string columnName)
    {
        if (!tableLookup.TryGetValue($"{schema}.{tableName}", out var table))
        {
            return false;
        }

        return table.Columns.Any(column => string.Equals(column.Name, columnName, StringComparison.OrdinalIgnoreCase));
    }

    private string NormalizeSchema(string? schema)
    {
        return string.IsNullOrWhiteSpace(schema) ? _textToSqlOptions.DefaultSchema : schema.Trim();
    }
}
