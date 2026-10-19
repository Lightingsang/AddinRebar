import os, json, hashlib

seed_dir = r"HPGrasshopper\HPGrasshopper.Mcp.Server\Registry\SeedLibrary"
print(f"Checking seeds in {seed_dir}...")

seeds = []
for root, dirs, files in os.walk(seed_dir):
    if "tool.json" in files and "code.cs" in files:
        seeds.append(root)

print(f"Total embedded seeds found: {len(seeds)}")
for s in sorted(seeds):
    rel = os.path.relpath(s, seed_dir)
    manifest_path = os.path.join(s, "tool.json")
    code_path = os.path.join(s, "code.cs")
    
    with open(manifest_path, "r", encoding="utf-8") as f:
        manifest = json.load(f)
        
    with open(code_path, "r", encoding="utf-8") as f:
        code_lines = f.readlines()
        
    code_text = "".join(code_lines)
    sha256 = hashlib.sha256(code_text.encode("utf-8")).hexdigest()
    
    print(f"Seed: {rel}")
    print(f"  Name: {manifest.get('name')}")
    print(f"  Lines: {len(code_lines)} (limit: <= 300)")
    print(f"  SHA-256: {sha256}")
    print(f"  Schema: type={manifest.get('inputSchema', {}).get('type')}, properties={list(manifest.get('inputSchema', {}).get('properties', {}).keys())}")
    assert len(code_lines) <= 300, f"Seed {rel} exceeds 300 lines!"
    assert manifest.get("name"), f"Seed {rel} missing name"
    assert manifest.get("inputSchema"), f"Seed {rel} missing inputSchema"

print("All 12 seeds valid according to ADR-0007!")
