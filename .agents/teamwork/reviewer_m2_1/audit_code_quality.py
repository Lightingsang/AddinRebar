import os
import re

def check_methods_and_comments(filepath):
    with open(filepath, 'r', encoding='utf-8') as f:
        lines = f.readlines()
    
    # Check commented-out code heuristics: lines starting with // followed by C# keywords like var, if, for, return, new, using
    commented_code = []
    for idx, line in enumerate(lines, 1):
        stripped = line.strip()
        if re.match(r"^//\s*(var|if|for|foreach|while|return|public|private|protected|class|void|string|int|bool|new)\b", stripped):
            commented_code.append((idx, stripped))
            
    print(f"\n--- Checking {filepath} ---")
    if commented_code:
        print(f"SUSPECT COMMENTED CODE in {filepath}:")
        for lno, text in commented_code:
            print(f"  Line {lno}: {text}")
    else:
        print("0 commented-out code lines found.")

check_methods_and_comments(r"HPBim/src/HPBim.Client/Recovery/ExtensibleStorageReceiptReader.cs")
check_methods_and_comments(r"HPBim/src/HPBim.Client/Recovery/ReconcileService.cs")
check_methods_and_comments(r"HPBim/src/HPBim.Client/Recovery/CoordinationReceiptRecord.cs")
check_methods_and_comments(r"HPBim/src/HPBim.Client/Recovery/ExtensibleStorageQueryScriptTemplate.cs")
