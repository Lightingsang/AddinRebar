import subprocess
import os
import sys

BRIDGE_TEST_EXE = r'.\HPGrasshopper\HPGrasshopper.McpBridge.Tests\bin\Release\net10.0-windows7.0\HPGrasshopper.McpBridge.Tests.exe'

def run_test(filter_name=None):
    cmd = [BRIDGE_TEST_EXE, '--no-progress', '--timeout', '60s']
    if filter_name:
        cmd.extend(['--filter-method', f'*{filter_name}*'])
    res = subprocess.run(cmd, capture_output=True, text=True, cwd=os.getcwd())
    return res.returncode, res.stdout + res.stderr

def build_bridge():
    cmd1 = ['dotnet', 'build', r'HPGrasshopper\HPGrasshopper.McpBridge\HPGrasshopper.McpBridge.csproj', '-c', 'Release', '-p:DeployAddin=false', '-m:1']
    res1 = subprocess.run(cmd1, capture_output=True, text=True, cwd=os.getcwd())
    if res1.returncode != 0:
        print("BRIDGE BUILD FAILED:\n", res1.stdout, res1.stderr)
        return False
    
    cmd2 = ['dotnet', 'build', r'HPGrasshopper\HPGrasshopper.McpBridge.Tests\HPGrasshopper.McpBridge.Tests.csproj', '-c', 'Release', '-p:DeployAddin=false', '-m:1']
    res2 = subprocess.run(cmd2, capture_output=True, text=True, cwd=os.getcwd())
    if res2.returncode != 0:
        print("TESTS BUILD FAILED:\n", res2.stdout, res2.stderr)
        return False
    return True

def test_mutation(name, file_path, original_snippet, mutated_snippet, test_filter, requires_build=True):
    print(f"==================================================")
    print(f"RUNNING MUTATION: {name}")
    print(f"File: {file_path}")
    print(f"Test Filter: {test_filter}")
    
    with open(file_path, 'r', encoding='utf-8') as f:
        content = f.read()
    
    if original_snippet not in content:
        print(f"ERROR: original snippet not found in {file_path}")
        return False
    
    mutated_content = content.replace(original_snippet, mutated_snippet, 1)
    
    try:
        # Step 1: Apply mutation
        with open(file_path, 'w', encoding='utf-8') as f:
            f.write(mutated_content)
        
        if requires_build:
            built = build_bridge()
            if not built:
                print("Failed to build mutated code")
                return False
        
        # Step 2: Run test on mutated code — EXPECT FAILURE
        code, output = run_test(test_filter)
        if code != 0:
            print(f"-> SUCCESS: Test FAILED as expected on mutated invariant! (exit code {code})")
            for line in output.splitlines():
                if 'failed' in line.lower() or 'error' in line.lower() or 'assert' in line.lower():
                    print("   Observed failure:", line.strip())
                    break
            mutation_caught = True
        else:
            print(f"-> DANGER: Test PASSED despite mutated invariant! (Weak assertion!)")
            mutation_caught = False
            
    finally:
        # Step 3: Restore original
        with open(file_path, 'w', encoding='utf-8') as f:
            f.write(content)
        
        if requires_build:
            build_bridge()
            
        # Step 4: Verify test passes again
        restore_code, restore_out = run_test(test_filter)
        if restore_code == 0:
            print(f"-> Restored code passed test cleanly.")
        else:
            print(f"-> ERROR: Restored code failed test! {restore_out}")
            
    return mutation_caught

def main():
    results = []

    # Mutation 1: F13 Zoom clamp removed
    res1 = test_mutation(
        name="F13: ImageExporter zoom clamp bypass",
        file_path=r'HPGrasshopper/HPGrasshopper.McpBridge/Tools/ImageExporter.cs',
        original_snippet='double clampedZoom = Math.Clamp(zoom, MinZoom, MaxZoom);',
        mutated_snippet='double clampedZoom = zoom;',
        test_filter='F13_ImageExporter_CalculateDimensions_ClampsZoomToStrictBounds',
        requires_build=True
    )
    results.append(("F13: Zoom clamp", res1))

    # Mutation 2: F1 Write tools execution disabled check bypass
    res2 = test_mutation(
        name="F1: Write tools ExecutionEnabled check bypass",
        file_path=r'HPGrasshopper/HPGrasshopper.McpBridge/Host/GrasshopperRpcDispatcher.cs',
        original_snippet='if (isWrite && !EffectiveSettings.ExecutionEnabled)',
        mutated_snippet='if (false && isWrite && !EffectiveSettings.ExecutionEnabled)',
        test_filter='F1_NativeRpc_WriteTools_WhenExecutionDisabled_RejectWith32001',
        requires_build=True
    )
    results.append(("F1: Write tools ExecutionEnabled", res2))

    # Mutation 3: F8 Seed contract honesty transaction mode "none" mutated to "auto"
    res3 = test_mutation(
        name="F8: Seed tool transaction mode changed to auto",
        file_path=r'HPGrasshopper/HPGrasshopper.Mcp.Server/Registry/SeedLibrary/Geometry/facade_panelization/tool.json',
        original_snippet='"transaction": "none",',
        mutated_snippet='"transaction": "auto",',
        test_filter='F8_SeedContractHonesty_TransactionModeNone_AllFourSeeds',
        requires_build=False
    )
    results.append(("F8: Seed contract mode none", res3))

    # Mutation 4: F11 PlayerRunner duplicate NickName disambiguation removed
    res4 = test_mutation(
        name="F11: PlayerRunner duplicate NickName disambiguation removed",
        file_path=r'HPGrasshopper/HPGrasshopper.McpBridge/Tools/PlayerRunner.cs',
        original_snippet="""                    var outputKey = result.ContainsKey(param.NickName)
                        ? $"{param.NickName}#{param.InstanceGuid.ToString("D")[..8]}"
                        : param.NickName;""",
        mutated_snippet="""                    var outputKey = param.NickName;""",
        test_filter='F11_PlayerRunner_OutputDisambiguation_HandlesDuplicateNickNames',
        requires_build=True
    )
    results.append(("F11: Output NickName disambiguation", res4))

    # Mutation 5: F3 ScriptAnalyzer transaction type detection bypass
    res5 = test_mutation(
        name="F3: ScriptAnalyzer IsTransactionType bypass",
        file_path=r'HPGrasshopper/HPGrasshopper.McpBridge/Execution/ScriptAnalyzer.cs',
        original_snippet='return name is not null && profile.TransactionTypeNames.Contains(name);',
        mutated_snippet='return false;',
        test_filter='F3_ModeNone_RejectsExplicitAndTargetTypedTransactions',
        requires_build=True
    )
    results.append(("F3: Transaction detection (IsTransactionType)", res5))

    print("\n==================================================")
    print("MUTATION / ASSERTION STRENGTH SUMMARY:")
    all_caught = True
    for name, caught in results:
        status = "CAUGHT (ASSERTION ROBUST)" if caught else "FAILED (WEAK ASSERTION)"
        print(f"  [{status}] {name}")
        if not caught:
            all_caught = False

    if all_caught:
        print("\nALL 5 INVARIANT MUTATIONS EMPIRICALLY CAUGHT BY REGRESSION SUITE!")
    else:
        print("\nWARNING: Some mutations were not caught!")

if __name__ == '__main__':
    main()
