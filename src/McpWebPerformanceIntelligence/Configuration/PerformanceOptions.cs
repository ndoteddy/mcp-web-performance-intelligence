namespace McpWebPerformanceIntelligence.Configuration;

/// <summary>
/// Central configuration for the performance analysis pipeline.
/// Mirrors the thresholds and API settings from the original Python
/// <c>Config</c> class, bound from appsettings.json and overridable via
/// environment variables (<c>PAGESPEED_API_KEY</c>, <c>DB_FILE</c>).
/// </summary>
public sealed class PerformanceOptions
{
    public const string SectionName = "Performance";

    public string PageSpeedApiKey { get; set; } = string.Empty;

    public string PageSpeedApiUrl { get; set; } =
        "https://www.googleapis.com/pagespeedonline/v5/runPagespeed";

    public int ApiTimeoutSeconds { get; set; } = 60;

    public string DatabasePath { get; set; } = "mcp_performance.db";

    public double PerformanceScoreThreshold { get; set; } = 70.0;

    public double FcpThresholdMs { get; set; } = 1800.0;

    public double TtiThresholdMs { get; set; } = 3800.0;

    public double TbtThresholdMs { get; set; } = 300.0;
}
