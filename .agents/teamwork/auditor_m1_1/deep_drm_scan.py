import os
import re
import sys

REPO_ROOT = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar"

target_dirs = [
    os.path.join(REPO_ROOT, r".agents\teamwork\orchestrator_16"),
    os.path.join(REPO_ROOT, r".agents\teamwork\teamwork_preview_spec_miner_survey_1"),
    os.path.join(REPO_ROOT, r".agents\teamwork\teamwork_preview_spec_miner_survey_2"),
    os.path.join(REPO_ROOT, r".agents\teamwork\teamwork_preview_spec_miner_survey_3"),
    os.path.join(REPO_ROOT, r".agents\teamwork\worker_m1"),
    os.path.join(REPO_ROOT, r"docs\hpkata"),
]

patterns = [
    re.compile(r'Security\.dll', re.IGNORECASE),
    re.compile(r'\bhasp\b', re.IGNORECASE),
    re.compile(r'\bsentinel\b', re.IGNORECASE),
    re.compile(r'\bdongle\b', re.IGNORECASE),
    re.compile(r'\bhardware\s*lock\b', re.IGNORECASE),
    re.compile(r'\bcrack\b', re.IGNORECASE),
    re.compile(r'\bbypass\b', re.IGNORECASE),
    re.compile(r'\bkeygen\b', re.IGNORECASE),
    re.compile(r'\blicens(e|ing)\b', re.IGNORECASE),
]

findings = []

for d in target_dirs:
    if not os.path.exists(d):
        continue
    for root, dirs, files in os.walk(d):
        for f in files:
            if f.endswith(('.md', '.txt', '.cs', '.py', '.json', '.lsp', '.scr', '.ps1')):
                fpath = os.path.join(root, f)
                with open(fpath, 'r', encoding='utf-8', errors='replace') as fp:
                    for lno, line in enumerate(fp, 1):
                        for p in patterns:
                            if p.search(line):
                                findings.append((fpath, lno, p.pattern, line.strip()))

output_lines = [f"Total pattern matches across M1 artifacts: {len(findings)}"]
for fpath, lno, pat, line in findings:
    rel = os.path.relpath(fpath, REPO_ROOT)
    # clean line for safe printing
    safe_line = line[:140].encode('ascii', errors='backslashreplace').decode('ascii')
    output_lines.append(f"[{pat}] {rel}:{lno} -> {safe_line}")

output_text = "\n".join(output_lines)
with open(os.path.join(REPO_ROOT, r".agents\teamwork\auditor_m1_1\drm_scan_results.txt"), "w", encoding="utf-8") as out:
    out.write(output_text)

print(f"Done. Wrote {len(findings)} findings to drm_scan_results.txt")
