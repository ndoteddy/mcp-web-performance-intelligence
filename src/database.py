"""Database layer for performance metrics persistence."""

import sqlite3
import json
import logging
from contextlib import contextmanager
from datetime import datetime
from typing import Dict, Optional, Any

from src.config import Config

logger = logging.getLogger(__name__)


class PerformanceDatabase:
    """Manages SQLite persistence for performance metrics."""

    def __init__(self, db_file: str = None):
        """Initialize database connection.

        Args:
            db_file: Path to SQLite database file
        """
        self.db_file = db_file or Config.DB_FILE
        self._init_db()

    def _init_db(self) -> None:
        """Create performance_cache table if it doesn't exist."""
        with self._get_connection() as conn:
            cursor = conn.cursor()
            cursor.execute("""
                CREATE TABLE IF NOT EXISTS performance_cache (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    url TEXT NOT NULL UNIQUE,
                    metrics TEXT NOT NULL,
                    analysis_summary TEXT,
                    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
                    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP
                )
            """)
            cursor.execute("""
                CREATE INDEX IF NOT EXISTS idx_url ON performance_cache(url)
            """)
            conn.commit()
            logger.info(f"Database initialized: {self.db_file}")

    @contextmanager
    def _get_connection(self):
        """Context manager for database connections.

        Yields:
            sqlite3.Connection object
        """
        conn = sqlite3.connect(self.db_file, timeout=Config.DB_TIMEOUT)
        conn.row_factory = sqlite3.Row
        try:
            yield conn
        finally:
            conn.close()

    def insert_metrics(self, url: str, metrics: Dict[str, Any], analysis: Optional[str] = None) -> bool:
        """Insert or update performance metrics.

        Args:
            url: Website URL
            metrics: Dictionary of performance metrics
            analysis: Optional analysis summary

        Returns:
            True if successful, False otherwise
        """
        try:
            with self._get_connection() as conn:
                cursor = conn.cursor()
                metrics_json = json.dumps(metrics)
                cursor.execute("""
                    INSERT OR REPLACE INTO performance_cache
                    (url, metrics, analysis_summary, updated_at)
                    VALUES (?, ?, ?, CURRENT_TIMESTAMP)
                """, (url, metrics_json, analysis))
                conn.commit()
                logger.info(f"Metrics inserted for {url}")
                return True
        except sqlite3.Error as e:
            logger.error(f"Database error inserting metrics: {e}")
            return False

    def get_metrics(self, url: str) -> Optional[Dict[str, Any]]:
        """Retrieve performance metrics for a URL.

        Args:
            url: Website URL

        Returns:
            Dictionary of metrics or None if not found
        """
        try:
            with self._get_connection() as conn:
                cursor = conn.cursor()
                cursor.execute(
                    "SELECT metrics FROM performance_cache WHERE url = ?",
                    (url,)
                )
                row = cursor.fetchone()
                if row:
                    return json.loads(row[0])
                return None
        except sqlite3.Error as e:
            logger.error(f"Database error retrieving metrics: {e}")
            return None

    def get_all_metrics(self) -> list:
        """Retrieve all cached metrics.

        Returns:
            List of tuples (url, metrics_dict, updated_at)
        """
        try:
            with self._get_connection() as conn:
                cursor = conn.cursor()
                cursor.execute(
                    "SELECT url, metrics, updated_at FROM performance_cache ORDER BY updated_at DESC"
                )
                rows = cursor.fetchall()
                return [
                    (row[0], json.loads(row[1]), row[2])
                    for row in rows
                ]
        except sqlite3.Error as e:
            logger.error(f"Database error retrieving all metrics: {e}")
            return []

    def delete_metrics(self, url: str) -> bool:
        """Delete metrics for a URL.

        Args:
            url: Website URL

        Returns:
            True if successful, False otherwise
        """
        try:
            with self._get_connection() as conn:
                cursor = conn.cursor()
                cursor.execute("DELETE FROM performance_cache WHERE url = ?", (url,))
                conn.commit()
                logger.info(f"Metrics deleted for {url}")
                return True
        except sqlite3.Error as e:
            logger.error(f"Database error deleting metrics: {e}")
            return False

    def clear_cache(self) -> bool:
        """Clear all cached metrics.

        Returns:
            True if successful, False otherwise
        """
        try:
            with self._get_connection() as conn:
                cursor = conn.cursor()
                cursor.execute("DELETE FROM performance_cache")
                conn.commit()
                logger.info("Performance cache cleared")
                return True
        except sqlite3.Error as e:
            logger.error(f"Database error clearing cache: {e}")
            return False
