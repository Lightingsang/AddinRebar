import subprocess

ilspy = r"C:\Users\STR-HP03\.dotnet\tools\ilspycmd.exe"
target = r"C:\Dce Pro\V.2020\DCE Excel.dll"

cmd = [ilspy, target, "-l", "c"]
res = subprocess.run(cmd, stdout=subprocess.PIPE, stderr=subprocess.PIPE, text=True, errors="replace")
lines = [l.strip() for l in res.stdout.splitlines() if l.strip()]

print(f"=== DCE Excel.dll: {len(lines)} classes ===")
# Group by namespace
ns_map = {}
for l in lines:
    parts = l.split(" ")
    if len(parts) >= 2:
        fullname = parts[1]
        ns = ".".join(fullname.split(".")[:-1]) if "." in fullname else "<global>"
        ns_map[ns] = ns_map.get(ns, 0) + 1

for ns, count in sorted(ns_map.items(), key=lambda x: -x[1])[:30]:
    print(f"  Namespace: {ns:40} | Count: {count}")
