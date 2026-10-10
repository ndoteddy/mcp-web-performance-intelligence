using System.Text.Json;
using McpWebPerformanceIntelligence.Configuration;
using McpWebPerformanceIntelligence.Models;
using McpWebPerformanceIntelligence.Serialization;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace McpWebPerformanceIntelligence.Services;

/// <summary>
/// SQLite-backed persistence for the <c>performance_cache</c> table, ported from the
/// Python <c>PerformanceDatabase</c> class. Each call opens and closes its own
/// connection, matching the original's short-lived <c>with sqlite3.connect(...)</c> usage.
/// </summary>
public sealed class SqlitePerformanceRepository : IPerformanceRepository
{
    private readonly string _connectionString;
    private readonly ILogger<SqlitePerformanceRepository> _logger;

    public SqlitePerformanceRepository(IOptions<PerformanceOptions> options, ILogger<SqlitePerformanceRepository> logger)
    {
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = options.Value.DatabasePath,
        }.ToString();
        _logger = logger;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        var command = connection.CreateCommand();
        command.CommandText =
            """
            CREATE TABLE IF NOT EXISTS performance_cache (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                url TEXT NOT NULL UNIQUE,
                metrics TEXT NOT NULL,
                analysis_summary TEXT,
                created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
                updated_at DATETIME DEFAULT CURRENT_TIMESTAMP
            );
            CREATE INDEX IF NOT EXISTS idx_url ON performance_cache(url);
            """;
        await command.ExecuteNonQueryAsync(cancellationToken);
        _logger.LogInformation("Database initialized: {DataSource}", connection.DataSource);
    }

    public async Task<bool> UpsertMetricsAsync(string url, PerformanceMetrics metrics, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = await OpenConnectionAsync(cancellationToken);
            var command = connection.CreateCommand();
            command.CommandText =
                """
                INSERT INTO performance_cache (url, metrics, updated_at)
                VALUES ($url, $metrics, CURRENT_TIMESTAMP)
                ON CONFLICT(url) DO UPDATE SET
                    metrics = excluded.metrics,
                    updated_at = CURRENT_TIMESTAMP;
                """;
            command.Parameters.AddWithValue("$url", url);
            command.Parameters.AddWithValue("$metrics", JsonSerializer.Serialize(metrics, JsonDefaults.Options));
            await command.ExecuteNonQueryAsync(cancellationToken);
            _logger.LogInformation("Metrics upserted for {Url}", url);
            return true;
        }
        catch (SqliteException ex)
        {
            _logger.LogError(ex, "Database error inserting metrics for {Url}", url);
            return false;
        }
    }

    public async Task<PerformanceMetrics?> GetMetricsAsync(string url, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = await OpenConnectionAsync(cancellationToken);
            var command = connection.CreateCommand();
            command.CommandText = "SELECT metrics FROM performance_cache WHERE url = $url;";
            command.Parameters.AddWithValue("$url", url);

            var result = await command.ExecuteScalarAsync(cancellationToken);
            return result is string json
                ? JsonSerializer.Deserialize<PerformanceMetrics>(json, JsonDefaults.Options)
                : null;
        }
        catch (SqliteException ex)
        {
            _logger.LogError(ex, "Database error retrieving metrics for {Url}", url);
            return null;
        }
    }

    public async Task<bool> DeleteMetricsAsync(string url, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = await OpenConnectionAsync(cancellationToken);
            var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM performance_cache WHERE url = $url;";
            command.Parameters.AddWithValue("$url", url);
            await command.ExecuteNonQueryAsync(cancellationToken);
            _logger.LogInformation("Metrics deleted for {Url}", url);
            return true;
        }
        catch (SqliteException ex)
        {
            _logger.LogError(ex, "Database error deleting metrics for {Url}", url);
            return false;
        }
    }

    public async Task<IReadOnlyList<CachedMetrics>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = await OpenConnectionAsync(cancellationToken);
            var command = connection.CreateCommand();
            command.CommandText =
                "SELECT url, metrics, updated_at FROM performance_cache ORDER BY updated_at DESC;";

            var results = new List<CachedMetrics>();
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var url = reader.GetString(0);
                var metricsJson = reader.GetString(1);
                var updatedAt = reader.GetDateTime(2);
                var metrics = JsonSerializer.Deserialize<PerformanceMetrics>(metricsJson, JsonDefaults.Options);
                if (metrics is not null)
                {
                    results.Add(new CachedMetrics(url, metrics, updatedAt));
                }
            }

            return results;
        }
        catch (SqliteException ex)
        {
            _logger.LogError(ex, "Database error retrieving all metrics");
            return Array.Empty<CachedMetrics>();
        }
    }

    public async Task<bool> ClearAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = await OpenConnectionAsync(cancellationToken);
            var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM performance_cache;";
            await command.ExecuteNonQueryAsync(cancellationToken);
            _logger.LogInformation("Performance cache cleared");
            return true;
        }
        catch (SqliteException ex)
        {
            _logger.LogError(ex, "Database error clearing cache");
            return false;
        }
    }

    private async Task<SqliteConnection> OpenConnectionAsync(CancellationToken cancellationToken)
    {
        var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }
}
