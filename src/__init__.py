"""MCP Web Performance Intelligence - Enterprise-grade observability platform."""

__version__ = "1.0.0"
__author__ = "Hernando Ivan Teddy"
__description__ = "Enterprise-grade Agentic AI observability platform built with FastMCP, SQLite, and PageSpeed Insights"

from src.agent import PerformanceAgent
from src.analyzer import PerformanceAnalyzer
from src.database import PerformanceDatabase

__all__ = ["PerformanceAgent", "PerformanceAnalyzer", "PerformanceDatabase"]
