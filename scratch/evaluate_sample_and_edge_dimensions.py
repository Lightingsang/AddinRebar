"""
EVALUATION OF SAMPLE AND EDGE DIMENSIONS ACROSS REBAR SHAPES
Author: challenger_m1_1
"""

import math
import sys

sys.stdout.reconfigure(encoding='utf-8')

def get_cog90(d):
    return max(10.0 * d, 75.0)

def get_hook135(d):
    return max(7.5 * d, 75.0)

def get_hook180(d):
    return max(9.0 * d, 60.0)

def bend_deduction_90(d, r_inner=None):
    if r_inner is None:
        r_inner = 2.0 * d if d <= 16 else 3.5 * d
    # Delta L_90 = 2*(r + d) - (pi/2)*(r + d/2)
    return 2.0 * (r_inner + d) - (math.pi / 2.0) * (r_inner + d / 2.0)

def bend_deduction_135(d):
    return 1.5 * d

print("=== BEND DEDUCTION TABLE (SECTION 6.1) ===")
print(f"{'d (mm)':6s} | {'r_inner (mm)':12s} | {'Delta L_90 (mm)':15s} | {'Delta L_135 (mm)':16s}")
print("-" * 55)
for d in [6, 8, 10, 12, 16, 20, 25, 32]:
    r = 2.0 * d if d <= 16 else 3.5 * d
    d90 = bend_deduction_90(d, r)
    d135 = bend_deduction_135(d)
    print(f"{d:6d} | {r:12.1f} | {d90:15.2f} | {d135:16.2f}")

print("\n=== SAMPLE EVALUATIONS (A=200, B=5000, C=200, d=20, hook=90 deg, 135 deg) ===")
# Shape 01: Straight bar
L_01 = 5000.0
print(f"dang01 (Straight bar B=5000): L_cut = {L_01:.1f} mm")

# Shape 02: Symmetric U-bar (A=200, B=5000, C=200)
L_02_raw = 2 * 200.0 + 5000.0
# With 2 x 90 deg bends deduction:
ded_02 = 2 * bend_deduction_90(20)
L_02_ded = L_02_raw - ded_02
print(f"dang02 (U-bar 200-5000-200): Out-to-out = {L_02_raw:.1f} mm, Centerline = {L_02_ded:.1f} mm (deduction: {ded_02:.1f} mm)")

# Shape 02a: U-bar with 135 deg hooks
h135_d20 = get_hook135(20)
L_02a_raw = 2 * 200.0 + 5000.0 + 2 * h135_d20
print(f"dang02a (U-bar + 2x135 hooks, hook={h135_d20:.1f}): Out-to-out = {L_02a_raw:.1f} mm")

# Shape 03: L-bar (A=200, B=5000)
L_03_raw = 200.0 + 5000.0
ded_03 = bend_deduction_90(20)
print(f"dang03 (L-bar 200-5000): Out-to-out = {L_03_raw:.1f} mm, Centerline = {L_03_raw - ded_03:.1f} mm")

# Shape 09: U-bar with 90 deg Cogs (Cog = 10d = 200 mm)
cog90_d20 = get_cog90(20)
L_09_raw = 2 * 200.0 + 5000.0 + 2 * cog90_d20
print(f"dang09 (U-bar + 2x90 Cogs, Cog={cog90_d20:.1f}): Out-to-out = {L_09_raw:.1f} mm")

# Shape 09a: U-bar with 180 deg hooks (hook180 = 9d = 180 mm)
h180_d20 = get_hook180(20)
L_09a_raw = 2 * 200.0 + 5000.0 + 2 * h180_d20
print(f"dang09a (U-bar + 2x180 hooks, hook180={h180_d20:.1f}): Out-to-out = {L_09a_raw:.1f} mm")

# Shape 22: Closed stirrup 300x500, d=8 mm, hook135
d_stirrup = 8
h135_d8 = get_hook135(d_stirrup)
L_22_raw = 2 * (300.0 + 500.0) + 2 * h135_d8
# With bend deduction (3 x 90 deg + 2 x 135 deg):
ded_22 = 3 * bend_deduction_90(d_stirrup) + 2 * bend_deduction_135(d_stirrup)
print(f"dang22 (Stirrup 300x500 d=8, hook135={h135_d8:.1f}): Out-to-out = {L_22_raw:.1f} mm, Centerline = {L_22_raw - ded_22:.1f} mm (deduction: {ded_22:.1f} mm)")

# Shape 40: Circular Hoop D=600 mm, d=10 mm, lap=40d=400 mm
L_40 = math.pi * 600.0 + 400.0
print(f"dang40 (Circular Hoop D=600, lap=400): L_cut = {L_40:.1f} mm")

# Shape 40a: Circular Hoop with 135 deg hooks
h135_d10 = get_hook135(10)
L_40a = math.pi * 600.0 + 2 * h135_d10
print(f"dang40a (Circular Hoop D=600, 2x135 hook={h135_d10:.1f}): L_cut = {L_40a:.1f} mm")

# Shape 67: Helical Spiral D=800 mm, pitch s=100 mm, n=50 turns
L_67 = 50 * math.sqrt((math.pi * 800.0)**2 + 100.0**2) + 1.5 * math.pi * 800.0
print(f"dang67 (Spiral D=800, s=100, n=50): L_cut = {L_67:.1f} mm ({L_67/1000:.2f} m)")

print("\n=== EDGE CASE CRASH TESTS ===")
# Test dang06 with alpha=0, 45, 90
L1, L2, L3 = 1000.0, 300.0, 1000.0
for alpha_deg in [0, 15, 30, 45, 60, 90]:
    if alpha_deg == 0:
        try:
            val = L1 + L2 / math.sin(math.radians(alpha_deg)) + L3
            print(f"dang06 (alpha={alpha_deg} deg): {val}")
        except ZeroDivisionError:
            print(f"dang06 (alpha={alpha_deg} deg): CRASH -> ZeroDivisionError: division by zero!")
    else:
        val = L1 + L2 / math.sin(math.radians(alpha_deg)) + L3
        print(f"dang06 (alpha={alpha_deg:2d} deg): L_cut = {val:.1f} mm (sloped segment = {L2 / math.sin(math.radians(alpha_deg)):.1f} mm)")

# Test dang08 without Delta x
print("\ndang08 Crash Test:")
try:
    # Attempting to compute dang08 using only parameter list (L1, L2, L3, goc)
    params_provided = {'L1': 1000.0, 'L2': 600.0, 'L3': 1000.0, 'goc': 10.0}
    # delta_x is missing!
    val = params_provided['L1'] + math.sqrt(params_provided['Delta_x']**2 + params_provided['L2']**2) + params_provided['L3']
except KeyError as e:
    print(f"dang08 (missing Delta x): CRASH -> KeyError: {e} not in parameters!")

# Test dang34 without S_wave formula
print("\ndang34 Crash Test:")
try:
    params_provided = {'L1': 100.0, 'L2': 200.0, 'L3': 100.0, 'L4': 50.0, 'n': 10}
    val = params_provided['n'] * params_provided['S_wave']
except KeyError as e:
    print(f"dang34 (missing S_wave definition): CRASH -> KeyError: {e} cannot be computed!")

