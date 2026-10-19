import subprocess
import time
import sys

PROJECTS = [
    ("HPGrasshopper.McpBridge.Tests", r"HPGrasshopper/HPGrasshopper.McpBridge.Tests/HPGrasshopper.McpBridge.Tests.csproj", 630),
    ("HPGrasshopper.Mcp.Server.Tests", r"HPGrasshopper/HPGrasshopper.Mcp.Server.Tests/HPGrasshopper.Mcp.Server.Tests.csproj", 117),
    ("HPGrasshopper.E2E.Tests", r"HPGrasshopper/HPGrasshopper.E2E.Tests/HPGrasshopper.E2E.Tests.csproj", 181),
]

def run_pass(iteration):
    print(f"\n==================== TEST PASS ITERATION {iteration} ====================")
    total_passed = 0
    for name, csproj, expected in PROJECTS:
        cmd = f"dotnet test \"{csproj}\" -c Debug -p:DeployAddin=false"
        t0 = time.time()
        p = subprocess.run(cmd, shell=True, capture_output=True, text=True)
        dur = time.time() - t0
        
        # Parse output for "Passed: <count>" or "succeeded: <count>"
        out = p.stdout
        passed = 0
        if "Passed: " in out:
            # find Passed: N
            import re
            m = re.search(r'Passed:\s*(\d+)', out)
            if m:
                passed = int(m.group(1))
        elif "succeeded: " in out:
            import re
            m = re.search(r'succeeded:\s*(\d+)', out)
            if m:
                passed = int(m.group(1))
                
        failed = 1 if p.returncode != 0 else 0
        skipped = 0
        if "Skipped: " in out:
            import re
            m = re.search(r'Skipped:\s*(\d+)', out)
            if m:
                skipped = int(m.group(1))
                
        print(f"[{name}] Result: ExitCode={p.returncode}, Passed={passed}/{expected}, Skipped={skipped}, Failed={failed} ({dur:.2f}s)")
        if p.returncode != 0 or passed != expected or skipped > 0:
            print(f"FAILED OUTPUT FOR {name}:\n{out[:500]}")
            return False, 0
        total_passed += passed
    return True, total_passed

def main():
    for i in range(1, 3):
        ok, count = run_pass(i)
        if not ok or count != 928:
            print(f"FAILURE IN ITERATION {i}")
            sys.exit(1)
        print(f"Iteration {i} SUCCESS: {count}/928 tests passed.")
    print("\nALL STABILITY ITERATIONS PASSED: ZERO FLAKINESS.")

if __name__ == "__main__":
    main()
