import os
import re

repo_root = r'g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata'
empty_catches = []

for root, dirs, files in os.walk(repo_root):
    if "obj" in root or "bin" in root:
        continue
    for file in files:
        if file.endswith(".cs"):
            fpath = os.path.join(root, file)
            with open(fpath, 'r', encoding='utf-8') as f:
                content = f.read()
            # find catch blocks
            matches = list(re.finditer(r'catch\s*(\([^\)]*\))?\s*\{([^}]*)\}', content, re.MULTILINE | re.DOTALL))
            for m in matches:
                body = m.group(2).strip()
                # If body has no code or only comments:
                if not body:
                    empty_catches.append((os.path.relpath(fpath, repo_root), "Completely empty catch", m.group(0)))
                else:
                    # check if there's any non-comment code
                    lines = [l.strip() for l in body.splitlines() if l.strip()]
                    non_comment_lines = [l for l in lines if not l.startswith("//") and not l.startswith("/*") and not l.startswith("*")]
                    if not non_comment_lines:
                        # body has only comments. That is valid under Q-B2 ONLY if the comment explains the reason!
                        comment_text = " ".join(lines)
                        if len(comment_text) < 10:
                            empty_catches.append((os.path.relpath(fpath, repo_root), "Unexplained empty catch", m.group(0)))

print(f"Empty catch blocks found: {len(empty_catches)}")
for p, reason, snippet in empty_catches:
    print(f"  {p}: {reason}")
    print(f"    {snippet[:80]}...")
