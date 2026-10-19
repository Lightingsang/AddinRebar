import json
import os
import struct
import sys

sys.stdout.reconfigure(encoding='utf-8')

BASE_DIR = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar"
DCE_DIR = r"C:\Dce Pro"
DANHBA_DIR = os.path.join(DCE_DIR, "AiAgent", "DanhBa")

# Test compilation of stats
def compute_stats():
    # 1. CAD Commands (311)
    with open(os.path.join(DANHBA_DIR, "danhba_lenh.json"), 'r', encoding='utf-8') as f:
        cad_cmds = json.load(f).get('muc', [])
    
    # 2. Excel (73)
    with open(os.path.join(DANHBA_DIR, "danhba_dce_excel.json"), 'r', encoding='utf-8') as f:
        excel_cmds = json.load(f).get('muc', [])
        
    # 3. Thep (49)
    with open(os.path.join(DANHBA_DIR, "danhba_dce_thep.json"), 'r', encoding='utf-8') as f:
        thep_cmds = json.load(f).get('muc', [])

    # 4. MBKC (29)
    with open(os.path.join(DANHBA_DIR, "danhba_mbkc.json"), 'r', encoding='utf-8') as f:
        mbkc_cmds = json.load(f).get('muc', [])

    print(f"CAD: {len(cad_cmds)}, Excel: {len(excel_cmds)}, Thep: {len(thep_cmds)}, MBKC: {len(mbkc_cmds)}")

compute_stats()
