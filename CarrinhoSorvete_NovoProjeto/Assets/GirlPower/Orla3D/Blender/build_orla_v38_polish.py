"""
Orla das Fadas — v38 Polish Script (Blender 5.3):
1. OrlaEnvironmentBase.fbx:
   - Continuous light-stone promenade (Surf_Calcadao) with low curb split cleanly around the rest area & central open passage!
   - Curved cream sand beach sloping cleanly below the sea surface (y = -0.20) right at z ~ 8.3m -> pier & canoe meet real sparkling water!
2. PalmTree.fbx:
   - Smooth, softly tapered curved coconut trunk with gentle organic bark rings (no harsh stripes!).
   - 9 gracefully arched, tapering curved palm fronds with central midrib & soft drooping tips (no thick flat oval plates!).
3. CoastalBench.fbx:
   - Softly rounded, generously beveled cozy wooden slats & contoured armrests matching the protagonist & pets!
"""

import bpy
import bmesh
import math
import os
import time
from mathutils import Vector, Euler, Matrix

SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
MODELS_DIR = os.path.normpath(os.path.join(SCRIPT_DIR, "..", "Models"))
BLEND_OUT = os.path.join(SCRIPT_DIR, "OrlaDasFadas_v38_Polish.blend")
os.makedirs(MODELS_DIR, exist_ok=True)


def clear_scene():
    bpy.ops.wm.read_factory_settings(use_empty=True)


def hex_rgba(h):
    h = h.lstrip("#")
    return (int(h[0:2], 16) / 255.0, int(h[2:4], 16) / 255.0, int(h[4:6], 16) / 255.0, 1.0)


MATS = {}


def init_mats():
    MATS.clear()
    for k, hx in [
        ("AC_Sand", "#F4E6C8"),
        ("AC_SandWet", "#DEC9A4"),
        ("AC_StoneWhite", "#F4ECDC"),
        ("AC_StoneWarm", "#E6D9C2"),
        ("AC_Starfish", "#E28E7C"),
        ("AC_Shell", "#F8ECE0"),
        ("AC_PalmTrunk", "#C89E72"),
        ("AC_PalmRing", "#BE946A"),
        ("AC_PalmLeaf", "#5B9E78"),
        ("AC_PalmLeafTip", "#7BB892"),
        ("AC_Coconut", "#764E34"),
        ("AC_WoodLight", "#D4AE82"),
        ("AC_WoodDark", "#755238"),
        ("AC_MetalDark", "#3A424E"),
    ]:
        m = bpy.data.materials.new(name=k)
        bsdf = m.node_tree.nodes.get("Principled BSDF") if m.node_tree else None
        if bsdf:
            bsdf.inputs["Base Color"].default_value = hex_rgba(hx)
            bsdf.inputs["Roughness"].default_value = 0.86
        MATS[k] = m


def add_sphere(name, r, mat, col, loc=(0, 0, 0), scale=(1, 1, 1), u=14, v=10):
    mesh = bpy.data.meshes.new(name)
    bm = bmesh.new()
    bmesh.ops.create_uvsphere(bm, u_segments=u, v_segments=v, radius=r)
    bmesh.ops.scale(bm, vec=Vector(scale), verts=bm.verts)
    for f in bm.faces:
        f.smooth = True
    bm.to_mesh(mesh)
    bm.free()
    obj = bpy.data.objects.new(name, mesh)
    obj.location = loc
    obj.data.materials.append(mat)
    col.objects.link(obj)
    return obj


def add_rounded_box(name, size, mat, col, loc=(0, 0, 0), rot=(0, 0, 0), bevel=0.04, segs=3):
    mesh = bpy.data.meshes.new(name)
    bm = bmesh.new()
    bmesh.ops.create_cube(bm, size=1.0)
    bmesh.ops.scale(bm, vec=Vector(size), verts=bm.verts)
    b = min(bevel, min(size) * 0.44)
    if b > 0.001:
        bmesh.ops.bevel(bm, geom=list(bm.edges), offset=b, segments=segs, affect="EDGES", clamp_overlap=True)
    for f in bm.faces:
        f.smooth = True
    bm.normal_update()
    bm.to_mesh(mesh)
    bm.free()
    obj = bpy.data.objects.new(name, mesh)
    obj.location = loc
    obj.rotation_euler = rot
    obj.data.materials.append(mat)
    col.objects.link(obj)
    return obj


def join_parts(parts, name, col):
    bpy.ops.object.select_all(action="DESELECT")
    for p in parts:
        p.select_set(True)
    bpy.context.view_layer.objects.active = parts[0]
    bpy.ops.object.join()
    obj = bpy.context.active_object
    obj.name = name
    obj.data.name = name
    if obj.name not in col.objects:
        col.objects.link(obj)
    return obj


def export_fbx(objs, name):
    bpy.ops.object.select_all(action="DESELECT")
    for o in objs:
        o.select_set(True)
    bpy.context.view_layer.objects.active = objs[0]
    out_path = os.path.join(MODELS_DIR, f"{name}.fbx")
    for attempt in range(4):
        try:
            bpy.ops.export_scene.fbx(
                filepath=out_path,
                use_selection=True,
                global_scale=1.0,
                apply_unit_scale=True,
                apply_scale_options="FBX_SCALE_ALL",
                bake_space_transform=True,
                object_types={"MESH"},
                mesh_smooth_type="FACE",
                axis_forward="-Z",
                axis_up="Y",
                add_leaf_bones=False,
                bake_anim=False,
            )
            break
        except RuntimeError:
            if attempt == 3:
                raise
            time.sleep(0.35)
    print(f"[FBX v38] {out_path}")


# =========================================================================
# 1. CONTINUOUS COASTAL ENVIRONMENT (Open Promenade + Clean Underwater Slope!)
# =========================================================================
def build_subdivided_strip(name, x0, x1, y0, y1, z_top, thickness, segs_x, segs_y, mat, col):
    mesh = bpy.data.meshes.new(name)
    bm = bmesh.new()
    top_grid = []
    for ix in range(segs_x + 1):
        x = x0 + (x1 - x0) * (ix / segs_x)
        row = []
        for iy in range(segs_y + 1):
            y = y0 + (y1 - y0) * (iy / segs_y)
            row.append(bm.verts.new((x, y, z_top)))
        top_grid.append(row)
    for ix in range(segs_x):
        for iy in range(segs_y):
            bm.faces.new((top_grid[ix][iy], top_grid[ix + 1][iy], top_grid[ix + 1][iy + 1], top_grid[ix][iy + 1]))

    z_bot = z_top - thickness
    for iy_edge, flip in [(0, True), (segs_y, False)]:
        bot_row = [bm.verts.new((x0 + (x1 - x0) * (ix / segs_x), y0 if iy_edge == 0 else y1, z_bot)) for ix in range(segs_x + 1)]
        for ix in range(segs_x):
            v0 = top_grid[ix][iy_edge]
            v1 = top_grid[ix + 1][iy_edge]
            b0 = bot_row[ix]
            b1 = bot_row[ix + 1]
            if flip:
                bm.faces.new((v1, v0, b0, b1))
            else:
                bm.faces.new((v0, v1, b1, b0))

    for f in bm.faces:
        f.smooth = False
    bm.normal_update()
    bm.to_mesh(mesh)
    bm.free()
    obj = bpy.data.objects.new(name, mesh)
    obj.data.materials.append(mat)
    col.objects.link(obj)
    return obj


def build_continuous_environment(col):
    parts = []
    x0, x1 = -120.0, 144.0
    segs_x = 96

    # SURFACE A: Continuous light-stone promenade (Y = -3.75 .. +3.50, Z = 0.00)
    parts.append(build_subdivided_strip("Surf_Calcadao", x0, x1, -3.75, 3.50, 0.0, 0.22, segs_x, 4, MATS["AC_StoneWhite"], col))

    # Low, subtle rounded border lip along the south edge with WIDE OPEN PASSAGES next to the rest area & central plaza!
    for bname, bx0, bx1 in [
        ("Surf_BordaPraca_FarW", -120.0, -11.5),
        ("Surf_BordaPraca_RestW", -10.0, -5.2),
        ("Surf_BordaPraca_East",   2.8,  144.0),
    ]:
        b_segs = max(4, int((bx1 - bx0) / 3.0))
        parts.append(build_subdivided_strip(bname, bx0, bx1, -3.86, -3.72, 0.012, 0.07, b_segs, 1, MATS["AC_StoneWarm"], col))

    # SURFACE B: Continuous South Plaza extending to Y = -36.0 (Z = -0.03)
    parts.append(build_subdivided_strip("Surf_PracaContinua", x0, x1, -36.0, -3.75, -0.03, 0.22, segs_x, 10, MATS["AC_StoneWarm"], col))

    # Low, gentle coastal mureta segments flanking the 3 wide beach accesses (lower & cleaner!)
    for mname, mx0, mx1 in [
        ("Mureta_FarWest", -116.0, -7.2),
        ("Mureta_MidWest",   -3.3,  5.3),
        ("Mureta_MidEast",    9.7, 17.8),
        ("Mureta_FarEast",   22.2, 142.0),
    ]:
        m_segs = max(4, int((mx1 - mx0) / 3.0))
        parts.append(build_subdivided_strip(mname, mx0, mx1, 3.40, 3.68, 0.14, 0.20, m_segs, 1, MATS["AC_StoneWhite"], col))
        parts.append(build_subdivided_strip(mname + "_Cap", mx0 - 0.02, mx1 + 0.02, 3.37, 3.71, 0.17, 0.04, m_segs, 1, MATS["AC_StoneWarm"], col))

    # SURFACE C: Curved Cream Sand Beach sloping cleanly UNDER the ocean plane (y = -0.20) right at z ~ 8.35m!
    # So the pier, canoe, and coastline show rich turquoise water immediately behind the sand!
    mesh_dry = bpy.data.meshes.new("Surf_AreiaCreme")
    bm = bmesh.new()
    grid = []
    for ix in range(segs_x + 1):
        u = ix / segs_x
        x = x0 + (x1 - x0) * u
        coast_curve = math.sin(x * 0.045) * 0.72 + math.cos(x * 0.022) * 0.35
        ys = [
            3.35,
            5.80 + coast_curve * 0.30,
            8.10 + coast_curve * 0.75,
            10.20 + coast_curve * 0.90,
            15.50,
            24.00,
        ]
        zs = [-0.03, -0.11, -0.20, -0.58, -1.08, -1.45]
        row = [bm.verts.new((x, ys[r], zs[r])) for r in range(len(ys))]
        grid.append(row)
    for ix in range(segs_x):
        for r in range(len(grid[0]) - 1):
            bm.faces.new((grid[ix][r], grid[ix + 1][r], grid[ix + 1][r + 1], grid[ix][r + 1]))
    for f in bm.faces:
        f.smooth = True
    bm.normal_update()
    bm.to_mesh(mesh_dry)
    bm.free()
    sand_obj = bpy.data.objects.new("Surf_AreiaCreme", mesh_dry)
    sand_obj.data.materials.append(MATS["AC_Sand"])
    col.objects.link(sand_obj)
    parts.append(sand_obj)

    # Subtle shells & starfish along dry cream sand
    for idx, (sx, sy) in enumerate([(-3.6, 5.4), (4.2, 6.2), (13.4, 5.9), (20.8, 6.4)]):
        parts.append(add_sphere(f"Starfish_{idx}", 0.15, MATS["AC_Starfish"], col, loc=(sx, sy, -0.09), scale=(1.1, 1.1, 0.25), u=10, v=6))
        parts.append(add_sphere(f"Shell_{idx}", 0.10, MATS["AC_Shell"], col, loc=(sx + 0.55, sy + 0.35, -0.10), scale=(1.2, 0.9, 0.42), u=8, v=6))

    env = join_parts(parts, "OrlaEnvironmentBase", col)
    export_fbx([env], "OrlaEnvironmentBase")
    return env


# =========================================================================
# 2. SMOOTH ORGANIC PALM TREE (Curved drooping fronds + smooth blended trunk!)
# =========================================================================
def build_smooth_palm_tree(col):
    parts = []
    m_trunk = MATS["AC_PalmTrunk"]
    m_ring = MATS["AC_PalmRing"]
    m_leaf = MATS["AC_PalmLeaf"]
    m_tip = MATS["AC_PalmLeafTip"]
    m_coco = MATS["AC_Coconut"]

    # 1. Continuous curved, softly tapered smooth trunk (subtle organic bark ripples, ZERO harsh dark stripes!)
    height = 3.85
    curve_x = 0.52
    rings = 22
    radial = 14
    mesh_trunk = bpy.data.meshes.new("PalmTrunkSmooth")
    bm = bmesh.new()
    ring_verts = []
    for i in range(rings + 1):
        t = i / rings
        cx = math.sin(t * 1.15) * curve_x
        cz = t * height
        # Subtle organic bamboo/palm ring swell (smooth, never jagged!)
        swell = 1.0 + 0.045 * math.sin(t * math.pi * 16.0)
        # Wider cozy base flare, slender gentle top
        base_flare = 0.06 * math.exp(-t * 7.0)
        rad = (0.21 * (1.0 - t * 0.46) + base_flare) * swell
        # Tangent tilt along X
        dx_dt = math.cos(t * 1.15) * 1.15 * curve_x
        tilt = math.atan2(dx_dt, height)
        ct, st = math.cos(tilt), math.sin(tilt)
        ring = []
        for j in range(radial):
            ang = (j / radial) * math.tau
            lx = math.cos(ang) * rad
            ly = math.sin(ang) * rad
            # Rotate local X around Y by tilt so cross-section stays perpendicular to trunk curve
            wx = cx + lx * ct
            wy = ly
            wz = cz - lx * st
            ring.append(bm.verts.new((wx, wy, wz)))
        ring_verts.append(ring)

    for i in range(rings):
        for j in range(radial):
            jn = (j + 1) % radial
            f = bm.faces.new((ring_verts[i][j], ring_verts[i][jn], ring_verts[i + 1][jn], ring_verts[i + 1][j]))
            f.smooth = True
            f.material_index = 1 if (i % 3 == 1) else 0

    # Cap top & bottom
    bm.faces.new(ring_verts[0][::-1]).smooth = True
    bm.faces.new(ring_verts[-1]).smooth = True
    bm.normal_update()
    bm.to_mesh(mesh_trunk)
    bm.free()
    trunk_obj = bpy.data.objects.new("PalmTrunkSmooth", mesh_trunk)
    trunk_obj.data.materials.append(m_trunk)
    trunk_obj.data.materials.append(m_ring)
    col.objects.link(trunk_obj)
    parts.append(trunk_obj)

    top_pos = Vector((math.sin(1.15) * curve_x, 0.0, height))

    # 2. Plump smooth coconuts tucked naturally under the crown
    for k in range(3):
        ang = k * (math.tau / 3.0) + 0.45
        cpos = top_pos + Vector((math.cos(ang) * 0.19, math.sin(ang) * 0.19, -0.16))
        parts.append(add_sphere(f"Coconut_{k}", 0.145, m_coco, col, loc=cpos, scale=(1.0, 1.0, 1.14), u=14, v=10))

    # 3. 9 Gracefully Curved & Tapered Organic Palm Fronds (smooth arched V-profile ribbon with soft drooping tips!)
    num_fronds = 9
    for f_idx in range(num_fronds):
        yaw = (f_idx / num_fronds) * math.tau + (0.16 if f_idx % 2 else 0.0)
        is_upper = (f_idx % 2 == 1)
        frond_len = 1.65 if is_upper else 2.02
        lift = 0.56 if is_upper else 0.36
        droop_amt = 0.72 if is_upper else 1.08

        mesh_frond = bpy.data.meshes.new(f"PalmFrond_{f_idx}")
        bm = bmesh.new()
        steps = 11
        rows = []
        # Cross-section at each step along frond: left edge, midrib top, right edge, midrib bottom (plump soft curved blade!)
        for s in range(steps + 1):
            u = s / steps
            r_dist = u * frond_len
            # Graceful parabolic arch: rises from crown then droops smoothly toward tip
            z_arch = math.sin(u * math.pi * 0.68) * lift - (u * u) * droop_amt + (0.06 if is_upper else 0.0)
            # Natural leaf width envelope: narrow stem -> full graceful middle -> pointed soft tip
            # Subtle organic notches along outer margins so fronds read as tropical leaves, not flat ovals!
            notch = 1.0 - (0.14 * (0.5 - 0.5 * math.cos(u * math.pi * 8.0)) if (0.18 < u < 0.88) else 0.0)
            half_w = (0.36 * math.sin(math.pow(u, 0.72) * math.pi) + 0.025 * (1.0 - u)) * notch
            thickness = 0.032 * (1.0 - u * 0.70)
            v_droop = half_w * (0.28 + 0.16 * u)  # V-fold roof profile so light catches both halves softly!

            # Local frame along yaw
            cos_y, sin_y = math.cos(yaw), math.sin(yaw)
            fwd = Vector((cos_y, sin_y, 0.0))
            side = Vector((-sin_y, cos_y, 0.0))
            center = top_pos + fwd * r_dist + Vector((0.0, 0.0, z_arch + 0.04))

            v_l = bm.verts.new(center - side * half_w - Vector((0.0, 0.0, v_droop)))
            v_t = bm.verts.new(center + Vector((0.0, 0.0, thickness)))
            v_r = bm.verts.new(center + side * half_w - Vector((0.0, 0.0, v_droop)))
            v_b = bm.verts.new(center - Vector((0.0, 0.0, thickness * 1.15)))
            rows.append((v_l, v_t, v_r, v_b))

        for s in range(steps):
            a_l, a_t, a_r, a_b = rows[s]
            b_l, b_t, b_r, b_b = rows[s + 1]
            mat_i = 1 if s >= steps - 4 else 0
            for quad in [
                (a_l, b_l, b_t, a_t),
                (a_t, b_t, b_r, a_r),
                (a_r, b_r, b_b, a_b),
                (a_b, b_b, b_l, a_l),
            ]:
                face = bm.faces.new(quad)
                face.smooth = True
                face.material_index = mat_i

        # Close base & tip
        bm.faces.new((rows[0][0], rows[0][1], rows[0][2], rows[0][3])).smooth = True
        bm.faces.new((rows[-1][3], rows[-1][2], rows[-1][1], rows[-1][0])).smooth = True
        bm.normal_update()
        bm.to_mesh(mesh_frond)
        bm.free()
        frond_obj = bpy.data.objects.new(f"PalmFrond_{f_idx}", mesh_frond)
        frond_obj.data.materials.append(m_leaf)
        frond_obj.data.materials.append(m_tip)
        col.objects.link(frond_obj)
        parts.append(frond_obj)

    # Cozy crown tuft
    parts.append(add_sphere("PalmCrown", 0.20, m_leaf, col, loc=top_pos + Vector((0, 0, 0.05)), scale=(1.0, 1.0, 0.68), u=12, v=8))

    palm = join_parts(parts, "PalmTree", col)
    export_fbx([palm], "PalmTree")
    return palm


# =========================================================================
# 3. COZY ROUNDED COASTAL BENCH (Soft beveled wood matching characters!)
# =========================================================================
def build_smooth_coastal_bench(col):
    parts = []
    m_wood = MATS["AC_WoodLight"]
    m_wood_d = MATS["AC_WoodDark"]
    m_metal = MATS["AC_MetalDark"]

    # Generously rounded seat slats (pill-soft profile!)
    for idx, sy in enumerate((-0.20, 0.0, 0.20)):
        parts.append(add_rounded_box(f"Seat_{idx}", (1.74, 0.175, 0.072), m_wood, col, loc=(0, sy, 0.45), bevel=0.032, segs=3))

    # Contoured backrest slats with gentle cozy recline
    for idx, (sz, sy) in enumerate([(0.70, 0.31), (0.91, 0.35)]):
        parts.append(add_rounded_box(f"Back_{idx}", (1.74, 0.072, 0.175), m_wood, col, loc=(0, sy, sz), rot=(-0.18, 0, 0), bevel=0.032, segs=3))

    # Softly rounded legs & ergonomic curved-top wooden armrests
    for sx in (-0.72, 0.72):
        parts.append(add_rounded_box(f"LegF_{sx}", (0.095, 0.095, 0.46), m_metal, col, loc=(sx, -0.22, 0.23), bevel=0.035, segs=3))
        parts.append(add_rounded_box(f"LegB_{sx}", (0.095, 0.095, 1.00), m_metal, col, loc=(sx, 0.29, 0.50), rot=(-0.10, 0, 0), bevel=0.035, segs=3))
        parts.append(add_rounded_box(f"SideBar_{sx}", (0.085, 0.56, 0.08), m_metal, col, loc=(sx, 0.04, 0.40), bevel=0.030, segs=3))
        parts.append(add_rounded_box(f"Arm_{sx}", (0.125, 0.62, 0.082), m_wood_d, col, loc=(sx, 0.03, 0.65), bevel=0.038, segs=3))
        parts.append(add_rounded_box(f"ArmPost_{sx}", (0.078, 0.078, 0.22), m_metal, col, loc=(sx, -0.22, 0.54), bevel=0.028, segs=3))

    bench = join_parts(parts, "CoastalBench", col)
    export_fbx([bench], "CoastalBench")
    return bench


def main():
    clear_scene()
    init_mats()
    col = bpy.context.scene.collection

    build_continuous_environment(col)
    build_smooth_palm_tree(col)
    build_smooth_coastal_bench(col)

    bpy.ops.wm.save_as_mainfile(filepath=BLEND_OUT)
    print("[v38 Polish] All 3 FBX models exported successfully!")


if __name__ == "__main__":
    main()
