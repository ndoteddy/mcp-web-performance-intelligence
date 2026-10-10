using McpWebPerformanceIntelligence.Models;

namespace McpWebPerformanceIntelligence.Services;

/// <summary>Persists and retrieves cached performance metrics keyed by URL.</summary>
public interface IPerformanceRepository
{
    /// <summary>Creates the backing table/index if they do not already exist.</summary>
    Task InitializeAsync(CancellationToken cancellationToken = default);

    Task<bool> UpsertMetricsAsync(string url, PerformanceMetrics metrics, CancellationToken cancellationToken = default);

    Task<PerformanceMetrics?> GetMetricsAsync(string url, CancellationToken cancellationToken = default);

    Task<bool> DeleteMetricsAsync(string url, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CachedMetrics>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<bool> ClearAsync(CancellationToken cancellationToken = default);
}

/// <summary>A cached metrics row with its URL key and last-updated timestamp.</summary>
public sealed record CachedMetrics(string Url, PerformanceMetrics Metrics, DateTime UpdatedAt);
