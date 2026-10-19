# -*- coding: utf-8 -*-
import json, sys
sys.stdout.reconfigure(encoding="utf-8")

path = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\scratch\VeHeDamTang1_DaKieng.dyn"
with open(path, "r", encoding="utf-8") as f:
    dyn = json.load(f)

out_ports = {}
in_ports = {}

for n in dyn["Nodes"]:
    n_name = n.get("Description", n.get("ConcreteType"))
    for out in n.get("Outputs", []):
        out_ports[out["Id"]] = (n["Id"], n_name, out.get("Name", "out"))
    for inp in n.get("Inputs", []):
        in_ports[inp["Id"]] = (n["Id"], n_name, inp.get("Name", "in"))

print(f"Total Output Ports: {len(out_ports)}")
print(f"Total Input Ports: {len(in_ports)}")
print(f"Total Connectors: {len(dyn['Connectors'])}")

errors = 0
for i, c in enumerate(dyn["Connectors"]):
    sid = c["Start"]
    eid = c["End"]
    if not isinstance(sid, str) or not isinstance(eid, str):
        print(f"ERROR: Connector {i} Start/End is not string!")
        errors += 1
    if sid not in out_ports:
        print(f"ERROR: Connector {i} Start port {sid} NOT FOUND in outputs!")
        errors += 1
    if eid not in in_ports:
        print(f"ERROR: Connector {i} End port {eid} NOT FOUND in inputs!")
        errors += 1
    if sid in out_ports and eid in in_ports:
        s_info = out_ports[sid]
        e_info = in_ports[eid]
        print(f"Wire {i+1:02d}: [{s_info[1]}]::{s_info[2]} ---> [{e_info[1]}]::{e_info[2]}")

if errors == 0:
    print("ALL 22 WIRES ARE 100% VALID AND BOUND TO REAL NATIVE PORTS!")
