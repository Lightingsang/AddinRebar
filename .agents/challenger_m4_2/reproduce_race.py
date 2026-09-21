import subprocess

print("Reproducing race condition in Timeout_InformsModelThatChangesMayHavePersisted...")
failures = 0
runs = 10

for i in range(1, runs + 1):
    proc = subprocess.run(
        ["dotnet", "run", "--no-build", "--project", "HPRobot/HPRobot.Mcp.Server.Tests/HPRobot.Mcp.Server.Tests.csproj", "--", "--filter-method", "*Timeout_InformsModelThatChangesMayHavePersisted*"],
        cwd=r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar",
        capture_output=True,
        text=True
    )
    if proc.returncode != 0:
        failures += 1
        print(f"Run {i}: FAILED")
        for l in proc.stdout.splitlines():
            if "failed" in l.lower() or "assert" in l.lower():
                print("  " + l)
    else:
        print(f"Run {i}: Passed")

print(f"\nResult: {failures}/{runs} failed")
