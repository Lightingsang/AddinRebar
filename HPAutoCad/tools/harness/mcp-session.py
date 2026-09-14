"""One long-lived stdio MCP session with an HP MCP server exe, for harnesses that need to watch the server
react over time (notifications/tools/list_changed after a CLI approve, cancel_execution racing an execute).
`mcp-call.py` spawns one process per request; this keeps the process and exposes send/wait separately.

    from mcp_session import Server        # (imported by file path in live-verify.py)
    s = Server(exe); s.initialize(); s.tool("get_autocad_context", {"includeSelection": True}); s.close()
"""
import json, os, subprocess, threading, time


class Server:
    def __init__(self, exe, env=None, name="harness"):
        self.exe = exe
        self.proc = subprocess.Popen([exe], stdin=subprocess.PIPE, stdout=subprocess.PIPE, stderr=subprocess.PIPE, env=env or dict(os.environ))
        self.stderr = []
        threading.Thread(target=lambda: self.stderr.append(self.proc.stderr.read()), daemon=True).start()
        self.notifications = []      # (time, method, params) — everything the server pushed without an id
        self.pending = {}            # id -> response that arrived while waiting for another id
        self.next_id = 0
        self.name = name

    # ---- wire -------------------------------------------------------------------------------------------------
    def send(self, method, params=None):
        self.next_id += 1
        msg = {"jsonrpc": "2.0", "id": self.next_id, "method": method}
        if params is not None:
            msg["params"] = params
        self.proc.stdin.write((json.dumps(msg) + "\n").encode("utf-8"))
        self.proc.stdin.flush()
        return self.next_id

    def notify(self, method, params=None):
        msg = {"jsonrpc": "2.0", "method": method}
        if params is not None:
            msg["params"] = params
        self.proc.stdin.write((json.dumps(msg) + "\n").encode("utf-8"))
        self.proc.stdin.flush()

    def _read_one(self):
        line = self.proc.stdout.readline()
        if not line:
            err = b"".join(self.stderr).decode("utf-8", "replace")[-4000:]
            raise RuntimeError(f"{self.name}: server closed stdout; stderr tail:\n{err}")
        try:
            data = json.loads(line.decode("utf-8"))
        except json.JSONDecodeError:
            return None
        if "id" in data and ("result" in data or "error" in data):
            self.pending[data["id"]] = data
        elif "method" in data:
            self.notifications.append((time.time(), data["method"], data.get("params")))
        return data

    def wait(self, msg_id, timeout=180.0):
        deadline = time.time() + timeout
        while time.time() < deadline:
            if msg_id in self.pending:
                return self.pending.pop(msg_id)
            self._read_one()
        raise TimeoutError(f"{self.name}: no response for id {msg_id}")

    def rpc(self, method, params=None, timeout=180.0):
        return self.wait(self.send(method, params), timeout)

    def drain(self, seconds):
        """Read whatever the server pushes for a while (used to catch tools/list_changed)."""
        end = time.time() + seconds
        # stdout.readline blocks, so poll through a tiny request instead: ping the tool list.
        while time.time() < end:
            self.rpc("ping", None, timeout=30)
            time.sleep(0.25)

    # ---- MCP -------------------------------------------------------------------------------------------------
    def initialize(self):
        init = self.rpc("initialize", {"protocolVersion": "2025-06-18", "capabilities": {}, "clientInfo": {"name": self.name, "version": "0.1"}})
        self.notify("notifications/initialized")
        return init.get("result", {})

    def tools(self):
        return {t["name"]: t for t in self.rpc("tools/list", {}).get("result", {}).get("tools", [])}

    def call(self, name, args=None, timeout=180.0):
        """Raw CallToolResult (result dict with content/isError, or a JSON-RPC error)."""
        return self.rpc("tools/call", {"name": name, "arguments": args or {}}, timeout)

    def tool(self, name, args=None, timeout=180.0):
        """Parsed tool text: the JSON object the tool returned, or {isError, message} for a plain-text refusal."""
        r = self.call(name, args, timeout)
        return parse_tool_result(r)

    def prompt(self, name, args=None):
        return self.rpc("prompts/get", {"name": name, "arguments": args or {}}).get("result", {})

    def list_changed_since(self, t0):
        return [n for n in self.notifications if n[0] >= t0 and n[1] == "notifications/tools/list_changed"]

    def close(self):
        try:
            self.proc.stdin.close()
            self.proc.wait(timeout=10)
        except Exception:
            self.proc.kill()


def parse_tool_result(r):
    if "error" in r:
        return {"isError": True, "message": r["error"].get("message", ""), "code": r["error"].get("code"), "rpcError": True}
    result = r.get("result", {})
    text = "".join(c.get("text", "") for c in result.get("content", []) if c.get("type") == "text")
    try:
        data = json.loads(text)
        if isinstance(data, dict):
            if result.get("isError") and "isError" not in data:
                data["isError"] = True
            return data
        return {"isError": bool(result.get("isError")), "value": data}
    except json.JSONDecodeError:
        return {"isError": bool(result.get("isError")), "message": text}
