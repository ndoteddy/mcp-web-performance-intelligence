using System.Globalization;
using McpWebPerformanceIntelligence.Configuration;
using McpWebPerformanceIntelligence.Models;

namespace McpWebPerformanceIntelligence.Services;

/// <summary>
/// Generates prioritized, actionable recommendations from cached metrics. Pure and
/// side-effect free so it can be unit tested without a database or HTTP dependency.
/// Ported from the Python <c>_generate_recommendations</c> helper (originally present
/// in the project's deleted <c>src/tools.py</c>, never re-exposed after the notebook
/// was simplified).
/// </summary>
public static class RecommendationEngine
{
    public static List<Recommendation> Generate(PerformanceMetrics metrics, PerformanceOptions options)
    {
        var recommendations = new List<Recommendation>();

        if (metrics.Score < options.PerformanceScoreThreshold)
        {
            recommendations.Add(new Recommendation
            {
                Priority = 1,
                Category = "Overall Performance",
                Issue = $"Performance score is {metrics.Score} (threshold: {options.PerformanceScoreThreshold})",
                RecommendationText =
                    "Comprehensive performance audit required. Focus on JavaScript optimization and resource prioritization.",
                Impact = "High",
            });
        }

        if (TryParseLeadingNumber(metrics.TotalBlockingTime, "ms", out var totalBlockingTimeMs) &&
            totalBlockingTimeMs > options.TbtThresholdMs)
        {
            recommendations.Add(new Recommendation
            {
                Priority = 2,
                Category = "Main Thread Optimization",
                Issue = $"Total Blocking Time: {metrics.TotalBlockingTime}",
                RecommendationText =
                    "Implement task chunking using scheduler.yield(). Consider code-splitting and offloading third-party scripts to Web Workers.",
                Impact = "High",
            });
        }

        if (TryParseSecondsAsMilliseconds(metrics.TimeToInteractive, out var timeToInteractiveMs) &&
            timeToInteractiveMs > options.TtiThresholdMs)
        {
            recommendations.Add(new Recommendation
            {
                Priority = 3,
                Category = "Critical Path Optimization",
                Issue = $"Time to Interactive: {metrics.TimeToInteractive}",
                RecommendationText =
                    "Minimize critical resources. Use lazy loading for non-essential JavaScript. Consider selective hydration or Islands Architecture.",
                Impact = "Medium",
            });
        }

        if (recommendations.Count == 0)
        {
            recommendations.Add(new Recommendation
            {
                Priority = 1,
                Category = "Optimization Opportunities",
                Issue = "Site meets performance thresholds",
                RecommendationText =
                    "Continue monitoring performance. Implement Image optimization and Cache-Control headers. Regular performance audits recommended.",
                Impact = "Low",
            });
        }

        return recommendations;
    }

    /// <summary>Parses display values like "1,050 ms" once the given unit marker is present.</summary>
    private static bool TryParseLeadingNumber(string? displayValue, string unitMarker, out double value)
    {
        value = 0;
        if (string.IsNullOrWhiteSpace(displayValue) || displayValue == "N/A" || !displayValue.Contains(unitMarker))
        {
            return false;
        }

        var cleaned = displayValue.Replace(unitMarker, string.Empty).Replace(",", string.Empty).Trim();
        var firstToken = cleaned.Split(' ', ' ')[0];
        return double.TryParse(firstToken, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }

    /// <summary>Parses display values like "2.2 s" / "2.2&#160;s" and converts to milliseconds.</summary>
    private static bool TryParseSecondsAsMilliseconds(string? displayValue, out double milliseconds)
    {
        milliseconds = 0;
        if (string.IsNullOrWhiteSpace(displayValue) || displayValue == "N/A" || !displayValue.Contains('s'))
        {
            return false;
        }

        var firstToken = displayValue.Split(' ', ' ')[0];
        if (!double.TryParse(firstToken, NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds))
        {
            return false;
        }

        milliseconds = seconds * 1000;
        return true;
    }
}
