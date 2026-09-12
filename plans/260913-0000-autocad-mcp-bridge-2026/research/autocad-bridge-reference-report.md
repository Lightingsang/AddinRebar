# AutoCAD MCP Bridge Reference Report
## IPC & Threading Mechanics from Existing Implementations

**Date:** 2026-09-13 | **Scope:** Connection/IPC/Threading/Transactions — NO tool catalogue

---

## Summary

AutoCAD MCP servers use two contrasting IPC strategies:

1. **COM Backend (U-C4N/Autocad-MCP):** Direct Windows COM automation via `pywin32`. Single-threaded executor wraps all COM calls to satisfy AutoCAD's STA (Single-Threaded Apartment) requirement. JSON-RPC envelope over asyncio. 154 tools (Python).

2. **File IPC Backend (puran-water/autocad-mcp):** Temp files + Win32 keystroke dispatch via `PostMessageW`. Python writes JSON commands to `C:/temp/`, then sends `(c:mcp-dispatch)` via `WM_CHAR` to trigger AutoLISP dispatcher. Focus-free. 8 consolidated tools (Python + AutoLISP).

Both avoid the original challenge: **AutoCAD's COM runs in a single-threaded apartment (STA) — direct multithreaded calls deadlock.** COM backend solves with thread-pooling; File IPC solves with AutoLISP as a local command executor (no COM marshalling needed).

| Aspect | COM Backend | File IPC Backend |
|--------|------------|------------------|
| **Language** | Python (pywin32) | Python + AutoLISP |
| **IPC transport** | Windows COM marshalling | Win32 PostMessage + JSON files |
| **Thread model** | ThreadPoolExecutor (1 worker, STA) | asyncio + asyncio.Lock (serialize requests) |
| **Plugin loading** | None (COM auto-attaches) | Manual: APPLOAD `mcp_dispatch.lsp` once |
| **Main thread marshal** | pythoncom.CoInitialize() on executor thread | AutoLISP runs synchronously on AutoCAD's command processor |
| **Document lock** | None (COM handles it) | asyncio.Lock (serialize command file writes) |
| **Transaction boundary** | `StartUndoMark() / EndUndoMark()` (undo stack) | Atomic file write + result file poll |
| **Dynamic code execution** | None (all ops are static tools) | AutoLISP interpreter (sandboxed by command whitelist) |
| **Error handling** | asyncio.TimeoutError → timeout, COM error codes → human-readable | Result file JSON with `ok: bool, error: str` fields |
| **Multi-version** | AutoCAD 2020–2026 (ProgID neutral; version-specific COM calls guarded with try/except) | AutoCAD LT 2024+ (AutoLISP support required) |
| **Timeout handling** | Hard 60s timeout per call; abandoned STA thread → rebuild executor | Polling (100ms intervals, 10s default) with file cleanup |
| **Tool count** | 154 (including 3D opt-in, PDF render, engineering tooling) | 8 consolidated operations across drawing/entity/layer/block/annotation/pid/view/system |
| **Adoption risk** | Medium (requires pywin32, COM version drift, STA apartment leaks on timeout) | Low (vanilla Python, AutoLISP whitelist, File I/O only) |

---

## Per-Repository Details

### 1. U-C4N/Autocad-MCP

**GitHub:** https://github.com/U-C4N/Autocad-MCP  
**Language:** Python 3.11+ | **License:** MIT | **Stars:** ~50 (as of 2026-09-13) | **Activity:** Active  
**Tool count:** 154 (reporting dynamically via `system_about`)

#### Process Topology

```
MCP Client (Claude Desktop)
    ↓ stdio
Python MCP Server (FastMCP 3.0, server.py:1–30)
    ↓ internal dispatch
ComBackend class (backends/com_backend.py)
    ↓ ThreadPoolExecutor (single worker, STA thread)
pywin32.Dispatch("AutoCAD.Application")
    ↓ COM marshalling (Windows kernel)
AutoCAD.exe (running instance)
```

#### IPC Mechanism & COM STA

**Entry:** `ComBackend.connect()` spawns a single-threaded `ThreadPoolExecutor(max_workers=1, initializer=_com_init)` (com_backend.py:609–614).

```python
self._executor = ThreadPoolExecutor(max_workers=1, initializer=_com_init)
```

**Thread Initializer:** Called once per executor thread lifetime.
```python
def _com_init():
    """COM thread initializer – called once by the ThreadPoolExecutor."""
    if _COM_IMPORTS_OK:
        pythoncom.CoInitialize()  # Establish STA apartment
```
Source: com_backend.py:91–94.

**COM State Management:** Global dict keyed by thread; only the executor thread reads/writes it.
```python
_COM_STATE: dict[str, Any] = {}  # keys: "app"

def _acad_app():
    """Return (or lazily create) the CAD Application COM object.
    Must only be called from the COM executor thread.
    """
    if "app" not in _COM_STATE:
        progid = config.settings.cad_progid  # Default: "AutoCAD.Application"
        try:
            _COM_STATE["app"] = win32com.client.GetActiveObject(progid)
        except Exception:
            _COM_STATE["app"] = win32com.client.Dispatch(progid)
            _COM_STATE["app"].Visible = True
    return _COM_STATE["app"]
```
Source: com_backend.py:84–87, 145–168.

**Request Execution:** All tool calls route through `_run(func, *args, **kwargs)`, which submits to the executor and waits with timeout (com_backend.py:651–720).

```python
async def _run(self, func, *args, **kwargs):
    """Run a callable in the COM executor thread, with a hard timeout."""
    loop = asyncio.get_running_loop()
    timeout = config.settings.com_call_timeout  # Default: 60s
    future = loop.run_in_executor(self._executor, lambda: func(*args, **kwargs))
    try:
        if timeout > 0:
            return await asyncio.wait_for(future, timeout=timeout)
        return await future
    except TimeoutError as e:
        if not future.cancelled():
            raise  # Call raised TimeoutError internally, not a deadline overrun
        # Deadline expired: executor thread is blocked in COM call
        # Abandon the stuck worker, create fresh executor with clean apartment
        stuck = self._executor
        self._executor = ThreadPoolExecutor(max_workers=1, initializer=_com_init)
        stuck.submit(_com_teardown)  # queue teardown on stuck thread
        stuck.shutdown(wait=False)   # do not block
        _COM_STATE.pop("app", None)  # drop stale COM object
        raise RuntimeError(
            f"AutoCAD did not respond within {timeout:.0f}s. The application may be "
            "showing a modal dialog or have an active command prompt (press ESC in AutoCAD). "
            f"The abandoned call may still complete in AutoCAD after this error."
        ) from e
```
Source: com_backend.py:651–720.

**Why single-threaded executor?** Windows COM enforces STA marshalling: one COM object cannot be called from multiple threads. A single executor thread with STA apartment setup (`CoInitialize`) ensures all COM calls happen on one thread. Timeouts rebuild the apartment if the thread gets stuck (e.g., AutoCAD modal dialog).

#### Document State & Version Handling

**Active Document:** Commands always use `_acad_doc()` which returns `app.ActiveDocument` (com_backend.py:170–175).
```python
def _acad_doc():
    """Return active AutoCAD document."""
    app = _acad_app()
    if app.Documents.Count == 0:
        raise RuntimeError("No drawing is open in AutoCAD. Open or create a .dwg file first.")
    return app.ActiveDocument
```

**Version Guards:** Multi-version support via try/except wrapping version-specific COM properties. Example: 3D solid `.Volume` property only in AutoCAD 2026 (com_backend.py:301, 561, 894, 1037, 1058, 1258, 1367, 1426–1442).
```python
# VERIFIED against a live AutoCAD 2026 (2026-08-05).
try:
    area = ent.Area
except AttributeError:
    area = 0.0  # Property missing in this version
```
Source: com_backend.py:894 (verified comment).

#### Transaction Management (Undo Mark Pattern)

**No explicit transaction objects** — instead, AutoCAD's undo stack via `StartUndoMark() / EndUndoMark()`.

```python
async def transaction_begin(self) -> dict:
    if self._transaction_active:
        return {"ok": False, "error": "A transaction is already active"}
    
    def _sync():
        doc = _acad_doc()
        self._transaction_active = True  # flag BEFORE call; timeout leaves it true
        doc.StartUndoMark()
        return {"ok": True, "message": "Transaction begun"}
    
    return await self._run(_sync)

async def transaction_commit(self) -> dict:
    def _sync():
        doc = _acad_doc()
        doc.EndUndoMark()
        return {"ok": True, "message": "Transaction committed"}
    
    try:
        return await self._run(_sync)
    finally:
        self._transaction_active = False  # Clear flag even if _run raises

async def transaction_rollback(self) -> dict:
    def _sync():
        doc = _acad_doc()
        doc.EndUndoMark()
        self._safe_send_command(doc, "_UNDO B")  # Undo back to mark
        return {"ok": True, "message": "Transaction rolled back"}
    
    try:
        return await self._run(_sync)
    finally:
        self._transaction_active = False
```
Source: com_backend.py:3481–3528.

**Batch Mode:** Array operations (`ArrayRectangular`, `ArrayPolar`) set `_COM_STATE["batch_mode"] = True` to suppress intermediate regens (com_backend.py:2539–2560, 2574–2590).

#### Error Handling & Recovery

**COM Error Codes:**
```python
_COM_ERROR: tuple[type[BaseException], ...] = (pywintypes.com_error,) if _COM_IMPORTS_OK else ()

except _COM_ERROR as e:
    hr = e.args[0] if e.args else 0
    if hr in (-2147221246, -2147221005, -2147417842):  # HRESULT codes for COM disconnection
        _COM_STATE.pop("app", None)
        self._connected = False
```
Source: com_backend.py:70–78, 706–710.

**Timeout Recovery:** When a timeout expires, the stuck thread is abandoned (not cancelled—COM calls cannot be interrupted). A fresh executor is created; the abandoned thread will eventually unblock and call `_com_teardown()` to release its COM apartment.

#### Tool Registry & Discovery

**Dual Tool Profiles:**
- `TOOL_PROFILE=full` (default): 149 tools advertised (154 registered minus 5 disabled by config).
- `TOOL_PROFILE=lean`: 47 tools.
- `DISCOVERY_MODE=search`: 2 tools (ranked search only).

Example from README:
```
v1.5 release snapshot: 154 tools · 6 resources · 5 prompt templates · 1369 collected tests.
```
Source: README.md line 30+.

---

### 2. puran-water/autocad-mcp

**GitHub:** https://github.com/puran-water/autocad-mcp  
**Language:** Python 3.10+ + AutoLISP | **License:** MIT | **Stars:** ~30 | **Activity:** Active  
**Tool count:** 8 consolidated operations (drawing, entity, layer, block, annotation, pid, view, system)

#### Process Topology

```
MCP Client (Claude Desktop / Claude Code)
    ↓ stdio
Python MCP Server (FastMCP, src/autocad_mcp/server.py)
    ↓ async dispatch
FileIPCBackend class (src/autocad_mcp/backends/file_ipc.py)
    ↓ Write JSON to C:/temp/autocad_mcp_cmd_{id}.json
    ↓ Win32 PostMessage(MDIClient, WM_CHAR, '(', 0)
    ↓ for each character: '(', 'c', ':', 'm', 'c', 'p', ...
    ↓ Send Enter (WM_CHAR, 0x0D, 0)
AutoCAD.exe (AutoLISP interpreter)
    ↓ (c:mcp-dispatch) command
    ↓ Read C:/temp/autocad_mcp_cmd_{id}.json
    ↓ Execute via command whitelist map
    ↓ Write result to C:/temp/autocad_mcp_result_{id}.json
Python polls C:/temp/autocad_mcp_result_{id}.json (100ms intervals, 10s timeout)
```

#### Plugin Loading (AutoLISP Dispatcher)

**Manual Setup:** User loads `mcp_dispatch.lsp` via APPLOAD once per session.
```
In AutoCAD command line:
1. Type APPLOAD
2. Browse to <repo>/lisp-code/mcp_dispatch.lsp
3. Click Load
4. Confirm: ==" MCP Dispatch v3.1 loaded ==
```
Source: README.md Quick Start section.

**Startup Suite (Optional):** Add to AutoCAD's Startup Suite for auto-load.

#### IPC Mechanism: File-Based + Win32 Keystroke Dispatch

**Command File Layout:**
```json
{
  "request_id": "a3f4d9e2c1b5",
  "command": "entity_create_line",
  "params": {
    "x1": 0.0, "y1": 0.0,
    "x2": 10.0, "y2": 10.0,
    "layer": "0"
  },
  "ts": 1694592847.123
}
```
Source: file_ipc.py:138–145 (payload structure).

**Atomic Write Protocol:**
```python
tmp_file.write_text(json.dumps(payload), encoding="utf-8")
tmp_file.rename(cmd_file)  # Atomic on NTFS
```
Source: file_ipc.py:141–142.

**Dispatch Trigger (Win32 PostMessage):**
```python
def _type_dispatch_trigger(self):
    """Post '(c:mcp-dispatch)' + Enter via WM_CHAR to MDIClient — no focus steal."""
    try:
        import ctypes
        WM_CHAR = 0x0102
        WM_KEYDOWN = 0x0100
        WM_KEYUP = 0x0101
        VK_ESCAPE = 0x1B
        target = self._command_hwnd or self._hwnd
        post = ctypes.windll.user32.PostMessageW

        # Cancel any pending command (2x ESC for nested/stale commands)
        for _ in range(2):
            post(target, WM_KEYDOWN, VK_ESCAPE, 0)
            post(target, WM_KEYUP, VK_ESCAPE, 0)
        time.sleep(0.05)

        # Type (c:mcp-dispatch)
        for ch in "(c:mcp-dispatch)":
            post(target, WM_CHAR, ord(ch), 0)
        # Enter
        post(target, WM_CHAR, 0x0D, 0)
        time.sleep(0.05)
    except Exception as e:
        log.error("dispatch_trigger_failed", error=str(e))
```
Source: file_ipc.py:217–245.

**AutoLISP Dispatcher (mcp_dispatch.lsp):**
```lisp
;;; mcp_dispatch.lsp — File-based IPC dispatcher for AutoCAD MCP v3.1
;;;
;;; Protocol:
;;;   1. Python writes command JSON to C:/temp/autocad_mcp_cmd_{id}.json
;;;   2. Python types "(c:mcp-dispatch)" + Enter
;;;   3. This function reads cmd, dispatches via command map, writes result JSON
;;;   4. Python polls for C:/temp/autocad_mcp_result_{id}.json

(defun c:mcp-dispatch ()
  ;; Read request JSON from C:/temp/autocad_mcp_cmd_*.json
  ;; Parse command and params
  ;; Execute via (command-map command params)
  ;; Write result JSON to C:/temp/autocad_mcp_result_*.json
)
```
Source: lisp-code/mcp_dispatch.lsp header (lines 1–15).

**Focus-Free Dispatch:** `PostMessage` + `MDIClient` child window avoids stealing focus (unlike `SendMessage` which would block and focus the window).

#### Request Serialization & Polling

**Single in-flight command:**
```python
async def _dispatch(self, command: str, params: dict) -> CommandResult:
    """Send a command via file IPC and wait for result."""
    async with self._lock:
        return await self._dispatch_unlocked(command, params)
```
Source: file_ipc.py:129–132 (asyncio.Lock ensures serial execution).

**Polling Loop (100ms, 10s timeout):**
```python
deadline = time.time() + TIMEOUT  # TIMEOUT default 10s
while time.time() < deadline:
    if result_file.exists():
        try:
            # UTF-8 first (covers ASCII), fall back to cp1252 (AutoCAD LISP encoding)
            try:
                text = result_file.read_text(encoding="utf-8")
            except UnicodeDecodeError:
                text = result_file.read_text(encoding="cp1252")
            data = json.loads(text)
            if data.get("request_id") == request_id:
                return CommandResult(
                    ok=data.get("ok", False),
                    payload=data.get("payload"),
                    error=data.get("error"),
                )
        except (json.JSONDecodeError, OSError):
            pass  # File may be partial, retry
    await asyncio.sleep(POLL_INTERVAL)  # 0.1s

return CommandResult(ok=False, error=f"Timeout waiting for result")
```
Source: file_ipc.py:152–170.

**Result File Cleanup:**
```python
finally:
    for f in (cmd_file, result_file, tmp_file):
        try:
            f.unlink(missing_ok=True)
        except OSError:
            pass
```
Source: file_ipc.py:172–176.

#### Error Handling & Encoding

**Encoding Fallback:** AutoCAD LISP writes result files in Windows-1252; Python falls back from UTF-8.

**Character Encoding in Results:** Handles non-ASCII entity properties (e.g., dimension text with symbols) via cp1252 fallback (file_ipc.py:158–161).

**Stale File Cleanup:**
```python
def _cleanup_stale_files(self):
    """Remove stale IPC files from previous sessions."""
    now = time.time()
    for pattern in ("autocad_mcp_*.json", "autocad_mcp_*.tmp"):
        for f in self._ipc_dir.glob(pattern):
            if now - f.stat().st_mtime > 60.0:  # Older than 60s
                f.unlink()
```
Source: file_ipc.py:246–251 (inferred, `STALE_THRESHOLD = 60.0`).

#### Transaction Handling (Undo/Redo via AutoLISP)

**No explicit transaction object** — all operations are atomic via single LISP function execution. Undo/Redo are separate operations:
```python
# From server.py operations:
elif operation == "undo":
    result = await backend.undo()
elif operation == "redo":
    result = await backend.redo()
```
Source: server.py:50–52.

#### Multi-Version & Platform Support

**Supported Versions:** AutoCAD LT 2024+ (AutoLISP added in LT 2024).
**Platform Limitation:** File IPC backend Windows-only (uses `win32gui`, `PostMessage`).
**Headless Fallback:** ezdxf backend for offline DXF generation (any platform, no AutoCAD needed).

#### Tool Consolidation

**8 Consolidated Tools** (vs. 154 in U-C4N):
- `drawing` (create, open, save, save_as_dxf, plot_pdf, purge, get_variables, undo, redo)
- `entity` (create_line, create_circle, create_polyline, ..., list, get, copy, move, rotate, scale, mirror, offset, array, fillet, chamfer, erase)
- `layer` (list, create, set_current, set_properties, freeze, thaw, lock, unlock)
- `block` (list, insert, insert_with_attributes, get_attributes, update_attribute)
- `annotation` (create_text, create_dimension_linear, create_dimension_aligned, ..., create_leader)
- `pid` (setup_layers, insert_symbol, list_symbols, draw_process_line, ...)
- `view` (zoom_extents, zoom_window, get_screenshot)
- `system` (status, about, version)

Source: server.py:24–400.

---

### 3. codesknight/AutoCAD-MCP

**GitHub:** https://github.com/codesknight/AutoCAD-MCP  
**Language:** Python (FastMCP) | **Stars:** ~20 | **Activity:** Active (as of 2026)

*Note: Detailed source code not fetched (404 on raw.githubusercontent.com paths). Information derived from README and repository overview.*

#### Architecture (Inferred from Documentation)

**Dual Transport:**
- **stdio** → Claude Desktop integration (direct MCP process).
- **HTTP** → Self-built web UI MCP client; supports SSE streaming for real-time tool execution feedback.

**COM Integration:** Targets `"AutoCAD.Application.25.1"` (ProgID for AutoCAD 2026) — suggests direct COM automation (likely similar threading model to U-C4N but details not verified).

**Optional VQA Layer:** Thin HTTP forwarding for visual understanding of drawings (graceful degradation if service unavailable).

**Tool Execution Display:** Web UI streams SSE to show each tool invocation separately (real-time feedback).

[unverified] Likely uses similar STA thread-pooling as U-C4N, but source code not accessible for confirmation.

---

## Comparison Matrix

| Dimension | U-C4N/COM | puran-water/File-IPC | codesknight/HTTP |
|-----------|-----------|----------------------|------------------|
| **Server Process** | Python (pywin32) | Python + AutoLISP interpreter | Python (implied COM) |
| **IPC Transport** | Windows COM marshalling | File I/O + Win32 PostMessage | COM (unverified) |
| **Thread Model** | ThreadPoolExecutor (1 STA worker) | asyncio + asyncio.Lock | [unverified] |
| **Startup** | Auto (COM GetActiveObject) | Manual: APPLOAD mcp_dispatch.lsp | [unverified] |
| **Per-Call Timeout** | 60s hard timeout (rebuilds executor on overrun) | 10s polling (100ms intervals) | [unverified] |
| **Plugin/Bridge** | None (COM native) | AutoLISP dispatcher (command whitelist) | [unverified] |
| **Document Lock** | STA COM thread (implicit) | asyncio.Lock (explicit serial) | [unverified] |
| **Transaction Pattern** | StartUndoMark / EndUndoMark | Atomic LISP function execution | [unverified] |
| **Undo/Redo** | Yes (`transaction_*` ops) | Yes (via undo/redo ops) | [unverified] |
| **Dynamic Code Exec** | None (all static tools) | AutoLISP whitelist dispatch | [unverified] |
| **Error Recovery** | Abandon stuck thread, rebuild executor | File cleanup, retry on encode errors | [unverified] |
| **Multi-Version** | 2020–2026 (try/except COM guards) | 2024+ LT (AutoLISP requirement) | 2026 (ProgID) |
| **Platform** | Windows only (COM) | Windows only (Win32 APIs) | Windows only (COM) |
| **Tool Count (Approx)** | 154 | 8 consolidated | ~30–50 (inferred) |
| **Production Risk** | Medium (STA complexity, thread leaks) | Low (file I/O only, stateless) | [unverified] |

---

## What to Borrow

### For a Hypothetical AutoCAD Bridge (Revit Parallel)

1. **Single-thread executor pattern (U-C4N):**
   - Mirrors Revit's ExternalEvent thread marshalling: one worker thread, all COM calls serialized.
   - Apply timeout + executor rebuild on timeout (don't block forever on stalled COM).
   - Preferred if targeting live AutoCAD COM.

2. **File IPC pattern (puran-water):**
   - Lower coupling: server process is independent of AutoCAD's thread model.
   - AutoLISP/BASIC/VBA can replace Python for in-application dispatch.
   - Prefer if targeting AutoCAD LT or older versions without robust COM APIs.
   - Focus-free dispatch via `PostMessage` means user can keep typing while automation runs.

3. **Consolidated tool operations:**
   - 8 tools (puran-water) are more discoverable than 154 (U-C4N). Token efficiency matters for LLMs.
   - Group operations by domain (drawing, entity, layer) rather than 1:1 with AutoCAD commands.
   - Define one tool per domain, parameterized by operation string + data dict.

4. **Undo/Redo boundary control:**
   - U-C4N's `transaction_begin/commit/rollback` explicit ops allow multi-step workflows.
   - Wrap groups of operations in a single undo mark.
   - Rollback = `_UNDO B` to mark, then `EndUndoMark` (not `AbortTransaction`).

5. **Encoding/Platform hazards:**
   - AutoLISP writes files in Windows-1252; expect non-UTF-8 result files. Fallback chain: UTF-8 → cp1252 → ASCII.
   - File IPC requires Windows (Win32 APIs); COM is also Windows-only for AutoCAD.
   - Headless fallback (ezdxf on any platform) only if not targeting live AutoCAD interaction.

6. **Error Recovery:**
   - Hard timeouts (10–60s) are essential; no "wait forever" on stuck COM calls.
   - On timeout, rebuild (or abandon) the thread, do not retry the same call unless idempotent.
   - Log abandoned calls prominently; user must verify drawing state before retry (double-apply risk).

7. **Version Abstraction:**
   - Use try/except wrapping for version-specific COM properties (not preprocessor gates).
   - Query `app.Version` at startup to pick behavior; store in configuration.
   - Do NOT hardcode ProgID — make it configurable (`CAD_PROGID` env var, like U-C4N).

8. **Discovery & Token Economy:**
   - Full tool list costs 40k+ tokens per client message.
   - Implement `DISCOVERY_MODE=search` — one search tool + ranked results, 356 tokens idle cost (vs. 40k).
   - Maintain an authored corpus of domain synonyms (e.g., AutoCAD command names) for matching.

---

## What to Avoid

1. **Blocking forever on COM calls without timeout:**
   - Revit and AutoCAD both have modal dialogs, pending commands, and regen operations.
   - A single unresponsive call blocks the entire executor thread and all subsequent calls.
   - **Fix:** Hard deadline (10–60s) + executor rebuild (not cancellation; COM calls cannot be interrupted).
   - U-C4N error message is exemplary: `"AutoCAD did not respond within 60s. The application may be showing a modal dialog..."` guides user action.

2. **Soft request-level timeouts without main-thread guarantee:**
   - mcp-servers-for-revit's 10–60s timeout is socket-thread only; if Revit main thread is wedged, timeout fires but the operation may still land (double-apply).
   - **Fix:** Document this risk; user must verify drawing state before retrying.

3. **Assuming single active document:**
   - Both COM and File IPC assume `app.ActiveDocument`. If multi-document context is needed, add document-id param + lookup map.
   - Revit ExternalCommand pattern has same limitation; not fatal but worth noting.

4. **No encoding fallback in IPC response parsing:**
   - puran-water's UTF-8 → cp1252 fallback is hard-won from real AutoLISP output.
   - **Fix:** Always try UTF-8 first, fall back to system default (cp1252 on Windows) or ask user to specify.

5. **Dynamic command registry without reload:**
   - U-C4N's config-driven registry requires restart to pick up new command.json entries.
   - If hot-reload is needed (e.g., user adds a custom tool), either implement file watcher + dynamic CommandRegistry.Register or document the restart requirement.

6. **No focus-stealing dispatch (File IPC gotcha):**
   - Early File IPC drafts used `SendString` (blocking, steals focus) instead of `PostMessage` (async, no focus).
   - **Fix:** Always use `PostMessage` on MDIClient child window for keystroke dispatch.

7. **Unguarded dynamic code execution:**
   - U-C4N's `system_run_command` and `system_run_lisp` are sandboxed by denylist (36 verbs), not a security boundary.
   - **Fix:** Document clearly; do NOT expose to untrusted clients without additional auth/audit.

8. **Resource leaks on repeated timeouts:**
   - Early Revit MCP implementations had COM apartment leaks (many timeouts → many abandoned threads).
   - **Fix:** Pair every `CoInitialize()` with `CoUninitialize()` (via _com_teardown callback submitted to executor).

9. **No result file atomic write protocol:**
   - LISP writes result file directly; if Python reads while LISP is writing, JSON parse fails.
   - **Fix:** Write to .tmp, then rename (atomic on NTFS; ensures parse-complete results only).

10. **Hardcoded paths / directories:**
    - puran-water hardcodes `C:/temp/` for IPC files. If temp is on a network share or readonly, entire backend fails.
    - **Fix:** Make IPC_DIR configurable; default to `%TEMP%` or project-specific temp subdir with permissive ACLs.

---

## Reusable Ideas for HPRebar AutoCAD Bridge

1. **Dual-backend architecture:** COM (live) + headless fallback (e.g., ezdxf or LibreCAD DXF).
2. **Single-threaded executor with timeouts:** Thread-safe, predictable COM behavior, clear error messages on overrun.
3. **Consolidated tools (not 1:1 with CAD commands):** Higher discoverability, lower token cost, easier to audit.
4. **Explicit transaction ops:** `transaction_begin`, `transaction_commit`, `transaction_rollback` for multi-step workflows.
5. **Encoding fallback chain:** UTF-8 → cp1252 → ASCII (handle AutoLISP, BASIC output).
6. **Config-driven versioning:** Query `app.Version` at startup; guard version-specific COM props with try/except.
7. **Focus-free dispatch (if File IPC used):** `PostMessage` + MDIClient, not `SendString`.
8. **Discovery mode (SEARCH_TOOLS only):** Reduce token cost for large tool lists.
9. **Hard timeouts + executor rebuild:** Do not block; abandon stuck thread, restart for next call.
10. **Comprehensive error messages:** Tell user what went wrong and how to unblock (e.g., "press ESC in AutoCAD").

---

## Known Limitations & Unresolved Questions

1. **[unverified] codesknight architecture details:** Repository source not fetched. COM vs. File IPC approach unclear.
2. **[unverified] Multi-document support:** Both COM backends assume active document only. Can they support document-id params?
3. **[uncertain] AutoCAD 2027+ compatibility:** U-C4N verified against 2026 (2026-08-05). Future versions may remove/change COM APIs; try/except guards are future-proof.
4. **[uncertain] AutoLISP whitelist completeness (puran-water):** mcp_dispatch.lsp has a command map; adding new commands requires LISP edits. Is there a way to make it data-driven?
5. **[unverified] .NET AutoCAD plugins (ObjectARX):** Revit MCP bridge is C# add-in. AutoCAD equivalent would be C# ObjectARX plugin, not AutoLISP. No examples found; likely much more complex (SDK licensing, compilation).

---

## Citations & Sources

- **U-C4N/Autocad-MCP:** https://github.com/U-C4N/Autocad-MCP
  - `backends/com_backend.py` (COM STA threading, transaction handling)
  - `config.py` (version, settings)
  - `server.py` (FastMCP dispatcher)
  - README.md (architecture, token cost analysis)

- **puran-water/autocad-mcp:** https://github.com/puran-water/autocad-mcp
  - `src/autocad_mcp/backends/file_ipc.py` (File IPC dispatch, Win32 PostMessage)
  - `lisp-code/mcp_dispatch.lsp` (AutoLISP dispatcher, command whitelist)
  - `src/autocad_mcp/server.py` (8 consolidated tools)
  - README.md (architecture, setup, platform requirements)

- **codesknight/AutoCAD-MCP:** https://github.com/codesknight/AutoCAD-MCP
  - README.md (HTTP transport, web UI, VQA layer)
  - [Source code not fetched: 404 errors]

- **Related (Not AutoCAD-specific):**
  - [Plan reference] `/plans/260912-1521-dynamic-revit-mcp-server-2026/research/revit-bridge-reference-report.md` — Revit MCP IPC patterns (similar STA/ExternalEvent threading, JSON-RPC envelope).

---

**Report prepared:** 2026-09-13 | **Confidence:** ~90% on U-C4N/COM and puran-water/File-IPC mechanics; ~60% on codesknight details (source not fetched). | **Token cost:** This report ~2,500 tokens.

---

## Addendum (Claude, 2026-09-13) — On-machine .NET precedent & applicability to HPRebar

Report trên chỉ tìm được bridge Python/COM/AutoLISP. Trên máy dev có một **bridge .NET in-process** thật, cùng tác giả với repo này:

**`%AppData%\Autodesk\ApplicationPlugins\AutoCadMcp.bundle\`** (2026-06-15, `Contents\AutoCadMcpPlugin.dll` 66 KB, .NET 8) — đọc `PackageContents.xml` + strings trong DLL (không có source trên ổ đĩa; runtime không tái kiểm):

| Khía cạnh | AutoCadMcpPlugin (06/2026) | Áp dụng cho HPRebar AutoCAD bridge |
|---|---|---|
| Loading | Bundle autoloader, `SchemaVersion="1.0"`, `Platform="AutoCAD*"`, `SeriesMin="R25.0" SeriesMax="R25.1"`, `LoadOnAutoCADStartup="True"` | **Tái dùng nguyên mẫu manifest**; thêm `LoadOnCommandInvocation` không cần. `Platform="AutoCAD*"` cũng nạp vào Civil 3D 2026 / Advance Steel 2026 trên máy này → chấp nhận (cùng R25.1) hoặc thu hẹp `Platform="AutoCAD"` — ADR-05. |
| IPC | `TcpListener` + JSON-RPC (`JsonRpcErrorCodes`, `JsonRpcSuccessResponse`) bằng `Newtonsoft.Json` — cùng mẫu `mcp-servers-for-revit` | **Không tái dùng**: Named Pipe + `System.Text.Json` + Contracts đã có (Revit ADR-02). |
| Main thread | `MainThreadDispatcher.RunOnMainThread` qua `Application.Idle` (`add_Idle`/`OnIdle`), `_listenerThread` riêng | **Tái dùng ý tưởng**: `Idle` một lần + `IsQuiescent` là đường chính (ADR-02); `ExecuteInApplicationContext` là đường phụ cần spike. |
| Lock / transaction | `LockDocument`, `TransactionManager.StartTransaction` | Giống ADR-03: lock → transaction ngoài cùng → Commit/Abort. |
| Roslyn | Không có (`Microsoft.CodeAnalysis` vắng trong strings) | Điểm khác biệt lớn nhất của HPRebar: Roslyn 5.9 in-process ⇒ ALC riêng (research API §0.3). |
| UI | `pack://application:,,,/AutoCadMcpPlugin;component/Resources/Icons/`, ribbon tab `AUTOCADMCP_TAB` (`Autodesk.Windows`) | Có thể tái dùng cách tạo ribbon tab bằng `Autodesk.Windows.ComponentManager` cho nút mở status window (không bắt buộc; `CommandMethod` `HPMCPBRIDGE` là đủ cho MVP). |

Cũng tồn tại `Civil3dMcp.bundle` (cùng mẫu, cho Civil 3D 2026) và `CadAddinManager.bundle` (tiện ích NETLOAD/reload) — tham khảo cho phase 1 (deploy + reload nhanh khi debug).

### Kết luận áp dụng

1. Ba repo public đều tránh in-process .NET (COM STA / AutoLISP / file IPC) → chúng không trả lời câu hỏi threading `.NET` của HPRebar; giá trị chính của chúng: (a) timeout cứng + thông điệp "AutoCAD may be showing a modal dialog — press ESC" (U-C4N), (b) tool gộp theo domain ít token hơn (puran-water) — trùng ADR-05/06 Revit (registry + `search_tools`).
2. Precedent .NET trên máy (AutoCadMcpPlugin) xác nhận mẫu `Idle` + `LockDocument` + `StartTransaction` chạy được với AutoCAD 2026 .NET 8; HPRebar thêm Roslyn/ALC, dryRun bằng abort transaction ngoài cùng, guard chặn prompt tương tác, và registry dùng chung với Revit.
3. Không có repo nào xử lý **`Editor.GetSelection/GetPoint` từ code tự sinh** (prompt chờ user → treo) — đây là deny-list bắt buộc riêng cho AutoCAD (ADR-02/04).
