import sqlite3
import sys
sys.stdout.reconfigure(encoding='utf-8')

db_path = r"C:\Dce Pro\MayHoc\mauhoc.db"
con = sqlite3.connect(db_path)
cur = con.cursor()
cur.execute("SELECT name, sql FROM sqlite_master WHERE type='table';")
tables = cur.fetchall()

print(f"Total tables: {len(tables)}")
for name, sql in tables:
    print(f"\n=== Table: {name} ===")
    print(sql)
    cur.execute(f"SELECT COUNT(*) FROM [{name}]")
    count = cur.fetchone()[0]
    print(f"Row count: {count}")
    cur.execute(f"SELECT * FROM [{name}] LIMIT 3")
    rows = cur.fetchall()
    print("Sample rows:")
    for r in rows:
        print(f"  {r}")

con.close()
