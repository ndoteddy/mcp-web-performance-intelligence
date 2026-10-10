using System.ComponentModel;
using System.Text.Json;
using McpWebPerformanceIntelligence.Configuration;
using McpWebPerformanceIntelligence.Models;
using McpWebPerformanceIntelligence.Serialization;
using McpWebPerformanceIntelligence.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;

namespace McpWebPerformanceIntelligence.Tools;

/// <summary>
/// MCP tool surface for website performance diagnostics. This is the direct analog of
/// the Python <c>FastMCP</c> server's <c>@mcp.tool()</c>-decorated functions: an MCP
/// client (an LLM agent, an IDE, Claude Desktop, etc.) discovers and calls
/// <c>analyze_website</c> and <c>suggest_improvements</c> exactly as it would any other
/// MCP tool, regardless of which model is orchestrating the conversation.
/// </summary>
[McpServerToolType]
public sealed class WebPerformanceTools
{
    private readonly IPerformanceAnalyzer _analyzer;
    private readonly IPerformanceRepository _repository;
    private readonly PerformanceOptions _options;
    private readonly ILogger<WebPerformanceTools> _logger;

    public WebPerformanceTools(
        IPerformanceAnalyzer analyzer,
        IPerformanceRepository repository,
        IOptions<PerformanceOptions> options,
        ILogger<WebPerformanceTools> logger)
    {
        _analyzer = analyzer;
        _repository = repository;
        _options = options.Value;
        _logger = logger;
    }

    [McpServerTool(Name = "analyze_website")]
    [Description(
        "Analyzes website performance using Google PageSpeed Insights. Returns JSON metrics: " +
        "score, first_contentful_paint, speed_index, largest_contentful_paint, time_to_interactive, " +
        "total_blocking_time, cumulative_layout_shift. Call this before suggest_improvements.")]
    public async Task<string> AnalyzeWebsiteAsync(
        [Description("The full URL of the website to analyze, including scheme (e.g. https://example.com)")]
        string url,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var metrics = await _analyzer.AnalyzeAsync(url, cancellationToken);
            if (metrics is null)
            {
                return Serialize(new ErrorResponse { Error = "Failed to fetch metrics from PageSpeed API" });
            }

            if (!_analyzer.ValidateMetrics(metrics))
            {
                _logger.LogWarning("Metrics validation failed for {Url}", url);
                return Serialize(new ErrorResponse { Error = "Metrics validation failed" });
            }

            await _repository.UpsertMetricsAsync(url, metrics, cancellationToken);
            return Serialize(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in AnalyzeWebsiteAsync for {Url}", url);
            return Serialize(new ErrorResponse { Error = ex.Message });
        }
    }

    [McpServerTool(Name = "suggest_improvements")]
    [Description(
        "Generates 3 prioritized, actionable technical recommendations based on the most recently " +
        "cached analysis for a URL. Requires analyze_website to have been called first for that URL.")]
    public async Task<string> SuggestImprovementsAsync(
        [Description("The full URL of the website to get suggestions for")]
        string url,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var metrics = await _repository.GetMetricsAsync(url, cancellationToken);
            if (metrics is null)
            {
                return Serialize(new ErrorResponse { Error = "No cached metrics found. Run analyze_website first." });
            }

            var response = new SuggestionsResponse
            {
                Url = url,
                Recommendations = RecommendationEngine.Generate(metrics, _options),
                MetricsAnalyzed = new MetricsSummary
                {
                    Score = metrics.Score,
                    Status = _analyzer.GetPerformanceStatus(metrics.Score),
                },
            };

            return Serialize(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in SuggestImprovementsAsync for {Url}", url);
            return Serialize(new ErrorResponse { Error = ex.Message });
        }
    }

    private static string Serialize<T>(T value) => JsonSerializer.Serialize(value, JsonDefaults.Options);
}
