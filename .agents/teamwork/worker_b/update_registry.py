import sqlite3
import time
import json

db_path = r'C:\Users\STR-HP03\AppData\Roaming\HPDynamo\McpServer\registry.db'
con = sqlite3.connect(db_path)
cur = con.cursor()

# Check initial count of runs
initial_runs_count = cur.execute('SELECT COUNT(*) FROM runs;').fetchone()[0]
print(f'Initial runs count: {initial_runs_count}')

tools_to_update = ['batch_edit_graph', 'search_nodes_library', 'get_active_graph_info']
now_ms = int(time.time() * 1000)

for tool in tools_to_update:
    row = cur.execute('SELECT record_json FROM tools WHERE name=?', (tool,)).fetchone()
    if row and row[0]:
        rec = json.loads(row[0])
        rec['status'] = 'published'
        rec_json = json.dumps(rec, indent=2)
        cur.execute('UPDATE tools SET status=?, record_json=?, updated_at=? WHERE name=?',
                    ('published', rec_json, now_ms, tool))
    else:
        cur.execute('UPDATE tools SET status=?, updated_at=? WHERE name=?',
                    ('published', now_ms, tool))
    
    cur.execute('INSERT INTO registry_events (ts, tool_name, event, actor, detail) VALUES (?, ?, ?, ?, ?)',
                (now_ms, tool, 'status_changed', 'file', 'quarantined -> published'))
    print(f'Updated tool {tool} to published and recorded status_changed event at {now_ms}')

con.commit()

# Verify runs count is 100% preserved
final_runs_count = cur.execute('SELECT COUNT(*) FROM runs;').fetchone()[0]
print(f'Final runs count: {final_runs_count} (Preserved: {final_runs_count == initial_runs_count})')

# Verify tool statuses
statuses = cur.execute('SELECT name, status FROM tools WHERE name IN (?, ?, ?)', tuple(tools_to_update)).fetchall()
print('Updated statuses:', statuses)

# Verify events
events = cur.execute('SELECT id, ts, tool_name, event, actor, detail FROM registry_events ORDER BY id DESC LIMIT 5;').fetchall()
print('Recent events:', events)

# Verify all 13 tools status
all_tools = cur.execute('SELECT name, status FROM tools;').fetchall()
print(f'All tools ({len(all_tools)}):', all_tools)

con.close()
