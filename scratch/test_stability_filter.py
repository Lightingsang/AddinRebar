import sqlite3
import os

db_path = os.path.expandvars(r"%APPDATA%\HPDynamo\McpServer\registry.db")
conn = sqlite3.connect(db_path)
cur = conn.cursor()

stability_filter = """
kind <> 'test' AND (error IS NULL OR error NOT LIKE 'Argument%Exception:%')
AND ts >= COALESCE((SELECT MAX(e.ts) FROM registry_events e WHERE e.tool_name = runs.tool_name AND e.event IN ('approved', 'published', 'restore', 'proposed_version', 'imported', 'status_changed')), 0)
"""

for target in ["batch_edit_graph", "search_nodes_library", "get_active_graph_info"]:
    cur.execute(f"SELECT COUNT(*) FROM runs WHERE tool_name = ? AND {stability_filter};", (target,))
    active_stability_runs = cur.fetchone()[0]
    cur.execute("SELECT COUNT(*) FROM runs WHERE tool_name = ?;", (target,))
    total_runs = cur.fetchone()[0]
    print(f"Tool '{target}': total runs in history = {total_runs}, active runs under StabilityRunFilter = {active_stability_runs}")

conn.close()
