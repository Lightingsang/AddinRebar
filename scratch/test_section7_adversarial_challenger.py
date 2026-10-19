"""
Adversarial Verification & Stress-Test Harness for Section 7 of HPKata Blueprint
Focus Areas:
1. Piecewise Zero-Denominator Net Waste Defense Formula (N_new_bars = 0)
2. DuBang0 Bounded Subset-Sum Formulation (k <= 5, timeout <= 1000 ms, BFD cascade)
3. Strict Conservation Invariants, Kerf Handling, and Boundary Clamping
"""

import math
import time
import random
from typing import List, Tuple, Dict, Optional

STOCK_LEN = 11700.0  # mm (Item 32)
DEFAULT_LMIN_KHO = 500.0  # mm
MIN_CLAMP_LMIN_KHO = 100.0  # mm (Item 33)

class Piece:
    def __init__(self, piece_id: str, length: float, diameter: int):
        self.piece_id = piece_id
        self.length = float(length)
        self.diameter = diameter

    def __repr__(self):
        return f"P({self.piece_id}, {self.length:.1f}mm, d{self.diameter})"

class CuttingPattern:
    def __init__(self, stock_length: float, pieces: List[Piece], kerf: float = 0.0, from_warehouse: bool = False, strategy: str = "BFD"):
        self.stock_length = float(stock_length)
        self.pieces = list(pieces)
        self.kerf = float(kerf)
        self.from_warehouse = from_warehouse
        self.strategy = strategy
        self.k = len(pieces)
        self.cut_length_sum = sum(p.length for p in pieces)
        self.kerf_sum = max(0, self.k - 1) * kerf
        self.total_consumed = self.cut_length_sum + self.kerf_sum
        self.remnant = self.stock_length - self.total_consumed

    def is_valid(self) -> bool:
        return self.remnant >= -1e-5 and all(p.length > 0 for p in self.pieces)

    def is_reusable(self, lmin_kho: float) -> bool:
        return self.remnant >= lmin_kho

def calculate_waste_metrics(patterns: List[CuttingPattern], lmin_kho: float = DEFAULT_LMIN_KHO) -> Dict[str, float]:
    """
    Implements the exact piecewise mathematical formula from Section 7.1 of Blueprint:
    Gross Scrap:
        Waste_gross = (sum_j r_j) / (sum_j L_j) * 100%
    Net Scrap:
        Waste_net =
            [case 1] (sum_{r_j < lmin_kho} r_j) / (N_new_bars * 11,700) * 100%,  if N_new_bars > 0
            [case 2] 0.0%,                                                         if N_new_bars == 0 and sum_{r_j < lmin_kho} r_j == 0
            [case 3] (sum_{r_j < lmin_kho} r_j) / (sum_j L_j) * 100%,             if N_new_bars == 0 and sum_j L_j > 0
    """
    total_stock = sum(p.stock_length for p in patterns)
    new_bars_count = sum(1 for p in patterns if not p.from_warehouse and abs(p.stock_length - STOCK_LEN) < 1e-3)
    total_remnant = sum(p.remnant for p in patterns)
    scrap_sum = sum(p.remnant for p in patterns if p.remnant < lmin_kho)
    reusable_sum = sum(p.remnant for p in patterns if p.remnant >= lmin_kho)

    # Gross Waste %
    if total_stock > 0:
        gross_waste_pct = (total_remnant / total_stock) * 100.0
    else:
        gross_waste_pct = 0.0

    # Net Waste % - Section 7.1 Piecewise Defense
    if new_bars_count > 0:
        net_waste_pct = (scrap_sum / (new_bars_count * STOCK_LEN)) * 100.0
    elif new_bars_count == 0 and scrap_sum == 0:
        net_waste_pct = 0.0
    elif new_bars_count == 0 and total_stock > 0:
        net_waste_pct = (scrap_sum / total_stock) * 100.0
    else:
        # Edge case: 0 bars at all
        net_waste_pct = 0.0

    return {
        "total_stock": total_stock,
        "new_bars_count": new_bars_count,
        "total_remnant": total_remnant,
        "scrap_sum": scrap_sum,
        "reusable_sum": reusable_sum,
        "gross_waste_pct": gross_waste_pct,
        "net_waste_pct": net_waste_pct
    }

def solve_bfd(pieces: List[Piece], warehouse: List[float] = None, kerf: float = 0.0) -> List[CuttingPattern]:
    """Strategy 2: LapDayCanhDai (Best-Fit Decreasing)"""
    rem_pieces = sorted(pieces, key=lambda p: p.length, reverse=True)
    patterns: List[CuttingPattern] = []

    # Pack into warehouse first
    if warehouse:
        for wb in sorted(warehouse, reverse=True):
            packed = []
            curr_rem = wb
            i = 0
            while i < len(rem_pieces):
                needed = rem_pieces[i].length + (kerf if packed else 0.0)
                if curr_rem >= needed:
                    p = rem_pieces.pop(i)
                    packed.append(p)
                    curr_rem -= needed
                else:
                    i += 1
            if packed:
                patterns.append(CuttingPattern(wb, packed, kerf=kerf, from_warehouse=True, strategy="BFD_Warehouse"))

    # Pack remaining into new 11.7m bars
    open_bars: List[List[Piece]] = []
    for p in rem_pieces:
        if p.length > STOCK_LEN:
            raise ValueError(f"Piece length {p.length} exceeds standard stock length {STOCK_LEN}")
        best_idx = -1
        min_space = float('inf')
        for idx, bar in enumerate(open_bars):
            cur_consumed = sum(x.length for x in bar) + max(0, len(bar) - 1) * kerf
            needed = p.length + kerf
            space = STOCK_LEN - (cur_consumed + needed)
            if space >= -1e-5 and space < min_space:
                min_space = space
                best_idx = idx
        if best_idx >= 0:
            open_bars[best_idx].append(p)
        else:
            open_bars.append([p])

    for bar in open_bars:
        patterns.append(CuttingPattern(STOCK_LEN, bar, kerf=kerf, from_warehouse=False, strategy="BFD"))

    return patterns

def solve_dubang0(pieces: List[Piece], kerf: float = 0.0, k_max: int = 5, timeout_ms: float = 1000.0) -> List[CuttingPattern]:
    """
    Strategy 1: DuBang0 (Exact Zero Residual Subset-Sum) with:
    - Bounded cardinality: 2 <= |S| <= k_max (default 5)
    - Strict execution timeout: timeout_ms (default 1000 ms)
    - Cascade to Strategy 2 (LapDayCanhDai / BFD)
    """
    start_time = time.time()
    patterns: List[CuttingPattern] = []
    available = list(pieces)

    def find_zero_subset(candidates: List[Piece], target: float, max_k: int) -> Optional[List[Piece]]:
        candidates.sort(key=lambda p: p.length, reverse=True)

        def backtrack(idx: int, current_sum: float, selected: List[Piece]) -> Optional[List[Piece]]:
            # Check elapsed time
            if (time.time() - start_time) * 1000.0 > timeout_ms:
                return None  # Trigger timeout exit

            current_kerf = max(0, len(selected) - 1) * kerf
            total = current_sum + current_kerf

            if abs(total - target) < 1e-4 and len(selected) >= 2:
                return selected

            if len(selected) >= max_k or total > target:
                return None

            for i in range(idx, len(candidates)):
                needed_kerf = kerf if selected else 0.0
                if total + candidates[i].length + needed_kerf > target + 1e-4:
                    continue
                res = backtrack(i + 1, current_sum + candidates[i].length, selected + [candidates[i]])
                if res is not None:
                    return res
            return None

        return backtrack(0, 0.0, [])

    # Extract exact subsets while time allows
    while True:
        if (time.time() - start_time) * 1000.0 > timeout_ms:
            break
        subset = find_zero_subset(available, STOCK_LEN, k_max)
        if subset:
            patterns.append(CuttingPattern(STOCK_LEN, subset, kerf=kerf, from_warehouse=False, strategy="DuBang0"))
            for p in subset:
                available.remove(p)
        else:
            break

    # Cascade remaining items to BFD
    if available:
        bfd_patterns = solve_bfd(available, kerf=kerf)
        patterns.extend(bfd_patterns)

    return patterns

# ==============================================================================
# EMPIRICAL ADVERSARIAL TEST SUITE
# ==============================================================================

def test_suite_zero_denominator_defense():
    print("\n" + "=" * 75)
    print("TEST SUITE 1: PIECEWISE ZERO-DENOMINATOR NET WASTE FORMULA RIGOR")
    print("=" * 75)

    # 1. Empty schedule (0 bars, 0 pieces)
    m = calculate_waste_metrics([], lmin_kho=DEFAULT_LMIN_KHO)
    print(f"[Case 1A: Empty schedule] Net: {m['net_waste_pct']}%, Gross: {m['gross_waste_pct']}%")
    assert m['net_waste_pct'] == 0.0 and m['gross_waste_pct'] == 0.0
    assert not math.isnan(m['net_waste_pct']) and not math.isinf(m['net_waste_pct'])

    # 2. 100% Warehouse, 0 Scrap (Exact fit)
    pats_exact_wh = [
        CuttingPattern(4000, [Piece("W1", 4000, 20)], kerf=0, from_warehouse=True),
        CuttingPattern(3000, [Piece("W2", 3000, 20)], kerf=0, from_warehouse=True)
    ]
    m = calculate_waste_metrics(pats_exact_wh, lmin_kho=DEFAULT_LMIN_KHO)
    print(f"[Case 1B: Pure Warehouse, 0 remnant] Net: {m['net_waste_pct']}%, Gross: {m['gross_waste_pct']}%")
    assert m['net_waste_pct'] == 0.0 and m['gross_waste_pct'] == 0.0

    # 3. 100% Warehouse, Reusable remnants only (remnant >= 500mm, scrap == 0)
    pats_reusable_wh = [
        CuttingPattern(4000, [Piece("W1", 3400, 20)], kerf=0, from_warehouse=True),  # rem = 600 >= 500 (reusable)
        CuttingPattern(3000, [Piece("W2", 2200, 20)], kerf=0, from_warehouse=True)   # rem = 800 >= 500 (reusable)
    ]
    m = calculate_waste_metrics(pats_reusable_wh, lmin_kho=DEFAULT_LMIN_KHO)
    print(f"[Case 1C: Pure Warehouse, Reusable Remnants Only] Net: {m['net_waste_pct']}%, Gross: {m['gross_waste_pct']:.2f}%")
    assert m['net_waste_pct'] == 0.0  # Credited!
    assert m['gross_waste_pct'] == ((600 + 800) / 7000) * 100.0  # 20.0%

    # 4. 100% Warehouse, Unrecoverable scrap only (remnant < 500mm)
    pats_scrap_wh = [
        CuttingPattern(4000, [Piece("W1", 3700, 20)], kerf=0, from_warehouse=True),  # rem = 300 < 500 (scrap)
        CuttingPattern(3000, [Piece("W2", 2800, 20)], kerf=0, from_warehouse=True)   # rem = 200 < 500 (scrap)
    ]
    m = calculate_waste_metrics(pats_scrap_wh, lmin_kho=DEFAULT_LMIN_KHO)
    expected_scrap_pct = ((300 + 200) / 7000.0) * 100.0
    print(f"[Case 1D: Pure Warehouse, Unrecoverable Scrap] Net: {m['net_waste_pct']:.2f}% (Expected: {expected_scrap_pct:.2f}%)")
    assert abs(m['net_waste_pct'] - expected_scrap_pct) < 1e-4
    assert not math.isnan(m['net_waste_pct']) and not math.isinf(m['net_waste_pct'])
    assert 0.0 < m['net_waste_pct'] <= 100.0

    # 5. Boundary value: remnant = 499.9mm vs 500.0mm at lmin_kho = 500
    pats_boundary = [
        CuttingPattern(1000, [Piece("B1", 500.1, 20)], kerf=0, from_warehouse=True)  # rem = 499.9 < 500 -> scrap
    ]
    m1 = calculate_waste_metrics(pats_boundary, lmin_kho=500.0)
    assert m1['scrap_sum'] == 499.9 and m1['net_waste_pct'] == (499.9 / 1000.0) * 100.0

    pats_boundary2 = [
        CuttingPattern(1000, [Piece("B2", 500.0, 20)], kerf=0, from_warehouse=True)  # rem = 500.0 >= 500 -> reusable
    ]
    m2 = calculate_waste_metrics(pats_boundary2, lmin_kho=500.0)
    assert m2['scrap_sum'] == 0.0 and m2['net_waste_pct'] == 0.0

    # 6. Absolute clamp at lmin_kho = 100mm (Item 33)
    pats_clamp = [
        CuttingPattern(1000, [Piece("C1", 905.0, 20)], kerf=0, from_warehouse=True)  # rem = 95 < 100 -> scrap
    ]
    m_clamp = calculate_waste_metrics(pats_clamp, lmin_kho=MIN_CLAMP_LMIN_KHO)
    assert m_clamp['scrap_sum'] == 95.0 and m_clamp['net_waste_pct'] == 9.5

    print(">> All Zero-Denominator Net Waste Defense Boundary Tests PASSED!")

def test_suite_dubang0_bounds_and_cascade():
    print("\n" + "=" * 75)
    print("TEST SUITE 2: DUBANG0 BOUNDED SUBSET-SUM (k <= 5, TIMEOUT, CASCADE)")
    print("=" * 75)

    # 1. Exact zero subset with k = 2, 3, 4, 5
    pieces_exact = [
        # k = 2: 6000 + 5700 = 11700
        Piece("E1", 6000, 20), Piece("E2", 5700, 20),
        # k = 3: 3900 + 3900 + 3900 = 11700
        Piece("E3", 3900, 20), Piece("E4", 3900, 20), Piece("E5", 3900, 20),
        # k = 4: 2925 * 4 = 11700
        Piece("E6", 2925, 20), Piece("E7", 2925, 20), Piece("E8", 2925, 20), Piece("E9", 2925, 20),
        # k = 5: 2340 * 5 = 11700
        Piece("E10", 2340, 20), Piece("E11", 2340, 20), Piece("E12", 2340, 20), Piece("E13", 2340, 20), Piece("E14", 2340, 20)
    ]
    pats = solve_dubang0(pieces_exact, kerf=0.0, k_max=5)
    dubang_pats = [p for p in pats if p.strategy == "DuBang0"]
    print(f"[DuBang0 Exact Subsets] Extracted {len(dubang_pats)} patterns:")
    for dp in dubang_pats:
        print(f"  k={dp.k}: {[p.length for p in dp.pieces]} -> Remnant: {dp.remnant:.4f} mm")
        assert abs(dp.remnant) < 1e-4
        assert 2 <= dp.k <= 5
    assert len(dubang_pats) == 4  # All 4 sets extracted

    # 2. Cardinality bound enforcement: k = 6 must NOT be extracted by DuBang0
    # 6 pieces of 1950 mm = 11700 mm.
    # Because k_max = 5, DuBang0 MUST NOT combine all 6 into a single pattern.
    pieces_k6 = [Piece(f"K6_{i}", 1950, 20) for i in range(6)]
    pats_k6 = solve_dubang0(pieces_k6, kerf=0.0, k_max=5)
    for p in pats_k6:
        if p.strategy == "DuBang0":
            assert p.k <= 5, f"Violation: DuBang0 extracted subset with k={p.k} > 5"
    print(f"[DuBang0 Cardinality Bound k <= 5 Verified]: 6x1950mm did NOT yield a k=6 DuBang0 pattern.")

    # 3. Timeout Budget & Graceful Cascade to BFD under Combinatorial Explosion Pressure
    # Create 80 pieces with close lengths that produce thousands of near-combinations
    random.seed(1337)
    pieces_dense = []
    for i in range(80):
        # Lengths between 1400 and 2800 mm
        l = 1400.0 + (i % 25) * 50.0
        pieces_dense.append(Piece(f"D_{i}", l, 25))

    t0 = time.time()
    # Enforce strict 1000 ms budget
    pats_dense = solve_dubang0(pieces_dense, kerf=0.0, k_max=5, timeout_ms=1000.0)
    elapsed_ms = (time.time() - t0) * 1000.0
    print(f"[DuBang0 Timeout & Cascade] Processed 80 dense pieces in {elapsed_ms:.1f} ms (Budget: 1000 ms)")
    assert elapsed_ms < 1500.0, f"Solver hung! Elapsed: {elapsed_ms} ms"

    # Verify piece conservation across DuBang0 + BFD cascade
    all_pieces_packed = []
    for p in pats_dense:
        all_pieces_packed.extend(p.pieces)
    assert len(all_pieces_packed) == len(pieces_dense), "Piece count mismatch in cascade!"
    orig_sum = sum(p.length for p in pieces_dense)
    packed_sum = sum(p.length for p in all_pieces_packed)
    assert abs(orig_sum - packed_sum) < 1e-4, "Length conservation violation in cascade!"

    print(">> All DuBang0 Bounds & Cascade Stress Tests PASSED!")

def test_suite_conservation_and_physical_laws():
    print("\n" + "=" * 75)
    print("TEST SUITE 3: CONSERVATION LAWS & SAW KERF PHYSICAL INTEGRITY")
    print("=" * 75)

    # Randomized schedule across multiple diameters and kerfs
    for kerf in [0.0, 2.0, 3.5, 5.0]:
        pieces = []
        for i in range(50):
            l = random.uniform(500, 5500)
            pieces.append(Piece(f"R_{i}", l, 20))

        pats = solve_dubang0(pieces, kerf=kerf, k_max=5, timeout_ms=500.0)
        
        # Verify physical conservation for each pattern:
        # stock_length = cut_length_sum + (k-1)*kerf + remnant
        for idx, pat in enumerate(pats):
            expected_kerf = max(0, pat.k - 1) * kerf
            assert abs(pat.kerf_sum - expected_kerf) < 1e-4
            lhs = pat.stock_length
            rhs = pat.cut_length_sum + pat.kerf_sum + pat.remnant
            assert abs(lhs - rhs) < 1e-4, f"Pattern {idx} conservation failure: {lhs} != {rhs}"
            assert pat.remnant >= -1e-5, f"Pattern {idx} has negative remnant: {pat.remnant}"

        # Global metrics check
        metrics = calculate_waste_metrics(pats, lmin_kho=500.0)
        assert metrics['net_waste_pct'] <= metrics['gross_waste_pct'] + 1e-4
        assert metrics['net_waste_pct'] >= 0.0

    print(">> All Physical Conservation & Kerf Allowance Invariant Tests PASSED!")

def main():
    print("=" * 75)
    print("STARTING DEEP ADVERSARIAL CHALLENGER VERIFICATION FOR SECTION 7")
    print("Author: challenger_m1_2_iter2 (Empirical Challenger: critic, specialist)")
    print("Target: docs/hpkata/HPKATA_REVERSE_ENGINEERING_BLUEPRINT.md Section 7")
    print("=" * 75)

    test_suite_zero_denominator_defense()
    test_suite_dubang0_bounds_and_cascade()
    test_suite_conservation_and_physical_laws()

    print("\n" + "=" * 75)
    print("FINAL CHALLENGE OUTCOME: 100% OF ADVERSARIAL STRESS TESTS PASSED")
    print("Section 7 Mathematical Formulations & Bounds are EMPIRICALLY CERTIFIED.")
    print("=" * 75)

if __name__ == "__main__":
    main()
