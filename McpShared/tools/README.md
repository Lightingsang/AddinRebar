# McpShared/tools — host-neutral harness scripts

Python helpers every HP MCP harness uses to talk to a server exe over stdio. They know nothing about a host:
the exe path and the tool names come from the caller.

| Script | Use |
|---|---|
| `mcp-call.py` | One process per request: `python mcp-call.py <exe> initialize`, `… tools/list [--out file.json]`, `… tools/call <name> '<json>'`. `--env KEY=VAL` sets server options (e.g. an isolated registry root `HPNAVIS_MCP_Registry__LibraryPath=…`). Set `PYTHONIOENCODING=utf-8` on Windows — tool descriptions contain non-ASCII. |
| `mcp-session.py` | One long-lived session (`Server(exe, env)`: `initialize()`, `tools()`, `tool(name, args)`, `call`, `prompt`, `send`/`wait` for racing requests, `list_changed_since(t0)`, `close()`); `parse_tool_result` returns the tool's JSON object itself, `{isError, value}` for a bare value, `{isError, message}` for text. Imported by file path (`importlib.util.spec_from_file_location`). |

Canonical copies since 2026-09-15; `HPNavis/tools/harness/*` import from here. `HPAutoCad/tools/harness/` still carries its own
identical copies (repointing it is a follow-up outside the Navisworks plan). Dependency direction stays MCP folder → `McpShared/`.
