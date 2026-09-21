import subprocess
import sys

failed_runs = 0
total_runs = 5

print(f"Running {total_runs} consecutive runs of 'dotnet test HPRobot/HPRobot.slnx'...")

for i in range(1, total_runs + 1):
    print(f"--- Run {i}/{total_runs} ---")
    proc = subprocess.run(
        ["dotnet", "test", "HPRobot/HPRobot.slnx", "--no-build"],
        cwd="g:/09-PROJECT AI/01_Revit/02_CshapRevit/01_AddinRebar",
        capture_output=True,
        text=True
    )
    if proc.returncode != 0:
        failed_runs += 1
        print(f"RUN {i} FAILED with exit code {proc.returncode}")
        # Print relevant error lines
        for line in proc.stdout.splitlines():
            if "failed" in line.lower() or "error" in line.lower() or "assert" in line.lower():
                print("  " + line)
    else:
        print(f"RUN {i} PASSED")

print(f"\nFinal Stress Test Result: {failed_runs} / {total_runs} failed.")
