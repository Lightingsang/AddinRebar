import os
import re

TEST_PROJECTS = {
    "HPGrasshopper.McpBridge.Tests": 630,
    "HPGrasshopper.Mcp.Server.Tests": 117,
    "HPGrasshopper.E2E.Tests": 181,
}

BASE_DIR = r"HPGrasshopper"

def analyze_test_project(proj_name, expected_count):
    proj_dir = os.path.join(BASE_DIR, proj_name)
    test_files = []
    for r, d, fs in os.walk(proj_dir):
        if "bin" in r.split(os.sep) or "obj" in r.split(os.sep):
            continue
        for f in fs:
            if f.endswith(".cs"):
                test_files.append(os.path.join(r, f))

    test_methods = []
    empty_tests = []
    no_assert_tests = []

    # Regex to find test methods
    fact_regex = re.compile(r'\[(?:Fact|Theory)(?:\([^)]*\))?\]', re.MULTILINE)
    
    for tf in test_files:
        with open(tf, "r", encoding="utf-8") as fp:
            content = fp.read()
        
        # Tokenize or scan for [Fact] / [Theory]
        lines = content.splitlines()
        for idx, line in enumerate(lines):
            stripped = line.strip()
            if stripped.startswith("[Fact") or stripped.startswith("[Theory"):
                # The method declaration is shortly after
                j = idx + 1
                decl = ""
                while j < len(lines) and not ("(" in decl and ")" in decl):
                    decl += " " + lines[j].strip()
                    j += 1
                
                # Find body
                brace_start = -1
                k = j
                while k < len(lines) and "{" not in lines[k]:
                    k += 1
                if k < len(lines):
                    # count braces
                    depth = 0
                    body_lines = []
                    for b_idx in range(k, len(lines)):
                        b_line = lines[b_idx]
                        depth += b_line.count("{") - b_line.count("}")
                        body_lines.append(b_line)
                        if depth == 0:
                            break
                    body_text = "\n".join(body_lines)
                    method_name = decl.split("(")[0].split()[-1] if "(" in decl else f"Line_{idx+1}"
                    
                    test_methods.append({
                        "file": tf,
                        "line": idx + 1,
                        "name": method_name,
                        "body": body_text
                    })
                    
                    # Check for empty body
                    inner = re.sub(r'[\s\{\}]', '', body_text)
                    if not inner:
                        empty_tests.append((tf, idx + 1, method_name))
                    
                    # Check for assertion or verification call
                    has_assertion = any(term in body_text for term in [
                        "Assert.", "Should", "AssertFail", "AssertEqual",
                        "AssertThrows", "AssertTrue", "AssertFalse",
                        "Verify", "ThrowsAsync", "Throws<"
                    ])
                    if not has_assertion:
                        no_assert_tests.append((tf, idx + 1, method_name))

    print(f"=== Project: {proj_name} ===")
    print(f"  Discovered test methods declared: {len(test_methods)}")
    print(f"  Expected test count (MTP runner): {expected_count}")
    print(f"  Empty test methods: {len(empty_tests)}")
    print(f"  Methods without standard Assert: {len(no_assert_tests)}")
    if no_assert_tests:
        print(f"  Sample methods without standard Assert (could be helper or custom oracle):")
        for tf, ln, nm in no_assert_tests[:5]:
            print(f"    {os.path.relpath(tf, BASE_DIR)}:{ln} {nm}")
    return len(test_methods), empty_tests, no_assert_tests

def main():
    total_found = 0
    all_empty = []
    for proj, exp in TEST_PROJECTS.items():
        count, empty, no_assert = analyze_test_project(proj, exp)
        total_found += count
        all_empty.extend(empty)
    
    print("\n" + "=" * 60)
    print(f"TOTAL TEST DECLARATIONS FOUND: {total_found}")
    print(f"TOTAL EMPTY TESTS: {len(all_empty)}")

if __name__ == "__main__":
    main()
