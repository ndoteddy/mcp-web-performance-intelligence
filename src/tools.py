"""FastMCP tool definitions for agentic workflow."""

import logging
from typing import Dict, Any, Optional
from fastmcp import FastMCP

from src.analyzer import PerformanceAnalyzer
from src.database import PerformanceDatabase
from src.config import Config

logger = logging.getLogger(__name__)

# Initialize tools
mcp = FastMCP("WebPerformanceAnalyzer")
analyzer = PerformanceAnalyzer()
db = PerformanceDatabase()


@mcp.tool()
def analyze_website(url: str) -> str:
    """Analyze website performance using Google PageSpeed Insights.

    Fetches comprehensive performance metrics including:
    - Performance Score (0-100)
    - First Contentful Paint (FCP)
    - Speed Index
    - Largest Contentful Paint (LCP)
    - Time to Interactive (TTI)
    - Total Blocking Time (TBT)
    - Cumulative Layout Shift (CLS)

    Args:
        url: The website URL to analyze

    Returns:
        JSON string containing extracted performance metrics
    """
    try:
        logger.info(f"[MCP Tool] Analyzing: {url}")
        metrics = analyzer.analyze_website(url)

        if not metrics:
            return '{"error": "Failed to fetch metrics from PageSpeed API"}'

        if not analyzer.validate_metrics(metrics):
            logger.warning(f"Metrics validation failed for {url}")
            return '{"error": "Metrics validation failed"}'

        # Persist metrics to database
        db.insert_metrics(url, metrics)

        import json
        return json.dumps(metrics)

    except Exception as e:
        logger.error(f"Error in analyze_website: {e}")
        return f'{{"error": "{str(e)}"}}'


@mcp.tool()
def suggest_improvements(url: str) -> str:
    """Generate improvement recommendations based on cached metrics.

    Provides 3 actionable technical recommendations tailored to the website's
    specific performance bottlenecks identified in the analysis.

    Args:
        url: The website URL to generate suggestions for

    Returns:
        JSON string containing prioritized improvement recommendations
    """
    try:
        logger.info(f"[MCP Tool] Generating suggestions for: {url}")

        # Retrieve cached metrics
        metrics = db.get_metrics(url)
        if not metrics:
            return '{"error": "No cached metrics found. Run analyze_website first."}'

        recommendations = _generate_recommendations(metrics)

        import json
        return json.dumps({
            "url": url,
            "recommendations": recommendations,
            "metrics_analyzed": {
                "score": metrics.get("score"),
                "status": analyzer.get_performance_status(metrics.get("score", 0))
            }
        })

    except Exception as e:
        logger.error(f"Error in suggest_improvements: {e}")
        return f'{{"error": "{str(e)}"}}'


def _generate_recommendations(metrics: Dict[str, Any]) -> list:
    """Generate prioritized recommendations based on metrics.

    Args:
        metrics: Performance metrics dictionary

    Returns:
        List of recommendation dictionaries
    """
    recommendations = []
    score = metrics.get("score", 0)

    # Priority 1: Low performance score
    if score < Config.PERFORMANCE_SCORE_THRESHOLD:
        recommendations.append({
            "priority": 1,
            "category": "Overall Performance",
            "issue": f"Performance score is {score} (threshold: {Config.PERFORMANCE_SCORE_THRESHOLD})",
            "recommendation": "Comprehensive performance audit required. Focus on JavaScript optimization and resource prioritization.",
            "impact": "High"
        })

    # Priority 2: High Total Blocking Time
    tbt_str = metrics.get("total_blocking_time", "N/A")
    if tbt_str != "N/A" and "ms" in tbt_str:
        try:
            tbt_value = float(tbt_str.split()[0].replace(",", ""))
            if tbt_value > Config.TBT_THRESHOLD_MS:
                recommendations.append({
                    "priority": 2,
                    "category": "Main Thread Optimization",
                    "issue": f"Total Blocking Time: {tbt_str}",
                    "recommendation": "Implement task chunking using scheduler.yield(). Consider code-splitting and offloading third-party scripts to Web Workers.",
                    "impact": "High"
                })
        except (ValueError, IndexError):
            logger.warning(f"Could not parse TBT value: {tbt_str}")

    # Priority 3: Slow Time to Interactive
    tti_str = metrics.get("time_to_interactive", "N/A")
    if tti_str != "N/A" and "s" in tti_str:
        try:
            tti_value = float(tti_str.split()[0]) * 1000  # Convert to ms
            if tti_value > Config.TTI_THRESHOLD_MS:
                recommendations.append({
                    "priority": 3,
                    "category": "Critical Path Optimization",
                    "issue": f"Time to Interactive: {tti_str}",
                    "recommendation": "Minimize critical resources. Use lazy loading for non-essential JavaScript. Consider selective hydration or Islands Architecture.",
                    "impact": "Medium"
                })
        except (ValueError, IndexError):
            logger.warning(f"Could not parse TTI value: {tti_str}")

    # If no critical issues, provide general optimizations
    if not recommendations:
        recommendations.append({
            "priority": 1,
            "category": "Optimization Opportunities",
            "issue": "Site meets performance thresholds",
            "recommendation": "Continue monitoring performance. Implement Image optimization and Cache-Control headers. Regular performance audits recommended.",
            "impact": "Low"
        })

    return recommendations


def get_mcp_tools() -> FastMCP:
    """Export configured MCP instance with tools.

    Returns:
        FastMCP instance with registered tools
    """
    return mcp
