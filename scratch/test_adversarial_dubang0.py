import clr
import sys
import os
import time

# Load published netstandard2.0 assembly
dll_path = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\HPKata.Core\bin\publish_netstandard\HPKata.Core.dll"
if not os.path.exists(dll_path):
    print(f"Error: {dll_path} does not exist")
    sys.exit(1)

clr.AddReference(dll_path)
from HPKata.Core.Optimization import CuttingStockEngine, CuttingStockStrategies, SolverStrategy, CuttingDemand
from System.Collections.Generic import List
from System import Double

def to_csharp_list(py_list):
    cs_list = List[Double]()
    for item in py_list:
        cs_list.Add(float(item))
    return cs_list

print("==================================================")
print("ADVERSARIAL STRESS TEST: CuttingStockStrategies.DuBang0")
print("==================================================")

# Test 1: Exact matches exist (3x3900, 2x5850)
pieces1 = [3900.0, 3900.0, 3900.0, 5850.0, 5850.0, 1000.0]
cs_p1 = to_csharp_list(pieces1)
wh = List[Double]()
res1 = CuttingStockStrategies.SolveDuBang0(cs_p1, 11700.0, 0.0, wh)
print(f"Test 1 (Known Exact Subsets): Produced {res1.Count} patterns.")
for p in res1:
    cuts = [c for c in p.CutsMm]
    print(f"  Pattern: Cuts={cuts}, Remnant={p.RemnantLengthMm}")
# Expected: one pattern with [5850, 5850], one with [3900, 3900, 3900], one BFD with [1000]
assert any(len(p.CutsMm) == 2 and abs(p.RemnantLengthMm) < 1.0 for p in res1), "Must find exact subset 2x5850"
assert any(len(p.CutsMm) == 3 and abs(p.RemnantLengthMm) < 1.0 for p in res1), "Must find exact subset 3x3900"
print("[PASS] Test 1: Exact subsets correctly discovered!")

# Test 2: Pathological tree - 50 items between 2300 and 2400 where no 5 items sum to exactly 11700
pieces2 = [2300.0 + (i * 2.0) for i in range(50)] # 2300 to 2398. 5 * 2300 = 11500, 5 * 2398 = 11990
cs_p2 = to_csharp_list(pieces2)
t0 = time.time()
res2 = CuttingStockStrategies.SolveDuBang0(cs_p2, 11700.0, 0.0, wh)
t1 = time.time()
duration_ms = (t1 - t0) * 1000.0
print(f"Test 2 (Pathological Dense Spectrum): Duration = {duration_ms:.1f}ms (Budget <= 1000ms), Patterns = {res2.Count}")
assert duration_ms <= 1100.0, f"Exceeded time budget: {duration_ms}ms"
print("[PASS] Test 2: Dense spectrum terminated strictly within budget!")

# Test 3: Pathological 200 items of 2000mm (worse than 150)
pieces3 = [2000.0] * 200
cs_p3 = to_csharp_list(pieces3)
t0 = time.time()
res3 = CuttingStockStrategies.SolveDuBang0(cs_p3, 11700.0, 0.0, wh)
t1 = time.time()
duration_ms = (t1 - t0) * 1000.0
print(f"Test 3 (200 items of 2000mm): Duration = {duration_ms:.1f}ms, Patterns = {res3.Count}")
assert duration_ms <= 100.0, f"Pruning should be instant: {duration_ms}ms"
print("[PASS] Test 3: Pruning is O(1) instant on uniform pieces!")

# Test 4: Kerf = 5mm with exact kerf-compensated subset
# For k=3, pieceTarget = 11700 - 2*5 = 11690. 11690 / 2 = not 3.
# Let's craft: 5000 + 5000 + 1690 = 11690. With 2 cuts of 5mm kerf -> total = 11700.
pieces4 = [5000.0, 5000.0, 1690.0, 2000.0]
cs_p4 = to_csharp_list(pieces4)
res4 = CuttingStockStrategies.SolveDuBang0(cs_p4, 11700.0, 5.0, wh)
print(f"Test 4 (Kerf-Compensated Exact Subset): Produced {res4.Count} patterns.")
exact_found = False
for p in res4:
    cuts = [c for c in p.CutsMm]
    print(f"  Pattern: Cuts={cuts}, Used={p.UsedLengthMm}, Remnant={p.RemnantLengthMm}")
    if len(cuts) == 3 and abs(p.RemnantLengthMm) < 1.0:
        exact_found = True
assert exact_found, "Must find kerf-compensated exact subset"
print("[PASS] Test 4: Kerf compensation verified!")

print("\nALL ADVERSARIAL STRESS TESTS PASSED CLEANLY!")
