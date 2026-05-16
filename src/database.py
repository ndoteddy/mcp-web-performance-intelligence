"""SQLite database layer for persistent performance metrics storage."""

import sqlite3
import json
import logging
from contextlib import contextmanager
from datetime import datetime
from typing import Optional, Dict, Any

from src.config import DatabaseConfig

logger = logging.getLogger(__name__)


class PerformanceDatabase:
    """Manages SQLite persistence for performance metrics and analysis history."""

    def __init__(self, config: DatabaseConfig):
        """Initialize database connection and schema.
        
        Args:
            config: Database configuration object
        """
        self.db_file = config.db_file
        self.connection_timeout = config.connection_timeout
        self.enable_wal = config.enable_wal
        self._init_schema()

    @contextmanager
    def get_connection(self):
        """Context manager for database connections.
        
        Yields:
            sqlite3.Connection: Database connection
        """
        conn = sqlite3.connect(self.db_file, timeout=self.connection_timeout)
        conn.row_factory = sqlite3.Row
        try:
            yield conn
        finally:
            conn.close()

    def _init_schema(self) -> None:
        """Initialize database schema with required tables."""
        with self.get_connection() as conn:
            cursor = conn.cursor()
            
            if self.enable_wal:
                cursor.execute("PRAGMA journal_mode=WAL")
            
            cursor.execute("""
                CREATE TABLE IF NOT EXISTS performance_cache (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    url TEXT UNIQUE NOT NULL,
                    metrics TEXT NOT NULL,
                    analysis_summary TEXT,
                    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
                    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP
                )
            """)
            
            cursor.execute("""
                CREATE TABLE IF NOT EXISTS audit_history (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    url TEXT NOT NULL,
                    performance_score REAL,
                    fcp_ms REAL,
                    speed_index_ms REAL,
                    tti_ms REAL,
                    tbt_ms REAL,
                    recommendations TEXT,
                    executed_at DATETIME DEFAULT CURRENT_TIMESTAMP
                )
            """)
            
            cursor.execute("""
                CREATE INDEX IF NOT EXISTS idx_url ON performance_cache(url)
            """)
            
            cursor.execute("""
                CREATE INDEX IF NOT EXISTS idx_audit_url ON audit_history(url)
            """)
            
            conn.commit()
            logger.info(f"Database schema initialized: {self.db_file}")

    def store_metrics(self, url: str, metrics: Dict[str, Any]) -> None:
        """Store performance metrics for a URL.
        
        Args:
            url: Target website URL
            metrics: Performance metrics dictionary
        """
        metrics_json = json.dumps(metrics)
        with self.get_connection() as conn:
            cursor = conn.cursor()
            cursor.execute(
                """
                INSERT OR REPLACE INTO performance_cache (url, metrics, updated_at)
                VALUES (?, ?, CURRENT_TIMESTAMP)
                """,
                (url, metrics_json),
            )
            conn.commit()
            logger.info(f"Metrics stored for URL: {url}")

    def get_metrics(self, url: str) -> Optional[Dict[str, Any]]:
        """Retrieve cached metrics for a URL.
        
        Args:
            url: Target website URL
            
        Returns:
            Metrics dictionary or None if not found
        """
        with self.get_connection() as conn:
            cursor = conn.cursor()
            cursor.execute(
                "SELECT metrics FROM performance_cache WHERE url = ?",
                (url,),
            )
            row = cursor.fetchone()
            if row:
                return json.loads(row["metrics"])
        return None

    def store_audit_record(self, url: str, audit_data: Dict[str, Any]) -> None:
        """Store audit history record.
        
        Args:
            url: Target website URL
            audit_data: Audit results dictionary
        """
        with self.get_connection() as conn:
            cursor = conn.cursor()
            cursor.execute(
                """
                INSERT INTO audit_history
                (url, performance_score, fcp_ms, speed_index_ms, tti_ms, tbt_ms, recommendations)
                VALUES (?, ?, ?, ?, ?, ?, ?)
                """,
                (
                    url,
                    audit_data.get("score"),
                    audit_data.get("fcp_ms"),
                    audit_data.get("speed_index_ms"),
                    audit_data.get("tti_ms"),
                    audit_data.get("tbt_ms"),
                    json.dumps(audit_data.get("recommendations", [])),
                ),
            )
            conn.commit()
            logger.info(f"Audit record stored for URL: {url}")

    def get_audit_history(self, url: str, limit: int = 10) -> list:
        """Retrieve audit history for a URL.
        
        Args:
            url: Target website URL
            limit: Maximum number of records to return
            
        Returns:
            List of audit history records
        """
        with self.get_connection() as conn:
            cursor = conn.cursor()
            cursor.execute(
                """
                SELECT * FROM audit_history WHERE url = ?
                ORDER BY executed_at DESC LIMIT ?
                """,
                (url, limit),
            )
            return [dict(row) for row in cursor.fetchall()]

    def cleanup_old_records(self, days: int = 30) -> int:
        """Remove audit records older than specified days.
        
        Args:
            days: Number of days to retain
            
        Returns:
            Number of deleted records
        """
        with self.get_connection() as conn:
            cursor = conn.cursor()
            cursor.execute(
                """
                DELETE FROM audit_history
                WHERE executed_at < datetime('now', '-' || ? || ' days')
                """,
                (days,),
            )
            conn.commit()
            deleted = cursor.rowcount
            logger.info(f"Cleaned up {deleted} old audit records")
            return deleted
