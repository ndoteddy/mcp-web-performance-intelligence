"""Utility functions and helpers."""

import json
from typing import Dict, Any


def format_report(data: Dict[str, Any], title: str = "Performance Report") -> str:
    """Format data as a professional report.

    Args:
        data: Dictionary containing report data
        title: Report title

    Returns:
        Formatted report string
    """
    separator = "=" * 70
    report = f"{separator}\n{title}\n{separator}\n\n"

    for key, value in data.items():
        if isinstance(value, dict):
            report += f"\n{key.upper()}:\n"
            for k, v in value.items():
                report += f"  {k}: {v}\n"
        else:
            report += f"{key}: {value}\n"

    return report


def parse_metric_value(value_str: str, unit: str = "ms") -> float:
    """Parse metric string to float value.

    Args:
        value_str: Metric string (e.g., "1.5 s", "500 ms")
        unit: Expected unit

    Returns:
        Numeric value or 0 if parsing fails
    """
    try:
        # Remove unit and whitespace, handle commas
        cleaned = value_str.replace(unit, "").strip().replace(",", "")
        return float(cleaned)
    except (ValueError, AttributeError):
        return 0.0


def validate_url(url: str) -> bool:
    """Validate URL format.

    Args:
        url: URL string to validate

    Returns:
        True if URL is valid, False otherwise
    """
    return url.startswith(("http://", "https://")) and "." in url


def metrics_to_json(metrics: Dict[str, Any]) -> str:
    """Convert metrics dictionary to formatted JSON.

    Args:
        metrics: Metrics dictionary

    Returns:
        Formatted JSON string
    """
    return json.dumps(metrics, indent=2)
