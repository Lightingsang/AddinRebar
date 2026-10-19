import math

def test_framing_off_line_support():
    # Beam along X axis: (0,0,0) to (10000,0,0)
    # Support at (5000, 50000, 0) -- 50 meters away!
    start = (0.0, 0.0, 0.0)
    end = (10000.0, 0.0, 0.0)
    dir_vec = (end[0]-start[0], end[1]-start[1], end[2]-start[2])
    total_len = 10000.0
    
    sup = (5000.0, 50000.0, 0.0)
    v = (sup[0]-start[0], sup[1]-start[1], sup[2]-start[2])
    dot = (v[0]*dir_vec[0] + v[1]*dir_vec[1] + v[2]*dir_vec[2]) / (total_len**2)
    # dot = (5000*10000 + 50000*0 + 0*0) / 10000^2 = 50000000 / 100000000 = 0.5
    # Since dot is 0.5 (between 0.01 and 0.99), it projects (5000, 0, 0) onto beam!
    print(f"Adversarial Finding 1 (Perpendicular Support Drift): Support 50m away projects dot={dot:.2f}")

def test_naming_axis_rollover():
    count = 30
    names = []
    for i in range(count):
        names.append(chr(ord('A') + (i % 26)))
    # index 0 is 'A', index 26 is 'A' -> Duplicate names!
    has_duplicates = len(names) != len(set(names))
    print(f"Adversarial Finding 2 (Alphabetical Axis Rollover): count={count}, duplicates={has_duplicates}")

def test_takeoff_zero_quantity():
    # If Quantity = 0, does Math.Max(1, 0) make it 1?
    qty = 0
    effective_qty = max(1, qty)
    print(f"Adversarial Finding 3 (Takeoff Zero Quantity Clamp): input qty={qty} -> effective qty={effective_qty} (1 bar phantom weight)")

if __name__ == "__main__":
    test_framing_off_line_support()
    test_naming_axis_rollover()
    test_takeoff_zero_quantity()
