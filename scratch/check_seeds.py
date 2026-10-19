import os
import glob

seed_dir = r"HPGrasshopper\HPGrasshopper.Mcp.Server\Registry\SeedLibrary"
files = glob.glob(os.path.join(seed_dir, "**", "code.cs"), recursive=True)
print(f"Found {len(files)} seed files:")
for f in files:
    with open(f, "r", encoding="utf-8") as fp:
        lines = fp.readlines()
    print(f"  {os.path.relpath(f, seed_dir)}: {len(lines)} lines")
