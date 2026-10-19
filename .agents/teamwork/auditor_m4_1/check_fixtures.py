import os
import hashlib

dirs = [
    r"HPGrasshopper/HPGrasshopper.McpBridge.Tests/Fixtures",
    r"HPGrasshopper/tools/harness/fixtures",
    r"HPGrasshopper/definitions"
]

def hash_file(path):
    h = hashlib.sha256()
    with open(path, "rb") as f:
        while chunk := f.read(65536):
            h.update(chunk)
    return h.hexdigest()

files_info = {}
for d in dirs:
    if not os.path.exists(d): continue
    for f in os.listdir(d):
        if f.endswith(".gh") or f.endswith(".ghx"):
            full = os.path.join(d, f)
            stat = os.stat(full)
            files_info[full] = {
                "mtime": stat.st_mtime,
                "size": stat.st_size,
                "sha256": hash_file(full)
            }
            print(f"{full} | Size: {stat.st_size} | SHA256: {files_info[full]['sha256'][:16]} | Mtime: {stat.st_mtime}")

print(f"\nTotal golden fixture files tracked: {len(files_info)}")
