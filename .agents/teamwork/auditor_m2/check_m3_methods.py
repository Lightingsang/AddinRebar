import os
import re

HPKATA_DIR = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata"

# Identify Phase 3 production files
phase3_dirs_and_files = [
    r"HPKata.Core\Columns",
    r"HPKata.Core\Walls",
    r"HPKata.UI\ViewModels\Column",
    r"HPKata.UI\ViewModels\Wall",
    r"HPKata.UI\ViewModels\EtabsColumnWallImportViewModel.cs",
    r"HPKata.UI\Views\ColumnDetailingWindow.xaml.cs",
    r"HPKata.UI\Views\WallDetailingWindow.xaml.cs",
    r"HPKata.AutoCad\Commands\ColumnCommands.cs",
    r"HPKata.AutoCad\Commands\WallCommands.cs",
    r"HPKata.AutoCad\Controllers\Column",
    r"HPKata.AutoCad\Controllers\Wall",
    r"HPKata.Revit\Commands\ColumnRebarCommands.cs",
    r"HPKata.Revit\Commands\WallRebarCommands.cs",
    r"HPKata.Revit\Commands\EtabsColumnWallImportCommand.cs",
    r"HPKata.Revit\Services\ColumnRebarGenerator.cs",
    r"HPKata.Revit\Services\WallRebarGenerator.cs",
    r"HPKata.Revit\Services\ColumnWallScheduleService.cs",
]

def is_phase3_file(fpath):
    rel = os.path.relpath(fpath, HPKATA_DIR)
    for p in phase3_dirs_and_files:
        if p in rel:
            return True
    return False

def analyze_methods_in_file(fpath):
    with open(fpath, "r", encoding="utf-8", errors="ignore") as f:
        lines = f.readlines()
    
    methods = []
    # Simplified C# method detector
    # Matches access modifiers or return types, method name, params, followed eventually by {
    # We track braces
    in_class = False
    brace_depth = 0
    method_start = -1
    method_name = ""
    method_header_lines = []

    # regex for method signature candidate
    # e.g. public static void Foo(...) or private int Bar(...) or public async Task<bool> Baz(...)
    sig_re = re.compile(r"^\s*(?:\[.*\]\s*)*(?:public|private|protected|internal|static|async|override|virtual|sealed)*\s*[\w\<\>\[\],\s\?]+\s+([A-Za-z0-9_]+)\s*\([^\)]*\)\s*(?:where\s+.*)?$")
    
    # We can also track basic brace blocks
    for i, line in enumerate(lines):
        clean = line.strip()
        
        # Check if line looks like method signature before brace open
        if brace_depth >= 1 and method_start == -1:
            # check single line or multi-line signature
            if "(" in clean and not clean.startswith("//") and not clean.startswith("if") and not clean.startswith("for") and not clean.startswith("foreach") and not clean.startswith("while") and not clean.startswith("switch") and not clean.startswith("using") and not clean.startswith("catch") and not clean.startswith("lock") and not clean.startswith("return") and not clean.startswith("throw"):
                m = re.search(r"\b([A-Za-z0-9_]+)\s*\([^\)]*\)\s*(?:where\s+.*)?\s*$", clean)
                if m and not clean.startswith("new ") and not clean.endswith(";"):
                    candidate_name = m.group(1)
                    if candidate_name not in ["if", "for", "foreach", "while", "switch", "using", "catch", "lock", "typeof", "sizeof"]:
                        method_header_lines.append((i, candidate_name))

        # Count braces
        # ignore braces in strings or char literals or comments
        # quick scrub
        scrubbed = re.sub(r'"(?:\\.|[^"\\])*"', '""', clean)
        scrubbed = re.sub(r"'(\\.|[^'\\])'", "''", scrubbed)
        if "//" in scrubbed:
            scrubbed = scrubbed.split("//")[0]

        for ch in scrubbed:
            if ch == '{':
                brace_depth += 1
                if method_start == -1 and method_header_lines:
                    # The last candidate header is our method
                    last_hdr_line, last_name = method_header_lines[-1]
                    method_start = last_hdr_line
                    method_name = last_name
                    method_header_lines = []
                    method_brace_level = brace_depth
            elif ch == '}':
                if method_start != -1 and brace_depth == method_brace_level:
                    # method end
                    method_end = i
                    length = method_end - method_start + 1
                    methods.append((method_name, method_start + 1, method_end + 1, length))
                    method_start = -1
                    method_name = ""
                brace_depth -= 1
        
        # If we didn't open a brace soon after candidate header, clear it
        if method_header_lines and method_start == -1:
            if i - method_header_lines[-1][0] > 5:
                method_header_lines = []

    return methods

def main():
    cs_files = []
    for dirpath, dirnames, filenames in os.walk(HPKATA_DIR):
        if "bin" in dirnames:
            dirnames.remove("bin")
        if "obj" in dirnames:
            dirnames.remove("obj")
        for f in filenames:
            if f.endswith(".cs"):
                cs_files.append(os.path.join(dirpath, f))

    phase3_files = [f for f in cs_files if is_phase3_file(f)]
    print(f"Total Phase 3 files identified: {len(phase3_files)}")

    violations = []
    total_methods = 0
    max_len = 0
    max_info = ""

    for fpath in phase3_files:
        rel = os.path.relpath(fpath, HPKATA_DIR)
        methods = analyze_methods_in_file(fpath)
        total_methods += len(methods)
        for mname, s, e, l in methods:
            if l > max_len:
                max_len = l
                max_info = f"{rel} -> {mname} ({l} lines, L{s}-L{e})"
            if l > 50:
                violations.append((rel, mname, s, e, l))

    print(f"Total methods analyzed in Phase 3 code: {total_methods}")
    print(f"Longest method: {max_info}")
    if violations:
        print(f"\nFAIL: {len(violations)} methods in Phase 3 code exceed 50 lines:")
        for rel, mname, s, e, l in violations:
            print(f"  - {rel} :: {mname} ({l} lines, lines {s}-{e})")
    else:
        print("\nPASS: All methods in Phase 3 code are <= 50 lines (Rule M3).")

if __name__ == "__main__":
    main()
