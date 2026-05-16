# MCP Web Performance Intelligence

Enterprise-grade Agentic AI observability platform built with FastMCP, SQLite, and PageSpeed Insights for autonomous website diagnostics, persistent context management, intelligent tool orchestration, and AI-driven remediation recommendations.

Made by Hernando Ivan Teddy - with innovation

## Overview

MCP Web Performance Intelligence is a proof-of-concept observability platform demonstrating autonomous, AI-driven analysis of website performance metrics using the Model Context Protocol (MCP).

## Repository Contents

This repository contains:

- **mcp_agent_performance_poc.ipynb** - Complete proof-of-concept implementation demonstrating the agentic workflow with FastMCP, Gemini integration, and PageSpeed Insights API integration
- **README.md** - This file

## Features

- **Agentic AI Analysis** - Autonomous agents powered by Gemini that understand and analyze web performance data
- **FastMCP Integration** - High-performance Model Context Protocol for seamless tool orchestration
- **PageSpeed Insights** - Direct integration with Google's PageSpeed Insights API for comprehensive performance metrics
- **SQLite Persistence** - Lightweight, file-based database for persistent context management and historical data
- **Intelligent Tool Orchestration** - Multi-step agentic workflow with automatic function calling
- **Two-Step Analysis** - Analyze → Suggest workflow coordinated by the AI agent

## Getting Started

### Prerequisites

- Python 3.10+
- Google Gemini API key
- Google PageSpeed Insights API key
- Jupyter Notebook or Google Colab

### Setup

1. Clone the repository:
```bash
git clone https://github.com/ndoteddy/mcp-web-performance-intelligence.git
cd mcp-web-performance-intelligence
```

2. Open the notebook in Google Colab or Jupyter:
```bash
jupyter notebook mcp_agent_performance_poc.ipynb
```

3. Add your API credentials:
   - Store `GOOGLE_API_KEY` in Colab secrets or environment
   - Store `PAGESPEED_API_KEY` in Colab secrets or environment

### Usage

The notebook demonstrates:

1. **Environment Setup** - Configure API keys and initialize SQLite database
2. **Tool Definition** - Register `analyze_website` and `suggest_improvements` with FastMCP
3. **Agent Orchestration** - Use Gemini with function calling to autonomously sequence tools
4. **Result Synthesis** - Generate professional technical reports

Run the notebook cells in order to execute the complete performance audit workflow.

## Architecture

### System Flow

```
User Request
    ↓
Gemini Model (Agent Brain)
    ↓
  ├─→ analyze_website (FastMCP Tool)
  │      ↓
  │   PageSpeed Insights API
  │      ↓
  │   SQLite Database (Cache)
  │
  └─→ suggest_improvements (FastMCP Tool)
         ↓
      Database Retrieval
         ↓
      Recommendation Engine
         ↓
Professional Technical Report
```

### Key Components

1. **FastMCP Server** - Defines and manages tool schemas
2. **Gemini Model** - Acts as the orchestrator with function calling
3. **PageSpeed Analyzer** - Fetches and parses performance metrics
4. **SQLite Database** - Persists metrics for recommendations
5. **Recommendation Engine** - Generates prioritized suggestions

## Key Implementation Details

### analyze_website Tool
- Calls Google PageSpeed Insights V5 API
- Extracts: Performance Score, FCP, Speed Index, LCP, TTI, TBT, CLS
- Persists metrics to SQLite for caching
- Returns JSON metrics for agent processing

### suggest_improvements Tool
- Retrieves cached metrics from database
- Analyzes against performance thresholds
- Generates 3 prioritized recommendations
- Includes impact assessment and technical details

### Agentic Workflow
1. User provides URL
2. Gemini receives instruction to use both tools
3. Agent autonomously calls `analyze_website`
4. Agent calls `suggest_improvements` with results
5. Agent synthesizes findings into professional report

## Performance Thresholds

The system evaluates against these industry standards:

- **Performance Score**: >= 70 is considered good
- **First Contentful Paint (FCP)**: < 1.8s is ideal
- **Time to Interactive (TTI)**: < 3.8s is ideal
- **Total Blocking Time (TBT)**: < 300ms is ideal

## API Integration

### Google PageSpeed Insights
- **Endpoint**: `https://www.googleapis.com/pagespeedonline/v5/runPagespeed`
- **Authentication**: API key in query parameters
- **Timeout**: 60 seconds
- **Response**: Lighthouse audit data with performance metrics

### Google Gemini
- **Model**: `gemini-2.5-flash-lite`
- **Features**: Function calling, system instructions, multi-turn chat
- **Quota Management**: Includes retry logic with exponential backoff

## Database Schema

```sql
CREATE TABLE performance_cache (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    url TEXT PRIMARY KEY,
    metrics TEXT,              -- JSON string of performance metrics
    analysis_summary TEXT,      -- Optional analysis notes
    updated_at DATETIME
);
```

## Error Handling

- **API Failures**: Graceful error messages returned in JSON
- **Quota Exhaustion**: Automatic retry with exponential backoff
- **Invalid Data**: Validation checks on metrics before persistence
- **Database Errors**: Transaction rollback and logging

## Future Enhancements

Potential extensions to this proof-of-concept:

1. **Production Backend** - Flask/FastAPI server wrapper
2. **Web Dashboard** - Real-time performance monitoring UI
3. **Multi-URL Batch Processing** - Process 100+ URLs automatically
4. **Historical Trending** - Track performance changes over time
5. **Custom Thresholds** - User-defined performance targets
6. **Slack Integration** - Automated audit notifications
7. **Scheduled Audits** - Periodic performance monitoring
8. **Advanced Analytics** - ML-driven pattern recognition

## Contributing

Contributions are welcome! Please note that this is a proof-of-concept. For production use, consider:

- Refactoring into modular Python packages
- Adding comprehensive test coverage
- Implementing CI/CD pipelines
- Setting up Docker containerization
- Adding observability/monitoring

## License

MIT License - See LICENSE file for details

## Support & Contact

For questions, issues, or suggestions:
- Open an issue on GitHub
- Contact: Hernando Ivan Teddy

## Acknowledgments

- [FastMCP](https://github.com/jlouis/fastmcp) - High-performance Model Context Protocol
- [Google Gemini](https://gemini.google.com/) - Advanced AI model with function calling
- [Google PageSpeed Insights](https://developers.google.com/speed/pagespeed/insights) - Comprehensive performance metrics
- [SQLite](https://www.sqlite.org/) - Embedded database engine

---

**Made by Hernando Ivan Teddy - with innovation**

Built to demonstrate enterprise-grade agentic AI workflows for autonomous observability.
