using System.Text.Json.Serialization;

namespace McpWebPerformanceIntelligence.Models;

/// <summary>A single prioritized, actionable remediation item.</summary>
public sealed class Recommendation
{
    [JsonPropertyName("priority")]
    public int Priority { get; init; }

    [JsonPropertyName("category")]
    public string Category { get; init; } = string.Empty;

    [JsonPropertyName("issue")]
    public string Issue { get; init; } = string.Empty;

    [JsonPropertyName("recommendation")]
    public string RecommendationText { get; init; } = string.Empty;

    [JsonPropertyName("impact")]
    public string Impact { get; init; } = string.Empty;
}
