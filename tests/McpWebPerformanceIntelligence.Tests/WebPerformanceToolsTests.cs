using System.Text.Json;
using McpWebPerformanceIntelligence.Configuration;
using McpWebPerformanceIntelligence.Models;
using McpWebPerformanceIntelligence.Serialization;
using McpWebPerformanceIntelligence.Services;
using McpWebPerformanceIntelligence.Tools;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace McpWebPerformanceIntelligence.Tests;

public class WebPerformanceToolsTests
{
    private readonly Mock<IPerformanceAnalyzer> _analyzer = new();
    private readonly Mock<IPerformanceRepository> _repository = new();
    private readonly PerformanceOptions _options = new();
    private readonly WebPerformanceTools _tools;

    public WebPerformanceToolsTests()
    {
        _tools = new WebPerformanceTools(
            _analyzer.Object,
            _repository.Object,
            Options.Create(_options),
            NullLogger<WebPerformanceTools>.Instance);
    }

    private static PerformanceMetrics SampleMetrics(double score = 76) => new()
    {
        Score = score,
        FirstContentfulPaint = "0.5 s",
        SpeedIndex = "0.7 s",
        LargestContentfulPaint = "0.9 s",
        TimeToInteractive = "1.9 s",
        TotalBlockingTime = "50 ms",
        CumulativeLayoutShift = "0.001",
    };

    [Fact]
    public async Task AnalyzeWebsiteAsync_ValidMetrics_PersistsAndReturnsMetricsJson()
    {
        var metrics = SampleMetrics();
        _analyzer.Setup(a => a.AnalyzeAsync("https://example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(metrics);
        _analyzer.Setup(a => a.ValidateMetrics(metrics)).Returns(true);

        var json = await _tools.AnalyzeWebsiteAsync("https://example.com");
        var deserialized = JsonSerializer.Deserialize<PerformanceMetrics>(json, JsonDefaults.Options);

        Assert.NotNull(deserialized);
        Assert.Equal(76, deserialized!.Score);
        _repository.Verify(r => r.UpsertMetricsAsync("https://example.com", metrics, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AnalyzeWebsiteAsync_AnalyzerReturnsNull_ReturnsErrorAndDoesNotPersist()
    {
        _analyzer.Setup(a => a.AnalyzeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PerformanceMetrics?)null);

        var json = await _tools.AnalyzeWebsiteAsync("https://example.com");
        var error = JsonSerializer.Deserialize<ErrorResponse>(json, JsonDefaults.Options);

        Assert.NotNull(error);
        Assert.Contains("Failed to fetch metrics", error!.Error);
        _repository.Verify(r => r.UpsertMetricsAsync(It.IsAny<string>(), It.IsAny<PerformanceMetrics>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AnalyzeWebsiteAsync_ValidationFails_ReturnsErrorAndDoesNotPersist()
    {
        var metrics = SampleMetrics();
        _analyzer.Setup(a => a.AnalyzeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(metrics);
        _analyzer.Setup(a => a.ValidateMetrics(metrics)).Returns(false);

        var json = await _tools.AnalyzeWebsiteAsync("https://example.com");
        var error = JsonSerializer.Deserialize<ErrorResponse>(json, JsonDefaults.Options);

        Assert.NotNull(error);
        Assert.Contains("validation failed", error!.Error);
        _repository.Verify(r => r.UpsertMetricsAsync(It.IsAny<string>(), It.IsAny<PerformanceMetrics>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AnalyzeWebsiteAsync_AnalyzerThrows_ReturnsErrorEnvelope()
    {
        _analyzer.Setup(a => a.AnalyzeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var json = await _tools.AnalyzeWebsiteAsync("https://example.com");
        var error = JsonSerializer.Deserialize<ErrorResponse>(json, JsonDefaults.Options);

        Assert.NotNull(error);
        Assert.Equal("boom", error!.Error);
    }

    [Fact]
    public async Task SuggestImprovementsAsync_NoCachedMetrics_ReturnsError()
    {
        _repository.Setup(r => r.GetMetricsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PerformanceMetrics?)null);

        var json = await _tools.SuggestImprovementsAsync("https://example.com");
        var error = JsonSerializer.Deserialize<ErrorResponse>(json, JsonDefaults.Options);

        Assert.NotNull(error);
        Assert.Contains("Run analyze_website first", error!.Error);
    }

    [Fact]
    public async Task SuggestImprovementsAsync_CachedMetrics_ReturnsRecommendationsAndSummary()
    {
        var metrics = SampleMetrics(score: 40);
        _repository.Setup(r => r.GetMetricsAsync("https://example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(metrics);
        _analyzer.Setup(a => a.GetPerformanceStatus(40)).Returns("Needs Improvement");

        var json = await _tools.SuggestImprovementsAsync("https://example.com");
        var response = JsonSerializer.Deserialize<SuggestionsResponse>(json, JsonDefaults.Options);

        Assert.NotNull(response);
        Assert.Equal("https://example.com", response!.Url);
        Assert.NotEmpty(response.Recommendations);
        Assert.Equal(40, response.MetricsAnalyzed.Score);
        Assert.Equal("Needs Improvement", response.MetricsAnalyzed.Status);
    }
}
