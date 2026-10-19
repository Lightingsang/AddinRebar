import os
import sys
import subprocess
import time
import xml.etree.ElementTree as ET

def run_cmd(cmd, cwd=None):
    t0 = time.time()
    res = subprocess.run(cmd, cwd=cwd, shell=True, capture_output=True, text=True, encoding="utf-8", errors="replace")
    dt = time.time() - t0
    return res.returncode, res.stdout, res.stderr, dt

def check_fixtures():
    fixtures_dir = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPGrasshopper\HPGrasshopper.McpBridge.Tests\Fixtures"
    print(f"Checking fixtures in: {fixtures_dir}")
    fixture_bases = ["MathSlider", "CurveGenerator", "RevitDirectShapeSyncMock"]
    results = {}
    for base in fixture_bases:
        gh_path = os.path.join(fixtures_dir, f"{base}.gh")
        ghx_path = os.path.join(fixtures_dir, f"{base}.ghx")
        
        gh_exists = os.path.isfile(gh_path)
        ghx_exists = os.path.isfile(ghx_path)
        gh_size = os.path.getsize(gh_path) if gh_exists else -1
        ghx_size = os.path.getsize(ghx_path) if ghx_exists else -1
        
        # Check ghx XML validity
        xml_valid = False
        xml_root_tag = ""
        chunk_count = 0
        if ghx_exists:
            try:
                tree = ET.parse(ghx_path)
                root = tree.getroot()
                xml_root_tag = root.tag
                xml_valid = True
                # Count chunks
                chunks = root.findall(".//chunk")
                chunk_count = len(chunks)
            except Exception as e:
                xml_valid = False
                xml_root_tag = str(e)
                
        # Check gh binary header
        bin_header = b""
        if gh_exists:
            with open(gh_path, "rb") as f:
                bin_header = f.read(16)
                
        results[base] = {
            "gh_exists": gh_exists,
            "gh_size": gh_size,
            "gh_header_hex": bin_header.hex(),
            "ghx_exists": ghx_exists,
            "ghx_size": ghx_size,
            "ghx_valid_xml": xml_valid,
            "ghx_root": xml_root_tag,
            "ghx_chunk_count": chunk_count
        }
        print(f"[{base}] .gh: {gh_size} bytes (header={bin_header.hex()[:16]}), .ghx: {ghx_size} bytes (valid_xml={xml_valid}, chunks={chunk_count})")
    return results

def main():
    root_dir = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar"
    print("=" * 60)
    print("M4 EMPIRICAL CHALLENGER: Stress Testing McpBridge.Tests")
    print("=" * 60)
    
    # 1. Check Fixtures
    fixtures = check_fixtures()
    for base, data in fixtures.items():
        if not data["gh_exists"] or not data["ghx_exists"] or not data["ghx_valid_xml"]:
            print(f"ERROR: Fixture {base} failed integrity verification!")
            sys.exit(1)
            
    print("\nFixtures verified on disk.")
    
    # 2. Run FixtureDefinitionTests specifically with detailed output
    print("\nRunning FixtureDefinitionTests specifically...")
    code, stdout, stderr, dt = run_cmd("dotnet test HPGrasshopper/HPGrasshopper.McpBridge.Tests -c Release --no-build --filter FullyQualifiedName~FixtureDefinitionTests", cwd=root_dir)
    print(f"FixtureDefinitionTests completed in {dt:.2f}s, exit code: {code}")
    print(stdout)
    if code != 0:
        print("ERROR: FixtureDefinitionTests failed!")
        sys.exit(1)

    # 3. Stress Test: Multiple Iterations of full McpBridge.Tests (110 tests)
    iterations = 3
    print(f"\nRunning {iterations} consecutive full test suite iterations for concurrency & determinism...")
    iter_results = []
    for i in range(1, iterations + 1):
        print(f"--- Iteration {i}/{iterations} ---")
        code, stdout, stderr, dt = run_cmd("dotnet test HPGrasshopper/HPGrasshopper.McpBridge.Tests -c Release --no-build", cwd=root_dir)
        passed = "Passed! - Failed: 0, Passed: 110" in stdout or "Passed: 110" in stdout
        iter_results.append({
            "iteration": i,
            "exit_code": code,
            "duration": dt,
            "passed": passed
        })
        print(f"Iteration {i}: ExitCode={code}, Duration={dt:.2f}s, Passed={passed}")
        if code != 0 or not passed:
            print(f"ERROR: Iteration {i} failed!")
            print(stdout)
            print(stderr)
            sys.exit(1)
            
    print("\n" + "=" * 60)
    print(f"STRESS TEST SUMMARY: {len(iter_results)}/{iterations} iterations PASSED deterministically.")
    for r in iter_results:
        print(f"  Iteration {r['iteration']}: {r['duration']:.2f}s, 110/110 passed")
    print("=" * 60)

if __name__ == "__main__":
    main()
