"""Configuration management for MCP Web Performance Intelligence."""

import os
from typing import Optional


class Config:
    """Central configuration object."""

    # API Keys
    GOOGLE_API_KEY: str = os.getenv("GOOGLE_API_KEY", "")
    PAGESPEED_API_KEY: str = os.getenv("PAGESPEED_API_KEY", "")

    # Model Configuration
    MODEL_NAME: str = os.getenv("MODEL_NAME", "gemini-2.5-flash-lite")
    TIMEOUT_SECONDS: int = int(os.getenv("TIMEOUT_SECONDS", "60"))

    # Database Configuration
    DB_FILE: str = os.getenv("DB_FILE", "mcp_performance.db")
    DB_TIMEOUT: int = int(os.getenv("DB_TIMEOUT", "30"))

    # Logging Configuration
    LOG_LEVEL: str = os.getenv("LOG_LEVEL", "INFO")
    LOG_FILE: Optional[str] = os.getenv("LOG_FILE")

    # Performance Thresholds
    PERFORMANCE_SCORE_THRESHOLD: float = 70.0
    FCP_THRESHOLD_MS: float = 1800.0  # milliseconds
    TTI_THRESHOLD_MS: float = 3800.0  # milliseconds
    TBT_THRESHOLD_MS: float = 300.0   # milliseconds

    # API Configuration
    PAGESPEED_API_URL: str = "https://www.googleapis.com/pagespeedonline/v5/runPagespeed"
    PAGESPEED_API_TIMEOUT: float = 60.0
    MAX_RETRIES: int = 3
    RETRY_BACKOFF_FACTOR: int = 40  # seconds

    @classmethod
    def validate(cls) -> None:
        """Validate critical configuration parameters."""
        if not cls.GOOGLE_API_KEY:
            raise ValueError("GOOGLE_API_KEY environment variable is required")
        if not cls.PAGESPEED_API_KEY:
            raise ValueError("PAGESPEED_API_KEY environment variable is required")


class DevelopmentConfig(Config):
    """Development environment configuration."""

    DEBUG: bool = True
    LOG_LEVEL: str = "DEBUG"
    DB_FILE: str = "dev_mcp_performance.db"


class ProductionConfig(Config):
    """Production environment configuration."""

    DEBUG: bool = False
    LOG_LEVEL: str = "WARNING"
    DB_FILE: str = "/var/data/mcp_performance.db"


def get_config(environment: str = "development") -> Config:
    """Factory function to retrieve appropriate configuration.

    Args:
        environment: Either "development" or "production"

    Returns:
        Config object with appropriate settings
    """
    if environment.lower() == "production":
        return ProductionConfig()
    return DevelopmentConfig()
