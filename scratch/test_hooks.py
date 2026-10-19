import json
import math
import sys

sys.stdout.reconfigure(encoding='utf-8')

# Let's inspect the Hook and Cog formulas defined in Section 6.1:
# Section 6.1 defines:
# - 90 deg hook (Cog): 10d to 12d, clamped >= 75 mm
# - 135 deg stirrup hook: 7.5d to 10d, clamped >= 75 mm
# - 180 deg circular hook: 5d + 4d = 9d, clamped >= 60 mm

def calc_cog90(d, multiplier=10.0):
    val = multiplier * d
    return max(val, 75.0)

def calc_hook135(d, multiplier=7.5):
    val = multiplier * d
    return max(val, 75.0)

def calc_hook180(d):
    val = 5.0 * d + 4.0 * d  # 9d
    return max(val, 60.0)

# Diameters from Section 6.2:
diameters = [6, 8, 10, 12, 14, 16, 18, 20, 22, 25, 28, 32, 36, 40]

print("=== HOOK VALUES BY DIAMETER (TCVN & KATA SECTION 6.1) ===")
print(f"{'d (mm)':6s} | {'Cog 90 (10d)':12s} | {'hook135 (7.5d)':14s} | {'hook180 (9d)':12s}")
print("-" * 52)
for d in diameters:
    c90 = calc_cog90(d)
    h135 = calc_hook135(d)
    h180 = calc_hook180(d)
    print(f"{d:6d} | {c90:12.1f} | {h135:14.1f} | {h180:12.1f}")
