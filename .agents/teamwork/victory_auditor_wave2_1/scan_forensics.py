import os
import re
import sys

repo_root = r"G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar"

def scan_for_suspicious_patterns():
    # Scan HPBim
    hpbim_root = os.path.join(repo_root, "HPBim")
    suspicious = []
    
    facade_patterns = [
        re.compile(r'return\s+true\s*;\s*//\s*TODO', re.IGNORECASE),
        re.compile(r'throw\s+new\s+NotImplementedException', re.IGNORECASE),
        re.compile(r'Assert\.True\(true\)', re.IGNORECASE),
        re.compile(r'Assert\.Equal\(1,\s*1\)', re.IGNORECASE),
    ]
    
    for dp, _, fns in os.walk(hpbim_root):
        if any(x in dp for x in ["\\bin\\", "/bin/", "\\obj\\", "/obj/"]):
            continue
        for fn in fns:
            if fn.endswith(".cs"):
                p = os.path.join(dp, fn)
                with open(p, "r", encoding="utf-8-sig", errors="replace") as fp:
                    lines = fp.readlines()
                for idx, line in enumerate(lines, 1):
                    for pat in facade_patterns:
                        if pat.search(line):
                            suspicious.append((p, idx, line.strip()))
                            
    return suspicious

if __name__ == "__main__":
    findings = scan_for_suspicious_patterns()
    print(f"Suspicious facade/self-certifying patterns found: {len(findings)}")
    for p, idx, l in findings:
        print(f"  {p}:{idx} -> {l}")
