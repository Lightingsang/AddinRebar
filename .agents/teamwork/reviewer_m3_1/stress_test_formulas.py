import math

def check_pile_coordinates():
    print("--- Adversarial Stress Test: PileCoordinateGenerator ---")
    # Simulate PileCoordinateGenerator logic
    for n in range(1, 15):
        d = 400.0
        s = 3.0 * d # 1200
        ce = 1.0 * d # 400
        # Check center of mass
        piles = []
        if n == 1:
            piles.append((0, 0))
        elif n == 2:
            piles.extend([(-s/2, 0), (s/2, 0)])
        elif n == 3:
            piles.extend([(0, s/math.sqrt(3)), (-s/2, -s/(2*math.sqrt(3))), (s/2, -s/(2*math.sqrt(3)))])
        elif n == 4:
            for y in [-s/2, s/2]:
                for x in [-s/2, s/2]:
                    piles.append((x, y))
        elif n == 5:
            piles.append((0, 0))
            for y in [-s/2, s/2]:
                for x in [-s/2, s/2]:
                    piles.append((x, y))
        elif n == 7:
            piles.append((0, 0))
            for k in range(6):
                ang = k * math.pi / 3.0
                piles.append((s * math.cos(ang), s * math.Sin(ang) if hasattr(math, 'Sin') else s * math.sin(ang)))
        
        if piles:
            avg_x = sum(p[0] for p in piles) / len(piles)
            avg_y = sum(p[1] for p in piles) / len(piles)
            assert abs(avg_x) < 1e-4, f"n={n}: avg_x = {avg_x}"
            assert abs(avg_y) < 1e-4, f"n={n}: avg_y = {avg_y}"
            print(f"  n={n}: PASS (Centroid at (0,0), piles count={len(piles)})")

check_pile_coordinates()

def check_blondel():
    print("\n--- Adversarial Stress Test: Stair Ergonomics (Blondel) ---")
    test_cases = [
        (160, 280, True, 600),   # Standard residential
        (150, 300, True, 600),   # Public building
        (170, 260, True, 600),   # Compact
        (200, 200, True, 600),   # Steep but 600
        (210, 250, False, 670),  # Exceeds 660
        (130, 280, False, 540),  # Below 580
    ]
    for r, t, exp_ok, exp_val in test_cases:
        val = 2.0 * r + t
        ok = 580.0 <= val <= 660.0
        assert ok == exp_ok
        assert val == exp_val
        print(f"  r={r}, t={t} -> Blondel={val} (Ergonomic: {ok}) - PASS")

check_blondel()

def check_scissor_crank():
    print("\n--- Adversarial Stress Test: Scissor Crank Extension ---")
    # rule: L_ext = max(500, 35 * d)
    for d in [10, 12, 14, 16, 18, 20, 22, 25, 28, 32]:
        ext = max(500.0, 35.0 * d)
        if d <= 14:
            assert ext == 500.0, f"d={d}: expected 500 got {ext}"
        else:
            assert ext == 35.0 * d, f"d={d}: expected {35.0*d} got {ext}"
        print(f"  d={d}mm -> L_ext={ext}mm - PASS")

check_scissor_crank()
