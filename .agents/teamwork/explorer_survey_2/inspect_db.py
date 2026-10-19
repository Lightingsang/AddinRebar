import sqlite3
import json
import sys

# Ensure UTF-8 output
sys.stdout.reconfigure(encoding='utf-8')

db_path = r"C:\Dce Pro\MayHoc\mauhoc.db"
conn = sqlite3.connect(db_path)
cur = conn.cursor()

print("=== TABLES ===")
cur.execute("SELECT name, sql FROM sqlite_master WHERE type='table'")
tables = cur.fetchall()
for tname, tsql in tables:
    print(f"\n--- TABLE: {tname} ---")
    print(tsql)
    cur.execute(f"SELECT COUNT(*) FROM [{tname}]")
    cnt = cur.fetchone()[0]
    print(f"Row count: {cnt}")

print("\n=== INDEXES ===")
cur.execute("SELECT name, tbl_name, sql FROM sqlite_master WHERE type='index'")
for iname, itbl, isql in cur.fetchall():
    print(f"{iname} ON {itbl}: {isql}")

print("\n=== SAMPLE ROWS ===")
for tname, _ in tables:
    if tname == 'sqlite_sequence': continue
    print(f"\n--- SAMPLE 5 ROWS FROM {tname} ---")
    cur.execute(f"PRAGMA table_info([{tname}])")
    cols = [c[1] for c in cur.fetchall()]
    print("Cols:", cols)
    cur.execute(f"SELECT * FROM [{tname}] LIMIT 5")
    rows = cur.fetchall()
    for r in rows:
        row_dict = dict(zip(cols, r))
        print(json.dumps(row_dict, ensure_ascii=False, indent=2))

print("\n=== CauHinhModel ALL ROWS ===")
cur.execute("SELECT * FROM CauHinhModel")
cols = [c[0] for c in cur.description]
for r in cur.fetchall():
    print(dict(zip(cols, r)))

print("\n=== LichSuSua ALL ROWS ===")
cur.execute("SELECT * FROM LichSuSua")
cols = [c[0] for c in cur.description]
for r in cur.fetchall():
    print(dict(zip(cols, r)))

print("\n=== DISTINCT LABELS (Nhan) / SOURCES (Nguon) IN MauHoc ===")
cur.execute("SELECT Nhan, COUNT(*) FROM MauHoc GROUP BY Nhan ORDER BY COUNT(*) DESC LIMIT 20")
for nhan, cnt in cur.fetchall():
    print(f"Label: {nhan} -> {cnt} samples")

cur.execute("SELECT Nguon, COUNT(*) FROM MauHoc GROUP BY Nguon ORDER BY COUNT(*) DESC")
for nguon, cnt in cur.fetchall():
    print(f"Nguon: {nguon} -> {cnt} samples")

conn.close()
