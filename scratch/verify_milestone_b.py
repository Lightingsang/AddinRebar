import json
import glob
import os
import sqlite3

print("=== 1. SOURCE TREE SEED TOOL.JSON FILES ===")
source_seeds = glob.glob(r"HPDynamo/HPDynamo.Mcp.Server/Registry/SeedLibrary/**/tool.json", recursive=True)
print(f"Total source tool.json found: {len(source_seeds)}")
source_statuses = {}
for p in sorted(source_seeds):
    with open(p, "r", encoding="utf-8") as f:
        data = json.load(f)
    name = data.get("name", os.path.basename(os.path.dirname(p)))
    status = data.get("status")
    source_statuses[name] = status
    print(f"  {name}: status = {status}")

print("\n=== 2. APPDATA TOOLS-LIBRARY TOOL.JSON FILES ===")
appdata = os.path.expandvars(r"%APPDATA%\HPDynamo\McpServer\tools-library")
appdata_seeds = glob.glob(os.path.join(appdata, "**", "tool.json"), recursive=True)
print(f"Total appdata tool.json found: {len(appdata_seeds)}")
appdata_statuses = {}
for p in sorted(appdata_seeds):
    with open(p, "r", encoding="utf-8") as f:
        data = json.load(f)
    name = data.get("name", os.path.basename(os.path.dirname(p)))
    status = data.get("status")
    appdata_statuses[name] = status
    print(f"  {name}: status = {status}")

print("\n=== 3. SQLITE DATABASE CHECKS ===")
db_path = os.path.expandvars(r"%APPDATA%\HPDynamo\McpServer\registry.db")
print("Database path:", db_path)
print("Database exists:", os.path.exists(db_path))

conn = sqlite3.connect(db_path)
cur = conn.cursor()

cur.execute("PRAGMA integrity_check;")
print("Integrity check:", cur.fetchall())

cur.execute("PRAGMA foreign_key_check;")
print("Foreign key check:", cur.fetchall())

cur.execute("SELECT name FROM sqlite_master WHERE type='table';")
tables = [row[0] for row in cur.fetchall()]
print("Tables:", tables)

cur.execute("SELECT COUNT(*) FROM tools;")
tools_count = cur.fetchone()[0]
print(f"Tools total count: {tools_count}")

cur.execute("SELECT name, status, version FROM tools ORDER BY name;")
db_tools = cur.fetchall()
for row in db_tools:
    print(f"  tool: {row[0]}, status: {row[1]}, version: {row[2]}")

cur.execute("SELECT COUNT(*) FROM registry_events;")
events_count = cur.fetchone()[0]
print(f"Registry events count: {events_count}")

cur.execute("SELECT id, tool_name, event_type, timestamp, payload FROM registry_events WHERE event_type='status_changed' ORDER BY id;")
status_events = cur.fetchall()
print(f"status_changed events count: {len(status_events)}")
for row in status_events:
    print(f"  id: {row[0]}, tool: {row[1]}, ts: {row[3]}, payload: {row[4]}")

cur.execute("SELECT COUNT(*) FROM runs;")
runs_count = cur.fetchone()[0]
print(f"Runs total count: {runs_count}")

cur.execute("SELECT status, COUNT(*) FROM runs GROUP BY status;")
print("Runs grouped by status:", cur.fetchall())

# Let's inspect the 3 specific tools in runs table
for target in ["batch_edit_graph", "search_nodes_library", "get_active_graph_info"]:
    cur.execute("SELECT COUNT(*) FROM runs WHERE tool_name=?;", (target,))
    t_count = cur.fetchone()[0]
    cur.execute("SELECT status, COUNT(*) FROM runs WHERE tool_name=? GROUP BY status;", (target,))
    t_group = cur.fetchall()
    print(f"  Runs for {target}: total = {t_count}, breakdown = {t_group}")

conn.close()
