import os
import glob
import re

tier_dir = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPGrasshopper\HPGrasshopper.E2E.Tests\Tiers"

suspicious_tests = []

for filepath in sorted(glob.glob(os.path.join(tier_dir, "*.cs"))):
    fname = os.path.basename(filepath)
    with open(filepath, "r", encoding="utf-8") as f:
        content = f.read()
    
    pattern = r"(\[(?:Fact|Theory)\][\s\S]*?(?:public void|public async Task)\s+(\w+)\s*\([^)]*\)[\s\S]*?\{([\s\S]*?\n    \}))"
    matches = re.findall(pattern, content)
    
    for full_decl, mname, body in matches:
        body_text = body.strip()
        lines = [l.strip() for l in body_text.split("\n") if l.strip() and not l.strip().startswith("//")]
        
        # Check if the test has NO calls to bridge, ScriptGuard, HostProfile, PipeNaming, JsonRpc, Harness, DTO methods
        # Just pure local operations
        has_substantive_call = any(
            any(k in l for k in [
                "bridge.", "ScriptGuard.", "HostProfile.", "PipeNaming.", "JsonRpc",
                "GrasshopperE2EHarness.CreateSample", "GrasshopperE2EHarness.AddComponent",
                "GrasshopperE2EHarness.ConnectPorts", "GrasshopperE2EHarness.RemoveComponent",
                "GrasshopperPipeSimulator", "ScriptArgs.", "GuardProfile.", "AnalyzerProfile.",
                "ToolValidator", "ToolLifecycle", "SeedLibrary"
            ])
            for l in lines
        )
        
        # Also check if it asserts on literals
        has_literal_assert = any(
            any(k in l for k in [
                "Assert.True(isReadOnly)", "Assert.True(timeout <= 600)", "Assert.True(durationMs > 0)",
                "Assert.Equal(72, expectedPanels)", "Assert.Equal(288, expectedCornerPoints)",
                "Assert.Contains(\"doc.Create\", preview)", "Assert.Contains(\"2.5\", argsJson)",
                "Assert.Equal(2000, widthScaled)", "Assert.True(theme == \"Light\" || theme == \"Dark\")",
                "targetMaxLines < maxAllowedLines", "targetMaxMethodLines < maxAllowedMethodLines",
                "Interlocked.Exchange(ref busy, 0)", "is \"auto\" or \"manual\"", "Math.Clamp"
            ])
            for l in lines
        )
        
        if not has_substantive_call or has_literal_assert:
            suspicious_tests.append((fname, mname, lines))

print(f"Total found: {len(suspicious_tests)}")
for fname, mname, lines in suspicious_tests:
    print(f"{fname} :: {mname}")
