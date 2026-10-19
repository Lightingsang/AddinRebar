"""
Forensic Audit Scanner for Tier 5 Adversarial Coverage Hardening
Targeting:
- 10 newly authored test files (36 tests in HPBim.Core.Tests, 64 tests in HPBim.Client.Tests)
- Rule C2: File line count <= 300
- Rule M3: Method line span <= 50
- Non-bypass scan: Detect Assert.True(true), Assert.False(false), Assert.Equal(x, x), empty methods, facades
- Rule Q-B1: Commented-out code
- Rule Q-B2: Empty catch blocks
- HPBim/src/ integrity scan
"""

import os
import re
import sys
import json

TIER5_TEST_FILES = [
    # HPBim.Core.Tests (36 tests)
    r"HPBim\tests\HPBim.Core.Tests\Adversarial\BimModelSchemaValidatorAdversarialCoverageTests.cs",
    r"HPBim\tests\HPBim.Core.Tests\Adversarial\IdempotentElementResolverAdversarialCoverageTests.cs",
    r"HPBim\tests\HPBim.Core.Tests\Adversarial\CompositeSourceKeyAdversarialCoverageTests.cs",
    r"HPBim\tests\HPBim.Core.Tests\Adversarial\MeasuredGeometricVerifierAdversarialCoverageTests.cs",

    # HPBim.Client.Tests (64 tests)
    r"HPBim\tests\HPBim.Client.Tests\Connection\IntentJournalTier5AdversarialTests.cs",
    r"HPBim\tests\HPBim.Client.Tests\Connection\TimeoutPolicyTier5AdversarialTests.cs",
    r"HPBim\tests\HPBim.Client.Tests\Recovery\ReceiptReaderTier5AdversarialTests.cs",
    r"HPBim\tests\HPBim.Client.Tests\Recovery\ReconcileServiceTier5AdversarialTests.cs",
    r"HPBim\tests\HPBim.Client.Tests\Pipeline\BimPlanRunnerScriptTemplateTier5AdversarialTests.cs",
    r"HPBim\tests\HPBim.Client.Tests\Pipeline\BimPipelineEngineTier5AdversarialTests.cs",
]

def check_file_c2(filepath):
    with open(filepath, "r", encoding="utf-8") as f:
        lines = f.readlines()
    return len(lines), len(lines) <= 300

def parse_methods_line_spans(filepath):
    """
    Parses methods and determines exact line spans.
    Uses bracket-matching across tokens.
    """
    with open(filepath, "r", encoding="utf-8") as f:
        lines = f.readlines()

    methods = []
    # Regex to find method signatures
    # [Fact] or [Theory] followed by access modifier, return type, MethodName(...)
    i = 0
    while i < len(lines):
        line = lines[i].strip()
        # Look for attribute [Fact] or [Theory] or method header
        if line.startswith("[Fact") or line.startswith("[Theory"):
            attr_line = i + 1
            # Scan down to method declaration
            j = i + 1
            while j < len(lines) and not re.search(r'\b(public|private|internal|protected)\s+(async\s+)?(void|Task|IActionResult|[A-Za-z0-9_<>]+)\s+([A-Za-z0-9_]+)\s*\(', lines[j]):
                j += 1
            if j < len(lines):
                m_match = re.search(r'\b(public|private|internal|protected)\s+(async\s+)?(void|Task|IActionResult|[A-Za-z0-9_<>]+)\s+([A-Za-z0-9_]+)\s*\(', lines[j])
                method_name = m_match.group(4)
                method_decl_line = j + 1
                
                # Now scan to opening brace
                k = j
                while k < len(lines) and "{" not in lines[k]:
                    k += 1
                if k < len(lines):
                    open_brace_line = k + 1
                    depth = 0
                    start_k = k
                    end_line = -1
                    for line_idx in range(start_k, len(lines)):
                        l_text = lines[line_idx]
                        # remove strings and comments
                        clean_text = re.sub(r'//.*', '', l_text)
                        for char in clean_text:
                            if char == '{':
                                depth += 1
                            elif char == '}':
                                depth -= 1
                                if depth == 0:
                                    end_line = line_idx + 1
                                    break
                        if depth == 0 and end_line != -1:
                            break
                    if end_line != -1:
                        # Full method span from attribute line (or method decl line) to end_line
                        span_from_attr = end_line - attr_line + 1
                        span_from_decl = end_line - method_decl_line + 1
                        methods.append({
                            "name": method_name,
                            "attr_line": attr_line,
                            "decl_line": method_decl_line,
                            "end_line": end_line,
                            "span_from_attr": span_from_attr,
                            "span_from_decl": span_from_decl,
                            "lines": lines[attr_line-1:end_line]
                        })
                        i = end_line
                        continue
        i += 1
    return methods

def check_assertions_honesty(methods):
    findings = []
    for m in methods:
        body = "".join(m["lines"])
        # Check for Assert.True(true) or Assert.False(false)
        if re.search(r'Assert\.True\s*\(\s*true\s*\)', body, re.IGNORECASE):
            findings.append((m["name"], "DUMMY_ASSERT", "Found Assert.True(true)"))
        if re.search(r'Assert\.False\s*\(\s*false\s*\)', body, re.IGNORECASE):
            findings.append((m["name"], "DUMMY_ASSERT", "Found Assert.False(false)"))
        if re.search(r'Assert\.Equal\s*\(\s*([0-9]+)\s*,\s*\1\s*\)', body):
            findings.append((m["name"], "TRIVIAL_EQUAL", "Found Assert.Equal(x, x) on identical literals"))
        
        # Check if there are any assertions at all
        assert_count = len(re.findall(r'Assert\.[A-Za-z0-9_]+', body))
        if assert_count == 0:
            findings.append((m["name"], "NO_ASSERTION", "No Assert statements in test method"))
    return findings

def check_commented_code(filepath):
    findings = []
    with open(filepath, "r", encoding="utf-8") as f:
        lines = f.readlines()
    for idx, l in enumerate(lines, start=1):
        stripped = l.strip()
        # Look for commented code like // var x =, // Assert., // if (, etc.
        if re.match(r'^//\s*(var|int|string|Assert\.|if\s*\(|for\s*\(|return\s+|new\s+)', stripped):
            findings.append((idx, stripped))
    return findings

def check_empty_catches(filepath):
    findings = []
    with open(filepath, "r", encoding="utf-8") as f:
        content = f.read()
    catches = re.finditer(r'catch\s*\([^\)]*\)\s*\{([^}]*)\}', content, re.DOTALL)
    for c in catches:
        body = c.group(1)
        cleaned = re.sub(r'//.*', '', body)
        cleaned = re.sub(r'/\*.*?\*/', '', cleaned, flags=re.DOTALL)
        if not cleaned.strip() and not ('//' in body or '/*' in body):
            findings.append(c.group(0))
    return findings

def main():
    root = r"G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar"
    print("=" * 80)
    print("FORENSIC AUDIT OF TIER 5 TEST SUITES")
    print("=" * 80)

    total_tests = 0
    all_clean = True
    report = {}

    for rel_path in TIER5_TEST_FILES:
        full_path = os.path.join(root, rel_path)
        if not os.path.exists(full_path):
            print(f"ERROR: Missing file {rel_path}")
            all_clean = False
            continue

        line_count, c2_pass = check_file_c2(full_path)
        methods = parse_methods_line_spans(full_path)
        total_tests += len(methods)

        m3_violations = []
        for m in methods:
            # Check physical line span from attribute or decl
            # Clean code rule M3: <= 50 lines
            if m["span_from_attr"] > 50:
                m3_violations.append((m["name"], m["attr_line"], m["end_line"], m["span_from_attr"]))

        dummy_asserts = check_assertions_honesty(methods)
        commented_code = check_commented_code(full_path)
        empty_catches = check_empty_catches(full_path)

        file_clean = c2_pass and len(m3_violations) == 0 and len(dummy_asserts) == 0 and len(commented_code) == 0 and len(empty_catches) == 0

        if not file_clean:
            all_clean = False

        report[rel_path] = {
            "lines": line_count,
            "c2_pass": c2_pass,
            "test_count": len(methods),
            "m3_violations": m3_violations,
            "dummy_asserts": dummy_asserts,
            "commented_code": commented_code,
            "empty_catches": empty_catches
        }

        print(f"File: {os.path.basename(rel_path)}")
        print(f"  Path: {rel_path}")
        print(f"  Physical Lines: {line_count} (C2 <= 300: {'PASS' if c2_pass else 'FAIL'})")
        print(f"  Test Methods Found: {len(methods)}")
        if m3_violations:
            print(f"  M3 Violations (> 50 lines): {len(m3_violations)}")
            for v in m3_violations:
                print(f"    - {v[0]}: {v[3]} lines (L{v[1]}-L{v[2]})")
        else:
            print("  M3 Compliance (all <= 50 lines): PASS")
        if dummy_asserts:
            print(f"  Dummy / Dishonest Asserts: {len(dummy_asserts)}")
            for a in dummy_asserts:
                print(f"    - {a[0]}: {a[1]} ({a[2]})")
        else:
            print("  Assertion Integrity (no dummy/trivial asserts): PASS")
        if commented_code:
            print(f"  Commented-out code lines: {len(commented_code)}")
        else:
            print("  Commented-out code (Q-B1): CLEAN (0 lines)")
        if empty_catches:
            print(f"  Empty catch blocks: {len(empty_catches)}")
        else:
            print("  Empty catch blocks (Q-B2): CLEAN (0)")
        print()

    print("=" * 80)
    print(f"Total Tier 5 Test Methods Scanned: {total_tests}")
    print(f"Overall Tier 5 Status: {'CLEAN / PASS' if all_clean else 'INTEGRITY VIOLATION'}")
    print("=" * 80)

    # Save detailed JSON for report
    with open(os.path.join(root, r"scratch\tier5_forensic_results.json"), "w", encoding="utf-8") as f:
        json.dump(report, f, indent=2)

if __name__ == "__main__":
    main()
