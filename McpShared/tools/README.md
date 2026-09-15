# McpShared/tools — host-neutral harness scripts

Python helpers every HP MCP harness uses to talk to a server exe over stdio and to keep score. They know nothing
about a host: the exe path and the tool names come from the caller.

| Script | Use |
|---|---|
| `mcp-call.py` | One process per request: `python mcp-call.py <exe> initialize`, `… tools/list [--out file.json]`, `… tools/call <name> '<json>'`. `--env KEY=VAL` sets server options (e.g. an isolated registry root `HPNAVIS_MCP_Registry__LibraryPath=…`). Set `PYTHONIOENCODING=utf-8` on Windows — tool descriptions contain non-ASCII. |
| `mcp-session.py` | One long-lived session (`Server(exe, env)`: `initialize()`, `tools()`, `tool(name, args)`, `call`, `prompt`, `send`/`wait` for racing requests, `list_changed_since(t0)`, `close()`); `parse_tool_result` returns the tool's JSON object itself, `{isError, value}` for a bare value, `{isError, message}` for text. Loaded by file path (the hyphen keeps it out of `import`). |
| `harness_common.py` | The bookkeeping the scenario scripts share (snake_case on purpose — it is `import`ed): `utf8_console()`; `Server`/`parse_tool_result`/`mcp_session` re-exported from `mcp-session.py`; `ok(r)`, `short(o, n)`; `Checklist` — `check(name, cond, detail)` / `skip(name, reason)` print `PASS`/`FAIL`/`SKIP` lines and keep the results, `save(name, obj)` writes JSON into `out_dir` when set, `finish(save_as=None, **extra)` saves the summary + results, prints the one-line JSON summary (`passed` excludes skips, `skipped`, `total`, `failed`, `skippedNames`, plus `extra` such as `phase`) and returns the exit code (0 = no FAIL). `python harness_common.py` runs its self-test. |

Consumers (all by relative path — dependency direction stays MCP folder → `McpShared/`, never the other way):

```python
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, "..", "..", "..", "McpShared", "tools"))
from harness_common import Checklist, Server, mcp_session, ok, short, utf8_console

utf8_console()
CL = Checklist()
check, skip, save = CL.check, CL.skip, CL.save
...
CL.out_dir = args.out
sys.exit(CL.finish("summary-main", phase="main"))
```

`HPNavis/tools/harness/{live-verify, seeds-live, server-smoke}.py` and `HPAutoCad/tools/harness/live-verify.py` use it;
the PowerShell wrappers call `mcp-call.py` directly. The pipe-level scripts (`pipe-scenarios.py` in both harnesses) talk
NDJSON to the bridge without a server and keep their own three-line `check`.
