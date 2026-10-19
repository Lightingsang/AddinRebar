import math

class Point3d:
    def __init__(self, x, y, z):
        self.x = float(x)
        self.y = float(y)
        self.z = float(z)
    def __repr__(self):
        return f"Point3d({self.x:.3f}, {self.y:.3f}, {self.z:.3f})"
    def distance_to(self, other):
        return math.sqrt((self.x - other.x)**2 + (self.y - other.y)**2 + (self.z - other.z)**2)
    def is_finite(self):
        return math.isfinite(self.x) and math.isfinite(self.y) and math.isfinite(self.z)

class Vector3d:
    def __init__(self, x, y, z):
        self.x = float(x)
        self.y = float(y)
        self.z = float(z)
    def __mul__(self, scalar):
        return Vector3d(self.x * scalar, self.y * scalar, self.z * scalar)
    def __add__(self, other):
        return Vector3d(self.x + other.x, self.y + other.y, self.z + other.z)
    def __sub__(self, other):
        return Vector3d(self.x - other.x, self.y - other.y, self.z - other.z)

def add_pt_vec(p, v):
    return Point3d(p.x + v.x, p.y + v.y, p.z + v.z)

def sub_pt_vec(p, v):
    return Point3d(p.x - v.x, p.y - v.y, p.z - v.z)

def sub_pt_pt(p2, p1):
    return Vector3d(p2.x - p1.x, p2.y - p1.y, p2.z - p1.z)

# 1. VeMBKCJig.CalculateCorners
def ve_mbkc_calculate_corners(p1, p2, width, alignment):
    v = sub_pt_pt(p2, p1)
    length = math.sqrt(v.x * v.x + v.y * v.y)
    n = Vector3d(-v.y / length, v.x / length, 0) if length > 1e-4 else Vector3d(0, 1, 0)
    
    align_upper = (alignment or "Mid").upper()
    if align_upper == "TOP":
        shift = -width / 2.0
    elif align_upper == "BOT":
        shift = width / 2.0
    else:
        shift = 0.0
        
    offset_top = n * (width / 2.0 + shift)
    offset_bot = n * (-width / 2.0 + shift)
    
    d1 = add_pt_vec(p1, offset_bot)
    d2 = add_pt_vec(p2, offset_bot)
    d3 = add_pt_vec(p2, offset_top)
    d4 = add_pt_vec(p1, offset_top)
    d_text = add_pt_vec(Point3d((p1.x + p2.x)/2.0, (p1.y + p2.y)/2.0, (p1.z + p2.z)/2.0), n * shift)
    return (d1, d2, d3, d4, d_text)

# 2. JigSlabRebar.CalculateRebarVertices
def jig_slab_rebar_calculate_rebar_vertices(p1, p2, offset_l, hook_l, offset_r, hook_r, diameter):
    dx = p2.x - p1.x
    dy = p2.y - p1.y
    length = math.sqrt(dx * dx + dy * dy)
    if length < 1e-4:
        return [p1, p2]
    
    ux = dx / length
    uy = dy / length
    nx = -uy
    ny = ux
    
    start_bar = Point3d(p1.x + ux * offset_l, p1.y + uy * offset_l, p1.z)
    end_bar = Point3d(p2.x - ux * offset_r, p2.y - uy * offset_r, p2.z)
    
    vertices = []
    if hook_l > 0:
        vertices.append(Point3d(start_bar.x - nx * hook_l, start_bar.y - ny * hook_l, start_bar.z))
    vertices.append(start_bar)
    vertices.append(end_bar)
    if hook_r > 0:
        vertices.append(Point3d(end_bar.x - nx * hook_r, end_bar.y - ny * hook_r, end_bar.z))
    return vertices

# 3. TagLeaderJig.CalculateLeaderPoints
def tag_leader_calculate_leader_points(p1, cursor, shelf_length, mirror):
    p2 = cursor
    dir_val = 1.0 if cursor.x >= p1.x else -1.0
    if mirror:
        dir_val = -dir_val
    p3 = Point3d(p2.x + (dir_val * shelf_length), p2.y, p2.z)
    text_offset = 25.0
    text_origin = Point3d(
        p2.x + 20.0 if dir_val > 0 else p3.x + 20.0,
        p2.y + text_offset,
        p2.z
    )
    return (p1, p2, p3, text_origin)

# 4. TagChiaThepJig.CalculateDistributionTicks
def tag_chia_thep_calculate_distribution_ticks(p1, p2, spacing, tick_len):
    ticks = []
    v = sub_pt_pt(p2, p1)
    length = math.sqrt(v.x * v.x + v.y * v.y)
    if length < 1e-4 or spacing <= 0:
        return (p1, p2, ticks, 1)
    
    u = Vector3d(v.x / length, v.y / length, 0)
    n = Vector3d(-u.y, u.x, 0) * (tick_len / 2.0)
    
    intervals = int(math.floor(length / spacing))
    if intervals < 1:
        intervals = 1
    
    for i in range(intervals + 1):
        dist = i * spacing
        if dist > length:
            dist = length
        pt = add_pt_vec(p1, u * dist)
        ticks.append((sub_pt_vec(pt, n), add_pt_vec(pt, n)))
        
    if intervals * spacing < length - 1e-3:
        ticks.append((sub_pt_vec(p2, n), add_pt_vec(p2, n)))
        
    return (p1, p2, ticks, intervals)

# 5. JigRaiThepChamTron.CalculateDotPositions
def jig_rai_thep_cham_tron_calculate_dot_positions(p1, p2, ostrai, osphai, osvg, spacing, bo_dau, bo_cuoi):
    dots = []
    v = sub_pt_pt(p2, p1)
    length = math.sqrt(v.x * v.x + v.y * v.y)
    if length < 1e-4 or spacing <= 0:
        return dots
    
    u = Vector3d(v.x / length, v.y / length, 0)
    n = Vector3d(-u.y, u.x, 0)
    
    effective_len = length - ostrai - osphai
    if effective_len < 0:
        effective_len = 0.0
        
    start_pt = add_pt_vec(add_pt_vec(p1, u * ostrai), n * osvg)
    count = int(math.floor(effective_len / spacing))
    if count < 1:
        count = 1
        
    for i in range(count + 1):
        if i == 0 and bo_dau:
            continue
        if i == count and bo_cuoi:
            continue
        d = i * spacing
        if d > effective_len:
            d = effective_len
        dots.append(add_pt_vec(start_pt, u * d))
        
    return dots

# 6. JigVeDaiVong.CalculateHoopGeometry
def jig_ve_dai_vong_calculate_hoop_geometry(center, cursor, hook_length, diameter):
    v = sub_pt_pt(cursor, center)
    radius = math.sqrt(v.x * v.x + v.y * v.y)
    if radius < 1.0:
        radius = 100.0
        
    hook_len = hook_length if hook_length > 0 else max(75.0, 10.0 * diameter)
    angle_135_rad = 135.0 * math.pi / 180.0
    
    splice_pt = Point3d(center.x, center.y + radius, center.z)
    hook1 = Point3d(
        splice_pt.x - (hook_len * math.cos(angle_135_rad - math.pi / 2.0)),
        splice_pt.y - (hook_len * math.sin(angle_135_rad - math.pi / 2.0)),
        center.z
    )
    hook2 = Point3d(
        splice_pt.x + (hook_len * math.cos(angle_135_rad - math.pi / 2.0)),
        splice_pt.y - (hook_len * math.sin(angle_135_rad - math.pi / 2.0)),
        center.z
    )
    return (center, radius, hook1, hook2)

# 7. JigVeDaiC.CalculateCTiePoints
def jig_ve_dai_c_calculate_ctie_points(p1, p2, hook_left, hook_right, diameter):
    v = sub_pt_pt(p2, p1)
    length = math.sqrt(v.x * v.x + v.y * v.y)
    if length < 1e-4:
        return [p1, p2]
    
    u = Vector3d(v.x / length, v.y / length, 0)
    n = Vector3d(-u.y, u.x, 0)
    
    h_l = hook_left if hook_left > 0 else max(60.0, 8.0 * diameter)
    h_r = hook_right if hook_right > 0 else max(60.0, 8.0 * diameter)
    
    p_start_hook = add_pt_vec(p1, n * h_l)
    p_end_hook = add_pt_vec(p2, n * h_r)
    return [p_start_hook, p1, p2, p_end_hook]

# 8. VePolylineBatKyJig.CalculatePolylineVertices
def ve_polyline_bat_ky_calculate_polyline_vertices(fixed_points, cursor):
    result = []
    if fixed_points:
        result.extend(fixed_points)
    if len(result) == 0 or result[-1].distance_to(cursor) > 1e-4:
        result.append(cursor)
    return result

# 9. JigVeThepVachCt.CalculateBoundaryCage
def jig_ve_thep_vach_ct_calculate_boundary_cage(p1, p2, tw, lc, cover, boundary_bar_count, web_bar_count):
    v = sub_pt_pt(p2, p1)
    length = math.sqrt(v.x * v.x + v.y * v.y)
    if length < 1e-4:
        length = 1000.0
    u = Vector3d(v.x / length, v.y / length, 0) if length > 1e-4 else Vector3d(1, 0, 0)
    n = Vector3d(-u.y, u.x, 0)
    
    w1 = add_pt_vec(p1, n * (tw / 2.0))
    w2 = add_pt_vec(p2, n * (tw / 2.0))
    w3 = sub_pt_vec(p2, n * (tw / 2.0))
    w4 = sub_pt_vec(p1, n * (tw / 2.0))
    wall_outline = [w1, w2, w3, w4]
    
    eff_lc = min(lc, length / 2.0 - 50.0)
    if eff_lc < 100.0:
        eff_lc = 100.0
        
    half_core_y = (tw / 2.0) - cover
    c1 = add_pt_vec(add_pt_vec(p1, u * cover), n * half_core_y)
    c2 = add_pt_vec(add_pt_vec(p1, u * (cover + eff_lc)), n * half_core_y)
    c3 = sub_pt_vec(add_pt_vec(p1, u * (cover + eff_lc)), n * half_core_y)
    c4 = sub_pt_vec(add_pt_vec(p1, u * cover), n * half_core_y)
    left_cage = [c1, c2, c3, c4]
    
    r1 = add_pt_vec(sub_pt_vec(p2, u * (cover + eff_lc)), n * half_core_y)
    r2 = add_pt_vec(sub_pt_vec(p2, u * cover), n * half_core_y)
    r3 = sub_pt_vec(sub_pt_vec(p2, u * cover), n * half_core_y)
    r4 = sub_pt_vec(sub_pt_vec(p2, u * (cover + eff_lc)), n * half_core_y)
    right_cage = [r1, r2, r3, r4]
    
    dots = [c1, c2, c3, c4, r1, r2, r3, r4]
    return (wall_outline, left_cage, right_cage, dots)

# 10. JigVeVachHam.CalculateRetainingWallSection
def jig_ve_vach_ham_calculate_retaining_wall_section(base_pt, height, tw, cover_left, cover_right, waterstop):
    h = abs(height) if abs(height) > 10.0 else 3000.0
    w = tw if tw > 50.0 else 350.0
    
    p1 = base_pt
    p2 = Point3d(base_pt.x + w, base_pt.y, base_pt.z)
    p3 = Point3d(base_pt.x + w, base_pt.y + h, base_pt.z)
    p4 = Point3d(base_pt.x, base_pt.y + h, base_pt.z)
    outline = [p1, p2, p3, p4]
    
    earth_start = Point3d(base_pt.x + cover_left, base_pt.y + 50.0, base_pt.z)
    earth_end = Point3d(base_pt.x + cover_left, base_pt.y + h - 50.0, base_pt.z)
    int_start = Point3d(base_pt.x + w - cover_right, base_pt.y + 50.0, base_pt.z)
    int_end = Point3d(base_pt.x + w - cover_right, base_pt.y + h - 50.0, base_pt.z)
    
    ws = []
    if waterstop:
        mid_x = base_pt.x + (w / 2.0)
        ws = [Point3d(mid_x, base_pt.y - 100.0, base_pt.z), Point3d(mid_x, base_pt.y + 100.0, base_pt.z)]
    return (outline, ws, earth_start, earth_end, int_start, int_end)

# 11. DimNhanhMbkcJig.CalculateDimensionChains
def dim_nhanh_calculate_dimension_chains(p1, p2, intermediate_nodes, dim_pt, tier_spacing, two_tiers):
    tier1 = []
    v = sub_pt_pt(p2, p1)
    length = math.sqrt(v.x * v.x + v.y * v.y)
    if length < 1e-4:
        return (tier1, None)
    
    u = Vector3d(v.x / length, v.y / length, 0)
    n = Vector3d(-u.y, u.x, 0)
    
    to_dim = sub_pt_pt(dim_pt, p1)
    perp_dist = (to_dim.x * n.x) + (to_dim.y * n.y)
    tier1_dim_pt = add_pt_vec(p1, n * perp_dist)
    
    sorted_nodes = [p1]
    if intermediate_nodes:
        sorted_nodes.extend(intermediate_nodes)
    sorted_nodes.append(p2)
    
    for i in range(len(sorted_nodes) - 1):
        tier1.append((sorted_nodes[i], sorted_nodes[i+1], tier1_dim_pt))
        
    tier2 = None
    if two_tiers:
        tier2_sign = 1.0 if perp_dist >= 0 else -1.0
        tier2_dim_pt = add_pt_vec(tier1_dim_pt, n * (tier2_sign * tier_spacing))
        tier2 = (p1, p2, tier2_dim_pt)
        
    return (tier1, tier2)

def run_stress_tests():
    failures = []
    passes = 0
    
    def check(condition, desc):
        nonlocal passes
        if condition:
            passes += 1
        else:
            failures.append(desc)

    p0 = Point3d(0, 0, 0)
    p_tiny = Point3d(1e-5, 1e-5, 0)
    p_large = Point3d(1e8, 1e8, 1e4)
    p_inv = Point3d(-5000, -3000, 0)
    
    # 1. VeMBKC
    r1 = ve_mbkc_calculate_corners(p0, p0, 220.0, "Mid")
    check(all(p.is_finite() for p in r1), "VeMBKC zero span finiteness")
    r2 = ve_mbkc_calculate_corners(p0, p_tiny, 220.0, "Top")
    check(all(p.is_finite() for p in r2), "VeMBKC sub-threshold span finiteness")
    r3 = ve_mbkc_calculate_corners(p0, p_inv, 300.0, "Bot")
    check(all(p.is_finite() for p in r3), "VeMBKC inverted coordinates finiteness")
    r4 = ve_mbkc_calculate_corners(p0, Point3d(4000, 0, 0), -220.0, None)
    check(all(p.is_finite() for p in r4), "VeMBKC negative width & null alignment")

    # 2. JigSlabRebar
    s1 = jig_slab_rebar_calculate_rebar_vertices(p0, p0, 50, 100, 50, 100, 10)
    check(len(s1) == 2 and all(p.is_finite() for p in s1), "JigSlabRebar zero span")
    s2 = jig_slab_rebar_calculate_rebar_vertices(p0, p_tiny, 50, 100, 50, 100, 10)
    check(len(s2) == 2 and all(p.is_finite() for p in s2), "JigSlabRebar tiny span fallback")
    s3 = jig_slab_rebar_calculate_rebar_vertices(p_large, p0, 50, 100, 50, 100, 25)
    check(len(s3) == 4 and all(p.is_finite() for p in s3), "JigSlabRebar large span inverted")
    s4 = jig_slab_rebar_calculate_rebar_vertices(p0, Point3d(3000, 0, 0), 100, -50, 100, -50, 0)
    check(len(s4) == 2, "JigSlabRebar negative hooks omission")

    # 3. TagLeaderJig
    l1 = tag_leader_calculate_leader_points(p0, p0, 300, False)
    check(all(p.is_finite() for p in l1), "TagLeader zero span")
    l2 = tag_leader_calculate_leader_points(p0, p_inv, -300, True)
    check(all(p.is_finite() for p in l2), "TagLeader inverted negative shelf mirror")
    l3 = tag_leader_calculate_leader_points(p_large, p0, 500, False)
    check(all(p.is_finite() for p in l3), "TagLeader large coords")

    # 4. TagChiaThepJig
    t1 = tag_chia_thep_calculate_distribution_ticks(p0, p0, 200, 60)
    check(t1[3] == 1 and len(t1[2]) == 0, "TagChiaThep zero span")
    t2 = tag_chia_thep_calculate_distribution_ticks(p0, Point3d(2000, 0, 0), 0, 60)
    check(t2[3] == 1 and len(t2[2]) == 0, "TagChiaThep zero spacing")
    t3 = tag_chia_thep_calculate_distribution_ticks(p0, Point3d(2000, 0, 0), -150, 60)
    check(t3[3] == 1 and len(t3[2]) == 0, "TagChiaThep negative spacing")
    t4 = tag_chia_thep_calculate_distribution_ticks(Point3d(5000, 0, 0), p0, 200, -50)
    check(t4[3] == 25 and len(t4[2]) >= 25, "TagChiaThep inverted points negative tickLen")

    # 5. JigRaiThepChamTron
    c1 = jig_rai_thep_cham_tron_calculate_dot_positions(p0, p0, 35, 35, 0, 150, False, False)
    check(len(c1) == 0, "JigRaiThepChamTron zero span")
    c2 = jig_rai_thep_cham_tron_calculate_dot_positions(p0, Point3d(1500, 0, 0), 35, 35, 0, -100, False, False)
    check(len(c2) == 0, "JigRaiThepChamTron negative spacing")
    c3 = jig_rai_thep_cham_tron_calculate_dot_positions(Point3d(3000, 0, 0), p0, 50, 50, 30, 200, True, True)
    check(all(p.is_finite() for p in c3), "JigRaiThepChamTron inverted with skip first/last")
    c4 = jig_rai_thep_cham_tron_calculate_dot_positions(p0, Point3d(100, 0, 0), 1000, 1000, 0, 150, False, False)
    check(len(c4) > 0 and all(p.is_finite() for p in c4), "JigRaiThepChamTron huge offsets clamp")

    # 6. JigVeDaiVong
    h1 = jig_ve_dai_vong_calculate_hoop_geometry(p0, p0, 100, 10)
    check(h1[1] == 100.0 and all(p.is_finite() for p in [h1[0], h1[2], h1[3]]), "JigVeDaiVong zero radius fallback")
    h2 = jig_ve_dai_vong_calculate_hoop_geometry(p_large, add_pt_vec(p_large, Point3d(500, 0, 0)), -1, -5)
    check(h2[1] == 500.0 and all(p.is_finite() for p in [h2[0], h2[2], h2[3]]), "JigVeDaiVong negative hook and diameter")

    # 7. JigVeDaiC
    d1 = jig_ve_dai_c_calculate_ctie_points(p0, p0, 80, 80, 8)
    check(len(d1) == 2, "JigVeDaiC zero span")
    d2 = jig_ve_dai_c_calculate_ctie_points(p0, p_inv, -50, -50, -10)
    check(len(d2) == 4 and all(p.is_finite() for p in d2), "JigVeDaiC negative hooks fallback and inverted")

    # 8. VePolylineBatKy
    poly1 = ve_polyline_bat_ky_calculate_polyline_vertices(None, p0)
    check(len(poly1) == 1, "VePolyline null fixed points")
    poly2 = ve_polyline_bat_ky_calculate_polyline_vertices([p0, Point3d(100, 0, 0)], Point3d(100, 0, 0))
    check(len(poly2) == 2, "VePolyline duplicate endpoint rejection")

    # 9. JigVeThepVachCt
    v1 = jig_ve_thep_vach_ct_calculate_boundary_cage(p0, p0, 250, 400, 25, 6, 4)
    check(len(v1[0]) == 4 and all(p.is_finite() for p in v1[0]), "JigVeThepVachCt zero span")
    v2 = jig_ve_thep_vach_ct_calculate_boundary_cage(Point3d(5000, 0, 0), p0, 300, 800, 30, 8, 6)
    check(len(v2[1]) == 4 and len(v2[2]) == 4 and all(p.is_finite() for p in v2[3]), "JigVeThepVachCt inverted")
    v3 = jig_ve_thep_vach_ct_calculate_boundary_cage(p0, Point3d(300, 0, 0), 250, 1000, 25, 6, 4)
    check(all(p.is_finite() for p in v3[3]), "JigVeThepVachCt short wall boundary clamp")

    # 10. JigVeVachHam
    w1 = jig_ve_vach_ham_calculate_retaining_wall_section(p0, 0, 350, 40, 30, True)
    check(w1[0][2].y == 3000.0, "JigVeVachHam zero height fallback to 3000")
    w2 = jig_ve_vach_ham_calculate_retaining_wall_section(p0, -4200, 10, 40, 30, False)
    check(w2[0][2].y == 4200.0 and w2[0][1].x == 350.0, "JigVeVachHam negative height and small thickness fallback")

    # 11. DimNhanhMbkc
    m1 = dim_nhanh_calculate_dimension_chains(p0, p0, None, Point3d(100, 100, 0), 500, True)
    check(m1[1] is None and len(m1[0]) == 0, "DimNhanhMbkc zero span")
    m2 = dim_nhanh_calculate_dimension_chains(Point3d(6000, 0, 0), p0, [Point3d(4000, 0, 0), Point3d(2000, 0, 0)], Point3d(3000, -500, 0), 300, True)
    check(len(m2[0]) == 3 and m2[1] is not None, "DimNhanhMbkc inverted with intermediates")
    m3 = dim_nhanh_calculate_dimension_chains(p0, Point3d(3000, 0, 0), None, p0, 300, False)
    check(len(m3[0]) == 1 and m3[1] is None, "DimNhanhMbkc single tier with dimPt at origin")

    print(f"\nTotal Adversarial Assertions: {passes + len(failures)}")
    print(f"Passed: {passes}")
    print(f"Failed: {len(failures)}")
    if failures:
        for f in failures:
            print(f"  FAILED ASSERTION: {f}")
    return len(failures) == 0

if __name__ == "__main__":
    success = run_stress_tests()
    exit(0 if success else 1)
