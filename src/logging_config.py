"""Centralized logging configuration."""

import logging
import logging.handlers
from pathlib import Path


def configure_logging(log_level: str = "INFO", log_file: str = "mcp_performance.log") -> None:
    """Configure application-wide logging.
    
    Args:
        log_level: Logging level (DEBUG, INFO, WARNING, ERROR, CRITICAL)
        log_file: Path to log file
    """
    log_dir = Path("logs")
    log_dir.mkdir(exist_ok=True)
    
    log_format = (
        "[%(asctime)s] %(levelname)-8s [%(name)s:%(lineno)d] %(message)s"
    )
    
    root_logger = logging.getLogger()
    root_logger.setLevel(getattr(logging, log_level.upper()))
    
    console_handler = logging.StreamHandler()
    console_handler.setFormatter(logging.Formatter(log_format))
    root_logger.addHandler(console_handler)
    
    file_handler = logging.handlers.RotatingFileHandler(
        log_dir / log_file,
        maxBytes=10 * 1024 * 1024,
        backupCount=5,
    )
    file_handler.setFormatter(logging.Formatter(log_format))
    root_logger.addHandler(file_handler)
