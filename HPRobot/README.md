# HPRobot — Autodesk Robot Structural Analysis Professional 2026 MCP Subsystem

A Model Context Protocol (MCP) subsystem enabling AI coding agents and LLMs to interact safely, performantly, and expressively with Autodesk Robot Structural Analysis Professional 2026.

## Architecture

- **`HPRobot.McpBridge/`**: Standalone WPF desktop application (.NET 8.0-windows) running beside `robot.exe`, holding the COM attachment (`RobotOM.dll`), Named Pipe listener (`hprobot-mcp-2026`), 3-tier safety system, automated `.rtd` model snapshots, Metric units standardizer (`RobotUnitsPolicy`), and MaterialDesignThemes 5.3.2 UI.
- **`HPRobot.Mcp.Server/`**: Standalone console stdio server (.NET 10.0) exposing 24 tools (4 core, 8 registry meta, 12 embedded seeds) to MCP clients.
- **`HPRobot.McpBridge.Tests/`**: Unit tests for safety gating, AST tier analyzer, snapshot manager, and units policy.
- **`HPRobot.Mcp.Server.Tests/`**: Unit tests for tool catalog, schemas, fake executor round-trips, and Roslyn seed compilations.
