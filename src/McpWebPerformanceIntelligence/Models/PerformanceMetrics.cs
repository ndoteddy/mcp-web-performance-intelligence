namespace McpWebPerformanceIntelligence.Models;

/// <summary>
/// Performance metrics extracted from a Google PageSpeed Insights (Lighthouse) report.
/// Field names mirror the original Python analyzer so cached JSON stays stable across ports.
/// </summary>
public sealed class PerformanceMetrics
{
    public double Score { get; init; }

    public string FirstContentfulPaint { get; init; } = "N/A";

    public string SpeedIndex { get; init; } = "N/A";

    public string LargestContentfulPaint { get; init; } = "N/A";

    public string TimeToInteractive { get; init; } = "N/A";

    public string TotalBlockingTime { get; init; } = "N/A";

    public string CumulativeLayoutShift { get; init; } = "N/A";
}
