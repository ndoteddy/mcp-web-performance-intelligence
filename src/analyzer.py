"""Website performance analysis engine using Google PageSpeed Insights API."""

import json
import logging
from typing import Dict, Any, Optional
import httpx

from src.config import APIConfig

logger = logging.getLogger(__name__)


class PerformanceAnalyzer:
    """Analyzes website performance using Google PageSpeed Insights API."""

    def __init__(self, config: APIConfig):
        """Initialize analyzer with API configuration.
        
        Args:
            config: API configuration object
        """
        self.config = config
        self.client = httpx.Client(timeout=config.request_timeout)

    def analyze_website(self, url: str) -> Dict[str, Any]:
        """Fetch and parse PageSpeed Insights data for a website.
        
        Args:
            url: Website URL to analyze
            
        Returns:
            Dictionary containing performance metrics
            
        Raises:
            httpx.HTTPError: If API request fails
        """
        logger.info(f"Analyzing website: {url}")
        
        api_url = (
            f"{self.config.pagespeed_endpoint}"
            f"?url={url}&key={self.config.pagespeed_api_key}"
        )
        
        response = self.client.get(api_url)
        response.raise_for_status()
        
        data = response.json()
        metrics = self._extract_metrics(data)
        
        logger.info(f"Analysis complete for {url}: Score={metrics.get('score')}")
        return metrics

    def _extract_metrics(self, api_response: Dict[str, Any]) -> Dict[str, Any]:
        """Extract key performance metrics from API response.
        
        Args:
            api_response: Raw API response dictionary
            
        Returns:
            Structured metrics dictionary
        """
        result = api_response.get("lighthouseResult", {})
        categories = result.get("categories", {})
        audits = result.get("audits", {})
        
        metrics = {
            "score": categories.get("performance", {}).get("score", 0) * 100,
            "first_contentful_paint": audits.get("first-contentful-paint", {}).get(
                "displayValue", "N/A"
            ),
            "speed_index": audits.get("speed-index", {}).get("displayValue", "N/A"),
            "largest_contentful_paint": audits.get("largest-contentful-paint", {}).get(
                "displayValue", "N/A"
            ),
            "interactive": audits.get("interactive", {}).get("displayValue", "N/A"),
            "total_blocking_time": audits.get("total-blocking-time", {}).get(
                "displayValue", "N/A"
            ),
            "cumulative_layout_shift": audits.get("cumulative-layout-shift", {}).get(
                "displayValue", "N/A"
            ),
        }
        
        return metrics

    def classify_performance(self, score: float) -> str:
        """Classify performance score into categories.
        
        Args:
            score: Performance score (0-100)
            
        Returns:
            Classification string
        """
        if score >= 90:
            return "Excellent"
        elif score >= 75:
            return "Good"
        elif score >= 50:
            return "Needs Improvement"
        else:
            return "Poor"

    def __del__(self):
        """Cleanup HTTP client on object destruction."""
        if hasattr(self, "client"):
            self.client.close()
