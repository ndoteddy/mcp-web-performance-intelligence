"""Configuration management for MCP Web Performance Intelligence."""

import os
from dataclasses import dataclass
from typing import Optional


@dataclass
class APIConfig:
    """API configuration settings."""
    google_api_key: str
    pagespeed_api_key: str
    pagespeed_endpoint: str = "https://www.googleapis.com/pagespeedonline/v5/runPagespeed"
    request_timeout: float = 60.0
    max_retries: int = 3
    retry_delay_seconds: int = 40


@dataclass
class DatabaseConfig:
    """Database configuration settings."""
    db_file: str = "mcp_performance.db"
    connection_timeout: float = 30.0
    enable_wal: bool = True


@dataclass
class ModelConfig:
    """Generative AI model configuration."""
    model_name: str = "gemini-2.5-flash-lite"
    system_instruction: str = (
        "You are a Senior Performance Engineer. Use your tools to perform a comprehensive audit: "
        "1. Analyze website speed using 'analyze_website'. "
        "2. Immediately follow up with 'suggest_improvements' for that same URL. "
        "3. Provide a high-detail technical report including raw JSON metrics in a code block."
    )
    temperature: float = 0.7
    max_output_tokens: int = 2048


@dataclass
class Config:
    """Master configuration container."""
    api: APIConfig
    database: DatabaseConfig
    model: ModelConfig

    @classmethod
    def from_env(cls) -> "Config":
        """Load configuration from environment variables."""
        return cls(
            api=APIConfig(
                google_api_key=os.getenv("GOOGLE_API_KEY", ""),
                pagespeed_api_key=os.getenv("PAGESPEED_API_KEY", ""),
            ),
            database=DatabaseConfig(
                db_file=os.getenv("DB_FILE", "mcp_performance.db"),
            ),
            model=ModelConfig(
                model_name=os.getenv("MODEL_NAME", "gemini-2.5-flash-lite"),
            ),
        )

    def validate(self) -> bool:
        """Validate configuration completeness."""
        if not self.api.google_api_key:
            raise ValueError("GOOGLE_API_KEY is required")
        if not self.api.pagespeed_api_key:
            raise ValueError("PAGESPEED_API_KEY is required")
        return True
