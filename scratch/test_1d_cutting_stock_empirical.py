"""
Empirical Adversarial Test Suite for 1D Cutting Stock Optimization Engine
Based on Section 7 of HPKATA_REVERSE_ENGINEERING_BLUEPRINT.md
"""

import math
from typing import List, Tuple, Dict, Optional

STOCK_LEN = 11700 # mm
DEFAULT_LMIN_KHO = 500 # mm

class Piece:
    def __init__(self, piece_id: str, length: float, diameter: int):
        self.piece_id = piece_id
        self.length = length
        self.diameter = diameter

    def __repr__(self):
        return f"Piece({self.piece_id}, L={self.length}, D={self.diameter})"

class CuttingPattern:
    def __init__(self, stock_length: float, pieces: List[Piece], kerf: float = 0.0, from_warehouse: bool = False):
        self.stock_length = stock_length
        self.pieces = pieces
        self.kerf = kerf
        self.from_warehouse = from_warehouse
        self.k = len(pieces)
        self.cut_length_sum = sum(p.length for p in pieces)
        self.kerf_sum = max(0, self.k - 1) * kerf
        self.total_consumed = self.cut_length_sum + self.kerf_sum
        self.remnant = self.stock_length - self.total_consumed

    def is_valid(self) -> bool:
        # Constraint: total_consumed <= stock_length
        # and remnant >= 0 (no negative waste)
        return self.remnant >= -1e-6 and all(p.length > 0 for p in self.pieces)

    def is_reusable(self, lmin_kho: float) -> bool:
        return self.remnant >= lmin_kho

    def __repr__(self):
        pts = "+".join(str(int(p.length)) for p in self.pieces)
        return f"Pattern(Stock={self.stock_length}, Pieces=[{pts}], Kerf={self.kerf_sum}, Remnant={self.remnant:.1f}, Reusable={self.is_reusable(DEFAULT_LMIN_KHO)})"

class CuttingStockResult:
    def __init__(self, patterns: List[CuttingPattern], lmin_kho: float = DEFAULT_LMIN_KHO):
        self.patterns = patterns
        self.lmin_kho = lmin_kho

        self.total_stock_used = sum(p.stock_length for p in patterns)
        self.new_bars_count = sum(1 for p in patterns if not p.from_warehouse and abs(p.stock_length - STOCK_LEN) < 1e-3)
        self.new_bars_length = self.new_bars_count * STOCK_LEN
        self.warehouse_bars_used = sum(1 for p in patterns if p.from_warehouse)
        self.warehouse_length_used = sum(p.stock_length for p in patterns if p.from_warehouse)

        self.total_cut_length = sum(p.cut_length_sum for p in patterns)
        self.total_kerf = sum(p.kerf_sum for p in patterns)
        self.total_remnant = sum(p.remnant for p in patterns)

        self.reusable_remnants = [p.remnant for p in patterns if p.remnant >= lmin_kho]
        self.scrap_remnants = [p.remnant for p in patterns if p.remnant < lmin_kho]

        self.total_reusable_length = sum(self.reusable_remnants)
        self.total_scrap_length = sum(self.scrap_remnants)

        # Gross Waste %
        if self.total_stock_used > 0:
            self.gross_waste_pct = (self.total_remnant / self.total_stock_used) * 100.0
        else:
            self.gross_waste_pct = 0.0

        # Net Waste % (Crediting reusable warehouse offcuts)
        # Formula in blueprint: sum(r_j < lmin_kho) / sum(L_j == 11700) * 100%
        if self.new_bars_length > 0:
            self.net_waste_pct = (self.total_scrap_length / self.new_bars_length) * 100.0
        else:
            # All from warehouse! Guard against division by zero
            self.net_waste_pct = 0.0 if self.total_scrap_length == 0 else (self.total_scrap_length / self.total_stock_used) * 100.0

    def verify_conservation(self) -> Tuple[bool, str]:
        # Conservation: Total Stock = Total Cut + Total Kerf + Total Remnant
        lhs = self.total_stock_used
        rhs = self.total_cut_length + self.total_kerf + self.total_remnant
        diff = abs(lhs - rhs)
        if diff > 1e-4:
            return False, f"Conservation violation: TotalStock ({lhs}) != Cut({self.total_cut_length}) + Kerf({self.total_kerf}) + Remnant({self.total_remnant}), diff={diff}"
        
        # Check non-negative remnant
        for i, pat in enumerate(self.patterns):
            if pat.remnant < -1e-5:
                return False, f"Negative waste in pattern {i}: remnant={pat.remnant}"

        # Check remnant split
        rem_sum_check = abs(self.total_remnant - (self.total_reusable_length + self.total_scrap_length))
        if rem_sum_check > 1e-4:
            return False, f"Remnant partition violation: total_remnant != reusable + scrap"

        return True, "OK"

# --- Heuristic Solvers ---

def solve_bfd(demands: List[Tuple[float, int]], warehouse: List[float] = None, kerf: float = 0.0, lmin_kho: float = DEFAULT_LMIN_KHO) -> CuttingStockResult:
    """Best-Fit Decreasing heuristic (LapDayCanhDai)"""
    pieces: List[Piece] = []
    pid = 1
    for length, qty in demands:
        for _ in range(qty):
            pieces.append(Piece(f"P{pid}", length, 20))
            pid += 1

    # Sort descending
    pieces.sort(key=lambda p: p.length, reverse=True)

    # Warehouse bars
    wh_bars = sorted(warehouse if warehouse else [], reverse=True)
    patterns: List[CuttingPattern] = []

    # Pack into warehouse bars first
    for wb_len in wh_bars:
        curr_pieces = []
        rem = wb_len
        i = 0
        while i < len(pieces):
            needed = pieces[i].length + (kerf if curr_pieces else 0.0)
            if rem >= needed:
                p = pieces.pop(i)
                curr_pieces.append(p)
                rem -= needed
            else:
                i += 1
        if curr_pieces:
            patterns.append(CuttingPattern(wb_len, curr_pieces, kerf=kerf, from_warehouse=True))

    # Remaining pieces into new 11700 mm bars using BFD
    open_bars: List[List[Piece]] = []
    for p in pieces:
        best_bar_idx = -1
        min_space_left = float('inf')
        for idx, bar in enumerate(open_bars):
            cur_consumed = sum(x.length for x in bar) + max(0, len(bar) - 1) * kerf
            needed = p.length + kerf # adding one piece increases kerf count by 1
            space_left = STOCK_LEN - cur_consumed
            if space_left >= needed and (space_left - needed) < min_space_left:
                min_space_left = space_left - needed
                best_bar_idx = idx
        
        if best_bar_idx >= 0:
            open_bars[best_bar_idx].append(p)
        else:
            # Open new 11700 bar
            if p.length > STOCK_LEN:
                raise ValueError(f"Piece length {p.length} exceeds standard stock length {STOCK_LEN}")
            open_bars.append([p])

    for bar in open_bars:
        patterns.append(CuttingPattern(STOCK_LEN, bar, kerf=kerf, from_warehouse=False))

    return CuttingStockResult(patterns, lmin_kho=lmin_kho)

def solve_exact_zero_subset(demands: List[Tuple[float, int]], kerf: float = 0.0, lmin_kho: float = DEFAULT_LMIN_KHO) -> CuttingStockResult:
    """DuBang0 exact subset sum + fallback to BFD"""
    pieces: List[Piece] = []
    pid = 1
    for length, qty in demands:
        for _ in range(qty):
            pieces.append(Piece(f"P{pid}", length, 20))
            pid += 1

    patterns: List[CuttingPattern] = []
    available_pieces = list(pieces)

    # Recursive subset sum finder for exact matches to STOCK_LEN
    def find_subset_sum(target: float, candidates: List[Piece], max_len: int = 5) -> Optional[List[Piece]]:
        # Sort descending
        candidates.sort(key=lambda p: p.length, reverse=True)
        
        def backtrack(start_idx: int, current_sum: float, current_pieces: List[Piece]) -> Optional[List[Piece]]:
            current_kerf = max(0, len(current_pieces) - 1) * kerf
            if abs((current_sum + current_kerf) - target) < 1e-4:
                return current_pieces
            if len(current_pieces) >= max_len:
                return None
            if current_sum + current_kerf > target:
                return None

            for i in range(start_idx, len(candidates)):
                # Pruning
                needed_kerf = kerf if current_pieces else 0.0
                if current_sum + candidates[i].length + current_kerf + needed_kerf > target + 1e-4:
                    continue
                res = backtrack(i + 1, current_sum + candidates[i].length, current_pieces + [candidates[i]])
                if res is not None:
                    return res
            return None

        return backtrack(0, 0.0, [])

    # Greedily extract all exact 11700 subsets
    while True:
        subset = find_subset_sum(STOCK_LEN, available_pieces)
        if subset:
            patterns.append(CuttingPattern(STOCK_LEN, subset, kerf=kerf, from_warehouse=False))
            for p in subset:
                available_pieces.remove(p)
        else:
            break

    # Pack remaining pieces via BFD
    if available_pieces:
        rem_demands: Dict[float, int] = {}
        for p in available_pieces:
            rem_demands[p.length] = rem_demands.get(p.length, 0) + 1
        rem_result = solve_bfd(list(rem_demands.items()), kerf=kerf, lmin_kho=lmin_kho)
        patterns.extend(rem_result.patterns)

    return CuttingStockResult(patterns, lmin_kho=lmin_kho)

# --- Run Empirical Stress Tests ---

def run_all_tests():
    print("=" * 70)
    print("STARTING EMPIRICAL 1D CUTTING STOCK STRESS TESTS")
    print("=" * 70)
    passed = 0
    total = 0

    # 1. Conservation on standard schedule
    total += 1
    print("\n[TEST 1] Standard Beam Schedule (Kerf=0, Stock=11700)")
    schedule_1 = [(5800, 10), (3600, 12), (2300, 8), (1200, 20)]
    res1 = solve_bfd(schedule_1, kerf=0.0, lmin_kho=500)
    ok, msg = res1.verify_conservation()
    print(f"Conservation: {ok} ({msg})")
    print(f"Total Stock: {res1.total_stock_used} mm ({res1.new_bars_count} bars)")
    print(f"Total Cut: {res1.total_cut_length} mm, Total Remnant: {res1.total_remnant} mm")
    print(f"Reusable (>500mm): {res1.total_reusable_length} mm ({len(res1.reusable_remnants)} offcuts)")
    print(f"Scrap (<500mm): {res1.total_scrap_length} mm ({len(res1.scrap_remnants)} offcuts)")
    print(f"Gross Waste: {res1.gross_waste_pct:.2f}% | Net Waste: {res1.net_waste_pct:.2f}%")
    assert ok and res1.gross_waste_pct >= 0 and res1.net_waste_pct >= 0 and res1.net_waste_pct <= res1.gross_waste_pct
    passed += 1

    # 2. Saw kerf test (kerf = 3 mm)
    total += 1
    print("\n[TEST 2] Kerf Allowance Impact (Kerf=3 mm)")
    res2 = solve_bfd(schedule_1, kerf=3.0, lmin_kho=500)
    ok, msg = res2.verify_conservation()
    print(f"Conservation with Kerf: {ok} ({msg})")
    print(f"Total Kerf Lost: {res2.total_kerf} mm")
    print(f"Gross Waste: {res2.gross_waste_pct:.2f}% | Net Waste: {res2.net_waste_pct:.2f}%")
    assert ok and res2.total_kerf > 0
    passed += 1

    # 3. Exact divisor pattern (PatternCat)
    total += 1
    print("\n[TEST 3] Exact Modular Divisors (PatternCat: 5850*2, 3900*3, 2925*4)")
    modular_schedule = [(5850, 4), (3900, 6), (2925, 8)]
    res3 = solve_exact_zero_subset(modular_schedule, kerf=0.0, lmin_kho=500)
    ok, msg = res3.verify_conservation()
    print(f"Conservation: {ok} ({msg})")
    print(f"Gross Waste: {res3.gross_waste_pct:.2f}% | Net Waste: {res3.net_waste_pct:.2f}%")
    print(f"Total Remnant: {res3.total_remnant} mm")
    assert ok and res3.gross_waste_pct < 1e-4 and res3.total_remnant < 1e-4
    passed += 1

    # 4. Warehouse offcut reuse and priority
    total += 1
    print("\n[TEST 4] Warehouse Offcut Credit & Priority Reuse")
    warehouse_stock = [6000, 4500, 3200, 1500]
    wh_schedule = [(5800, 2), (4000, 2), (2800, 2), (1200, 4)]
    res4 = solve_bfd(wh_schedule, warehouse=warehouse_stock, kerf=0.0, lmin_kho=500)
    ok, msg = res4.verify_conservation()
    print(f"Conservation: {ok} ({msg})")
    print(f"Warehouse bars used: {res4.warehouse_bars_used} | New 11.7m bars used: {res4.new_bars_count}")
    print(f"Gross Waste: {res4.gross_waste_pct:.2f}% | Net Waste: {res4.net_waste_pct:.2f}%")
    assert ok and res4.warehouse_bars_used > 0
    passed += 1

    # 5. Edge Case: 100% satisfied from warehouse (Zero new 11.7m bars)
    total += 1
    print("\n[TEST 5] Edge Case: 100% Demand Met from Warehouse (Division by Zero Defense)")
    pure_wh_stock = [5000, 4000, 3000]
    pure_wh_sched = [(2000, 2), (1500, 2), (1000, 2)]
    res5 = solve_bfd(pure_wh_sched, warehouse=pure_wh_stock, kerf=0.0, lmin_kho=500)
    ok, msg = res5.verify_conservation()
    print(f"Conservation: {ok} ({msg})")
    print(f"New 11.7m bars: {res5.new_bars_count} | Net Waste %: {res5.net_waste_pct:.2f}%")
    assert ok and res5.new_bars_count == 0
    passed += 1

    # 6. Edge Case: Piece exceeding 11,700 mm
    total += 1
    print("\n[TEST 6] Edge Case: Rebar Piece > 11,700 mm (Oversize Rejection)")
    oversize_sched = [(12000, 1)]
    rejected = False
    try:
        solve_bfd(oversize_sched)
    except ValueError as e:
        rejected = True
        print(f"Caught expected oversize error: {e}")
    assert rejected
    passed += 1

    # 7. Stress Test: Large randomized schedule (100 pieces, varying lengths)
    total += 1
    print("\n[TEST 7] Stress Test: 100-Piece Randomized Schedule")
    import random
    random.seed(42)
    random_demands = []
    for _ in range(20):
        length = round(random.uniform(800, 6000) / 50.0) * 50 # rounded to 50mm
        qty = random.randint(1, 10)
        random_demands.append((length, qty))
    
    total_pieces = sum(q for _, q in random_demands)
    print(f"Total pieces to cut: {total_pieces}")
    res7 = solve_bfd(random_demands, kerf=0.0, lmin_kho=500)
    ok, msg = res7.verify_conservation()
    print(f"Conservation: {ok} ({msg})")
    print(f"Total New 11.7m Bars Used: {res7.new_bars_count}")
    print(f"Gross Waste: {res7.gross_waste_pct:.2f}% | Net Waste: {res7.net_waste_pct:.2f}%")
    assert ok and res7.gross_waste_pct >= 0 and res7.net_waste_pct <= res7.gross_waste_pct
    passed += 1

    print("\n" + "=" * 70)
    print(f"ALL {passed}/{total} EMPIRICAL 1D CUTTING STOCK STRESS TESTS PASSED!")
    print("=" * 70)

if __name__ == "__main__":
    run_all_tests()
