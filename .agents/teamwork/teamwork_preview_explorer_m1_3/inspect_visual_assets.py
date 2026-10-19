import os
import hashlib

dirs = [
    r'C:\kata_pro\dangthep',
    r'C:\kata_pro\Cot',
    r'C:\kata_pro\Icon',
    r'C:\kata_pro\IMG'
]

print("=== VISUAL ASSETS INVENTORY ===")
total_assets = 0
for d in dirs:
    if not os.path.exists(d):
        print(f"Directory not found: {d}")
        continue
    files = sorted(os.listdir(d))
    print(f"\nDirectory: {d} ({len(files)} files)")
    
    exts = {}
    for f in files:
        ext = os.path.splitext(f)[1].lower()
        exts[ext] = exts.get(ext, 0) + 1
    print(f"Extensions breakdown: {exts}")
    
    # Subdirectories?
    subdirs = [f for f in files if os.path.isdir(os.path.join(d, f))]
    if subdirs:
        print(f"Subdirectories: {subdirs}")
        for sd in subdirs:
            sd_path = os.path.join(d, sd)
            sd_files = sorted(os.listdir(sd_path))
            sd_exts = {}
            for sdf in sd_files:
                ext = os.path.splitext(sdf)[1].lower()
                sd_exts[ext] = sd_exts.get(ext, 0) + 1
            print(f"  Subdir: {sd} ({len(sd_files)} files): {sd_exts}")
    
    # Sample file listing
    non_dir_files = [f for f in files if os.path.isfile(os.path.join(d, f))]
    print(f"Sample files (first 10 of {len(non_dir_files)}): {non_dir_files[:10]}")
    print(f"Sample files (last 5): {non_dir_files[-5:]}")
    total_assets += len(files)

print(f"\nTotal visual asset entries inspected: {total_assets}")
