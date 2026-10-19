import os
import re
import sys

def scan_all_src():
    src_dir = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim\src"
    all_cs_files = []
    for root, dirs, files in os.walk(src_dir):
        # skip bin and obj
        if "bin" in root or "obj" in root:
            continue
        for f in files:
            if f.endswith(".cs"):
                all_cs_files.append(os.path.join(root, f))
                
    print(f"Scanning all {len(all_cs_files)} C# files in {src_dir}...")
    
    # 1. C2: file length <= 300
    c2_violations = []
    for path in all_cs_files:
        with open(path, "r", encoding="utf-8") as f:
            lines = f.readlines()
        if len(lines) > 300:
            c2_violations.append((path, len(lines)))
            
    print(f"C2 (>300 lines) violations: {len(c2_violations)}")
    for p, count in c2_violations:
        print(f"  {os.path.relpath(p, src_dir)}: {count} lines")
        
    # 2. Q-B1: commented code
    code_patterns = [
        re.compile(r"^\s*//\s*(var|int|string|bool|double|return|if|for|foreach|while|await|throw|new|public|private|protected)\s+"),
        re.compile(r"^\s*//\s*([a-zA-Z0-9_]+\s*=\s*[^;]+;)"),
        re.compile(r"^\s*//\s*([a-zA-Z0-9_.]+\([^)]*\);)"),
    ]
    qb1_violations = []
    for path in all_cs_files:
        with open(path, "r", encoding="utf-8") as f:
            lines = f.readlines()
        for idx, line in enumerate(lines, 1):
            stripped = line.strip()
            if stripped.startswith("///"):
                continue
            for pat in code_patterns:
                if pat.match(line):
                    if "e.g." in line or "example" in line.lower() or "todo" in line.lower() or "note" in line.lower():
                        continue
                    qb1_violations.append((path, idx, stripped))
    print(f"Q-B1 (commented code) violations: {len(qb1_violations)}")
    for p, idx, s in qb1_violations[:10]:
        print(f"  {os.path.relpath(p, src_dir)}:{idx}: {s}")
        
    # 3. Q-B2: empty catches
    catch_pattern = re.compile(r"catch\s*(\([^\)]*\))?\s*\{([^}]*)\}", re.DOTALL)
    qb2_violations = []
    for path in all_cs_files:
        with open(path, "r", encoding="utf-8") as f:
            content = f.read()
        for match in catch_pattern.finditer(content):
            body = match.group(2).strip()
            if not body or not any(c.isalnum() for c in body):
                qb2_violations.append((path, match.group(0)))
            else:
                code_lines = [l.strip() for l in body.split("\n") if l.strip() and not l.strip().startswith("//")]
                if not code_lines:
                    comment_text = body.replace("//", "").strip()
                    if len(comment_text) < 5:
                        qb2_violations.append((path, match.group(0)))
    print(f"Q-B2 (empty catch) violations: {len(qb2_violations)}")
    for p, c in qb2_violations:
        print(f"  {os.path.relpath(p, src_dir)}: {c}")

if __name__ == "__main__":
    scan_all_src()
