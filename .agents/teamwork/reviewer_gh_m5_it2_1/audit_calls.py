import os, re

tiers_dir = r"HPGrasshopper/HPGrasshopper.E2E.Tests/Tiers"
test_files = [os.path.join(tiers_dir, f) for f in os.listdir(tiers_dir) if f.startswith(("Tier1_", "Tier2_", "Tier3_")) and f.endswith(".cs")]
test_files.sort()

method_re = re.compile(r"\[Fact(?:|\([^\)]*\))\]\s*\n\s*public\s+(?:async\s+Task|void)\s+([A-Za-z0-9_]+)\s*\((.*?)\)\s*\{", re.MULTILINE)

all_methods = []

for tf in test_files:
    fname = os.path.basename(tf)
    with open(tf, "r", encoding="utf-8") as f:
        content = f.read()

    # parse methods
    lines = content.splitlines()
    i = 0
    while i < len(lines):
        line = lines[i]
        if "[Fact" in line:
            # find method header
            j = i + 1
            while j < len(lines) and "public" not in lines[j]:
                j += 1
            if j < len(lines):
                m_match = re.search(r"public\s+(?:async\s+Task|void)\s+([A-Za-z0-9_]+)\s*\(", lines[j])
                if m_match:
                    mname = m_match.group(1)
                    # collect body until matching brace
                    k = j
                    while k < len(lines) and "{" not in lines[k]:
                        k += 1
                    brace_count = 1
                    body_lines = []
                    k += 1
                    while k < len(lines) and brace_count > 0:
                        bline = lines[k]
                        brace_count += bline.count("{") - bline.count("}")
                        if brace_count > 0:
                            body_lines.append(bline)
                        k += 1
                    all_methods.append((fname, mname, j+1, "\n".join(body_lines)))
                    i = k
                    continue
        i += 1

print(f"Total Tier 1-3 test methods found: {len(all_methods)}")

# Look for methods that don't call anything in McpShared, HPGrasshopper, or Harness
calls_pattern = re.compile(r"(?:bridge\.|executor\.|simulator\.|ScriptGuard\.|ScriptQuality\.|ScriptAnalyzer\.|HostProfile\.|PipeNaming\.|ToolValidator\.|SeedInstaller\.|TransactionModes\.|RegistryToolFunction\.|GrasshopperE2EHarness\.|GrasshopperWorkloadScripts\.|GrasshopperE2EContracts\.|ScriptArgs|JsonRpc|ExecuteRequest|ToolRecord)")

non_calling = []
for fname, mname, lnum, body in all_methods:
    if not calls_pattern.search(body):
        non_calling.append((fname, mname, lnum, body))

print(f"Methods NOT calling any production/bridge/contract symbols: {len(non_calling)}")
for fname, mname, lnum, body in non_calling:
    print(f"=== {fname}:{lnum} ({mname}) ===")
    print(body)
    print()
