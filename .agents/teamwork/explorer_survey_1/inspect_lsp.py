import re

lsp_path = r"C:\Dce Pro\DCE Pro.lsp"
with open(lsp_path, "r", encoding="utf-8", errors="replace") as f:
    lines = f.readlines()

print(f"Total lines in DCE Pro.lsp: {len(lines)}")

commands = []
netloads = []
for i, line in enumerate(lines):
    line_clean = line.strip()
    if line_clean.startswith("(defun c:"):
        cmd_name = line_clean.split(":")[1].split()[0].split("(")[0].strip()
        commands.append((i+1, cmd_name, line_clean))
    if "netload" in line_clean.lower():
        netloads.append((i+1, line_clean))

print(f"\nNetload references ({len(netloads)}):")
for ln, nl in netloads:
    print(f"  Line {ln}: {nl}")

print(f"\nRegistered LISP Commands ({len(commands)}):")
for ln, cmd, full in commands[:50]:
    print(f"  Line {ln:4}: {cmd}")
