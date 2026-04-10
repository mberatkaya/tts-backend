using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TTS.Application.Configuration;
using TTS.Application.Interfaces;
using TTS.Domain.Models.Execution;
using TTS.Infrastructure.Configuration;

namespace TTS.Infrastructure.Repositories;

public sealed class SqlQueryExecutor : ISqlQueryExecutor
{
    private readonly SqlConnectionOptions _connectionOptions;
    private readonly TextToSqlOptions _textToSqlOptions;
    private readonly ILogger<SqlQueryExecutor> _logger;

    public SqlQueryExecutor(
        IOptions<SqlConnectionOptions> connectionOptions,
        IOptions<TextToSqlOptions> textToSqlOptions,
        ILogger<SqlQueryExecutor> logger)
    {
        _connectionOptions = connectionOptions.Value;
        _textToSqlOptions = textToSqlOptions.Value;
        _logger = logger;
    }

    public async Task<SqlExecutionResult> ExecuteAsync(string sql, CancellationToken cancellationToken)
    {
        if (!_textToSqlOptions.EnableQueryExecution)
        {
            _logger.LogInformation("SQL execution is disabled. Returning an empty result set.");

            return new SqlExecutionResult
            {
                Warnings = ["SQL execution is disabled in configuration. No database call was made."]
            };
        }

        if (string.IsNullOrWhiteSpace(_connectionOptions.SqlServer))
        {
            _logger.LogWarning("SQL execution is enabled but no SQL Server connection string is configured.");

            return new SqlExecutionResult
            {
                Warnings = ["SQL execution is enabled, but the SQL Server connection string is missing."]
            };
        }

        await using var connection = new SqlConnection(_connectionOptions.SqlServer);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(sql, connection)
        {
            CommandType = CommandType.Text,
            CommandTimeout = _textToSqlOptions.CommandTimeoutSeconds
        };

        var rows = new List<Dictionary<string, object?>>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

            for (var index = 0; index < reader.FieldCount; index++)
            {
                var value = reader.IsDBNull(index) ? null : reader.GetValue(index);
                row[reader.GetName(index)] = value;
            }

            rows.Add(row);
        }

        return new SqlExecutionResult
        {
            Rows = rows
        };
    }
}
