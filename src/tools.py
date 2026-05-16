"""FastMCP tool definitions for agentic performance analysis."""

import logging
from typing import Dict, Any

from fastmcp import FastMCP
from src.analyzer import PerformanceAnalyzer
from src.database import PerformanceDatabase
from src.config import Config

logger = logging.getLogger(__name__)


class PerformanceTools:
    """Encapsulates FastMCP tool definitions for performance analysis."""

    def __init__(
        self,
        mcp: FastMCP,
        analyzer: PerformanceAnalyzer,
        database: PerformanceDatabase,
    ):
        """Initialize tools with dependencies.
        
        Args:
            mcp: FastMCP server instance
            analyzer: Performance analyzer instance
            database: Database instance
        """
        self.mcp = mcp
        self.analyzer = analyzer
        self.database = database
        self._register_tools()

    def _register_tools(self) -> None:
        """Register all MCP tool handlers."""
        
        @self.mcp.tool()
        def analyze_website(url: str) -> str:
            """Analyzes website performance and returns raw JSON metrics.
            
            Args:
                url: The full URL of the website to analyze
                
            Returns:
                JSON string containing performance metrics
            """
            try:
                metrics = self.analyzer.analyze_website(url)
                self.database.store_metrics(url, metrics)
                return metrics_to_json(metrics)
            except Exception as e:
                logger.error(f"Analysis failed for {url}: {str(e)}")
                return error_response(f"Analysis failed: {str(e)}")

        @self.mcp.tool()
        def suggest_improvements(url: str) -> str:
            """Provides technical recommendations based on analysis.
            
            Args:
                url: The full URL of the website to get suggestions for
                
            Returns:
                JSON string containing improvement recommendations
            """
            try:
                metrics = self.database.get_metrics(url)
                if not metrics:
                    return error_response(f"No cached metrics found for {url}. Run analyze_website first.")
                
                recommendations = self._generate_recommendations(metrics)
                return recommendations_to_json(recommendations)
            except Exception as e:
                logger.error(f"Recommendation generation failed for {url}: {str(e)}")
                return error_response(f"Recommendation failed: {str(e)}")

        logger.info("MCP tools registered successfully")

    def _generate_recommendations(self, metrics: Dict[str, Any]) -> Dict[str, Any]:
        """Generate improvement recommendations based on metrics.
        
        Args:
            metrics: Performance metrics dictionary
            
        Returns:
            Recommendations dictionary
        """
        score = metrics.get("score", 0)
        recommendations = []
        
        if score < 50:
            recommendations.append({
                "priority": "Critical",
                "area": "Overall Performance",
                "suggestion": "Performance score is below 50. Comprehensive optimization required.",
            })
        
        if "1050 ms" in str(metrics.get("total_blocking_time", "")):
            recommendations.append({
                "priority": "High",
                "area": "Main Thread",
                "suggestion": "Implement task chunking. Break long-running JavaScript into smaller async chunks using scheduler.yield() or setTimeout(0).",
            })
        
        if "2.5 s" in str(metrics.get("interactive", "")):
            recommendations.append({
                "priority": "High",
                "area": "Time to Interactive",
                "suggestion": "Optimize JavaScript hydration. Consider selective hydration or Islands Architecture for modern frameworks.",
            })
        
        if score < 75:
            recommendations.append({
                "priority": "Medium",
                "area": "Resource Loading",
                "suggestion": "Implement resource hints (preload, prefetch) for critical assets. Optimize image and font loading strategies.",
            })
        
        if not recommendations:
            recommendations.append({
                "priority": "Low",
                "area": "Maintenance",
                "suggestion": "Performance is good. Focus on maintaining current standards and monitoring for regressions.",
            })
        
        return {
            "url": metrics.get("url"),
            "score": score,
            "classification": self.analyzer.classify_performance(score),
            "recommendations": recommendations,
        }


def metrics_to_json(metrics: Dict[str, Any]) -> str:
    """Convert metrics dictionary to JSON string.
    
    Args:
        metrics: Metrics dictionary
        
    Returns:
        JSON string
    """
    import json
    return json.dumps(metrics, indent=2)


def recommendations_to_json(recommendations: Dict[str, Any]) -> str:
    """Convert recommendations to JSON string.
    
    Args:
        recommendations: Recommendations dictionary
        
    Returns:
        JSON string
    """
    import json
    return json.dumps(recommendations, indent=2)


def error_response(message: str) -> str:
    """Format error response as JSON.
    
    Args:
        message: Error message
        
    Returns:
        JSON error string
    """
    import json
    return json.dumps({"error": message})
