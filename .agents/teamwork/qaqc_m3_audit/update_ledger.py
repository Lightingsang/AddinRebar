import json
from datetime import datetime, timezone

ledger_path = r'G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\state\bim_coordination_ledger.json'

with open(ledger_path, 'r', encoding='utf-8') as f:
    ledger = json.load(f)

ledger['updatedAt'] = datetime.now(timezone.utc).isoformat()
ledger['phaseStatus'] = "M3_QAQC_AUDIT_REQUEST_CHANGES"
ledger['qualityGate'] = "GATE_3_QAQC_AUDIT_FAILED_CHANGES_REQUESTED"
ledger['summary']['step4_QaqcAudit'] = len(ledger['elements'])
ledger['summary']['step5_RouteFix'] = 62 # 9 piles/footings + 53 beams
ledger['summary']['group2_ConflictingInfo'] = 62
ledger['summary']['group1_ReadyToModel'] = len(ledger['elements']) - 62

# Add RFIs for the 2 actions
ledger['rfis'] = [
    {
        "rfiId": "RFI-M3-001",
        "title": "9 Cọc D400 bị đâm ra ngoài đài móng tương ứng (20mm - 164mm)",
        "affectedElements": ["P75 (ĐC-2-04)", "P81 (ĐC-6-01)", "P83 (ĐC-6-01)", "P99 (ĐC-3'-02)", "P100 (ĐC-6-04)", "P106 (ĐC-2-19)", "P118 (ĐC-2-10)", "P126 (ĐC-3-04)", "P187 (ĐC-3-03)"],
        "assignedTo": "revit_assistant",
        "status": "OPEN",
        "actionRequired": "Dịch chuyển vị trí/biên dạng 8 đài móng để tâm đài trùng trọng tâm cọc và bao bọc 100% cọc."
    },
    {
        "rfiId": "RFI-M3-002",
        "title": "53 nhịp dầm giằng móng GM và dầm bó nền BN bị lệch tọa độ 100-150mm & 115 đầu mút lơ lửng",
        "affectedElements": ["GM-7", "GM-5a", "GM-3", "GM-10b", "GM-4b", "GM-12", "GM-21", "GM-10a", "GM-5b", "BN-1", "BN-2"],
        "assignedTo": "revit_assistant",
        "status": "OPEN",
        "actionRequired": "Hiệu chỉnh điểm mút dầm nối khít vào đài móng/cột, triệt tiêu đầu mút lơ lửng, sai số <= 1.0mm."
    }
]

with open(ledger_path, 'w', encoding='utf-8') as f:
    json.dump(ledger, f, ensure_ascii=False, indent=2)

print("Updated bim_coordination_ledger.json successfully.")
