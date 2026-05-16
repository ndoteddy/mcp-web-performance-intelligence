# MCP Web Performance Intelligence

Enterprise-grade Agentic AI observability platform built with FastMCP, SQLite, and PageSpeed Insights for autonomous website diagnostics, persistent context management, intelligent tool orchestration, and AI-driven remediation recommendations.

## Overview

MCP Web Performance Intelligence is a sophisticated observability platform designed to provide autonomous, AI-driven analysis of website performance metrics. It combines the power of Model Context Protocol (MCP) with advanced machine learning to deliver intelligent diagnostics and actionable remediation strategies.

### Key Features

- **Agentic AI Analysis** - Autonomous agents that understand and analyze web performance data
- **FastMCP Integration** - High-performance Model Context Protocol implementation for seamless tool orchestration
- **PageSpeed Insights** - Direct integration with Google's PageSpeed Insights API for comprehensive performance metrics
- **SQLite Persistence** - Lightweight, file-based database for persistent context management and historical data retention
- **Intelligent Recommendations** - AI-driven remediation suggestions based on performance analysis
- **Tool Orchestration** - Sophisticated multi-tool coordination for complex diagnostics

## Architecture

### Components

- **FastMCP Server** - Core protocol implementation for agent communication
- **Performance Analysis Engine** - AI-powered metrics processing and pattern recognition
- **SQLite Database** - Persistent storage for performance history and context
- **PageSpeed Integration** - API connector for Google's performance analytics
- **Remediation Engine** - AI-driven suggestion system for performance improvements

## Getting Started

### Prerequisites

- Python 3.10+
- FastMCP
- SQLite3
- Google PageSpeed Insights API credentials

### Installation

```bash
# Clone the repository
git clone https://github.com/ndoteddy/mcp-web-performance-intelligence.git
cd mcp-web-performance-intelligence

# Install dependencies
pip install -r requirements.txt
```

### Configuration

1. Set up your PageSpeed Insights API credentials
2. Configure database connection parameters
3. Initialize the SQLite database

```bash
python scripts/init_db.py
```

## Usage

```python
from mcp_intelligence import PerformanceAnalyzer

analyzer = PerformanceAnalyzer()
results = analyzer.diagnose_website("https://example.com")
recommendations = analyzer.get_recommendations(results)
```

## Project Structure

```
mcp-web-performance-intelligence/
├── README.md
├── requirements.txt
├── src/
│   ├── mcp_intelligence/
│   │   ├── __init__.py
│   │   ├── analyzer.py
│   │   ├── database.py
│   │   └── recommendations.py
│   └── server/
│       └── fastmcp_server.py
├── scripts/
│   └── init_db.py
└── tests/
    └── test_analyzer.py
```

## API Documentation

### Performance Analyzer

The core analysis engine provides methods for:
- Website performance diagnostics
- Metric aggregation and analysis
- Historical trend analysis
- Remediation recommendations

## Contributing

Contributions are welcome! Please follow these guidelines:

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Support

For issues, questions, or suggestions, please open an issue on the [GitHub Issues](https://github.com/ndoteddy/mcp-web-performance-intelligence/issues) page.

## Acknowledgments

- [FastMCP](https://github.com/jlouis/fastmcp) - High-performance MCP implementation
- [Google PageSpeed Insights](https://developers.google.com/speed/pagespeed/insights) - Performance metrics API
- [SQLite](https://www.sqlite.org/) - Embedded database engine
