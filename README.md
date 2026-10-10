# MCP Web Performance Intelligence

A .NET 10 **Model Context Protocol (MCP) server** that audits website performance via
Google PageSpeed Insights, caches results in SQLite, and generates prioritized
remediation recommendations — exposed as standard MCP tools any MCP client (Claude
Desktop, an IDE, a custom agent) can call.

Made by Hernando Ivan Teddy - with innovation

## Origin

This project started as a Python/Jupyter proof of concept using FastMCP, Gemini
function calling, and a Google Colab-only setup. That notebook is preserved at
[`doc/mcp_agent_performance_poc.ipynb`](doc/mcp_agent_performance_poc.ipynb) for
reference. The `.NET` implementation in this repository is a reverse-engineered,
production-shaped port: the Python notebook's cells were agent-orchestration specific
(bound to a particular Gemini model), so the actual **MCP server** — the reusable,
model-agnostic part — is now a standalone .NET application with full test coverage.

## What Changed From the Python POC

- **Standalone MCP server**, not a notebook cell. Runs over stdio like any other MCP
  server; works with any MCP client, not just a Gemini-specific chat loop.
- **`suggest_improvements` fully implemented.** The published notebook referenced this
  tool but never defined it in-notebook (it only existed in an earlier, later-deleted
  `src/tools.py`). The .NET port restores and implements the full recommendation logic.
- **Dependency-injected, testable architecture** instead of global notebook state
  (module-level `mcp`, `model`, `DB_FILE`).
- **Unit tests** for the analyzer, the SQLite cache, and the recommendation engine —
  none of which existed before.

See [`docs/architecture/C4.md`](docs/architecture/C4.md) for the full C4 diagrams and a
field-by-field mapping from the original Python modules to their .NET equivalents.

## Repository Layout

```
src/McpWebPerformanceIntelligence/   The MCP server (.NET 10 console app)
tests/McpWebPerformanceIntelligence.Tests/   xUnit test suite
docs/architecture/C4.md              C4 context/container/component diagrams
doc/mcp_agent_performance_poc.ipynb  Original Python/Gemini proof of concept
```

## Features

- **Two MCP tools**: `analyze_website` (PageSpeed Insights → metrics) and
  `suggest_improvements` (cached metrics → prioritized recommendations)
- **PageSpeed Insights integration** — Performance Score, FCP, Speed Index, LCP, TTI,
  TBT, CLS
- **SQLite persistence** (`performance_cache` table) so suggestions don't require a
  redundant API call
- **Configurable thresholds** for score/FCP/TTI/TBT via `appsettings.json` or
  environment variables

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- A [Google PageSpeed Insights API key](https://developers.google.com/speed/docs/insights/v5/get-started)

### Build & Test

```bash
dotnet restore
dotnet build
dotnet test
```

### Configuration

Set the PageSpeed API key via environment variable (preferred for secrets) or
`appsettings.json`:

```bash
export PAGESPEED_API_KEY="your-api-key-here"
# Optional overrides:
export DB_FILE="mcp_performance.db"
```

Other thresholds live in `src/McpWebPerformanceIntelligence/appsettings.json` under the
`Performance` section (`PerformanceScoreThreshold`, `FcpThresholdMs`, `TtiThresholdMs`,
`TbtThresholdMs`, `ApiTimeoutSeconds`).

### Running the Server

The server speaks MCP over stdio — it's meant to be launched by an MCP client, not run
interactively on its own.

```bash
dotnet run --project src/McpWebPerformanceIntelligence
```

To wire it into an MCP client (e.g. Claude Desktop's `claude_desktop_config.json`),
point the client at the built executable:

```json
{
  "mcpServers": {
    "web-performance-intelligence": {
      "command": "dotnet",
      "args": ["run", "--project", "/absolute/path/to/src/McpWebPerformanceIntelligence"],
      "env": {
        "PAGESPEED_API_KEY": "your-api-key-here"
      }
    }
  }
}
```

## Tools

### `analyze_website(url)`

Calls PageSpeed Insights V5, extracts Lighthouse metrics, validates them, and persists
them to SQLite. Returns JSON:

```json
{
  "score": 76.0,
  "first_contentful_paint": "0.5 s",
  "speed_index": "0.7 s",
  "largest_contentful_paint": "0.9 s",
  "time_to_interactive": "1.9 s",
  "total_blocking_time": "50 ms",
  "cumulative_layout_shift": "0.001"
}
```

### `suggest_improvements(url)`

Reads cached metrics for `url` (requires `analyze_website` to have run first) and
returns up to 3 prioritized recommendations plus a status summary:

```json
{
  "url": "https://example.com",
  "recommendations": [
    {
      "priority": 1,
      "category": "Overall Performance",
      "issue": "Performance score is 42 (threshold: 70)",
      "recommendation": "Comprehensive performance audit required...",
      "impact": "High"
    }
  ],
  "metrics_analyzed": { "score": 42, "status": "Needs Improvement" }
}
```

## Performance Thresholds

- **Performance Score**: >= 70 is considered good
- **First Contentful Paint (FCP)**: < 1.8s is ideal
- **Time to Interactive (TTI)**: < 3.8s is ideal
- **Total Blocking Time (TBT)**: < 300ms is ideal

## Architecture

See [`docs/architecture/C4.md`](docs/architecture/C4.md) for:
- System Context, Container, and Component (C4) diagrams
- A sequence diagram of the two-step `analyze_website` → `suggest_improvements` flow
- A table mapping each original Python module to its .NET equivalent

## Testing

`tests/McpWebPerformanceIntelligence.Tests` covers:
- `RecommendationEngineTests` — threshold logic for score/TBT/TTI, fallback case,
  boundary conditions
- `PerformanceAnalyzerTests` — Lighthouse JSON extraction, missing-field handling, HTTP
  failures, performance status banding (via a fake `HttpMessageHandler`, no live API calls)
- `SqlitePerformanceRepositoryTests` — insert/get/update/delete/clear round trips
  against a temp SQLite file
- `WebPerformanceToolsTests` — the MCP tool methods' success/error JSON contracts,
  using mocked analyzer/repository dependencies

```bash
dotnet test
```

## Future Enhancements

1. HTTP/SSE MCP transport (`ModelContextProtocol.AspNetCore`) for remote deployment
2. Historical trending across repeated audits of the same URL
3. Batch auditing across many URLs
4. Custom per-client thresholds

## License

MIT License - See LICENSE file for details

## Support & Contact

For questions, issues, or suggestions:
- Open an issue on GitHub
- Contact: Hernando Ivan Teddy

## Acknowledgments

- [Model Context Protocol](https://modelcontextprotocol.io/) — the protocol this server implements
- [Google PageSpeed Insights](https://developers.google.com/speed/pagespeed/insights) — performance metrics source
- [Microsoft.Data.Sqlite](https://learn.microsoft.com/dotnet/standard/data/sqlite/) — embedded database engine

---

**Made by Hernando Ivan Teddy - with innovation**
