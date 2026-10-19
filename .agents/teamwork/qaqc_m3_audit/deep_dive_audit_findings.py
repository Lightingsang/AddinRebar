import json
import sys
import math

sys.stdout.reconfigure(encoding="utf-8")

def deep_dive():
    with open(r'G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\qaqc_m3_audit\ket_qua_kiem_toan_phan_mong.json', 'r', encoding='utf-8') as f:
        data = json.load(f)

    print("=== DEEP DIVE AUDIT FINDINGS ===")
    s3 = data['steps']['step_3_positions_and_alignments']
    print(f"\nStep 3 Footing errors count: {len(s3.get('footing_errors', []))}")
    for fe in s3.get('footing_errors', [])[:15]:
        print("  ", fe)

    print(f"\nStep 3 Beam errors count: {len(s3.get('beam_errors', []))}")
    for be in s3.get('beam_errors', [])[:15]:
        print("  ", be)

    s6 = data['steps']['step_6_structural_relationships']
    print(f"\nStep 6 Pile containment failures: {len(s6.get('pile_containment_failures', []))}")
    for pcf in s6.get('pile_containment_failures', []):
        print("  ", pcf)

    print(f"\nStep 6 Beam connectivity failures: {len(s6.get('beam_connectivity_failures', []))}")
    for bcf in s6.get('beam_connectivity_failures', [])[:20]:
        print("  ", bcf)

if __name__ == '__main__':
    deep_dive()
