"""PageSpeed API integration and metrics extraction."""

import httpx
import json
import logging
from typing import Dict, Any, Optional

from src.config import Config

logger = logging.getLogger(__name__)


class PerformanceAnalyzer:
    """Interfaces with Google PageSpeed Insights API."""

    def __init__(self, api_key: Optional[str] = None):
        """Initialize analyzer with API credentials.

        Args:
            api_key: PageSpeed Insights API key (uses Config if not provided)
        """
        self.api_key = api_key or Config.PAGESPEED_API_KEY
        self.base_url = Config.PAGESPEED_API_URL
        self.timeout = Config.PAGESPEED_API_TIMEOUT

    def analyze_website(self, url: str) -> Optional[Dict[str, Any]]:
        """Fetch and parse PageSpeed metrics for a website.

        Args:
            url: Website URL to analyze

        Returns:
            Dictionary containing performance metrics or None on error
        """
        try:
            logger.info(f"Analyzing website: {url}")
            response = httpx.get(
                self.base_url,
                params={"url": url, "key": self.api_key},
                timeout=self.timeout
            )
            response.raise_for_status()
            data = response.json()

            metrics = self._extract_metrics(data)
            logger.info(f"Successfully extracted metrics for {url}")
            return metrics

        except httpx.HTTPError as e:
            logger.error(f"HTTP error analyzing {url}: {e}")
            return None
        except Exception as e:
            logger.error(f"Error analyzing {url}: {e}")
            return None

    def _extract_metrics(self, data: Dict[str, Any]) -> Dict[str, Any]:
        """Extract relevant metrics from PageSpeed API response.

        Args:
            data: Raw API response

        Returns:
            Dictionary of extracted metrics
        """
        lighthouse = data.get("lighthouseResult", {})
        categories = lighthouse.get("categories", {})
        audits = lighthouse.get("audits", {})

        performance_category = categories.get("performance", {})
        score = performance_category.get("score", 0) * 100

        metrics = {
            "score": round(score, 1),
            "first_contentful_paint": audits.get(
                "first-contentful-paint", {}
            ).get("displayValue", "N/A"),
            "speed_index": audits.get(
                "speed-index", {}
            ).get("displayValue", "N/A"),
            "largest_contentful_paint": audits.get(
                "largest-contentful-paint", {}
            ).get("displayValue", "N/A"),
            "time_to_interactive": audits.get(
                "interactive", {}
            ).get("displayValue", "N/A"),
            "total_blocking_time": audits.get(
                "total-blocking-time", {}
            ).get("displayValue", "N/A"),
            "cumulative_layout_shift": audits.get(
                "cumulative-layout-shift", {}
            ).get("displayValue", "N/A"),
        }
        return metrics

    def get_performance_status(self, score: float) -> str:
        """Determine performance status based on score.

        Args:
            score: Performance score (0-100)

        Returns:
            Status string: "Excellent", "Good", "Needs Improvement", or "Poor"
        """
        if score >= 90:
            return "Excellent"
        elif score >= 50:
            return "Good"
        elif score >= 25:
            return "Needs Improvement"
        else:
            return "Poor"

    def validate_metrics(self, metrics: Dict[str, Any]) -> bool:
        """Validate extracted metrics.

        Args:
            metrics: Metrics dictionary to validate

        Returns:
            True if metrics are valid, False otherwise
        """
        required_fields = [
            "score",
            "first_contentful_paint",
            "speed_index",
            "time_to_interactive",
        ]
        for field in required_fields:
            if field not in metrics or metrics[field] == "N/A":
                logger.warning(f"Missing required metric: {field}")
                return False
        return True
