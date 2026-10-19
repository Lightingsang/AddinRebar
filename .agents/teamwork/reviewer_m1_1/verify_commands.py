import re

lsp_path = r'C:\Dce Pro\DCE Pro.lsp'
with open(lsp_path, 'r', encoding='utf-8', errors='ignore') as f:
    lsp_content = f.read()

# Find all (defun c:...
commands = re.findall(r'\(defun\s+c:([^\s\(\)]+)', lsp_content, re.IGNORECASE)
print(f"Total commands found in DCE Pro.lsp: {len(commands)}")
print("Unique commands:", len(set(commands)))

# Check calls to (command "..." or netload or similar or external methods
methods = re.findall(r'\(c:([^\s\(\)]+)', lsp_content, re.IGNORECASE)

print("\nAll commands defined in DCE Pro.lsp:")
for idx, cmd in enumerate(commands, 1):
    print(f"{idx:2d}. c:{cmd}")

# Now let's extract commands from 04-command-mapping-and-roadmap.md
doc_path = r'g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\docs\rea-dce-pro\04-command-mapping-and-roadmap.md'
with open(doc_path, 'r', encoding='utf-8') as f:
    doc_content = f.read()

doc_cmds = re.findall(r'\|\s*\d+\s*\|\s*`c:([^`]+)`', doc_content)
print(f"\nTotal commands in 04-command-mapping-and-roadmap.md table: {len(doc_cmds)}")

lsp_cmd_set = set(c.lower() for c in commands)
doc_cmd_set = set(c.lower() for c in doc_cmds)

missing_in_doc = lsp_cmd_set - doc_cmd_set
extra_in_doc = doc_cmd_set - lsp_cmd_set

print(f"Commands in LSP but missing in Doc ({len(missing_in_doc)}): {missing_in_doc}")
print(f"Commands in Doc but missing in LSP ({len(extra_in_doc)}): {extra_in_doc}")
