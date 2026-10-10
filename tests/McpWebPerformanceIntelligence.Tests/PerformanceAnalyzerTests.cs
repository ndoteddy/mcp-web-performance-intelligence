using System.Net;
using McpWebPerformanceIntelligence.Configuration;
using McpWebPerformanceIntelligence.Services;
using McpWebPerformanceIntelligence.Tests.TestSupport;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace McpWebPerformanceIntelligence.Tests;

public class PerformanceAnalyzerTests
{
    private const string SampleLighthouseJson =
        """
        {
          "lighthouseResult": {
            "categories": {
              "performance": { "score": 0.76 }
            },
            "audits": {
              "first-contentful-paint": { "displayValue": "0.5 s" },
              "speed-index": { "displayValue": "0.7 s" },
              "largest-contentful-paint": { "displayValue": "0.9 s" },
              "interactive": { "displayValue": "1.9 s" },
              "total-blocking-time": { "displayValue": "50 ms" },
              "cumulative-layout-shift": { "displayValue": "0.001" }
            }
          }
        }
        """;

    private static PerformanceAnalyzer CreateAnalyzer(HttpMessageHandler handler, PerformanceOptions? options = null)
    {
        var httpClient = new HttpClient(handler);
        var performanceOptions = options ?? new PerformanceOptions { PageSpeedApiKey = "test-key" };
        return new PerformanceAnalyzer(httpClient, Options.Create(performanceOptions), NullLogger<PerformanceAnalyzer>.Instance);
    }

    [Fact]
    public async Task AnalyzeAsync_ValidResponse_ExtractsAllMetrics()
    {
        var analyzer = CreateAnalyzer(FakeHttpMessageHandler.ReturningJson(SampleLighthouseJson));

        var metrics = await analyzer.AnalyzeAsync("https://example.com");

        Assert.NotNull(metrics);
        Assert.Equal(76.0, metrics!.Score);
        Assert.Equal("0.5 s", metrics.FirstContentfulPaint);
        Assert.Equal("0.7 s", metrics.SpeedIndex);
        Assert.Equal("0.9 s", metrics.LargestContentfulPaint);
        Assert.Equal("1.9 s", metrics.TimeToInteractive);
        Assert.Equal("50 ms", metrics.TotalBlockingTime);
        Assert.Equal("0.001", metrics.CumulativeLayoutShift);
    }

    [Fact]
    public async Task AnalyzeAsync_HttpError_ReturnsNull()
    {
        var analyzer = CreateAnalyzer(FakeHttpMessageHandler.ReturningStatus(HttpStatusCode.ServiceUnavailable));

        var metrics = await analyzer.AnalyzeAsync("https://example.com");

        Assert.Null(metrics);
    }

    [Fact]
    public async Task AnalyzeAsync_MalformedJson_ReturnsNull()
    {
        var analyzer = CreateAnalyzer(FakeHttpMessageHandler.ReturningJson("not-json"));

        var metrics = await analyzer.AnalyzeAsync("https://example.com");

        Assert.Null(metrics);
    }

    [Fact]
    public async Task AnalyzeAsync_MissingAudits_FillsMissingFieldsWithNA()
    {
        var analyzer = CreateAnalyzer(FakeHttpMessageHandler.ReturningJson("{\"lighthouseResult\": {}}"));

        var metrics = await analyzer.AnalyzeAsync("https://example.com");

        Assert.NotNull(metrics);
        Assert.Equal(0, metrics!.Score);
        Assert.Equal("N/A", metrics.FirstContentfulPaint);
        Assert.Equal("N/A", metrics.TotalBlockingTime);
    }

    [Theory]
    [InlineData(95, "Excellent")]
    [InlineData(90, "Excellent")]
    [InlineData(89.9, "Good")]
    [InlineData(50, "Good")]
    [InlineData(49.9, "Needs Improvement")]
    [InlineData(25, "Needs Improvement")]
    [InlineData(24.9, "Poor")]
    [InlineData(0, "Poor")]
    public void GetPerformanceStatus_MapsScoreToBand(double score, string expected)
    {
        var analyzer = CreateAnalyzer(FakeHttpMessageHandler.ReturningJson(SampleLighthouseJson));

        Assert.Equal(expected, analyzer.GetPerformanceStatus(score));
    }

    [Fact]
    public void ValidateMetrics_AllRequiredFieldsPresent_ReturnsTrue()
    {
        var analyzer = CreateAnalyzer(FakeHttpMessageHandler.ReturningJson(SampleLighthouseJson));
        var metrics = new McpWebPerformanceIntelligence.Models.PerformanceMetrics
        {
            Score = 76,
            FirstContentfulPaint = "0.5 s",
            SpeedIndex = "0.7 s",
            TimeToInteractive = "1.9 s",
        };

        Assert.True(analyzer.ValidateMetrics(metrics));
    }

    [Fact]
    public void ValidateMetrics_MissingRequiredField_ReturnsFalse()
    {
        var analyzer = CreateAnalyzer(FakeHttpMessageHandler.ReturningJson(SampleLighthouseJson));
        var metrics = new McpWebPerformanceIntelligence.Models.PerformanceMetrics
        {
            Score = 76,
            FirstContentfulPaint = "N/A",
            SpeedIndex = "0.7 s",
            TimeToInteractive = "1.9 s",
        };

        Assert.False(analyzer.ValidateMetrics(metrics));
    }
}
