"""The Revit run of the script-quality live check — now the shared harness: McpShared/tools/live-verify-quality.py."""
import os
import subprocess
import sys

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", ".."))
sys.exit(subprocess.call([sys.executable, os.path.join(ROOT, "McpShared", "tools", "live-verify-quality.py"), "--host", "revit", *sys.argv[1:]]))
