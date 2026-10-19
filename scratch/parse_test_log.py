import os

log_path = r"G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPGrasshopper\HPGrasshopper.McpBridge.Tests\bin\Debug\net10.0-windows7.0\TestResults\HPGrasshopper.McpBridge.Tests_net10.0-windows7.0_x64.log"

for enc in ["utf-16", "utf-16-le", "utf-8", "latin-1"]:
    try:
        with open(log_path, "r", encoding=enc) as f:
            content = f.read()
        print(f"Success reading with {enc}, length: {len(content)}")
        lines = content.splitlines()
        for idx, line in enumerate(lines):
            if any(k in line.lower() for k in ["failed", "failure", "exception", "assert"]):
                if not any(k in line for k in ["--fail-", "Failed: 0", "Failures: 0"]):
                    print(f"L{idx+1}: {line}")
                    for offset in range(1, 6):
                        if idx + offset < len(lines):
                            print(f"  +>{lines[idx+offset]}")
        break
    except Exception as e:
        continue
