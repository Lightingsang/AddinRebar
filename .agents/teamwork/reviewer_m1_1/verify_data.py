import sqlite3
import json
import os

print("=== VERIFYING mauhoc.db ===")
db_path = r'C:\Dce Pro\MayHoc\mauhoc.db'
conn = sqlite3.connect(db_path)
cursor = conn.cursor()

# Tables
cursor.execute("SELECT name FROM sqlite_master WHERE type='table';")
tables = [row[0] for row in cursor.fetchall()]
print(f"Tables found: {tables}")

# MauHoc count
cursor.execute("SELECT COUNT(*) FROM MauHoc;")
total_mauhoc = cursor.fetchone()[0]
print(f"Total MauHoc rows: {total_mauhoc}")

# Distribution of Nhan
cursor.execute("SELECT Nhan, COUNT(*) FROM MauHoc GROUP BY Nhan ORDER BY COUNT(*) DESC;")
labels = cursor.fetchall()
print("Label distribution in MauHoc:")
for nhan, cnt in labels:
    print(f"  {nhan:<8}: {cnt}")

# Nguon distribution
cursor.execute("SELECT Nguon, COUNT(*) FROM MauHoc GROUP BY Nguon;")
nguon = cursor.fetchall()
print(f"Nguon distribution: {nguon}")

# LichSuSua count
cursor.execute("SELECT COUNT(*) FROM LichSuSua;")
total_lichsu = cursor.fetchone()[0]
print(f"Total LichSuSua rows: {total_lichsu}")

# CauHinhModel rows
cursor.execute("SELECT Ten, DuongDan, SoMau, ThoiGian FROM CauHinhModel;")
cauhinh = cursor.fetchall()
print(f"CauHinhModel entries: {cauhinh}")

conn.close()

print("\n=== VERIFYING DuAnMau.autostr ===")
autostr_path = r'C:\Dce Pro\AutoStr\DuAnMau.autostr'
with open(autostr_path, 'r', encoding='utf-8-sig') as f:
    autostr_data = json.load(f)

print(f"Top-level keys in DuAnMau.autostr ({len(autostr_data)} keys):")
for k in sorted(autostr_data.keys()):
    v = autostr_data[k]
    if isinstance(v, (str, int, float, bool)):
        print(f"  {k}: {v}")
    elif isinstance(v, dict):
        print(f"  {k}: [dict with {len(v)} keys: {list(v.keys())[:3]}...]")
    elif isinstance(v, list):
        print(f"  {k}: [list of {len(v)} items]")
