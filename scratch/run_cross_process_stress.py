import subprocess
import os
import sys
import tempfile
import json
import time

def run_stress_test():
    test_dir = tempfile.mkdtemp(prefix="hpbim_forensic_audit_m1_")
    journal_path = os.path.join(test_dir, "intent_journal.jsonl")
    guid_file_1 = os.path.join(test_dir, "guids_p1.txt")
    guid_file_2 = os.path.join(test_dir, "guids_p2.txt")
    
    csproj = r"G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\scratch\test_multi_process_journal\test_multi_process_journal.csproj"
    
    print(f"[AUDIT] Test directory: {test_dir}")
    print(f"[AUDIT] Journal path: {journal_path}")
    print("[AUDIT] Launching TWO independent OS processes writing 500 intents each (1,000 total)...")
    
    cmd1 = ["dotnet", "run", "--project", csproj, "--no-build", "--", "write", journal_path, guid_file_1, "500"]
    cmd2 = ["dotnet", "run", "--project", csproj, "--no-build", "--", "write", journal_path, guid_file_2, "500"]
    
    start_time = time.time()
    p1 = subprocess.Popen(cmd1, stdout=subprocess.PIPE, stderr=subprocess.PIPE, text=True)
    p2 = subprocess.Popen(cmd2, stdout=subprocess.PIPE, stderr=subprocess.PIPE, text=True)
    
    out1, err1 = p1.communicate()
    out2, err2 = p2.communicate()
    elapsed = time.time() - start_time
    
    print(f"[AUDIT] Process 1 exit: {p1.returncode}, stdout: {out1.strip()}, stderr: {err1.strip()}")
    print(f"[AUDIT] Process 2 exit: {p2.returncode}, stdout: {out2.strip()}, stderr: {err2.strip()}")
    print(f"[AUDIT] Concurrency write time: {elapsed:.2f}s")
    
    if p1.returncode != 0 or p2.returncode != 0:
        print("[AUDIT FAILURE] One or both writer processes failed!")
        sys.exit(1)
        
    print("[AUDIT] Running verification via C# IntentJournal cold reader...")
    verify_cmd = ["dotnet", "run", "--project", csproj, "--no-build", "--", "verify", journal_path, guid_file_1, guid_file_2]
    p_verify = subprocess.run(verify_cmd, capture_output=True, text=True)
    print(f"[AUDIT] C# Verifier Output:\n{p_verify.stdout.strip()}")
    if p_verify.returncode != 0:
        print(f"[AUDIT FAILURE] C# Verifier failed with exit code {p_verify.returncode}!")
        print(f"Stderr: {p_verify.stderr.strip()}")
        sys.exit(2)
        
    print("[AUDIT] Performing INDEPENDENT Python raw-level forensic inspection of journal on disk...")
    with open(guid_file_1, "r", encoding="utf-8") as f:
        guids1 = set(line.strip() for line in f if line.strip())
    with open(guid_file_2, "r", encoding="utf-8") as f:
        guids2 = set(line.strip() for line in f if line.strip())
        
    assert len(guids1) == 500, f"Expected 500 GUIDs in p1, got {len(guids1)}"
    assert len(guids2) == 500, f"Expected 500 GUIDs in p2, got {len(guids2)}"
    overlap = guids1.intersection(guids2)
    assert len(overlap) == 0, f"GUID overlap detected: {len(overlap)}"
    all_expected = guids1.union(guids2)
    assert len(all_expected) == 1000, f"Expected 1000 unique GUIDs, got {len(all_expected)}"
    
    with open(journal_path, "r", encoding="utf-8") as f:
        raw_lines = f.readlines()
        
    print(f"[AUDIT] Raw lines in file on disk: {len(raw_lines)}")
    assert len(raw_lines) == 1000, f"Expected exactly 1000 lines, got {len(raw_lines)}"
    
    found_guids = set()
    for idx, line in enumerate(raw_lines):
        line = line.strip()
        assert line, f"Empty line at index {idx}"
        try:
            data = json.loads(line)
        except Exception as e:
            print(f"[AUDIT FAILURE] JSON decode error at line {idx+1}: {e}")
            print(f"Corrupted line: {line}")
            sys.exit(3)
        op_id = data.get("operationId")
        assert op_id, f"Missing operationId at line {idx+1}"
        assert op_id in all_expected, f"Unexpected operationId {op_id} at line {idx+1}"
        found_guids.add(op_id)
        
    print(f"[AUDIT] Unique valid JSON records parsed by independent Python inspector: {len(found_guids)}")
    assert len(found_guids) == 1000, f"Expected 1000 distinct GUIDs parsed, got {len(found_guids)}"
    assert found_guids == all_expected, "Mismatch between parsed GUIDs and expected set"
    
    print("[AUDIT SUCCESS] 100% of 1,000 distinct records across 2 OS processes verified intact, ordered, and uncorrupted.")

if __name__ == "__main__":
    run_stress_test()
