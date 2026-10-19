import json
import os
import subprocess
import sys

sys.stdout.reconfigure(encoding="utf-8")

def run_cad_script(code, label="Reviewer CAD Audit"):
    req_args = {
        "code": code,
        "transaction": "none",
        "dryRun": False,
        "timeoutSeconds": 60,
        "label": label
    }
    cmd = [
        sys.executable,
        r"G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\McpShared\tools\mcp-call.py",
        r"G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPAutoCad\HPAutoCad.Mcp.Server\bin\Debug\net10.0\HPAutoCad.Mcp.Server.exe",
        "tools/call",
        "execute_autocad_code",
        json.dumps(req_args)
    ]
    p = subprocess.run(cmd, capture_output=True, text=True)
    if p.returncode != 0:
        print("Subprocess error:", p.stderr)
        return None
    try:
        data = json.loads(p.stdout)
        if "result" in data and "content" in data["result"]:
            txt = data["result"]["content"][0]["text"]
            try:
                return json.loads(txt)
            except:
                return txt
        return data
    except Exception as e:
        print("Parse error:", e, "stdout:", p.stdout)
        return None

if __name__ == '__main__':
    print("Testing AutoCAD MCP connection...")
    res = run_cad_script("return doc.Name;", "Check AutoCAD Doc")
    print("AutoCAD Doc:", res)
