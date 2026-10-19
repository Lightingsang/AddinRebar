"""In-memory Named Pipe test double for HPGrasshopper.McpBridge.
Listens on \\\\.\\pipe\\hpgrasshopper-mcp-2026 using Win32 API.
Enables headless, unattended CI/CD test runs without requiring GUI Revit or Rhino.
"""
import argparse
import ctypes
import json
import sys
import time

PIPE_ACCESS_DUPLEX = 0x00000003
PIPE_TYPE_BYTE = 0x00000000
PIPE_READMODE_BYTE = 0x00000000
PIPE_WAIT = 0x00000000
PIPE_UNLIMITED_INSTANCES = 255
BUFFER_SIZE = 65536
INVALID_HANDLE_VALUE = -1


def handle_request(line):
    try:
        req = json.loads(line)
    except Exception as ex:
        return {"jsonrpc": "2.0", "id": None, "error": {"code": -32700, "message": f"Parse error: {ex}"}}

    req_id = req.get("id")
    method = req.get("method", "")
    params = req.get("params") or {}

    # 1. Ping
    if method == "ping" or method.endswith(".ping"):
        return {
            "jsonrpc": "2.0",
            "id": req_id,
            "result": {
                "pong": True,
                "revitVersion": "2026",
                "executionEnabled": True,
                "busy": False,
            },
        }

    # 2. Context
    if method == "context" or method.endswith(".context"):
        return {
            "jsonrpc": "2.0",
            "id": req_id,
            "result": {
                "host": "grasshopper",
                "hostVersion": "2026",
                "revitVersion": "2026",
                "executionEnabled": True,
                "docTitle": "MathSlider.gh",
                "docPath": r"C:\Models\MathSlider.gh",
                "units": {"length": "mm"},
                "grasshopper": {
                    "isEditorLoaded": True,
                    "grasshopperVersion": "1.0.0007",
                    "rhinoVersion": "8.10",
                    "activeDefinitionName": "MathSlider.gh",
                    "activeDefinitionPath": r"C:\Models\MathSlider.gh",
                    "componentCount": 4,
                    "wireCount": 2,
                    "selectedComponentCount": 0,
                    "hasUnsavedChanges": False,
                    "isRhinoInsideRevitActive": True,
                    "rhinoInsideRevit": True,
                },
            },
        }

    # 3. Cancel
    if method == "cancel" or method.endswith(".cancel"):
        return {"jsonrpc": "2.0", "id": req_id, "result": {"cancelled": True, "wasRunning": False}}

    # 4. read_grasshopper_canvas
    if method.endswith(".read_grasshopper_canvas"):
        return {
            "jsonrpc": "2.0",
            "id": req_id,
            "result": {
                "document": {
                    "name": "MathSlider.gh",
                    "filePath": r"C:\Models\MathSlider.gh",
                    "open": True,
                    "objectCount": 4,
                    "solutionState": "Compiled",
                },
                "components": [
                    {"guid": "11111111-2222-3333-4444-555555555551", "name": "Number Slider", "nickName": "A", "category": "Math"},
                    {"guid": "22222222-3333-4444-5555-666666666662", "name": "Number Slider", "nickName": "B", "category": "Math"},
                    {"guid": "33333333-4444-5555-6666-777777777773", "name": "Addition", "nickName": "+", "category": "Math"},
                    {"guid": "44444444-5555-6666-7777-888888888884", "name": "Panel", "nickName": "Result", "category": "Params"},
                ],
                "wires": [
                    {"fromComponent": "11111111-2222-3333-4444-555555555551", "toComponent": "33333333-4444-5555-6666-777777777773"},
                    {"fromComponent": "22222222-3333-4444-5555-666666666662", "toComponent": "33333333-4444-5555-6666-777777777773"},
                    {"fromComponent": "33333333-4444-5555-6666-777777777773", "toComponent": "44444444-5555-6666-7777-888888888884"},
                ],
            },
        }

    # 5. run_grasshopper_player
    if method.endswith(".run_grasshopper_player"):
        file_path = params.get("filePath", "")
        if "NonExistent" in file_path:
            return {
                "jsonrpc": "2.0",
                "id": req_id,
                "result": {
                    "success": False,
                    "error": f"Definition file not found: {file_path}",
                },
            }
        return {
            "jsonrpc": "2.0",
            "id": req_id,
            "result": {
                "success": True,
                "durationMs": 35,
                "inputsApplied": 2,
                "outputs": {"Result": 35.0, "Area": 250.0},
            },
        }

    # 6. batch_edit_grasshopper_canvas
    if method.endswith(".batch_edit_grasshopper_canvas"):
        dry_run = bool(params.get("dryRun", False))
        return {
            "jsonrpc": "2.0",
            "id": req_id,
            "result": {
                "success": True,
                "dryRun": dry_run,
                "rolledBack": dry_run,
                "added": 1,
                "removed": 0,
                "connected": 1,
                "disconnected": 0,
            },
        }

    # 7. export_grasshopper_canvas_image
    if method.endswith(".export_grasshopper_canvas_image"):
        png_b64 = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNkYAAAAAYAAjCB0C8AAAAASUVORK5CYII="
        return {
            "jsonrpc": "2.0",
            "id": req_id,
            "result": {
                "success": True,
                "width": 800,
                "height": 600,
                "mode": params.get("mode", "FullCanvas"),
                "base64": png_b64,
                "imageBase64": png_b64,
            },
        }

    # 8. execute_grasshopper_code
    if method == "execute" or method.endswith(".execute"):
        code = params.get("code", "")
        dry_run = bool(params.get("dryRun", False))
        label = params.get("label", "")
        args = params.get("args") or {}

        val = {
            "success": True,
            "dryRun": dry_run,
            "rolledBack": dry_run,
            "executed": label or "script",
            "args": args,
        }
        return {
            "jsonrpc": "2.0",
            "id": req_id,
            "result": {
                "isError": False,
                "value": val,
                "message": None,
                "warnings": [],
                "logs": [],
                "diagnostics": [],
                "changed": {"added": 0, "modified": 0, "deleted": 0},
                "rolledBack": dry_run,
                "timedOut": False,
                "durationMs": 15,
                "truncated": False,
            },
        }

    # 9. analyze
    if method == "analyze" or method.endswith(".analyze"):
        code = params.get("code", "")
        guard_violations = []
        compiles = True
        if "System.Windows.Forms" in code:
            guard_violations.append({
                "line": 1,
                "column": 1,
                "id": "SEC001",
                "message": "Namespace System.Windows.Forms is denied",
            })
            compiles = False
        if "#r " in code:
            guard_violations.append({
                "line": 1,
                "column": 1,
                "id": "SEC002",
                "message": "Directive #r is refused",
            })
            compiles = False

        arg_keys = []
        if 'args.Number("x")' in code or 'args.X("x")' in code or "args." in code:
            arg_keys.append({"key": "x", "accessor": "Number", "line": 1})

        return {
            "jsonrpc": "2.0",
            "id": req_id,
            "result": {
                "compiles": compiles,
                "guardViolations": guard_violations,
                "diagnostics": [],
                "literals": [],
                "argKeys": arg_keys,
                "lineCount": len(code.splitlines()) if code else 1,
                "hasLoops": False,
                "usesTransaction": False,
                "cacheHit": False,
                "qualityAnalysed": True,
                "qualityFindings": [],
            },
        }

    return {"jsonrpc": "2.0", "id": req_id, "error": {"code": -32601, "message": f"Method not found: {method}"}}


def serve(pipe_name=r"\\.\pipe\hpgrasshopper-mcp-2026", timeout_sec=300):
    k32 = ctypes.windll.kernel32
    deadline = time.time() + timeout_sec
    print(f"Mock bridge listening on {pipe_name} (timeout {timeout_sec}s)...", flush=True)

    while time.time() < deadline:
        h_pipe = k32.CreateNamedPipeW(
            pipe_name,
            PIPE_ACCESS_DUPLEX,
            PIPE_TYPE_BYTE | PIPE_READMODE_BYTE | PIPE_WAIT,
            PIPE_UNLIMITED_INSTANCES,
            BUFFER_SIZE,
            BUFFER_SIZE,
            5000,
            None,
        )

        if h_pipe == INVALID_HANDLE_VALUE:
            print("Failed to create named pipe", file=sys.stderr, flush=True)
            time.sleep(0.5)
            continue

        # Wait for client to connect
        connected = k32.ConnectNamedPipe(h_pipe, None)
        if not connected and k32.GetLastError() != 535:  # ERROR_PIPE_CONNECTED
            k32.CloseHandle(h_pipe)
            continue

        buffer = bytearray()
        chunk = ctypes.create_string_buffer(4096)
        bytes_read = ctypes.c_ulong(0)

        while True:
            success = k32.ReadFile(h_pipe, chunk, 4096, ctypes.byref(bytes_read), None)
            if not success or bytes_read.value == 0:
                break

            buffer.extend(chunk.raw[: bytes_read.value])
            while b"\n" in buffer:
                line, buffer = buffer.split(b"\n", 1)
                line_str = line.decode("utf-8", errors="replace").strip()
                if not line_str:
                    continue

                resp = handle_request(line_str)
                resp_bytes = json.dumps(resp, ensure_ascii=False).encode("utf-8") + b"\n"
                bytes_written = ctypes.c_ulong(0)
                k32.WriteFile(h_pipe, resp_bytes, len(resp_bytes), ctypes.byref(bytes_written), None)
                k32.FlushFileBuffers(h_pipe)

        k32.DisconnectNamedPipe(h_pipe)
        k32.CloseHandle(h_pipe)


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="HPGrasshopper Mock Named Pipe Bridge")
    parser.add_argument("--pipe", default=r"\\.\pipe\hpgrasshopper-mcp-2026", help="Named pipe path")
    parser.add_argument("--timeout", type=int, default=300, help="Server timeout in seconds")
    args = parser.parse_args()
    serve(args.pipe, args.timeout)
