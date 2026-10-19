import sys
import os

sys.path.insert(0, os.path.abspath('tools/verification'))
from verify_truth_matrix import parse_truth_matrix, TRUTH_MATRIX_PATH

subsystems, summary_stats, defects = parse_truth_matrix(TRUTH_MATRIX_PATH)

sub1_mocks = [r for r in subsystems[1]['rows'] if r['status'] == 'ĐANG LÀ MOCK']
print(f"Total Subsystem 1 Mocks: {len(sub1_mocks)}")
target_files_count = {}
for m in sub1_mocks:
    tf = m['target_file'].replace('`', '').strip()
    base_tf = tf.split(':')[0].strip()
    target_files_count[base_tf] = target_files_count.get(base_tf, 0) + 1
    print(f"{m['identifier']:<30} | {tf:<55} | line {m['line']}")

print("\n--- GROUPED BY TARGET FILE ---")
for tf, cnt in sorted(target_files_count.items(), key=lambda x: x[1], reverse=True):
    print(f"{tf}: {cnt} commands")
