using System.Text.Json;

namespace McpWebPerformanceIntelligence.Serialization;

/// <summary>
/// Shared JSON options for tool payloads and cache serialization. Snake-case naming
/// keeps wire output compatible with the original Python implementation's field names
/// (e.g. <c>first_contentful_paint</c>) for any client already parsing that shape.
/// </summary>
public static class JsonDefaults
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };
}
