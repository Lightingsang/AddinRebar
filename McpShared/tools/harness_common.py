"""Shared plumbing of the HP MCP Python harnesses (Revit, AutoCAD, Navisworks): a UTF-8 console, the stdio
session helper (`mcp-session.py`, loaded by path because of the hyphen) and one `Checklist` that prints
PASS/FAIL/SKIP lines, keeps the results, saves JSON beside the run and produces the summary + exit code every
wrapper script parses. Scenario code stays in each host's harness; only this bookkeeping is shared.

    sys.path.insert(0, os.path.join(HERE, "..", "..", "..", "McpShared", "tools"))
    from harness_common import Checklist, Server, parse_tool_result, ok, short, utf8_console
    utf8_console()
    cl = Checklist(); check, skip, save = cl.check, cl.skip, cl.save
    ...
    sys.exit(cl.finish("summary-main", phase="main"))
"""
import importlib.util
import json
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))


def utf8_console():
    """Windows PowerShell 5.1 hands Python a cp1252 console; the servers answer with `≥`, `→`, Vietnamese."""
    for stream in (sys.stdout, sys.stderr):
        try:
            stream.reconfigure(encoding="utf-8", errors="replace")
        except (AttributeError, ValueError):
            pass


def load_mcp_session():
    spec = importlib.util.spec_from_file_location("mcp_session", os.path.join(HERE, "mcp-session.py"))
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


mcp_session = load_mcp_session()
Server = mcp_session.Server
parse_tool_result = mcp_session.parse_tool_result


def ok(r):
    """A parsed tool result that is neither a tool error nor a JSON-RPC error."""
    return not r.get("isError") and not r.get("rpcError")


def short(o, n=260):
    """One-line JSON preview for the detail column."""
    t = json.dumps(o, ensure_ascii=False, default=str)
    return t if len(t) <= n else t[:n] + "…"


class Checklist:
    """PASS/FAIL/SKIP lines + the JSON summary the PowerShell wrappers read (`{"passed": …, "failed": [...]}`)."""

    def __init__(self, out_dir=None, detail_limit=600, line_limit=500):
        self.out_dir = out_dir
        self.results = []
        self._detail_limit = detail_limit
        self._line_limit = line_limit

    def check(self, name, cond, detail=""):
        self.results.append({"name": name, "pass": bool(cond), "detail": str(detail)[:self._detail_limit]})
        print(f"{'PASS' if cond else 'FAIL'} {name} {detail}"[:self._line_limit], flush=True)
        return bool(cond)

    def skip(self, name, reason):
        self.results.append({"name": name, "pass": True, "skipped": True, "detail": reason})
        print(f"SKIP {name} {reason}", flush=True)

    def save(self, name, obj):
        """JSON beside the run when the wrapper gave an output folder; silently nothing otherwise."""
        if not self.out_dir:
            return
        os.makedirs(self.out_dir, exist_ok=True)
        with open(os.path.join(self.out_dir, name + ".json"), "w", encoding="utf-8") as f:
            json.dump(obj, f, indent=2, ensure_ascii=False, default=str)

    @property
    def failed(self):
        return [r["name"] for r in self.results if not r["pass"]]

    def summary(self, **extra):
        skipped = [r["name"] for r in self.results if r.get("skipped")]
        passed = sum(1 for r in self.results if r["pass"] and not r.get("skipped"))
        return {**extra, "passed": passed, "skipped": len(skipped), "total": len(self.results), "failed": self.failed, "skippedNames": skipped}

    def finish(self, save_as=None, **extra):
        """Save (optional), print the one-line JSON summary, return the process exit code (0 = no FAIL)."""
        summary = self.summary(**extra)
        if save_as:
            self.save(save_as, {"summary": summary, "results": self.results})
        print(json.dumps(summary), flush=True)
        return 0 if not summary["failed"] else 1


if __name__ == "__main__":
    # smoke of the bookkeeping itself: python McpShared/tools/harness_common.py
    utf8_console()
    cl = Checklist()
    cl.check("ok() accepts a plain value", ok({"value": 1}))
    cl.check("ok() rejects tool and rpc errors", not ok({"isError": True}) and not ok({"rpcError": True}))
    cl.check("short() truncates with an ellipsis", short({"k": "x" * 300}, 20).endswith("…"))
    cl.skip("nothing else", "self-test")
    code = cl.finish(phase="selftest")
    assert code == 0 and cl.summary()["passed"] == 3 and cl.summary()["skipped"] == 1
    sys.exit(code)
