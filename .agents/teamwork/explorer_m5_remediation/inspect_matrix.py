import sys
import os

sys.path.insert(0, os.path.abspath('tools/verification'))
from verify_truth_matrix import parse_truth_matrix, TRUTH_MATRIX_PATH

subsystems, summary_stats, defects = parse_truth_matrix(TRUTH_MATRIX_PATH)

print("=== SUMMARY OF MOCK ROWS IN TRUTH_MATRIX.MD ===")
total_mocks = 0
for sub_id, sub in subsystems.items():
    mocks = [r for r in sub['rows'] if r['status'] == 'ĐANG LÀ MOCK']
    total_mocks += len(mocks)
    print(f"\nSubsystem {sub_id} ({sub['name']}): {len(mocks)} MOCKS")
    for m in mocks:
        print(f"  [{m['line']}] {m['identifier']} | Target: {m['target_file']} | Status: {m['status']}")
        print(f"     Details: {m['inspection'][:100]}")

print(f"\nTOTAL MOCKS ACROSS ALL SUBSYSTEMS: {total_mocks}")
