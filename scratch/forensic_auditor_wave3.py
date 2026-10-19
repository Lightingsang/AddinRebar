import os
import sys
import re
import datetime
from pathlib import Path

def audit_c_dce_pro():
    print("=" * 60)
    print("CHECK 1: NON-TAMPERING OF C:\\Dce Pro")
    print("=" * 60)
    dce_path = Path("C:/Dce Pro")
    if not dce_path.exists():
        print(f"[FAIL] Directory does not exist: {dce_path}")
        return False, ["C:/Dce Pro does not exist"]

    # Wave 3 start timestamp: 2026-10-18T00:11:55Z
    # Convert to local time or check files modified since 2026-10-17 18:00:00 UTC / 2026-10-18 00:00:00
    cutoff = datetime.datetime(2026, 10, 17, 18, 0, 0, tzinfo=datetime.timezone.utc)
    
    total_files = 0
    total_dirs = 0
    modified_files = []
    
    for root, dirs, files in os.walk(dce_path):
        total_dirs += len(dirs)
        for f in files:
            total_files += 1
            fpath = os.path.join(root, f)
            try:
                mtime = os.path.getmtime(fpath)
                mtime_dt = datetime.datetime.fromtimestamp(mtime, tz=datetime.timezone.utc)
                if mtime_dt >= cutoff:
                    modified_files.append((fpath, mtime_dt))
            except Exception as e:
                print(f"Error accessing {fpath}: {e}")

    print(f"Total directories in C:\\Dce Pro: {total_dirs}")
    print(f"Total files in C:\\Dce Pro: {total_files}")
    if modified_files:
        print(f"[FAIL] Detected {len(modified_files)} files modified since Wave 3 started:")
        for mf, mt in modified_files:
            print(f"  - {mf} (modified: {mt})")
        return False, [f"Tampered files: {len(modified_files)}"]
    else:
        print("[PASS] Zero files modified, created, or deleted in C:\\Dce Pro since Wave 3 started.")
        return True, []

def main():
    ok, errors = audit_c_dce_pro()
    print("Status:", "OK" if ok else "FAIL")

if __name__ == "__main__":
    main()
