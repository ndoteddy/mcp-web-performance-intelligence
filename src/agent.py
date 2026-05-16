"""Agentic orchestration using Gemini and Model Context Protocol."""

import logging
import time
from typing import Optional, List, Any

import google.generativeai as genai
from fastmcp import FastMCP

from src.config import Config
from src.analyzer import PerformanceAnalyzer
from src.database import PerformanceDatabase
from src.tools import PerformanceTools

logger = logging.getLogger(__name__)


class PerformanceAgent:
    """Agentic orchestrator for website performance analysis using Gemini."""

    def __init__(self, config: Config):
        """Initialize the performance agent.
        
        Args:
            config: Configuration object containing API, database, and model settings
        """
        config.validate()
        self.config = config
        
        genai.configure(api_key=config.api.google_api_key)
        
        self.mcp = FastMCP("WebPerformanceAnalyzer")
        self.analyzer = PerformanceAnalyzer(config.api)
        self.database = PerformanceDatabase(config.database)
        self.tools = PerformanceTools(self.mcp, self.analyzer, self.database)
        
        self.model = genai.GenerativeModel(
            model_name=config.model.model_name,
            tools=[self.tools.mcp.tool],
            system_instruction=config.model.system_instruction,
        )
        
        logger.info(f"Performance agent initialized with model: {config.model.model_name}")

    def run_audit(self, url: str, retries: int = 3) -> Optional[str]:
        """Execute comprehensive performance audit for a website.
        
        Args:
            url: Target website URL
            retries: Number of retry attempts on failure
            
        Returns:
            Audit report as string, or None if all retries fail
        """
        logger.info(f"Starting performance audit for: {url}")
        
        chat = self.model.start_chat(enable_automatic_function_calling=True)
        
        prompt = (
            f"Please conduct a comprehensive performance audit and improvement plan for: {url}\n"
            "Include:\n"
            "1. Raw performance metrics in JSON format\n"
            "2. Performance classification and analysis\n"
            "3. Prioritized improvement recommendations\n"
            "4. Technical implementation guidance"
        )
        
        for attempt in range(retries):
            try:
                logger.info(f"Audit attempt {attempt + 1}/{retries}")
                
                response = chat.send_message(prompt)
                
                if response and hasattr(response, "text"):
                    logger.info(f"Audit completed successfully for: {url}")
                    return response.text
                
            except Exception as e:
                logger.warning(f"Attempt {attempt + 1} failed: {str(e)}")
                
                if "429" in str(e) and attempt < retries - 1:
                    wait_time = (attempt + 1) * self.config.api.retry_delay_seconds
                    logger.info(f"Rate limited. Waiting {wait_time}s before retry...")
                    time.sleep(wait_time)
                    continue
                
                if attempt == retries - 1:
                    logger.error(f"Audit failed after {retries} attempts: {str(e)}")
                    return None
        
        return None

    def run_batch_audit(self, urls: List[str]) -> dict:
        """Execute audits for multiple websites.
        
        Args:
            urls: List of website URLs to analyze
            
        Returns:
            Dictionary mapping URLs to audit results
        """
        logger.info(f"Starting batch audit for {len(urls)} websites")
        
        results = {}
        for url in urls:
            try:
                report = self.run_audit(url)
                results[url] = {
                    "status": "success" if report else "failed",
                    "report": report,
                }
            except Exception as e:
                logger.error(f"Batch audit failed for {url}: {str(e)}")
                results[url] = {
                    "status": "error",
                    "error": str(e),
                }
        
        logger.info(f"Batch audit complete. Success: {sum(1 for r in results.values() if r['status'] == 'success')}/{len(urls)}")
        return results

    def get_audit_history(self, url: str, limit: int = 10) -> List[dict]:
        """Retrieve audit history for a URL.
        
        Args:
            url: Website URL
            limit: Maximum records to return
            
        Returns:
            List of audit history records
        """
        return self.database.get_audit_history(url, limit)

    def cleanup_old_records(self, days: int = 30) -> int:
        """Remove old audit records.
        
        Args:
            days: Retention period in days
            
        Returns:
            Number of deleted records
        """
        return self.database.cleanup_old_records(days)
