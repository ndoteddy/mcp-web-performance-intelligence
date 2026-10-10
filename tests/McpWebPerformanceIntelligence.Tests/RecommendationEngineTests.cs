using McpWebPerformanceIntelligence.Configuration;
using McpWebPerformanceIntelligence.Models;
using McpWebPerformanceIntelligence.Services;
using Xunit;

namespace McpWebPerformanceIntelligence.Tests;

public class RecommendationEngineTests
{
    private static readonly PerformanceOptions Options = new()
    {
        PerformanceScoreThreshold = 70.0,
        TbtThresholdMs = 300.0,
        TtiThresholdMs = 3800.0,
    };

    [Fact]
    public void Generate_LowScore_AddsOverallPerformanceRecommendation()
    {
        var metrics = new PerformanceMetrics
        {
            Score = 42,
            FirstContentfulPaint = "0.5 s",
            SpeedIndex = "0.7 s",
            TimeToInteractive = "1.0 s",
            TotalBlockingTime = "0 ms",
        };

        var recommendations = RecommendationEngine.Generate(metrics, Options);

        Assert.Contains(recommendations, r => r.Category == "Overall Performance" && r.Priority == 1);
    }

    [Fact]
    public void Generate_HighTotalBlockingTime_AddsMainThreadRecommendation()
    {
        var metrics = new PerformanceMetrics
        {
            Score = 90,
            FirstContentfulPaint = "0.5 s",
            SpeedIndex = "0.7 s",
            TimeToInteractive = "1.0 s",
            TotalBlockingTime = "1,050 ms",
        };

        var recommendations = RecommendationEngine.Generate(metrics, Options);

        var recommendation = Assert.Single(recommendations);
        Assert.Equal("Main Thread Optimization", recommendation.Category);
        Assert.Equal(2, recommendation.Priority);
    }

    [Fact]
    public void Generate_TotalBlockingTimeAtThreshold_IsNotFlagged()
    {
        var metrics = new PerformanceMetrics
        {
            Score = 90,
            FirstContentfulPaint = "0.5 s",
            SpeedIndex = "0.7 s",
            TimeToInteractive = "1.0 s",
            TotalBlockingTime = "300 ms",
        };

        var recommendations = RecommendationEngine.Generate(metrics, Options);

        Assert.DoesNotContain(recommendations, r => r.Category == "Main Thread Optimization");
    }

    [Fact]
    public void Generate_SlowTimeToInteractive_AddsCriticalPathRecommendation()
    {
        var metrics = new PerformanceMetrics
        {
            Score = 90,
            FirstContentfulPaint = "0.5 s",
            SpeedIndex = "0.7 s",
            TimeToInteractive = "4.5 s",
            TotalBlockingTime = "0 ms",
        };

        var recommendations = RecommendationEngine.Generate(metrics, Options);

        var recommendation = Assert.Single(recommendations);
        Assert.Equal("Critical Path Optimization", recommendation.Category);
        Assert.Equal(3, recommendation.Priority);
    }

    [Fact]
    public void Generate_AllThresholdsBreached_ReturnsAllThreeInPriorityOrder()
    {
        var metrics = new PerformanceMetrics
        {
            Score = 40,
            FirstContentfulPaint = "0.5 s",
            SpeedIndex = "0.7 s",
            TimeToInteractive = "4.5 s",
            TotalBlockingTime = "1,050 ms",
        };

        var recommendations = RecommendationEngine.Generate(metrics, Options);

        Assert.Equal(3, recommendations.Count);
        Assert.Equal(new[] { 1, 2, 3 }, recommendations.Select(r => r.Priority));
    }

    [Fact]
    public void Generate_NoIssues_ReturnsFallbackOptimizationRecommendation()
    {
        var metrics = new PerformanceMetrics
        {
            Score = 95,
            FirstContentfulPaint = "0.3 s",
            SpeedIndex = "0.4 s",
            TimeToInteractive = "1.0 s",
            TotalBlockingTime = "0 ms",
        };

        var recommendations = RecommendationEngine.Generate(metrics, Options);

        var recommendation = Assert.Single(recommendations);
        Assert.Equal("Optimization Opportunities", recommendation.Category);
        Assert.Equal("Low", recommendation.Impact);
    }

    [Fact]
    public void Generate_TotalBlockingTimeWithoutMsUnit_IsIgnored()
    {
        // Mirrors the original Python behavior: only "ms"-suffixed TBT values are parsed.
        var metrics = new PerformanceMetrics
        {
            Score = 90,
            FirstContentfulPaint = "0.5 s",
            SpeedIndex = "0.7 s",
            TimeToInteractive = "1.0 s",
            TotalBlockingTime = "1.5 s",
        };

        var recommendations = RecommendationEngine.Generate(metrics, Options);

        Assert.DoesNotContain(recommendations, r => r.Category == "Main Thread Optimization");
    }
}
