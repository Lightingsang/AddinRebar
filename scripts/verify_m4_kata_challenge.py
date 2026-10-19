import glob
import os
import re
import sys

def check_methods(filepath):
    with open(filepath, 'r', encoding='utf-8') as fp:
        lines = fp.readlines()
    
    in_method = False
    method_name = ''
    method_start = 0
    brace_depth = 0
    max_len = 0
    longest_method = ''
    violations = []
    
    for idx, line in enumerate(lines, 1):
        stripped = line.strip()
        if not in_method and re.match(r'^(public|private|internal|protected)\s+(static\s+)?(void|int|bool|string|IEnumerable|HashSet|List|Type|MetroWindow|\w+)\s+\w+\(.*\)', stripped):
            in_method = True
            method_name = stripped.split('(')[0]
            method_start = idx
            brace_depth = 0
        
        if in_method:
            brace_depth += line.count('{') - line.count('}')
            if brace_depth == 0 and '{' in ''.join(lines[method_start-1:idx]):
                method_len = idx - method_start + 1
                if method_len > max_len:
                    max_len = method_len
                    longest_method = method_name
                if method_len > 50:
                    violations.append((method_name, method_len, method_start, idx))
                in_method = False
    return max_len, longest_method, violations

def main():
    print("=== EMPIRICAL CHALLENGER M4 HARNESS FOR HPKATA ===")
    views_dir = os.path.abspath(r"HPKata/HPKata.UI/Views")
    themes_dir = os.path.abspath(r"HPKata/HPKata.UI/Themes")
    test_views_dir = os.path.abspath(r"HPKata/HPKata.UI.Tests/Views")

    xaml_files = sorted(glob.glob(os.path.join(views_dir, "*.xaml")))
    print(f"1. Total XAML views in HPKata.UI/Views: {len(xaml_files)}")
    assert len(xaml_files) == 13, f"Expected 13 views, found {len(xaml_files)}"

    expected_views = [
        "BeamDetailingWindow.xaml",
        "ColumnDetailingWindow.xaml",
        "WallDetailingWindow.xaml",
        "FoundationDetailingWindow.xaml",
        "SlabDetailingWindow.xaml",
        "StairLintelDetailingWindow.xaml",
        "EtabsImportWindow.xaml",
        "QsTakeoffWindow.xaml",
        "DrawingUtilityWindow.xaml",
        "CuttingStockView.xaml",
        "DetailingView.xaml",
        "ScheduleView.xaml",
        "SetupView.xaml"
    ]

    actual_view_names = [os.path.basename(f) for f in xaml_files]
    assert sorted(actual_view_names) == sorted(expected_views), f"Views mismatch: {set(expected_views) ^ set(actual_view_names)}"
    print("   -> Confirmed: Exactly 13 expected view files exist on disk.")

    # Check AllWindowsXamlInstantiationTests.cs
    inst_test_path = os.path.join(test_views_dir, "AllWindowsXamlInstantiationTests.cs")
    with open(inst_test_path, "r", encoding="utf-8") as f:
        inst_test_content = f.read()

    print("\n2. Checking AllWindowsXamlInstantiationTests.cs explicit types:")
    expected_types = [os.path.splitext(v)[0] for v in expected_views]
    for vt in expected_types:
        pattern = rf"typeof\({vt}\)"
        if not re.search(pattern, inst_test_content):
            print(f"   [FAIL] Missing explicit typeof({vt}) in AllWindowsXamlInstantiationTests.cs")
            sys.exit(1)
        else:
            print(f"   [PASS] Found typeof({vt})")

    # Check StaticResourceParityScannerTests.cs
    scanner_test_path = os.path.join(test_views_dir, "StaticResourceParityScannerTests.cs")
    with open(scanner_test_path, "r", encoding="utf-8") as f:
        scanner_test_content = f.read()

    print("\n3. Checking StaticResourceParityScannerTests.cs explicit file names:")
    for vn in expected_views:
        if f'"{vn}"' not in scanner_test_content:
            print(f"   [FAIL] Missing file name \"{vn}\" in StaticResourceParityScannerTests.cs")
            sys.exit(1)
        else:
            print(f"   [PASS] Found \"{vn}\"")

    # Read Theme definitions
    theme_files = glob.glob(os.path.join(themes_dir, "*.xaml"))
    all_theme_keys = set()
    key_regex = re.compile(r'x:Key="([^"]+)"')
    for tf in theme_files:
        with open(tf, "r", encoding="utf-8") as f:
            t_content = f.read()
        keys = key_regex.findall(t_content)
        all_theme_keys.update(keys)

    print(f"\n4. Total keys defined across Themes/ (*.xaml): {len(all_theme_keys)}")

    # Extract all StaticResource usages across all 13 views
    static_res_regex = re.compile(r'\{StaticResource\s+([^}]+)\}')
    total_references = 0
    all_unresolved = []

    standard_mahapps_prefixes = ("MahApps.", "MetroWindow.", "{x:Type")

    print("\n5. Analyzing StaticResource references per view:")
    for fpath in xaml_files:
        fname = os.path.basename(fpath)
        with open(fpath, "r", encoding="utf-8") as f:
            content = f.read()

        local_keys = set(key_regex.findall(content))
        matches = static_res_regex.findall(content)
        total_references += len(matches)

        view_unresolved = []
        for m in matches:
            key = m.strip()
            if ',' in key:
                key = key.split(',')[0].strip()

            # Resolution check
            resolved = (
                key in local_keys or
                key in all_theme_keys or
                any(key.startswith(p) for p in standard_mahapps_prefixes)
            )

            if not resolved:
                view_unresolved.append(key)
                all_unresolved.append((fname, key))

        status = f"PASS ({len(matches)} keys, 0 unresolved)" if not view_unresolved else f"FAIL ({len(view_unresolved)} unresolved: {view_unresolved})"
        print(f"   - {fname}: {status}")

    print(f"\nTotal StaticResource references across all 13 views: {total_references}")
    print(f"Total unresolved keys: {len(all_unresolved)}")
    assert len(all_unresolved) == 0, f"Unresolved keys found: {all_unresolved}"

    # Check for forbidden MaterialDesign references
    print("\n6. Checking for forbidden MaterialDesign references:")
    forbidden_tokens = ["materialdesigninxaml.net", "MaterialDesignThemes", "MaterialDesignColors"]
    for fpath in xaml_files:
        fname = os.path.basename(fpath)
        with open(fpath, "r", encoding="utf-8") as f:
            content = f.read()
        for tok in forbidden_tokens:
            if tok.lower() in content.lower():
                print(f"   [FAIL] Found forbidden token '{tok}' in {fname}")
                sys.exit(1)
    print("   [PASS] 0 forbidden MaterialDesign tokens across all 13 views.")

    # Check Clean Code file lengths (Rule C2 <= 300) and method lengths (Rule M3 <= 50)
    print("\n7. Checking Clean Code compliance (Rule C2 <= 300 lines, Rule M3 <= 50 lines):")
    files_to_check = [
        inst_test_path,
        scanner_test_path,
        os.path.abspath(r"HPKata/HPKata.UI.Tests/Themes/ThemeInfrastructureTests.cs")
    ]
    for fp in files_to_check:
        with open(fp, "r", encoding="utf-8") as f:
            lines = f.readlines()
        max_m_len, longest_m, violations = check_methods(fp)
        base = os.path.basename(fp)
        print(f"   - {base}: {len(lines)} lines (C2 <= 300: PASS), longest method '{longest_m.strip()}' has {max_m_len} lines (M3 <= 50: {'PASS' if max_m_len <= 50 else 'FAIL'})")
        assert len(lines) <= 300, f"{fp} exceeds 300 lines ({len(lines)})"
        assert len(violations) == 0, f"Method length violations in {fp}: {violations}"

    print("\nALL EMPIRICAL CHECKS PASSED!")

if __name__ == "__main__":
    main()
