import os
import sys
import time
import hashlib
import subprocess
import glob

BASE_DIR = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar"

FIXTURE_FILES = [
    r"HPGrasshopper\HPGrasshopper.McpBridge.Tests\Fixtures\ContinuousBeamRenumbering.gh",
    r"HPGrasshopper\HPGrasshopper.McpBridge.Tests\Fixtures\ContinuousBeamRenumbering.ghx",
    r"HPGrasshopper\HPGrasshopper.McpBridge.Tests\Fixtures\CurveGenerator.gh",
    r"HPGrasshopper\HPGrasshopper.McpBridge.Tests\Fixtures\CurveGenerator.ghx",
    r"HPGrasshopper\HPGrasshopper.McpBridge.Tests\Fixtures\MathSlider.gh",
    r"HPGrasshopper\HPGrasshopper.McpBridge.Tests\Fixtures\MathSlider.ghx",
    r"HPGrasshopper\HPGrasshopper.McpBridge.Tests\Fixtures\RevitDirectShapeSyncMock.gh",
    r"HPGrasshopper\HPGrasshopper.McpBridge.Tests\Fixtures\RevitDirectShapeSyncMock.ghx",
    r"HPGrasshopper\tools\harness\fixtures\ContinuousBeamRenumbering.gh",
    r"HPGrasshopper\tools\harness\fixtures\ContinuousBeamRenumbering.ghx",
    r"HPGrasshopper\tools\harness\fixtures\CurveGenerator.gh",
    r"HPGrasshopper\tools\harness\fixtures\CurveGenerator.ghx",
    r"HPGrasshopper\tools\harness\fixtures\MathSlider.gh",
    r"HPGrasshopper\tools\harness\fixtures\MathSlider.ghx",
    r"HPGrasshopper\tools\harness\fixtures\RevitDirectShapeSyncMock.gh",
    r"HPGrasshopper\tools\harness\fixtures\RevitDirectShapeSyncMock.ghx",
    r"HPGrasshopper\definitions\ContinuousBeamRenumbering.gh",
    r"HPGrasshopper\definitions\ContinuousBeamRenumbering.ghx",
]

def hash_file(filepath):
    h = hashlib.sha256()
    with open(filepath, "rb") as f:
        while chunk := f.read(65536):
            h.update(chunk)
    return h.hexdigest()

def get_fixture_snapshot():
    snapshot = {}
    for rel in FIXTURE_FILES:
        full = os.path.join(BASE_DIR, rel)
        snapshot[rel] = {
            "size": os.path.getsize(full),
            "mtime": os.path.getmtime(full),
            "sha256": hash_file(full)
        }
    return snapshot

def count_temp_dirs():
    temp_dir = os.environ.get("TEMP", "")
    pattern = os.path.join(temp_dir, "HPGrasshopper_Fixtures_*")
    matches = glob.glob(pattern)
    return len(matches), matches

def run_cmd(cmd, cwd=BASE_DIR, timeout=120):
    t0 = time.time()
    p = subprocess.run(cmd, cwd=cwd, capture_output=True, text=True, timeout=timeout, shell=True)
    dur = time.time() - t0
    return p.returncode, p.stdout, p.stderr, dur

def main():
    print("=== STARTING 3-RUN EMPIRICAL STABILITY HARNESS ===")
    baseline = get_fixture_snapshot()
    initial_temp_count, _ = count_temp_dirs()
    print(f"Initial leaked %TEMP% directories: {initial_temp_count}")

    bridge_exe = os.path.join(
        BASE_DIR,
        r"HPGrasshopper\HPGrasshopper.McpBridge.Tests\bin\Release\net10.0-windows7.0\HPGrasshopper.McpBridge.Tests.exe"
    )
    server_proj = os.path.join(
        BASE_DIR,
        r"HPGrasshopper\HPGrasshopper.Mcp.Server.Tests\HPGrasshopper.Mcp.Server.Tests.csproj"
    )
    e2e_proj = os.path.join(
        BASE_DIR,
        r"HPGrasshopper\HPGrasshopper.E2E.Tests\HPGrasshopper.E2E.Tests.csproj"
    )

    all_passed = True
    for iteration in range(1, 4):
        print(f"\n--- ITERATION {iteration} / 3 ---")
        t_iter_start = time.time()

        # 1. Server tests
        code, out, err, dur = run_cmd(f'dotnet test "{server_proj}" -c Release --no-build')
        server_pass = "Passed: 77" in out and code == 0
        print(f"Iteration {iteration} Server Tests (77): {'PASS' if server_pass else 'FAIL'} in {dur:.2f}s")
        if not server_pass:
            print("Server test output:", out[:500])
            all_passed = False

        # 2. E2E tests
        code, out, err, dur = run_cmd(f'dotnet test "{e2e_proj}" -c Release --no-build')
        e2e_pass = "Passed: 181" in out and code == 0
        print(f"Iteration {iteration} E2E Tests (181): {'PASS' if e2e_pass else 'FAIL'} in {dur:.2f}s")
        if not e2e_pass:
            print("E2E test output:", out[:500])
            all_passed = False

        # 3. Bridge tests
        code, out, err, dur = run_cmd(f'"{bridge_exe}" --no-progress --timeout 120s')
        bridge_pass = "succeeded: 514" in out and "failed: 0" in out and code == 0
        print(f"Iteration {iteration} Bridge Tests (514): {'PASS' if bridge_pass else 'FAIL'} in {dur:.2f}s")
        if not bridge_pass:
            print("Bridge test output:", out[:500])
            all_passed = False

        # 4. Check fixture integrity
        current = get_fixture_snapshot()
        fixture_mutated = False
        for rel in FIXTURE_FILES:
            if current[rel]["sha256"] != baseline[rel]["sha256"]:
                print(f"VIOLATION: Fixture {rel} SHA256 modified!")
                fixture_mutated = True
            if current[rel]["mtime"] != baseline[rel]["mtime"]:
                print(f"VIOLATION: Fixture {rel} mtime modified!")
                fixture_mutated = True
        if fixture_mutated:
            all_passed = False
            print(f"Iteration {iteration} Fixture Check: FAIL (Fixtures modified)")
        else:
            print(f"Iteration {iteration} Fixture Check: PASS (18/18 fixtures byte-identical, mtimes unchanged)")

        # 5. Check %TEMP% leakage
        leaked_count, leaked_dirs = count_temp_dirs()
        if leaked_count != initial_temp_count:
            print(f"VIOLATION: {leaked_count} leaked directories in %TEMP%: {leaked_dirs}")
            all_passed = False
        else:
            print(f"Iteration {iteration} Temp Cleanup Check: PASS (0 leaked directories in %TEMP%)")

        iter_dur = time.time() - t_iter_start
        print(f"Iteration {iteration} completed in {iter_dur:.2f}s. Total tests verified: 772.")

    print("\n=== MULTI-RUN STABILITY SUMMARY ===")
    if all_passed:
        print("RESULT: ALL 3 ITERATIONS PASSED (3 x 772 = 2,316 tests passed). ZERO FLAKINESS, ZERO FIXTURE WRITES, ZERO TEMP LEAKS.")
    else:
        print("RESULT: FAILURE DETECTED DURING MULTI-RUN.")
        sys.exit(1)

if __name__ == "__main__":
    main()
