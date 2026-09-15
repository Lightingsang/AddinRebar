"""Minimal stdio MCP harness for any HP MCP server exe (Revit, AutoCAD, Navisworks): spawn it, run initialize, then one request.

Usage:
  python mcp_call.py <exe> tools/list [--env KEY=VAL ...] [--out file.json]
  python mcp_call.py <exe> tools/call <name> '<json args>' [--env ...]
  python mcp_call.py <exe> initialize
Prints the JSON-RPC result (or error) as JSON.
"""
import json, os, subprocess, sys, threading, time


def rpc(proc, msg_id, method, params=None):
    msg = {"jsonrpc": "2.0", "id": msg_id, "method": method}
    if params is not None:
        msg["params"] = params
    proc.stdin.write((json.dumps(msg) + "\n").encode("utf-8"))
    proc.stdin.flush()
    deadline = time.time() + 180
    while time.time() < deadline:
        line = proc.stdout.readline()
        if not line:
            time.sleep(0.2)
            raise SystemExit("server closed stdout; stderr:\n" + b"".join(stderr_chunks).decode("utf-8", "replace")[-4000:])
        try:
            data = json.loads(line.decode("utf-8"))
        except json.JSONDecodeError:
            continue
        if data.get("id") == msg_id:
            return data
    raise SystemExit("timeout waiting for " + method)


def notify(proc, method, params=None):
    msg = {"jsonrpc": "2.0", "method": method}
    if params is not None:
        msg["params"] = params
    proc.stdin.write((json.dumps(msg) + "\n").encode("utf-8"))
    proc.stdin.flush()


def main():
    args = sys.argv[1:]
    env = dict(os.environ)
    out = None
    rest = []
    i = 0
    while i < len(args):
        if args[i] == "--env":
            k, v = args[i + 1].split("=", 1)
            env[k] = v
            i += 2
        elif args[i] == "--out":
            out = args[i + 1]
            i += 2
        else:
            rest.append(args[i])
            i += 1
    exe, method = rest[0], rest[1]
    proc = subprocess.Popen([exe], stdin=subprocess.PIPE, stdout=subprocess.PIPE, stderr=subprocess.PIPE, env=env)
    # The server logs to stderr; drain it on a thread so a chatty server can never fill the pipe and stall.
    stderr_chunks = []
    threading.Thread(target=lambda: stderr_chunks.append(proc.stderr.read()), daemon=True).start()
    try:
        init = rpc(proc, 1, "initialize", {
            "protocolVersion": "2025-06-18",
            "capabilities": {},
            "clientInfo": {"name": "mcp_call", "version": "0.1"},
        })
        notify(proc, "notifications/initialized")
        if method == "initialize":
            result = init
        elif method == "tools/list":
            result = rpc(proc, 2, "tools/list", {})
        elif method == "tools/call":
            name = rest[2]
            arguments = json.loads(rest[3]) if len(rest) > 3 else {}
            result = rpc(proc, 2, "tools/call", {"name": name, "arguments": arguments})
        else:
            result = rpc(proc, 2, method, json.loads(rest[2]) if len(rest) > 2 else {})
        text = json.dumps(result, indent=2, ensure_ascii=False)
        if out:
            with open(out, "w", encoding="utf-8") as f:
                f.write(text)
            tools = result.get("result", {}).get("tools")
            if tools is not None:
                print(f"wrote {out}: {len(tools)} tools: " + ", ".join(sorted(t['name'] for t in tools)))
            else:
                print(f"wrote {out}")
        else:
            print(text)
    finally:
        try:
            proc.stdin.close()
            proc.wait(timeout=10)
        except Exception:
            proc.kill()


if __name__ == "__main__":
    main()
