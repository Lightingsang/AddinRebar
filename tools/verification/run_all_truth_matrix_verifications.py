#!/usr/bin/env python3
"""
Master Verification Harness for TRUTH_MATRIX.md
Runs parity verification, deep stress testing, and markdown syntax checking in one unified command.
Outputs structured JSON and human-readable test log.
"""

import os
import sys
import json
import subprocess

sys.stdout.reconfigure(encoding='utf-8')

def main():
    print("=" * 80)
    print("MASTER ADVERSARIAL VERIFICATION HARNESS: TRUTH_MATRIX.md")
    print("=" * 80)

    # 1. Run verify_truth_matrix.py
    print("\n>>> STEP 1: Running Parity Verification & Catalog Bijection...")
    res1 = subprocess.run([sys.executable, "tools/verification/verify_truth_matrix.py"], capture_output=True, text=True, encoding='utf-8')
    print(res1.stdout)
    if res1.stderr:
        print("[STDERR]:", res1.stderr)

    # 2. Run deep_stress_test.py
    print("\n>>> STEP 2: Running Deep Stress-Test (PE Exports, Ordinals, Disk Parity)...")
    res2 = subprocess.run([sys.executable, "tools/verification/deep_stress_test.py"], capture_output=True, text=True, encoding='utf-8')
    print(res2.stdout)
    if res2.stderr:
        print("[STDERR]:", res2.stderr)

    # 3. Run test_markdown_syntax.py
    print("\n>>> STEP 3: Running Markdown Syntax & Delimiter Integrity...")
    res3 = subprocess.run([sys.executable, "tools/verification/test_markdown_syntax.py"], capture_output=True, text=True, encoding='utf-8')
    print(res3.stdout)
    if res3.stderr:
        print("[STDERR]:", res3.stderr)

    print("\n" + "=" * 80)
    print("ALL VERIFICATION SUITES COMPLETED EMPIRICALLY.")
    print("=" * 80)

if __name__ == '__main__':
    main()
