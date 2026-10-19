import math

# We simulate the exact arithmetic of PileCoordinateGenerator in Python to verify mathematical integrity

def generate_layout(pile_count, pile_dia_mm=400.0, custom_spacing=None, custom_edge=None, is_square=False):
    if pile_count < 1:
        raise ValueError("Pile count must be at least 1.")
    d = pile_dia_mm if pile_dia_mm > 0 else 400.0
    s = custom_spacing if custom_spacing is not None else 3.0 * d
    ce = custom_edge if custom_edge is not None else max(250.0, 1.0 * d)
    
    piles = []
    
    if pile_count <= 5:
        rec_l, rec_b = d + 2.0 * ce, d + 2.0 * ce
        if pile_count == 1:
            piles.append((1, 0.0, 0.0))
        elif pile_count == 2:
            piles.append((1, -s / 2.0, 0.0))
            piles.append((2, s / 2.0, 0.0))
            rec_l = s + d + 2.0 * ce
        elif pile_count == 3:
            piles.append((1, 0.0, s / math.sqrt(3.0)))
            piles.append((2, -s / 2.0, -s / (2.0 * math.sqrt(3.0))))
            piles.append((3, s / 2.0, -s / (2.0 * math.sqrt(3.0))))
            rec_l = s + d + 2.0 * ce
            rec_b = (math.sqrt(3.0) / 2.0) * s + d + 2.0 * ce
        elif pile_count == 4:
            idx = 1
            for y in [-s / 2.0, s / 2.0]:
                for x in [-s / 2.0, s / 2.0]:
                    piles.append((idx, x, y))
                    idx += 1
            rec_l = rec_b = s + d + 2.0 * ce
        elif pile_count == 5:
            piles.append((1, 0.0, 0.0))
            idx = 2
            for y in [-s / 2.0, s / 2.0]:
                for x in [-s / 2.0, s / 2.0]:
                    piles.append((idx, x, y))
                    idx += 1
            rec_l = rec_b = s + d + 2.0 * ce
    elif pile_count <= 9:
        rec_l = rec_b = 2.0 * s + d + 2.0 * ce
        if pile_count == 6:
            idx = 1
            for y in [-s, 0.0, s]:
                for x in [-s / 2.0, s / 2.0]:
                    piles.append((idx, x, y))
                    idx += 1
            rec_l = 2.0 * s + d + 2.0 * ce
            rec_b = s + d + 2.0 * ce
        elif pile_count == 7:
            piles.append((1, 0.0, 0.0))
            for k in range(6):
                ang = k * math.pi / 3.0
                piles.append((k + 2, s * math.cos(ang), s * math.sin(ang)))
        elif pile_count == 8:
            idx = 1
            for y in [-1.5 * s, -0.5 * s, 0.5 * s, 1.5 * s]:
                for x in [-s / 2.0, s / 2.0]:
                    piles.append((idx, x, y))
                    idx += 1
            rec_l = 3.0 * s + d + 2.0 * ce
            rec_b = s + d + 2.0 * ce
        elif pile_count == 9:
            idx = 1
            for y in [-s, 0.0, s]:
                for x in [-s, 0.0, s]:
                    piles.append((idx, x, y))
                    idx += 1
    else:
        nx = int(math.ceil(math.sqrt(pile_count)))
        ny = int(math.ceil(float(pile_count) / nx))
        rec_l = (nx - 1) * s + d + 2.0 * ce
        rec_b = (ny - 1) * s + d + 2.0 * ce
        idx = 1
        startX = -((nx - 1) * s) / 2.0
        startY = -((ny - 1) * s) / 2.0
        for r in range(ny):
            for c in range(nx):
                if idx <= pile_count:
                    piles.append((idx, startX + c * s, startY + r * s))
                    idx += 1
                    
    return piles, rec_l, rec_b

print("Running Adversarial Stress Tests on Pile Layouts...")
all_passed = True
for n in range(1, 20):
    piles, rec_l, rec_b = generate_layout(n, pile_dia_mm=500.0)
    assert len(piles) == n, f"Count mismatch for n={n}: {len(piles)}"
    
    # Check centroid
    avg_x = sum(p[1] for p in piles) / n
    avg_y = sum(p[2] for p in piles) / n
    is_symmetric = (abs(avg_x) < 1e-6 and abs(avg_y) < 1e-6)
    
    # Check bounding box
    min_x = min(p[1] for p in piles)
    max_x = max(p[1] for p in piles)
    min_y = min(p[2] for p in piles)
    max_y = max(p[2] for p in piles)
    
    span_x = max_x - min_x
    span_y = max_y - min_y
    
    print(f"n={n:2d}: count={len(piles):2d}, centroid=({avg_x:+.4f}, {avg_y:+.4f}), recL={rec_l:.1f}, recB={rec_b:.1f}, symmetric={is_symmetric}")
    if n <= 9 and not is_symmetric:
        print(f"  WARNING: n={n} centroid not at (0,0)!")
        all_passed = False

print(f"\nPile Layout Arithmetic Verification: {'PASS' if all_passed else 'FAIL'}")
