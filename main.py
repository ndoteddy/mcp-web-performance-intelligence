"""Main entry point for MCP Web Performance Intelligence platform.

Made by Hernando Ivan Teddy - with innovation
"""

import sys
import logging
from typing import List

from src.logger import setup_logging
from src.agent import PerformanceAgent
from src.config import get_config
from src.utils import validate_url

# Setup logging
logger = setup_logging(__name__)


def run_single_audit(url: str) -> None:
    """Run performance audit for a single URL.

    Args:
        url: Website URL to analyze
    """
    if not validate_url(url):
        logger.error(f"Invalid URL: {url}")
        sys.exit(1)

    try:
        logger.info(f"Starting performance audit for {url}")
        agent = PerformanceAgent()
        report = agent.audit_website(url)

        if report:
            print("\n" + "=" * 70)
            print("PERFORMANCE AUDIT REPORT")
            print("=" * 70 + "\n")
            print(report)
            print("\n" + "=" * 70)
        else:
            logger.error("Failed to generate report")
            sys.exit(1)

    except Exception as e:
        logger.error(f"Audit failed: {e}")
        sys.exit(1)


def run_batch_audit(urls: List[str]) -> None:
    """Run performance audits for multiple URLs.

    Args:
        urls: List of website URLs to analyze
    """
    invalid_urls = [url for url in urls if not validate_url(url)]
    if invalid_urls:
        logger.error(f"Invalid URLs: {invalid_urls}")
        sys.exit(1)

    try:
        logger.info(f"Starting batch audit for {len(urls)} URLs")
        agent = PerformanceAgent()
        results = agent.batch_audit(urls)

        print("\n" + "=" * 70)
        print("BATCH AUDIT RESULTS")
        print("=" * 70 + "\n")

        for url, result in results.items():
            status = "SUCCESS" if result["status"] == "success" else "FAILED"
            print(f"\n[{status}] {url}")
            if result["status"] == "success" and result.get("report"):
                print(result["report"][:500] + "...\n")  # Print first 500 chars
            elif result["status"] == "error":
                print(f"Error: {result.get('error')}\n")

    except Exception as e:
        logger.error(f"Batch audit failed: {e}")
        sys.exit(1)


def main():
    """Main CLI entry point."""
    if len(sys.argv) < 2:
        print("""
MCP Web Performance Intelligence
Made by Hernando Ivan Teddy - with innovation

Usage:
  python main.py <url>              # Analyze single website
  python main.py <url1> <url2> ...  # Batch analysis

Example:
  python main.py https://example.com
  python main.py https://example.com https://google.com

Environment Variables Required:
  GOOGLE_API_KEY         - Google Gemini API key
  PAGESPEED_API_KEY      - Google PageSpeed Insights API key

Optional Environment Variables:
  MODEL_NAME             - Gemini model (default: gemini-2.5-flash-lite)
  DB_FILE                - Database path (default: mcp_performance.db)
  LOG_LEVEL              - Logging level (default: INFO)
  LOG_FILE               - Log file path (optional)
        """)
        sys.exit(1)

    urls = sys.argv[1:]

    # Load configuration
    try:
        config = get_config()
        config.validate()
    except ValueError as e:
        logger.error(f"Configuration error: {e}")
        sys.exit(1)

    # Execute audit
    if len(urls) == 1:
        run_single_audit(urls[0])
    else:
        run_batch_audit(urls)


if __name__ == "__main__":
    main()
