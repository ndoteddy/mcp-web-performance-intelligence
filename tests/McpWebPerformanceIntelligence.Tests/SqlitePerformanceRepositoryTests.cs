using McpWebPerformanceIntelligence.Configuration;
using McpWebPerformanceIntelligence.Models;
using McpWebPerformanceIntelligence.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace McpWebPerformanceIntelligence.Tests;

public class SqlitePerformanceRepositoryTests : IAsyncLifetime
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"mcp_perf_test_{Guid.NewGuid():N}.db");
    private SqlitePerformanceRepository _repository = null!;

    public async Task InitializeAsync()
    {
        var options = Options.Create(new PerformanceOptions { DatabasePath = _dbPath });
        _repository = new SqlitePerformanceRepository(options, NullLogger<SqlitePerformanceRepository>.Instance);
        await _repository.InitializeAsync();
    }

    public Task DisposeAsync()
    {
        // Microsoft.Data.Sqlite pools native connections per connection string, which
        // keeps a file handle open after the last SqliteConnection is disposed.
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();

        if (File.Exists(_dbPath))
        {
            File.Delete(_dbPath);
        }

        return Task.CompletedTask;
    }

    private static PerformanceMetrics SampleMetrics(double score = 76) => new()
    {
        Score = score,
        FirstContentfulPaint = "0.5 s",
        SpeedIndex = "0.7 s",
        LargestContentfulPaint = "0.9 s",
        TimeToInteractive = "1.9 s",
        TotalBlockingTime = "50 ms",
        CumulativeLayoutShift = "0.001",
    };

    [Fact]
    public async Task GetMetricsAsync_WithoutPriorInsert_ReturnsNull()
    {
        var result = await _repository.GetMetricsAsync("https://missing.example");

        Assert.Null(result);
    }

    [Fact]
    public async Task UpsertThenGet_RoundTripsMetrics()
    {
        var metrics = SampleMetrics();

        var inserted = await _repository.UpsertMetricsAsync("https://example.com", metrics);
        var retrieved = await _repository.GetMetricsAsync("https://example.com");

        Assert.True(inserted);
        Assert.NotNull(retrieved);
        Assert.Equal(metrics.Score, retrieved!.Score);
        Assert.Equal(metrics.FirstContentfulPaint, retrieved.FirstContentfulPaint);
        Assert.Equal(metrics.TotalBlockingTime, retrieved.TotalBlockingTime);
    }

    [Fact]
    public async Task Upsert_SameUrlTwice_OverwritesPreviousMetrics()
    {
        await _repository.UpsertMetricsAsync("https://example.com", SampleMetrics(score: 50));
        await _repository.UpsertMetricsAsync("https://example.com", SampleMetrics(score: 95));

        var retrieved = await _repository.GetMetricsAsync("https://example.com");

        Assert.NotNull(retrieved);
        Assert.Equal(95, retrieved!.Score);
    }

    [Fact]
    public async Task DeleteMetricsAsync_RemovesCachedRow()
    {
        await _repository.UpsertMetricsAsync("https://example.com", SampleMetrics());

        var deleted = await _repository.DeleteMetricsAsync("https://example.com");
        var retrieved = await _repository.GetMetricsAsync("https://example.com");

        Assert.True(deleted);
        Assert.Null(retrieved);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllCachedUrlsNewestFirst()
    {
        await _repository.UpsertMetricsAsync("https://first.example", SampleMetrics());
        await _repository.UpsertMetricsAsync("https://second.example", SampleMetrics());

        var all = await _repository.GetAllAsync();

        Assert.Equal(2, all.Count);
        Assert.Contains(all, row => row.Url == "https://first.example");
        Assert.Contains(all, row => row.Url == "https://second.example");
    }

    [Fact]
    public async Task ClearAsync_RemovesAllRows()
    {
        await _repository.UpsertMetricsAsync("https://example.com", SampleMetrics());

        var cleared = await _repository.ClearAsync();
        var all = await _repository.GetAllAsync();

        Assert.True(cleared);
        Assert.Empty(all);
    }
}
