import os
import hashlib
import json
import datetime

base_dir = r"C:\kata_pro"
results = []
for root, dirs, files in os.walk(base_dir):
    for f in sorted(files):
        full_path = os.path.join(root, f)
        rel_path = os.path.relpath(full_path, base_dir)
        size = os.path.getsize(full_path)
        mtime = os.path.getmtime(full_path)
        mtime_str = datetime.datetime.fromtimestamp(mtime, tz=datetime.timezone.utc).isoformat()
        with open(full_path, "rb") as fp:
            data = fp.read()
            h = hashlib.sha256(data).hexdigest().lower()
        results.append({
            "rel_path": rel_path,
            "bytes": size,
            "sha256": h,
            "mtime_utc": mtime_str
        })

out_file = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_explorer_m1_1\baseline_sha256.json"
with open(out_file, "w", encoding="utf-8") as fp:
    json.dump(results, fp, indent=2)

print(f"Computed baseline for {len(results)} files. Written to {out_file}")
for r in results:
    if r["rel_path"].endswith(".dll"):
        print(f"{r['rel_path']}: {r['bytes']} bytes, SHA256={r['sha256']}")
