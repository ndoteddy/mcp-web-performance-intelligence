using System.Text.Json.Serialization;

namespace McpWebPerformanceIntelligence.Models;

/// <summary>Uniform error envelope returned by MCP tools instead of throwing across the wire.</summary>
public sealed class ErrorResponse
{
    [JsonPropertyName("error")]
    public string Error { get; init; } = string.Empty;
}
