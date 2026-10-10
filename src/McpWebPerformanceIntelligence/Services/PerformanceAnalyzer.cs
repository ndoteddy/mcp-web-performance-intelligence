using System.Text.Json;
using McpWebPerformanceIntelligence.Configuration;
using McpWebPerformanceIntelligence.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace McpWebPerformanceIntelligence.Services;

/// <summary>
/// Calls the Google PageSpeed Insights V5 API and extracts the Lighthouse metrics the
/// agent reasons over. Ported from the Python <c>PerformanceAnalyzer</c>.
/// </summary>
public sealed class PerformanceAnalyzer : IPerformanceAnalyzer
{
    private readonly HttpClient _httpClient;
    private readonly PerformanceOptions _options;
    private readonly ILogger<PerformanceAnalyzer> _logger;

    public PerformanceAnalyzer(
        HttpClient httpClient,
        IOptions<PerformanceOptions> options,
        ILogger<PerformanceAnalyzer> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<PerformanceMetrics?> AnalyzeAsync(string url, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Analyzing website: {Url}", url);

            var requestUri =
                $"{_options.PageSpeedApiUrl}?url={Uri.EscapeDataString(url)}&key={Uri.EscapeDataString(_options.PageSpeedApiKey)}";

            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(_options.ApiTimeoutSeconds));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            using var response = await _httpClient.GetAsync(requestUri, linkedCts.Token);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(linkedCts.Token);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: linkedCts.Token);

            var metrics = ExtractMetrics(document.RootElement);
            _logger.LogInformation("Successfully extracted metrics for {Url}", url);
            return metrics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing {Url}", url);
            return null;
        }
    }

    /// <summary>Pulls the Lighthouse category score and display-value audits out of the raw API response.</summary>
    internal static PerformanceMetrics ExtractMetrics(JsonElement root)
    {
        var lighthouse = GetObjectOrDefault(root, "lighthouseResult");
        var categories = GetObjectOrDefault(lighthouse, "categories");
        var audits = GetObjectOrDefault(lighthouse, "audits");

        var score = 0.0;
        if (categories.ValueKind == JsonValueKind.Object &&
            categories.TryGetProperty("performance", out var performance) &&
            performance.ValueKind == JsonValueKind.Object &&
            performance.TryGetProperty("score", out var scoreElement) &&
            scoreElement.ValueKind == JsonValueKind.Number)
        {
            score = Math.Round(scoreElement.GetDouble() * 100, 1);
        }

        return new PerformanceMetrics
        {
            Score = score,
            FirstContentfulPaint = GetDisplayValue(audits, "first-contentful-paint"),
            SpeedIndex = GetDisplayValue(audits, "speed-index"),
            LargestContentfulPaint = GetDisplayValue(audits, "largest-contentful-paint"),
            TimeToInteractive = GetDisplayValue(audits, "interactive"),
            TotalBlockingTime = GetDisplayValue(audits, "total-blocking-time"),
            CumulativeLayoutShift = GetDisplayValue(audits, "cumulative-layout-shift"),
        };
    }

    private static JsonElement GetObjectOrDefault(JsonElement parent, string propertyName)
    {
        if (parent.ValueKind == JsonValueKind.Object && parent.TryGetProperty(propertyName, out var value))
        {
            return value;
        }

        return default;
    }

    private static string GetDisplayValue(JsonElement audits, string auditKey)
    {
        if (audits.ValueKind == JsonValueKind.Object &&
            audits.TryGetProperty(auditKey, out var audit) &&
            audit.ValueKind == JsonValueKind.Object &&
            audit.TryGetProperty("displayValue", out var displayValue) &&
            displayValue.ValueKind == JsonValueKind.String)
        {
            return displayValue.GetString() ?? "N/A";
        }

        return "N/A";
    }

    public bool ValidateMetrics(PerformanceMetrics metrics)
    {
        return metrics.FirstContentfulPaint != "N/A"
            && metrics.SpeedIndex != "N/A"
            && metrics.TimeToInteractive != "N/A";
    }

    public string GetPerformanceStatus(double score) => score switch
    {
        >= 90 => "Excellent",
        >= 50 => "Good",
        >= 25 => "Needs Improvement",
        _ => "Poor",
    };
}
