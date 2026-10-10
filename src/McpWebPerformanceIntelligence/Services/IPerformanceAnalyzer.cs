using McpWebPerformanceIntelligence.Models;

namespace McpWebPerformanceIntelligence.Services;

/// <summary>Interfaces with Google PageSpeed Insights and extracts performance metrics.</summary>
public interface IPerformanceAnalyzer
{
    /// <summary>
    /// Fetches and parses PageSpeed metrics for a website. Returns <c>null</c> on any
    /// network, HTTP, or parsing failure.
    /// </summary>
    Task<PerformanceMetrics?> AnalyzeAsync(string url, CancellationToken cancellationToken = default);

    /// <summary>Returns <c>true</c> when the required string metrics were populated by Lighthouse.</summary>
    bool ValidateMetrics(PerformanceMetrics metrics);

    /// <summary>Maps a 0-100 performance score to a human-readable status band.</summary>
    string GetPerformanceStatus(double score);
}
