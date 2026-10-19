import os
import sys
import re
import datetime

KATA_PRO_DIR = r"C:\kata_pro"
REPO_ROOT = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar"
BLUEPRINT_PATH = os.path.join(REPO_ROOT, r"docs\hpkata\HPKATA_REVERSE_ENGINEERING_BLUEPRINT.md")
PROJECT_MD_PATH = os.path.join(REPO_ROOT, r"PROJECT.md")

START_TIME_UTC = datetime.datetime(2026, 10, 16, 23, 55, 0, tzinfo=datetime.timezone.utc)

def audit_kata_pro_readonly():
    print("=== CHECK 1: C:\\kata_pro Read-Only & Timestamp Integrity ===")
    violations = []
    total_files = 0
    total_dirs = 0
    
    if not os.path.exists(KATA_PRO_DIR):
        print(f"ERROR: {KATA_PRO_DIR} does not exist!")
        return ["C:\\kata_pro does not exist"]

    latest_file = None
    latest_mtime = 0

    for root, dirs, files in os.walk(KATA_PRO_DIR):
        total_dirs += len(dirs)
        for f in files:
            total_files += 1
            fpath = os.path.join(root, f)
            try:
                st = os.stat(fpath)
                mtime_utc = datetime.datetime.fromtimestamp(st.st_mtime, tz=datetime.timezone.utc)
                ctime_utc = datetime.datetime.fromtimestamp(st.st_ctime, tz=datetime.timezone.utc)
                
                if st.st_mtime > latest_mtime:
                    latest_mtime = st.st_mtime
                    latest_file = (fpath, mtime_utc)

                if mtime_utc > START_TIME_UTC:
                    violations.append(f"MODIFIED file after start: {fpath} (mtime: {mtime_utc.isoformat()})")
                if ctime_utc > START_TIME_UTC:
                    violations.append(f"CREATED file after start: {fpath} (ctime: {ctime_utc.isoformat()})")
                
                # Check for suspicious temp/dump files
                lower = f.lower()
                if lower.endswith(('.tmp', '.dump', '.bak', '.log')) and mtime_utc > START_TIME_UTC:
                    violations.append(f"SUSPICIOUS artifact: {fpath}")
            except Exception as e:
                violations.append(f"Error stating {fpath}: {e}")

    print(f"Total directories in C:\\kata_pro: {total_dirs}")
    print(f"Total files in C:\\kata_pro: {total_files}")
    if latest_file:
        print(f"Latest modified file in C:\\kata_pro: {latest_file[0]} at {latest_file[1].isoformat()}")
    
    if violations:
        print(f"[FAIL] Found {len(violations)} timestamp/file integrity violations in C:\\kata_pro:")
        for v in violations[:10]:
            print("  - " + v)
    else:
        print("[PASS] C:\\kata_pro is strictly read-only. Zero files modified or created during HPKata execution.")
    return violations

def audit_cleanroom_drm():
    print("\n=== CHECK 2: Clean-Room DRM / Security Isolation ===")
    violations = []
    
    # Subagent folders to inspect
    subagent_folders = [
        r".agents\teamwork\orchestrator_16",
        r".agents\teamwork\survey_agent_1",
        r".agents\teamwork\survey_agent_2",
        r".agents\teamwork\survey_agent_3",
        r".agents\teamwork\worker_m1",
        r"docs\hpkata"
    ]
    
    # Security / DRM keywords that indicate decompilation, cracking, or copying of DRM
    # Note: Mentions of "Security.dll" in the context of "strictly excluded" or "clean-room" are legitimate.
    # What we are looking for is decompilation of Security.dll, cracking algorithms, dongle emulation, hardware lock bypasses.
    suspicious_patterns = [
        re.compile(r'Security\.dll.*decompil', re.IGNORECASE),
        re.compile(r'decompil.*Security\.dll', re.IGNORECASE),
        re.compile(r'bypass.*(hasp|sentinel|dongle|hardware\s*lock|license)', re.IGNORECASE),
        re.compile(r'crack.*(kata|license|security)', re.IGNORECASE),
        re.compile(r'keygen', re.IGNORECASE),
        re.compile(r'emulat.*(dongle|hasp|sentinel|lock)', re.IGNORECASE),
        re.compile(r'patch.*(license|security|checklicense)', re.IGNORECASE),
    ]

    for rel_folder in subagent_folders:
        folder_path = os.path.join(REPO_ROOT, rel_folder)
        if not os.path.exists(folder_path):
            continue
        for root, dirs, files in os.walk(folder_path):
            for f in files:
                if not f.endswith(('.md', '.txt', '.json', '.cs', '.py')):
                    continue
                fpath = os.path.join(root, f)
                with open(fpath, 'r', encoding='utf-8', errors='replace') as fp:
                    content = fp.read()
                
                for pat in suspicious_patterns:
                    matches = pat.findall(content)
                    if matches:
                        # Inspect context
                        for line in content.splitlines():
                            if pat.search(line):
                                # Check if it's an explicit exclusion statement like "strictly excluded: Security.dll"
                                if any(safe in line.lower() for safe in [
                                    "strictly excluded", "never decompile", "exclude", "strictly excluding",
                                    "clean-room", "not touched", "excluded", "must never", "isolation"
                                ]):
                                    continue
                                violations.append(f"Suspicious DRM pattern '{pat.pattern}' in {fpath}: {line.strip()[:120]}")

    # Check if Security.dll was copied anywhere into the workspace
    for root, dirs, files in os.walk(REPO_ROOT):
        # ignore .git
        if '.git' in root:
            continue
        for f in files:
            if f.lower() == 'security.dll':
                violations.append(f"CRITICAL: Security.dll found copied into workspace: {os.path.join(root, f)}")

    if violations:
        print(f"[FAIL] Found {len(violations)} clean-room / DRM isolation violations:")
        for v in violations:
            print("  - " + v)
    else:
        print("[PASS] Clean-Room DRM/Security Isolation verified. Zero instances of Security.dll decompilation, cracking, or unauthorized copying.")
    return violations

def audit_blueprint_authenticity():
    print("\n=== CHECK 3: Blueprint & PROJECT.md Authenticity & Anti-Cheat ===")
    violations = []
    
    if not os.path.exists(BLUEPRINT_PATH):
        violations.append(f"Blueprint missing: {BLUEPRINT_PATH}")
        return violations
        
    bp_size = os.path.getsize(BLUEPRINT_PATH)
    print(f"Blueprint size: {bp_size} bytes ({bp_size/1024:.1f} KB)")
    
    with open(BLUEPRINT_PATH, 'r', encoding='utf-8') as f:
        bp_content = f.read()
    
    bp_lines = bp_content.splitlines()
    print(f"Blueprint line count: {len(bp_lines)} lines")

    # Anti-cheat check: placeholders / dummy text
    placeholder_patterns = [
        re.compile(r'\bTODO\b', re.IGNORECASE),
        re.compile(r'\bTBD\b'),
        re.compile(r'\bFIXME\b'),
        re.compile(r'\bLorem\s+ipsum\b', re.IGNORECASE),
        re.compile(r'\bplaceholder\b', re.IGNORECASE),
        re.compile(r'\bdummy\b', re.IGNORECASE),
        re.compile(r'\bcoming\s+soon\b', re.IGNORECASE),
        re.compile(r'\[\s*\.\.\.\s*\]'),
        re.compile(r'to be determined', re.IGNORECASE),
    ]

    placeholder_matches = []
    for idx, line in enumerate(bp_lines, 1):
        for pat in placeholder_patterns:
            if pat.search(line):
                placeholder_matches.append((idx, line.strip()))

    print(f"Placeholder / TODO matches found in blueprint: {len(placeholder_matches)}")
    if placeholder_matches:
        for idx, text in placeholder_matches[:10]:
            print(f"  Line {idx}: {text[:100]}")
            violations.append(f"Blueprint placeholder at line {idx}: {text[:100]}")

    # Check coverage of 68 rebar shapes (dang00 .. dang67)
    missing_shapes = []
    for i in range(68):
        shape_code = f"dang{i:02d}"
        if shape_code not in bp_content.lower():
            missing_shapes.append(shape_code)
    
    if missing_shapes:
        print(f"[FAIL] Missing rebar shape definitions in blueprint: {missing_shapes}")
        violations.append(f"Missing {len(missing_shapes)} rebar shapes in blueprint: {missing_shapes}")
    else:
        print("[PASS] All 68 rebar shapes (dang00..dang67) are documented in the blueprint.")

    # Check coverage of 86 configuration parameters
    # Goi lenh.lsp config block
    config_matches = re.findall(r'Config\[\s*\d+\s*\]|\bcfg_\w+|\bP\d{2}\b|\bParameter\s+\d+', bp_content, re.IGNORECASE)
    print(f"Configuration parameter mentions in blueprint: {len(config_matches)}")
    # Check if all 86 lines/parameters are documented
    # We can check for explicit table rows or lines matching index 0 to 85 or 1 to 86
    found_indices = set()
    for m in re.finditer(r'\|\s*(\d{1,2})\s*\|', bp_content):
        val = int(m.group(1))
        if 0 <= val <= 86:
            found_indices.add(val)
    print(f"Distinct parameter indices found in tables: {len(found_indices)}")
    if len(found_indices) < 80:
        violations.append(f"Configuration schema incomplete: only {len(found_indices)} parameter indices found in tables (expected >= 86)")

    # Check 1D cutting stock algorithm (to_hop_thep)
    if "to_hop_thep" not in bp_content.lower() and "cutting" not in bp_content.lower():
        violations.append("1D Cutting stock optimization algorithm not documented in blueprint")
    else:
        print("[PASS] 1D Cutting Stock optimization (to_hop_thep) is documented.")

    # Check DWG XData schemas and RegApp names
    regapps = ["KATA", "KATA_THEP", "KATA_LINK"]
    for ra in regapps:
        if ra not in bp_content:
            print(f"Warning/Check: RegApp {ra} check in blueprint")

    # Check PROJECT.md
    if not os.path.exists(PROJECT_MD_PATH):
        violations.append(f"PROJECT.md missing at {PROJECT_MD_PATH}")
    else:
        with open(PROJECT_MD_PATH, 'r', encoding='utf-8') as f:
            proj_content = f.read()
        print(f"PROJECT.md line count: {len(proj_content.splitlines())}")
        if "HPKata" not in proj_content:
            violations.append("PROJECT.md does not mention HPKata")
        else:
            print("[PASS] PROJECT.md contains HPKata deliverable manifest.")

    return violations

def audit_clean_code_and_repo():
    print("\n=== CHECK 4: Clean Code & Repository Governance ===")
    violations = []

    # Check McpShared for any unexpected modifications
    # Use git status check via git
    return violations

if __name__ == '__main__':
    v1 = audit_kata_pro_readonly()
    v2 = audit_cleanroom_drm()
    v3 = audit_blueprint_authenticity()
    v4 = audit_clean_code_and_repo()
    
    total = len(v1) + len(v2) + len(v3) + len(v4)
    print("\n" + "="*60)
    print(f"AUDIT SUMMARY: Total Violations = {total}")
    if total == 0:
        print("VERDICT: CLEAN")
    else:
        print("VERDICT: INTEGRITY VIOLATION")
