using System.Text.Json.Serialization;

namespace McpWebPerformanceIntelligence.Models;

/// <summary>Response payload returned by the <c>suggest_improvements</c> tool.</summary>
public sealed class SuggestionsResponse
{
    [JsonPropertyName("url")]
    public string Url { get; init; } = string.Empty;

    [JsonPropertyName("recommendations")]
    public List<Recommendation> Recommendations { get; init; } = new();

    [JsonPropertyName("metrics_analyzed")]
    public MetricsSummary MetricsAnalyzed { get; init; } = new();
}

/// <summary>Condensed view of the metrics that drove a set of recommendations.</summary>
public sealed class MetricsSummary
{
    [JsonPropertyName("score")]
    public double Score { get; init; }

    [JsonPropertyName("status")]
    public string Status { get; init; } = string.Empty;
}
