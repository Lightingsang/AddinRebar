import math

# Verification 1: Pile Coordinate Centroid Invariance (mathematical simulation of PileCoordinateGenerator logic)
def check_pile_layout(count, d=400.0, s=1200.0, ce=400.0):
    piles = []
    if count == 1:
        piles.append((0, 0))
    elif count == 2:
        piles.append((-s/2.0, 0))
        piles.append((s/2.0, 0))
    elif count == 3:
        piles.append((0, s / math.sqrt(3.0)))
        piles.append((-s/2.0, -s / (2.0 * math.sqrt(3.0))))
        piles.append((s/2.0, -s / (2.0 * math.sqrt(3.0))))
    elif count == 4:
        for y in [-s/2.0, s/2.0]:
            for x in [-s/2.0, s/2.0]:
                piles.append((x, y))
    elif count == 5:
        piles.append((0, 0))
        for y in [-s/2.0, s/2.0]:
            for x in [-s/2.0, s/2.0]:
                piles.append((x, y))
    elif count == 6:
        for y in [-s, 0.0, s]:
            for x in [-s/2.0, s/2.0]:
                piles.append((x, y))
    elif count == 7:
        piles.append((0, 0))
        for k in range(6):
            ang = k * math.pi / 3.0
            piles.append((s * math.cos(ang), s * math.sin(ang)))
    elif count == 8:
        for y in [-1.5*s, -0.5*s, 0.5*s, 1.5*s]:
            for x in [-s/2.0, s/2.0]:
                piles.append((x, y))
    elif count == 9:
        for y in [-s, 0.0, s]:
            for x in [-s, 0.0, s]:
                piles.append((x, y))
    else: # Arbitrary grid
        nx = int(math.ceil(math.sqrt(count)))
        ny = int(math.ceil(count / nx))
        startX = -((nx - 1) * s) / 2.0
        startY = -((ny - 1) * s) / 2.0
        idx = 1
        for r in range(ny):
            for c in range(nx):
                if idx <= count:
                    piles.append((startX + c * s, startY + r * s))
                    idx += 1
                    
    x_bar = sum(p[0] for p in piles) / len(piles)
    y_bar = sum(p[1] for p in piles) / len(piles)
    return len(piles), x_bar, y_bar

print("Testing Pile Coordinate Centroid Invariance for counts 1..16:")
all_passed = True
for c in range(1, 17):
    n, xb, yb = check_pile_layout(c)
    passed = abs(xb) < 1e-4 and abs(yb) < 1e-4
    if not passed:
        all_passed = False
        print(f"FAIL: Count {c} -> X_bar={xb}, Y_bar={yb}")
if all_passed:
    print("PASS: All pile counts 1..16 have centroid at (0, 0) within 1e-4 mm!")

# Verification 2: Scissor Crank Extension formula in StairFlightModel
def scissor_extension(dia):
    # Rule: max(500.0, 35.0 * dia)
    return max(500.0, 35.0 * dia)

assert scissor_extension(10) == 500.0 # 35*10 = 350 <= 500
assert scissor_extension(12) == 500.0 # 35*12 = 420 <= 500
assert scissor_extension(16) == 560.0 # 35*16 = 560 > 500
assert scissor_extension(20) == 700.0 # 35*20 = 700 > 500
print("PASS: Scissor crank extension clamping verified.")

# Verification 3: Unit Converter Round-trip
MmPerFoot = 304.8
def mm_to_feet(mm): return mm / MmPerFoot
def feet_to_mm(ft): return ft * MmPerFoot

for val in [0.0, 1.0, 10.0, 100.0, 1800.0, 4000.0, 11700.0]:
    round_trip = feet_to_mm(mm_to_feet(val))
    assert abs(round_trip - val) < 1e-9
print("PASS: Unit converter roundtrip verified.")
