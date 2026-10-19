import os
import re

bridge_dir = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDynamo\HPDynamo.McpBridge"

print("=== METHOD LENGTH SCAN (Rule M3: <= 50 lines review trigger) ===")

for root, dirs, files in os.walk(bridge_dir):
    if "bin" in root or "obj" in root:
        continue
    for f in files:
        if f.endswith(".cs"):
            full_path = os.path.join(root, f)
            rel_path = os.path.relpath(full_path, bridge_dir)
            with open(full_path, "r", encoding="utf-8") as fp:
                lines = fp.readlines()

            brace_stack = []
            current_method = None
            method_start = 0

            for idx, line in enumerate(lines, 1):
                stripped = line.strip()
                # Check for method start
                if ("(" in stripped and not stripped.startswith("//") and not stripped.startswith("/*")
                    and not "delegate" in stripped and not "record" in stripped and not "class" in stripped
                    and any(mod in stripped for mod in ["public", "private", "protected", "internal"])
                    and not "=" in stripped.split("(")[0]):
                    potential_header = stripped
                    header_line = idx

                for char in line:
                    if char == '{':
                        brace_stack.append(idx)
                    elif char == '}':
                        if brace_stack:
                            open_line = brace_stack.pop()
                            # If this closes a top-level class member (brace_stack depth is 1, which means inside class)
                            if len(brace_stack) == 1:
                                span = idx - open_line + 1
                                if span > 50:
                                    print(f"{rel_path}:{open_line}-{idx} ({span} lines)")
