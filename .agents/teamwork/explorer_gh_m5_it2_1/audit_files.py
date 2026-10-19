import os
import glob
import re

tier_dir = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPGrasshopper\HPGrasshopper.E2E.Tests\Tiers"

all_flagged = []

for filepath in sorted(glob.glob(os.path.join(tier_dir, "*.cs"))):
    fname = os.path.basename(filepath)
    with open(filepath, "r", encoding="utf-8") as f:
        content = f.read()
    
    pattern = r"(\[(?:Fact|Theory)\][\s\S]*?(?:public void|public async Task)\s+(\w+)\s*\([^)]*\)[\s\S]*?\{([\s\S]*?\n    \}))"
    matches = re.findall(pattern, content)
    
    file_flagged = []
    for full_decl, mname, body in matches:
        body_text = body.strip()
        lines = [l.strip() for l in body_text.split("\n") if l.strip() and not l.strip().startswith("//")]
        
        reasons = []
        for l in lines:
            if "Assert.True(isReadOnly)" in l:
                reasons.append("Assert.True(isReadOnly)")
            if 'Assert.Contains("doc.Create", preview)' in l:
                reasons.append("Assert.Contains('doc.Create', preview)")
            if 'Assert.True(theme == "Light" || theme == "Dark")' in l:
                reasons.append("theme == 'Light' || theme == 'Dark'")
            if "Assert.True(durationMs > 0)" in l:
                reasons.append("Assert.True(durationMs > 0)")
            if 'Assert.Contains("2.5", argsJson)' in l:
                reasons.append("Assert.Contains('2.5', argsJson)")
            if "Assert.Equal(2000, widthScaled)" in l:
                reasons.append("Assert.Equal(2000, widthScaled)")
            if "Assert.True(targetMaxLines < maxAllowedLines)" in l:
                reasons.append("targetMaxLines < maxAllowedLines")
            if "Assert.True(targetMaxMethodLines < maxAllowedMethodLines)" in l:
                reasons.append("targetMaxMethodLines < maxAllowedMethodLines")
            if "Assert.True(timeout <= 600)" in l:
                reasons.append("timeout <= 600")
            if "Assert.Equal(72, expectedPanels)" in l:
                reasons.append("expectedPanels == 72")
            if "Assert.Equal(288, expectedCornerPoints)" in l:
                reasons.append("expectedCornerPoints == 288")
            if "Interlocked.Exchange(ref busy, 0)" in l:
                reasons.append("Interlocked.Exchange(ref busy, 0)")
            if 'is "auto" or "manual"' in l:
                reasons.append("transaction is 'auto' or 'manual'")
            if "Math.Clamp" in l and ("0.25" in l or "4.0" in l):
                reasons.append("Math.Clamp zoom factor local math")
            if "illegalPath.IndexOfAny" in l:
                reasons.append("illegalPath.IndexOfAny local string check")
            if "corruptedBytes[0] == 0x89" in l:
                reasons.append("corruptedBytes[0] == 0x89 local byte check")
            if "Assert.Empty(inputs)" in l and "new Dictionary<string, object>()" in body_text:
                reasons.append("Assert.Empty(new Dictionary)")
            if 'canPublish = (toolState == "tested")' in l:
                reasons.append("canPublish = (toolState == 'tested')")
            if 'hasElevations = argsJson.Contains("elevations")' in l:
                reasons.append("hasElevations on '{}'")
            if "hasElements = geom != null" in l:
                reasons.append("hasElements on null array")
            if "bool clipboardHandled = false" in l:
                reasons.append("clipboardHandled in local try/catch")
            if "bool cancelHandled = false" in l:
                reasons.append("cancelHandled in local try/catch")
            if "bool parseThrew = false" in l:
                reasons.append("parseThrew in local try/catch")
            if "truncated.Length" in l and "10_000" in body_text:
                reasons.append("truncated 10000 string locally")
            if "valid = elevations.Length > 0" in l:
                reasons.append("elevations.Length > 0 on Array.Empty")
            if "valid = len > 0.001" in l:
                reasons.append("len > 0.001 on local variable")
            if "isClosed = polygon.Count >= 3" in l:
                reasons.append("polygon.Count >= 3 on local list")
            if "valid = (dense > 0 && sparse > 0)" in l:
                reasons.append("dense > 0 && sparse > 0 on local double")
            if "validHour =" in l or "validMonth =" in l:
                reasons.append("validHour / validMonth on local int")
            if "isValid = (width > 0 && height > 0)" in l:
                reasons.append("width > 0 && height > 0 on local double")
            if "valid = !string.IsNullOrWhiteSpace(p) && File.Exists(p)" in l:
                reasons.append("File.Exists on invalid string")
            if 'isGh = (ext is ".gh" or ".ghx")' in l:
                reasons.append("ext is .gh or .ghx on local string")
            if 'req = new GrasshopperExecutionRequest("return 42;", DryRun: true)' in l and "Assert.True(req.DryRun)" in body_text and len(lines) <= 4:
                reasons.append("Assert.True(req.DryRun) on newly created DTO")
            if 'Assert.Equal("hpgrasshopper-mcp-2026", resolved)' in l and "GrasshopperE2EContracts.ResolvePipeName" in body_text:
                reasons.append("ResolvePipeName on shadow contract")
            if "GrasshopperE2EContracts.RegistryMetaTools" in body_text and len(lines) <= 4:
                reasons.append("Assert on shadow contract RegistryMetaTools")
            if "GrasshopperE2EContracts.DeniedNamespaces" in body_text and len(lines) <= 4:
                reasons.append("Assert on shadow contract DeniedNamespaces")
            if 'Assert.Equal("grasshopper", expectedHost)' in l and "GrasshopperE2EContracts.HostKey" in body_text and len(lines) <= 4:
                reasons.append("Assert on shadow contract HostKey")
            if "Assert.Equal(12, seeds.Count)" in l and "GrasshopperE2EContracts.EmbeddedSeeds" in body_text and len(lines) <= 4:
                reasons.append("Assert on shadow contract EmbeddedSeeds count")
            if 'Assert.Contains("generate_rebar_curves", seeds)' in l and len(lines) <= 4:
                reasons.append("Assert on shadow contract EmbeddedSeeds names")

        if reasons:
            file_flagged.append((mname, reasons, lines))
            all_flagged.append((fname, mname, reasons, lines))
            
    print(f"\nFILE: {fname} (Total: {len(matches)}, Flagged: {len(file_flagged)})")
    for mname, reasons, lines in file_flagged:
        print(f"  * {mname}: {', '.join(reasons)}")

print(f"\nTOTAL FLAGGED ACROSS ALL FILES: {len(all_flagged)}")
