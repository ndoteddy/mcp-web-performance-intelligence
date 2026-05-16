"""Agentic workflow orchestration using Gemini and MCP tools."""

import logging
import time
from typing import List, Optional, Any

import google.generativeai as genai

from src.config import Config
from src.tools import get_mcp_tools, analyze_website, suggest_improvements

logger = logging.getLogger(__name__)


class PerformanceAgent:
    """Orchestrates autonomous performance analysis workflow.

    This class implements an agentic pattern where Gemini:
    1. Receives performance analysis requests
    2. Autonomously selects and sequences tool calls
    3. Synthesizes results into actionable recommendations
    """

    SYSTEM_INSTRUCTION = (
        "You are a Senior Performance Engineer and Observability Architect. "
        "Your role is to conduct comprehensive website performance audits using available tools. "
        "Always follow this sequence: "
        "1. Use 'analyze_website' to fetch raw metrics from PageSpeed Insights. "
        "2. Use 'suggest_improvements' to generate targeted recommendations. "
        "3. Synthesize findings into a professional technical report. "
        "Focus on actionable insights and business impact. "
        "Provide metrics in JSON code blocks for clarity."
    )

    def __init__(self, model_name: Optional[str] = None):
        """Initialize the performance agent.

        Args:
            model_name: Gemini model to use (defaults to Config.MODEL_NAME)
        """
        Config.validate()
        self.model_name = model_name or Config.MODEL_NAME
        self.model = genai.GenerativeModel(
            model_name=self.model_name,
            tools=[analyze_website, suggest_improvements],
            system_instruction=self.SYSTEM_INSTRUCTION
        )
        self.chat = self.model.start_chat(enable_automatic_function_calling=True)
        logger.info(f"PerformanceAgent initialized with model: {self.model_name}")

    def audit_website(self, url: str, retries: int = Config.MAX_RETRIES) -> Optional[str]:
        """Execute complete performance audit workflow.

        Args:
            url: Website URL to audit
            retries: Number of retry attempts on quota exhaustion

        Returns:
            Technical report as string or None on failure
        """
        prompt = (
            f"Please conduct a comprehensive performance audit for {url}. "
            "Analyze current metrics, identify bottlenecks, and provide prioritized recommendations. "
            "Format the final report professionally."
        )

        for attempt in range(retries):
            try:
                logger.info(f"Executing audit for {url} (attempt {attempt + 1}/{retries})")
                response = self.chat.send_message(prompt)

                if response and hasattr(response, 'text'):
                    logger.info(f"Audit completed successfully for {url}")
                    return response.text
                else:
                    logger.warning(f"Invalid response format for {url}")
                    return None

            except Exception as e:
                error_msg = str(e)
                if "429" in error_msg or "quota" in error_msg.lower():
                    if attempt < retries - 1:
                        wait_time = (attempt + 1) * Config.RETRY_BACKOFF_FACTOR
                        logger.warning(
                            f"Quota exhausted. Retrying in {wait_time}s... "
                            f"(attempt {attempt + 1}/{retries})"
                        )
                        time.sleep(wait_time)
                        continue
                    else:
                        logger.error(f"Quota limit reached after {retries} attempts")
                        return None
                else:
                    logger.error(f"Error during audit: {error_msg}")
                    return None

        return None

    def batch_audit(self, urls: List[str]) -> dict:
        """Execute audits for multiple websites.

        Args:
            urls: List of website URLs

        Returns:
            Dictionary mapping URL to audit result
        """
        results = {}
        logger.info(f"Starting batch audit for {len(urls)} URLs")

        for url in urls:
            try:
                report = self.audit_website(url)
                results[url] = {
                    "status": "success",
                    "report": report
                }
            except Exception as e:
                logger.error(f"Batch audit error for {url}: {e}")
                results[url] = {
                    "status": "error",
                    "error": str(e)
                }

        logger.info(f"Batch audit completed. Successful: {sum(1 for r in results.values() if r['status'] == 'success')}/{len(urls)}")
        return results

    def get_chat_history(self) -> list:
        """Retrieve chat conversation history.

        Returns:
            List of chat messages
        """
        return self.chat.history if hasattr(self.chat, 'history') else []

    def reset_conversation(self) -> None:
        """Reset chat history for new audit session."""
        self.chat = self.model.start_chat(enable_automatic_function_calling=True)
        logger.info("Chat history reset")
