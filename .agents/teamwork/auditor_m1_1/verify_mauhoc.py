import sqlite3

db_path = r'C:\Dce Pro\MayHoc\mauhoc.db'
conn = sqlite3.connect(db_path)
cur = conn.cursor()

cur.execute("SELECT name FROM sqlite_master WHERE type='table';")
tables = [r[0] for r in cur.fetchall()]
print('Tables in mauhoc.db:', tables)

for t in tables:
    cur.execute(f"PRAGMA table_info({t});")
    cols = [(r[1], r[2]) for r in cur.fetchall()]
    cur.execute(f"SELECT COUNT(*) FROM {t};")
    cnt = cur.fetchone()[0]
    print(f"Table: {t} (count: {cnt})")
    print("  Columns:", cols)

cur.execute("SELECT Nhan, count(*) FROM MauHoc GROUP BY Nhan ORDER BY count(*) DESC;")
labels = cur.fetchall()
print("\nLabel distribution in MauHoc:")
for lbl, c in labels:
    print(f"  {lbl}: {c}")

cur.execute("SELECT Nguon, count(*) FROM MauHoc GROUP BY Nguon;")
nguon = cur.fetchall()
print("\nNguon distribution in MauHoc:")
for ng, c in nguon:
    print(f"  {ng}: {c}")
