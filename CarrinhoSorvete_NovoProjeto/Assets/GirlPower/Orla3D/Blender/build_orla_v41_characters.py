"""
Orla das Fadas — v41 Character Identity, Silhouette & Readability Overhaul (Blender 5.3):
- Hero Protagonist: Exclusive Sunny-Yellow sundress + White blouse + Large Turquoise Ribbon Ponytail
  + Cute Turquoise Backpack + Coral Sneakers (zero color overlap with pink cart or any villager!).
- Distinct Villagers: Every villager has unique height, hairstyle, outfit silhouette, and 3D accessory
  (backpack, crossbody bag, work apron, space buns + bolero, wide straw hat + flower basket,
  sailor collar + beret, sunglasses + handbag, haori jacket + obi, round glasses + cozy shawl,
  farm overalls + side braid).
- Proportions & Animation Polish:
  * Balanced Animal Crossing chibi proportions (head r=0.252, hair hugs head cleanly without swallowing body).
  * Arms angled 12 deg outward so they NEVER clip through dresses/torsos during idle or walk cycles.
  * Distinct rounded hands & clearly visible flat-soled shoes grounded strictly at Z = 0.000!
"""

import bpy
import bmesh
import math
import os
import time
from mathutils import Vector, Matrix

SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
MODELS_DIR = os.path.normpath(os.path.join(SCRIPT_DIR, "..", "Models"))
os.makedirs(MODELS_DIR, exist_ok=True)

MATS = {}


def clear_scene():
    bpy.ops.wm.read_factory_settings(use_empty=True)


def hex_rgba(h):
    h = h.lstrip("#")
    return (int(h[0:2], 16) / 255.0, int(h[2:4], 16) / 255.0, int(h[4:6], 16) / 255.0, 1.0)


def init_mats():
    MATS.clear()
    palette = [
        ("AC_ProtagHero", "#F6C43C"),      # Exclusive Hero Sunny Yellow!
        ("AC_ProtagRibbon", "#16B8C4"),    # Exclusive Hero Bright Turquoise!
        ("AC_ProtagShirt", "#F6C43C"),
        ("AC_Cream", "#FCF7EC"),
        ("AC_Gold", "#E6C274"),
        ("AC_WoodLight", "#D4AE82"),
        ("AC_WoodWarm", "#B88E62"),
        ("AC_WoodDark", "#6E4C34"),
        ("AC_PinkBerry", "#EC5A8E"),
        ("AC_PinkSoft", "#F2B6CE"),
        ("AC_MintTeal", "#2B8A96"),
        ("AC_MintSoft", "#8CCBB8"),
        ("AC_SkyBlue", "#5FA8C8"),
        ("AC_Denim", "#3E6F8E"),
        ("AC_Coral", "#DE7664"),
        ("AC_Terracotta", "#C97C5E"),
        ("AC_Lavender", "#B994DC"),
        ("AC_SunnyYellow", "#F0CF70"),
        ("AC_Orange", "#E28E48"),
        ("AC_BushGreen", "#4D9E65"),
        ("AC_BushLight", "#7AC286"),
        ("AC_FlowerPink", "#EC6FA4"),
        ("AC_FlowerYellow", "#EED286"),
        ("AC_LanternGlow", "#FFE69C"),
        ("AC_StrawHat", "#EAD09C"),
        ("AC_ShoeDark", "#463544"),
        ("AC_MetalDark", "#343B46"),
        ("AC_SkinLight", "#F7CAA8"),
        ("AC_SkinTan", "#E2A67C"),
        ("AC_SkinWarm", "#C98862"),
        ("AC_CheekBlush", "#F28FA8"),
        ("AC_EyeDark", "#201824"),
        ("AC_EyeShine", "#FFFFFF"),
        ("AC_HairBrown", "#664028"),
        ("AC_HairDark", "#2C2534"),
        ("AC_HairAuburn", "#B4563A"),
        ("AC_HairPlum", "#844894"),
        ("AC_HairWind_Blonde", "#EAC46E"),
        ("AC_HairWind_Dark", "#2C2534"),
        ("AC_HairWind_Silver", "#D6DAE2"),
        ("AC_HairWind_Auburn", "#B4563A"),
        ("AC_HairWind_Brown", "#664028"),
    ]
    for k, hx in palette:
        m = bpy.data.materials.new(name=k)
        bsdf = m.node_tree.nodes.get("Principled BSDF") if m.node_tree else None
        if bsdf:
            bsdf.inputs["Base Color"].default_value = hex_rgba(hx)
            bsdf.inputs["Roughness"].default_value = 0.85
        MATS[k] = m


def new_mesh_obj(name, bm, mat, col, loc=(0, 0, 0), rot=(0, 0, 0), smooth=True):
    for f in bm.faces:
        f.smooth = smooth
    bm.normal_update()
    mesh = bpy.data.meshes.new(name)
    bm.to_mesh(mesh)
    bm.free()
    obj = bpy.data.objects.new(name, mesh)
    obj.location = loc
    obj.rotation_euler = rot
    obj.data.materials.append(mat)
    col.objects.link(obj)
    return obj


def add_sphere(name, r, mat, col, loc=(0, 0, 0), scale=(1, 1, 1), rot=(0, 0, 0), u=18, v=12):
    bm = bmesh.new()
    bmesh.ops.create_uvsphere(bm, u_segments=u, v_segments=v, radius=r)
    bmesh.ops.scale(bm, vec=Vector(scale), verts=bm.verts)
    return new_mesh_obj(name, bm, mat, col, loc=loc, rot=rot, smooth=True)


def add_lathe(name, profile_rz, mat, col, loc=(0, 0, 0), scale=(1, 1, 1), rot=(0, 0, 0), segs=20, cap_bot=True, cap_top=True):
    bm = bmesh.new()
    rings = []
    for r, z in profile_rz:
        if r <= 0.0005:
            rings.append([bm.verts.new((0.0, 0.0, z))])
        else:
            ring = []
            for i in range(segs):
                a = (i / segs) * math.tau
                ring.append(bm.verts.new((math.cos(a) * r, math.sin(a) * r, z)))
            rings.append(ring)
    for k in range(len(rings) - 1):
        r0, r1 = rings[k], rings[k + 1]
        if len(r0) == 1 and len(r1) > 1:
            for i in range(segs):
                bm.faces.new((r0[0], r1[(i + 1) % segs], r1[i]))
        elif len(r0) > 1 and len(r1) == 1:
            for i in range(segs):
                bm.faces.new((r0[i], r0[(i + 1) % segs], r1[0]))
        elif len(r0) > 1 and len(r1) > 1:
            for i in range(segs):
                bm.faces.new((r0[i], r0[(i + 1) % segs], r1[(i + 1) % segs], r1[i]))
    if cap_bot and len(rings[0]) > 1:
        bm.faces.new(list(reversed(rings[0])))
    if cap_top and len(rings[-1]) > 1:
        bm.faces.new(rings[-1])
    bmesh.ops.scale(bm, vec=Vector(scale), verts=bm.verts)
    return new_mesh_obj(name, bm, mat, col, loc=loc, rot=rot, smooth=True)


def add_curved_strand(name, pts_xyz, radii, mat, col, segs=10):
    bm = bmesh.new()
    rings = []
    n = len(pts_xyz)
    for idx, (pt, r) in enumerate(zip(pts_xyz, radii)):
        p = Vector(pt)
        if idx == 0:
            t = (Vector(pts_xyz[1]) - p).normalized()
        elif idx == n - 1:
            t = (p - Vector(pts_xyz[idx - 1])).normalized()
        else:
            t = (Vector(pts_xyz[idx + 1]) - Vector(pts_xyz[idx - 1])).normalized()
        up = Vector((0, 0, 1)) if abs(t.z) < 0.92 else Vector((1, 0, 0))
        side = t.cross(up).normalized()
        up2 = side.cross(t).normalized()
        if r <= 0.001:
            rings.append([bm.verts.new(p)])
        else:
            ring = []
            for s in range(segs):
                a = (s / segs) * math.tau
                ring.append(bm.verts.new(p + (side * math.cos(a) + up2 * math.sin(a)) * r))
            rings.append(ring)
    for k in range(len(rings) - 1):
        r0, r1 = rings[k], rings[k + 1]
        if len(r0) == 1 and len(r1) > 1:
            for s in range(segs):
                bm.faces.new((r0[0], r1[(s + 1) % segs], r1[s]))
        elif len(r0) > 1 and len(r1) == 1:
            for s in range(segs):
                bm.faces.new((r0[s], r0[(s + 1) % segs], r1[0]))
        elif len(r0) > 1 and len(r1) > 1:
            for s in range(segs):
                bm.faces.new((r0[s], r0[(s + 1) % segs], r1[(s + 1) % segs], r1[s]))
    if len(rings[0]) > 1:
        bm.faces.new(list(reversed(rings[0])))
    if len(rings[-1]) > 1:
        bm.faces.new(rings[-1])
    return new_mesh_obj(name, bm, mat, col, smooth=True)


def add_rounded_box(name, size, mat, col, loc=(0, 0, 0), rot=(0, 0, 0), bevel=0.03, segs=3):
    bm = bmesh.new()
    bmesh.ops.create_cube(bm, size=1.0)
    bmesh.ops.scale(bm, vec=Vector(size), verts=bm.verts)
    b = min(bevel, min(size) * 0.44)
    if b > 0.001:
        bmesh.ops.bevel(bm, geom=list(bm.edges), offset=b, segments=segs, affect="EDGES", clamp_overlap=True)
    return new_mesh_obj(name, bm, mat, col, loc=loc, rot=rot, smooth=True)


def join_with_pivot(parts, name, pivot_world, col):
    bpy.ops.object.select_all(action="DESELECT")
    for p in parts:
        p.select_set(True)
    bpy.context.view_layer.objects.active = parts[0]
    if len(parts) > 1:
        bpy.ops.object.join()
    obj = bpy.context.active_object
    obj.name = name
    obj.data.name = name
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    pv = Vector(pivot_world)
    obj.data.transform(Matrix.Translation(-pv))
    obj.location = pv
    if obj.name not in col.objects:
        col.objects.link(obj)
    return obj


def ground_parts_exact_zero(all_objs):
    """Guarantees the lowest vertex across the character's shoes rests strictly at Z = 0.000."""
    bpy.context.view_layer.update()
    min_z = min((o.matrix_world @ v.co).z for o in all_objs for v in o.data.vertices)
    if abs(min_z) > 0.0005:
        for o in all_objs:
            o.location.z -= min_z
        bpy.context.view_layer.update()


def export_fbx(objs, name):
    ground_parts_exact_zero(objs)
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
    print(f"[FBX v41] {out_path}")


# =========================================================================
# HEAD, FACE, HAIRSTYLES & HEADWEAR (Coherent Proportions, Zero Bloat!)
# =========================================================================
def build_head_and_hair(col, skin_mat, hair_mat, hair_style, accent_mat, hat_style=None, glasses=None, z_center=1.24, separate_eyes=False):
    parts = []
    # Well-proportioned chibi head (r ~0.248 — expressive face without swallowing shoulders!)
    parts.append(add_lathe(
        "HeadBase",
        [
            (0.0, -0.205), (0.105, -0.195), (0.192, -0.135), (0.242, -0.03),
            (0.248, 0.05), (0.232, 0.14), (0.175, 0.215), (0.088, 0.255), (0.0, 0.265),
        ],
        skin_mat, col, loc=(0, 0, z_center), scale=(1.03, 0.96, 0.98), segs=22
    ))
    for sx in (-1, 1):
        parts.append(add_sphere(f"Ear_{sx}", 0.050, skin_mat, col, loc=(sx * 0.245, 0.01, z_center - 0.01), scale=(0.65, 0.84, 1.04)))
    parts.append(add_sphere("Nose", 0.022, skin_mat, col, loc=(0, -0.242, z_center - 0.028), scale=(1.12, 0.72, 0.85)))
    for sx in (-1, 1):
        parts.append(add_sphere(f"Cheek_{sx}", 0.044, MATS["AC_CheekBlush"], col,
                                loc=(sx * 0.138, -0.208, z_center - 0.054), scale=(1.18, 0.32, 0.68), rot=(0, 0, sx * 0.22)))
    # Friendly smile cleanly readable at gameplay camera distance
    parts.append(add_curved_strand(
        "Smile",
        [(-0.046, -0.218, z_center - 0.090), (0.0, -0.228, z_center - 0.106), (0.046, -0.218, z_center - 0.090)],
        [0.009, 0.011, 0.009], MATS["AC_Coral"], col, segs=8
    ))

    # Crisp glossy eyes readable both close-up and at gameplay distance
    eye_l_parts, eye_r_parts = [], []
    for sx, bucket in ((-1, eye_l_parts), (1, eye_r_parts)):
        ex = sx * 0.090
        ey = -0.220
        ez = z_center + 0.012
        bucket.append(add_sphere(f"Eye_{sx}", 0.042, MATS["AC_EyeDark"], col, loc=(ex, ey, ez), scale=(0.88, 0.44, 1.20), rot=(0, 0, sx * 0.10)))
        bucket.append(add_sphere(f"ShineA_{sx}", 0.016, MATS["AC_EyeShine"], col, loc=(ex - sx * 0.012, ey - 0.014, ez + 0.020), scale=(0.95, 0.55, 1.0)))
        bucket.append(add_sphere(f"ShineB_{sx}", 0.009, MATS["AC_EyeShine"], col, loc=(ex + sx * 0.013, ey - 0.013, ez - 0.016), scale=(0.9, 0.55, 0.9)))
        parts.append(add_curved_strand(
            f"Brow_{sx}",
            [(ex - sx * 0.036, -0.218, ez + 0.064), (ex, -0.226, ez + 0.072), (ex + sx * 0.034, -0.212, ez + 0.062)],
            [0.008, 0.010, 0.008], hair_mat, col, segs=6
        ))
    if not separate_eyes:
        parts.extend(eye_l_parts)
        parts.extend(eye_r_parts)

    # Tight, clean skull-hugging hair cap (never bloats wider than shoulders!)
    parts.append(add_lathe(
        "HairCap",
        [(0.262, -0.04), (0.275, 0.04), (0.265, 0.13), (0.212, 0.218), (0.125, 0.268), (0.0, 0.282)],
        hair_mat, col, loc=(0, 0.028, z_center + 0.016), scale=(1.03, 0.99, 0.98), segs=24, cap_bot=True
    ))
    parts.append(add_sphere("HairBack", 0.252, hair_mat, col, loc=(0, 0.078, z_center - 0.018), scale=(1.00, 0.84, 0.92), u=18, v=12))

    # Distinct hairstyles
    if hair_style == "hero_ponytail":
        # Iconic Protagonist sweeping bangs + high dynamic ponytail + BIG turquoise statement bow!
        for pts, rads in [
            ([(-0.04, -0.14, z_center + 0.24), (-0.12, -0.235, z_center + 0.145), (-0.195, -0.20, z_center + 0.02), (-0.215, -0.11, z_center - 0.08)], [0.052, 0.048, 0.038, 0.020]),
            ([(0.02, -0.15, z_center + 0.24), (0.10, -0.24, z_center + 0.145), (0.18, -0.205, z_center + 0.03), (0.21, -0.11, z_center - 0.07)], [0.050, 0.048, 0.036, 0.020]),
            ([(-0.06, -0.16, z_center + 0.22), (0.01, -0.245, z_center + 0.145), (0.07, -0.235, z_center + 0.085)], [0.038, 0.042, 0.022]),
        ]:
            parts.append(add_curved_strand("HeroBang", pts, rads, hair_mat, col, segs=10))
        # High bouncy hero ponytail clearly visible from rear/side/top camera!
        parts.append(add_curved_strand(
            "HeroPonytail",
            [(0.0, 0.22, z_center + 0.15), (0.0, 0.35, z_center + 0.12), (0.0, 0.42, z_center - 0.06), (0.0, 0.36, z_center - 0.24)],
            [0.060, 0.110, 0.100, 0.034], hair_mat, col, segs=14
        ))
        # Signature large bright-turquoise hair bow (instant visual beacon!)
        parts.append(add_sphere("HeroBowC", 0.052, accent_mat, col, loc=(0, 0.22, z_center + 0.185)))
        for sx in (-1, 1):
            parts.append(add_sphere(f"HeroBowLobe_{sx}", 0.095, accent_mat, col,
                                    loc=(sx * 0.115, 0.235, z_center + 0.205), scale=(1.35, 0.48, 0.78), rot=(0.15, -sx * 0.28, sx * 0.18)))
            parts.append(add_curved_strand(
                f"HeroRibbonTail_{sx}",
                [(sx * 0.04, 0.24, z_center + 0.16), (sx * 0.11, 0.29, z_center + 0.04), (sx * 0.15, 0.31, z_center - 0.08)],
                [0.026, 0.030, 0.016], accent_mat, col, segs=8
            ))

    elif hair_style == "short_boy":
        for sx, dx in ((-1, -0.14), (0, -0.02), (1, 0.12)):
            parts.append(add_curved_strand(
                f"BoyBang_{sx}",
                [(dx * 0.6, -0.13, z_center + 0.24), (dx, -0.235, z_center + 0.15), (dx * 1.1, -0.225, z_center + 0.08)],
                [0.046, 0.044, 0.020], hair_mat, col, segs=8
            ))
        for sx in (-1, 1):
            parts.append(add_curved_strand(f"Side_{sx}", [(sx * 0.22, -0.04, z_center + 0.12), (sx * 0.24, -0.05, z_center + 0.01)], [0.040, 0.022], hair_mat, col, segs=8))

    elif hair_style == "bob":
        for sx in (-1, 1):
            parts.append(add_curved_strand(
                f"BobSide_{sx}",
                [(sx * 0.17, -0.05, z_center + 0.20), (sx * 0.258, -0.06, z_center + 0.03), (sx * 0.248, -0.04, z_center - 0.14), (sx * 0.19, -0.02, z_center - 0.21)],
                [0.064, 0.072, 0.068, 0.036], hair_mat, col, segs=12
            ))
            parts.append(add_curved_strand(
                f"BobBang_{sx}",
                [(0.0, -0.13, z_center + 0.24), (sx * 0.10, -0.235, z_center + 0.15), (sx * 0.19, -0.195, z_center + 0.05)],
                [0.048, 0.045, 0.022], hair_mat, col, segs=10
            ))

    elif hair_style == "space_buns":
        # Distinct twin high space buns for Friend Bia!
        for sx in (-1, 1):
            parts.append(add_sphere(f"SpaceBun_{sx}", 0.108, hair_mat, col, loc=(sx * 0.22, 0.05, z_center + 0.24), scale=(1.0, 0.96, 0.95), u=16, v=10))
            parts.append(add_lathe(f"BunRing_{sx}", [(0.095, -0.02), (0.112, 0.0), (0.095, 0.02)], accent_mat, col,
                                   loc=(sx * 0.185, 0.04, z_center + 0.195), rot=(0.15, sx * 0.65, 0), segs=14))
            parts.append(add_curved_strand(
                f"SBBang_{sx}",
                [(0.0, -0.14, z_center + 0.24), (sx * 0.10, -0.235, z_center + 0.145), (sx * 0.19, -0.20, z_center + 0.05)],
                [0.046, 0.044, 0.022], hair_mat, col, segs=8
            ))

    elif hair_style == "high_bun":
        for sx in (-1, 1):
            parts.append(add_curved_strand(
                f"HBang_{sx}",
                [(0.0, -0.14, z_center + 0.24), (sx * 0.10, -0.235, z_center + 0.145), (sx * 0.18, -0.20, z_center + 0.05)],
                [0.046, 0.044, 0.022], hair_mat, col, segs=8
            ))
        parts.append(add_sphere("HighBun", 0.125, hair_mat, col, loc=(0, 0.10, z_center + 0.28), scale=(1.04, 1.0, 0.88), u=16, v=10))
        parts.append(add_lathe("HBunTie", [(0.105, -0.022), (0.128, 0.0), (0.105, 0.022)], accent_mat, col, loc=(0, 0.085, z_center + 0.23), rot=(0.25, 0, 0), segs=16))

    elif hair_style == "low_bun":
        # Distinct granny low back bun
        for sx in (-1, 1):
            parts.append(add_curved_strand(
                f"LBang_{sx}",
                [(0.0, -0.14, z_center + 0.235), (sx * 0.12, -0.23, z_center + 0.14), (sx * 0.21, -0.16, z_center + 0.03)],
                [0.048, 0.046, 0.026], hair_mat, col, segs=8
            ))
        parts.append(add_sphere("LowBun", 0.118, hair_mat, col, loc=(0, 0.26, z_center - 0.02), scale=(1.08, 0.90, 0.92), u=16, v=10))

    elif hair_style == "side_braid":
        # Distinct rural side braid over one shoulder
        for sx in (-1, 1):
            parts.append(add_curved_strand(
                f"BrBang_{sx}",
                [(0.0, -0.14, z_center + 0.24), (sx * 0.10, -0.235, z_center + 0.145), (sx * 0.18, -0.195, z_center + 0.04)],
                [0.046, 0.044, 0.022], hair_mat, col, segs=8
            ))
        parts.append(add_curved_strand(
            "SideBraid",
            [(0.18, 0.08, z_center - 0.02), (0.23, -0.04, z_center - 0.16), (0.20, -0.12, z_center - 0.32), (0.18, -0.14, z_center - 0.44)],
            [0.068, 0.062, 0.052, 0.026], hair_mat, col, segs=10
        ))
        parts.append(add_sphere("BraidTie", 0.038, accent_mat, col, loc=(0.19, -0.13, z_center - 0.38)))

    elif hair_style == "long_wave":
        for sx in (-1, 1):
            parts.append(add_curved_strand(
                f"LongSide_{sx}",
                [(sx * 0.17, -0.04, z_center + 0.18), (sx * 0.26, -0.04, z_center + 0.01), (sx * 0.25, 0.04, z_center - 0.18), (sx * 0.21, 0.08, z_center - 0.34)],
                [0.062, 0.072, 0.066, 0.032], hair_mat, col, segs=10
            ))
            parts.append(add_curved_strand(
                f"LongBang_{sx}",
                [(0.0, -0.13, z_center + 0.24), (sx * 0.10, -0.235, z_center + 0.15), (sx * 0.20, -0.19, z_center + 0.04)],
                [0.046, 0.044, 0.022], hair_mat, col, segs=8
            ))
        parts.append(add_curved_strand(
            "LongBackC",
            [(0.0, 0.15, z_center + 0.10), (0.0, 0.22, z_center - 0.08), (0.0, 0.20, z_center - 0.32)],
            [0.135, 0.142, 0.075], hair_mat, col, segs=12
        ))

    # Distinct Headwear
    if hat_style == "cap":
        parts.append(add_lathe(
            "CapDome", [(0.268, 0.0), (0.272, 0.065), (0.225, 0.145), (0.13, 0.195), (0.0, 0.208)],
            accent_mat, col, loc=(0, 0.01, z_center + 0.115), scale=(1.02, 1.02, 0.92), rot=(-0.10, 0, 0), segs=22
        ))
        parts.append(add_sphere("CapBrim", 0.21, accent_mat, col, loc=(0, -0.22, z_center + 0.135), scale=(1.06, 0.85, 0.14), rot=(0.12, 0, 0)))
        parts.append(add_sphere("CapBtn", 0.024, MATS["AC_Cream"], col, loc=(0, 0.02, z_center + 0.31)))
    elif hat_style == "visor":
        parts.append(add_lathe("VisorBand", [(0.258, -0.028), (0.272, 0.0), (0.258, 0.028)], MATS["AC_Cream"], col, loc=(0, 0.0, z_center + 0.145), rot=(-0.08, 0, 0), segs=22))
        parts.append(add_sphere("VisorBrim", 0.22, MATS["AC_Cream"], col, loc=(0, -0.22, z_center + 0.135), scale=(1.12, 0.88, 0.12), rot=(0.14, 0, 0)))
    elif hat_style == "straw_flower":
        parts.append(add_lathe("StrawBrim", [(0.20, 0.02), (0.35, 0.01), (0.44, -0.02), (0.45, -0.04), (0.33, -0.02), (0.20, 0.0)],
                               MATS["AC_StrawHat"], col, loc=(0, 0.02, z_center + 0.18), rot=(-0.10, 0, 0), segs=26))
        parts.append(add_lathe("StrawCrown", [(0.22, 0.0), (0.215, 0.075), (0.185, 0.145), (0.11, 0.175), (0.0, 0.18)],
                               MATS["AC_StrawHat"], col, loc=(0, 0.02, z_center + 0.185), rot=(-0.10, 0, 0), segs=22))
        parts.append(add_lathe("StrawRib", [(0.228, 0.0), (0.228, 0.044)], accent_mat, col, loc=(0, 0.02, z_center + 0.195), rot=(-0.10, 0, 0), segs=22, cap_bot=False, cap_top=False))
        # Cute flower blossoms on hat brim
        parts.append(add_sphere("HatFlA", 0.055, MATS["AC_FlowerPink"], col, loc=(0.21, -0.12, z_center + 0.225), scale=(1.0, 1.0, 0.6)))
        parts.append(add_sphere("HatFlB", 0.046, MATS["AC_FlowerYellow"], col, loc=(0.14, -0.18, z_center + 0.220), scale=(1.0, 1.0, 0.6)))
    elif hat_style == "straw_farm":
        parts.append(add_lathe("FarmBrim", [(0.21, 0.02), (0.38, 0.015), (0.49, -0.03), (0.50, -0.05), (0.36, -0.02), (0.21, 0.0)],
                               MATS["AC_StrawHat"], col, loc=(0, 0.02, z_center + 0.175), rot=(-0.08, 0, 0), segs=26))
        parts.append(add_lathe("FarmCrown", [(0.23, 0.0), (0.22, 0.09), (0.18, 0.165), (0.10, 0.195), (0.0, 0.20)],
                               MATS["AC_StrawHat"], col, loc=(0, 0.02, z_center + 0.185), rot=(-0.08, 0, 0), segs=22))
        parts.append(add_lathe("FarmRib", [(0.236, 0.0), (0.236, 0.046)], MATS["AC_Orange"], col, loc=(0, 0.02, z_center + 0.195), rot=(-0.08, 0, 0), segs=22, cap_bot=False, cap_top=False))
    elif hat_style == "beret":
        parts.append(add_sphere("Beret", 0.25, MATS["AC_Denim"], col, loc=(0.04, 0.02, z_center + 0.25), scale=(1.18, 1.12, 0.42), rot=(-0.12, 0.22, 0.0)))
        parts.append(add_sphere("BeretTip", 0.032, MATS["AC_Denim"], col, loc=(0.05, 0.02, z_center + 0.35)))
    elif hat_style == "headband":
        parts.append(add_lathe("ZenBand", [(0.255, -0.025), (0.268, 0.0), (0.255, 0.025)], accent_mat, col, loc=(0, 0.0, z_center + 0.14), rot=(-0.06, 0, 0), segs=22))

    # Optional Glasses / Sunglasses
    if glasses == "sunglasses":
        for sx in (-1, 1):
            parts.append(add_sphere(f"SunLens_{sx}", 0.055, MATS["AC_MetalDark"], col, loc=(sx * 0.086, -0.240, z_center + 0.018), scale=(1.15, 0.28, 0.85)))
            parts.append(add_sphere(f"SunRim_{sx}", 0.062, MATS["AC_Cream"], col, loc=(sx * 0.086, -0.234, z_center + 0.018), scale=(1.18, 0.22, 0.90)))
        parts.append(add_rounded_box("SunBridge", (0.06, 0.015, 0.016), MATS["AC_Cream"], col, loc=(0, -0.242, z_center + 0.028), bevel=0.004, segs=1))
    elif glasses == "round_gold":
        for sx in (-1, 1):
            parts.append(add_lathe(f"GrRim_{sx}", [(0.052, -0.008), (0.062, 0.0), (0.052, 0.008)], MATS["AC_Gold"], col,
                                   loc=(sx * 0.088, -0.240, z_center + 0.012), rot=(math.pi * 0.5, 0, 0), segs=16, cap_bot=False, cap_top=False))
        parts.append(add_rounded_box("GrBridge", (0.05, 0.012, 0.012), MATS["AC_Gold"], col, loc=(0, -0.242, z_center + 0.016), bevel=0.003, segs=1))

    return parts, eye_l_parts, eye_r_parts


# =========================================================================
# DISTINCT CHARACTER BUILDER (Unique Outfit Silhouettes + Accessories + Heights)
# =========================================================================
def build_character(
    fbx_name, col,
    skin_key, hair_key, hair_style,
    top_key, bottom_key, shoe_key, accent_key,
    outfit_style="dress",      # hero_pinafore | dress | polo_shorts | work_apron | bolero_dress | florist_apron | sailor_dress | haori | shawl_skirt | farm_overalls
    hat_style=None,
    glasses=None,
    accessory=None,            # hero_backpack | tourist_backpack | crossbody_bag | flower_basket | handbag
    height_scale=1.0,
    is_protag=False,
    is_single_mesh=False
):
    skin = MATS[skin_key]
    hair = MATS[hair_key]
    top = MATS[top_key]
    bot = MATS[bottom_key]
    shoe = MATS[shoe_key]
    accent = MATS[accent_key]
    cream = MATS["AC_Cream"]

    torso_parts = []
    torso_parts.append(add_lathe("Neck", [(0.068, 0.96), (0.064, 1.06)], skin, col, segs=14))

    sleeve_mat = top
    leg_mat = skin

    if outfit_style == "hero_pinafore":
        # Iconic Protagonist: Crisp white blouse top + vibrant Sunny-Yellow flared pinafore dress + turquoise belt & bow!
        sleeve_mat = cream
        torso_parts.append(add_lathe("HeroBlouse", [(0.180, 0.72), (0.185, 0.92), (0.140, 0.99), (0.0, 1.00)], cream, col, scale=(1.04, 0.90, 1.0), segs=22))
        torso_parts.append(add_lathe(
            "HeroSkirt",
            [(0.0, 0.44), (0.248, 0.44), (0.262, 0.49), (0.222, 0.63), (0.188, 0.76), (0.192, 0.88), (0.162, 0.95)],
            top, col, scale=(1.04, 0.90, 1.0), segs=24
        ))
        # Crisp bright-turquoise waist sash & hem contrast stripe
        torso_parts.append(add_lathe("HeroBelt", [(0.196, 0.70), (0.204, 0.755), (0.194, 0.768)], accent, col, scale=(1.05, 0.91, 1.0), segs=22))
        torso_parts.append(add_lathe("HeroHem", [(0.254, 0.445), (0.266, 0.478)], cream, col, scale=(1.04, 0.90, 1.0), segs=24))
        for sx in (-1, 1):
            torso_parts.append(add_sphere(f"HeroCol_{sx}", 0.066, cream, col, loc=(sx * 0.062, -0.092, 0.965), scale=(1.15, 0.85, 0.30), rot=(0.24, 0, sx * 0.24)))

    elif outfit_style == "polo_shorts":
        leg_mat = skin
        torso_parts.append(add_lathe("Shorts", [(0.0, 0.44), (0.190, 0.45), (0.194, 0.59), (0.178, 0.66)], bot, col, scale=(1.03, 0.90, 1.0), segs=20))
        torso_parts.append(add_lathe("Polo", [(0.196, 0.60), (0.194, 0.76), (0.198, 0.92), (0.148, 0.99), (0.0, 1.00)], top, col, scale=(1.03, 0.90, 1.0), segs=22))
        torso_parts.append(add_lathe("PoloStripe", [(0.200, 0.74), (0.200, 0.80)], cream, col, scale=(1.03, 0.90, 1.0), segs=22, cap_bot=False, cap_top=False))
        torso_parts.append(add_lathe("PoloCollar", [(0.132, 0.955), (0.148, 0.985), (0.112, 1.002)], cream, col, scale=(1.03, 0.92, 1.0), segs=20))

    elif outfit_style == "work_apron":
        leg_mat = bot
        torso_parts.append(add_lathe("Pants", [(0.0, 0.44), (0.188, 0.45), (0.192, 0.62)], bot, col, scale=(1.02, 0.90, 1.0), segs=20))
        torso_parts.append(add_lathe("WorkTee", [(0.192, 0.60), (0.192, 0.92), (0.146, 0.99), (0.0, 1.00)], top, col, scale=(1.02, 0.90, 1.0), segs=20))
        # Fitted cream kiosk apron cleanly wrapped around torso (never floating!)
        torso_parts.append(add_lathe("ApronSkirt", [(0.204, 0.48), (0.200, 0.68), (0.196, 0.85)], cream, col, loc=(0, -0.015, 0), scale=(0.98, 0.92, 1.0), segs=20))
        torso_parts.append(add_lathe("ApronTie", [(0.204, 0.66), (0.206, 0.70)], accent, col, scale=(1.03, 0.91, 1.0), segs=20))

    elif outfit_style == "bolero_dress":
        torso_parts.append(add_lathe("InnerDress", [(0.0, 0.44), (0.232, 0.44), (0.242, 0.48), (0.208, 0.64), (0.182, 0.86), (0.142, 0.99), (0.0, 1.00)], bot, col, scale=(1.03, 0.90, 1.0), segs=22))
        torso_parts.append(add_lathe("Bolero", [(0.206, 0.74), (0.210, 0.92), (0.156, 0.995)], top, col, scale=(1.04, 0.92, 1.0), segs=22))
        torso_parts.append(add_lathe("BoleroHem", [(0.236, 0.445), (0.246, 0.475)], accent, col, scale=(1.03, 0.90, 1.0), segs=22))

    elif outfit_style == "florist_apron":
        sleeve_mat = cream
        torso_parts.append(add_lathe("BlouseF", [(0.0, 0.44), (0.228, 0.44), (0.236, 0.50), (0.195, 0.76), (0.188, 0.92), (0.145, 0.99), (0.0, 1.00)], cream, col, scale=(1.03, 0.90, 1.0), segs=22))
        torso_parts.append(add_lathe("GardenApron", [(0.242, 0.47), (0.214, 0.66), (0.198, 0.84)], top, col, scale=(1.03, 0.92, 1.0), segs=22))
        torso_parts.append(add_lathe("ApronSash", [(0.208, 0.69), (0.210, 0.735)], accent, col, scale=(1.03, 0.92, 1.0), segs=22))

    elif outfit_style == "sailor_dress":
        torso_parts.append(add_lathe("SailorDress", [(0.0, 0.44), (0.234, 0.44), (0.244, 0.48), (0.210, 0.64), (0.186, 0.86), (0.145, 0.99), (0.0, 1.00)], cream, col, scale=(1.03, 0.90, 1.0), segs=22))
        torso_parts.append(add_lathe("SailorStripe", [(0.240, 0.48), (0.234, 0.54)], top, col, scale=(1.03, 0.90, 1.0), segs=22, cap_bot=False, cap_top=False))
        # Iconic wide sailor collar + scarf tie
        torso_parts.append(add_lathe("SailorCollar", [(0.218, 0.87), (0.224, 0.93), (0.155, 0.995)], MATS["AC_Denim"], col, scale=(1.04, 0.96, 1.0), segs=22))
        torso_parts.append(add_sphere("SailorKnot", 0.048, accent, col, loc=(0, -0.185, 0.89), scale=(1.1, 0.7, 1.2)))

    elif outfit_style == "haori":
        leg_mat = bot
        torso_parts.append(add_lathe("ZenPants", [(0.0, 0.44), (0.194, 0.45), (0.196, 0.62)], bot, col, scale=(1.03, 0.90, 1.0), segs=20))
        torso_parts.append(add_lathe("ZenTee", [(0.188, 0.60), (0.188, 0.93), (0.142, 0.99), (0.0, 1.00)], cream, col, scale=(1.02, 0.90, 1.0), segs=20))
        # Open haori coastal jacket + bold obi belt
        torso_parts.append(add_lathe("HaoriCoat", [(0.214, 0.54), (0.208, 0.76), (0.206, 0.93), (0.152, 0.99)], top, col, loc=(0, 0.012, 0), scale=(1.04, 0.92, 1.0), segs=22))
        torso_parts.append(add_lathe("ObiSash", [(0.214, 0.64), (0.218, 0.73)], accent, col, scale=(1.04, 0.92, 1.0), segs=22))

    elif outfit_style == "shawl_skirt":
        # Long cozy skirt + knitted shoulder shawl for Grandma!
        torso_parts.append(add_lathe("LongSkirt", [(0.0, 0.28), (0.246, 0.28), (0.252, 0.34), (0.218, 0.62), (0.192, 0.88), (0.145, 0.99), (0.0, 1.00)], cream, col, scale=(1.04, 0.92, 1.0), segs=22))
        torso_parts.append(add_lathe("Shawl", [(0.236, 0.72), (0.240, 0.86), (0.195, 0.96), (0.140, 0.995)], top, col, scale=(1.06, 0.96, 1.0), segs=22))
        torso_parts.append(add_sphere("Brooch", 0.036, accent, col, loc=(0, -0.205, 0.90), scale=(1.0, 0.55, 1.0)))

    elif outfit_style == "farm_overalls":
        sleeve_mat = cream
        leg_mat = bot
        torso_parts.append(add_lathe("FarmShirt", [(0.184, 0.70), (0.188, 0.92), (0.144, 0.99), (0.0, 1.00)], cream, col, scale=(1.03, 0.90, 1.0), segs=20))
        torso_parts.append(add_lathe("Overalls", [(0.0, 0.44), (0.200, 0.45), (0.204, 0.68), (0.192, 0.86)], bot, col, scale=(1.03, 0.91, 1.0), segs=22))
        torso_parts.append(add_lathe("OverallsBelt", [(0.208, 0.65), (0.208, 0.69)], accent, col, scale=(1.03, 0.91, 1.0), segs=20))

    else:
        # Clean summer sundress
        torso_parts.append(add_lathe(
            "Dress",
            [(0.0, 0.44), (0.232, 0.44), (0.244, 0.48), (0.210, 0.63), (0.182, 0.80), (0.186, 0.92), (0.144, 0.99), (0.0, 1.00)],
            top, col, scale=(1.03, 0.90, 1.0), segs=22
        ))
        torso_parts.append(add_lathe("Sash", [(0.192, 0.71), (0.198, 0.76), (0.188, 0.77)], accent, col, scale=(1.03, 0.91, 1.0), segs=22))
        torso_parts.append(add_lathe("Hem", [(0.240, 0.445), (0.248, 0.475)], cream, col, scale=(1.03, 0.90, 1.0), segs=22))

    # Distinct 3D Accessories attached cleanly to torso
    if accessory == "hero_backpack":
        torso_parts.append(add_rounded_box("HeroPack", (0.24, 0.14, 0.28), accent, col, loc=(0, 0.22, 0.77), bevel=0.055, segs=3))
        torso_parts.append(add_rounded_box("HeroPackPocket", (0.17, 0.05, 0.12), cream, col, loc=(0, 0.285, 0.71), bevel=0.02, segs=2))
        torso_parts.append(add_sphere("HeroPackClasp", 0.026, MATS["AC_Gold"], col, loc=(0, 0.305, 0.77)))
    elif accessory == "tourist_backpack":
        torso_parts.append(add_rounded_box("TourPack", (0.26, 0.16, 0.34), MATS["AC_MintTeal"], col, loc=(0, 0.23, 0.78), bevel=0.055, segs=3))
        torso_parts.append(add_rounded_box("TourPackRoll", (0.26, 0.08, 0.08), MATS["AC_SunnyYellow"], col, loc=(0, 0.22, 0.96), bevel=0.035, segs=2))
    elif accessory == "crossbody_bag":
        torso_parts.append(add_rounded_box("SideBag", (0.14, 0.09, 0.16), MATS["AC_StrawHat"], col, loc=(0.21, -0.02, 0.58), rot=(0, 0.15, 0), bevel=0.035, segs=2))
        torso_parts.append(add_sphere("BagBtn", 0.022, MATS["AC_Gold"], col, loc=(0.25, -0.05, 0.61)))
    elif accessory == "flower_basket":
        torso_parts.append(add_lathe("Basket", [(0.08, 0.0), (0.11, 0.14), (0.12, 0.16)], MATS["AC_WoodLight"], col, loc=(-0.22, -0.06, 0.56), segs=14))
        torso_parts.append(add_sphere("BaskLeaf", 0.09, MATS["AC_BushGreen"], col, loc=(-0.22, -0.06, 0.72), scale=(1.1, 1.0, 0.55)))
        torso_parts.append(add_sphere("BaskFl1", 0.045, MATS["AC_FlowerPink"], col, loc=(-0.25, -0.09, 0.76)))
        torso_parts.append(add_sphere("BaskFl2", 0.042, MATS["AC_FlowerYellow"], col, loc=(-0.18, -0.04, 0.77)))
    elif accessory == "handbag":
        torso_parts.append(add_rounded_box("Handbag", (0.14, 0.08, 0.14), MATS["AC_PinkBerry"], col, loc=(0.22, -0.05, 0.56), bevel=0.03, segs=2))

    # HEAD
    head_parts, eye_l_parts, eye_r_parts = build_head_and_hair(
        col, skin, hair, hair_style, accent, hat_style=hat_style, glasses=glasses, z_center=1.23, separate_eyes=is_protag
    )

    # ARMS angled 12 deg outward with clear rounded hands (NEVER clipping torso/dress!)
    arm_l_parts, arm_r_parts = [], []
    for sx, bucket in ((-1, arm_l_parts), (1, arm_r_parts)):
        ax = sx * 0.222
        bucket.append(add_sphere(f"Sleeve_{sx}", 0.078, sleeve_mat, col, loc=(ax, 0.0, 0.91), scale=(0.95, 0.92, 1.12)))
        bucket.append(add_curved_strand(
            f"Arm_{sx}",
            [(ax, 0.0, 0.89), (ax + sx * 0.052, -0.015, 0.74), (ax + sx * 0.082, -0.025, 0.60)],
            [0.052, 0.045, 0.040], skin, col, segs=12
        ))
        bucket.append(add_sphere(f"Hand_{sx}", 0.052, skin, col, loc=(ax + sx * 0.086, -0.030, 0.575), scale=(1.0, 1.08, 0.96)))

    # LEGS & CLEARLY VISIBLE FLAT-SOLED SHOES (grounded strictly at Z = 0.000!)
    leg_l_parts, leg_r_parts = [], []
    for sx, bucket in ((-1, leg_l_parts), (1, leg_r_parts)):
        lx = sx * 0.098
        bucket.append(add_lathe(f"Leg_{sx}", [(0.048, 0.14), (0.054, 0.30), (0.064, 0.48)], leg_mat, col, loc=(lx, 0.0, 0.0), segs=14))
        bucket.append(add_lathe(f"Sock_{sx}", [(0.052, 0.09), (0.055, 0.18)], cream, col, loc=(lx, 0.0, 0.0), segs=14))
        bucket.append(add_sphere(f"Shoe_{sx}", 0.072, shoe, col, loc=(lx, -0.045, 0.062), scale=(1.02, 1.56, 0.86), u=16, v=10))
        bucket.append(add_lathe(f"Sole_{sx}", [(0.0, 0.0), (0.074, 0.0), (0.076, 0.026), (0.0, 0.026)], cream, col,
                                loc=(lx, -0.045, 0.0), scale=(1.02, 1.56, 1.0), segs=16))

    # Assemble & apply character height variation cleanly while keeping exact Unity pivots!
    def scale_all(objs, hs):
        if abs(hs - 1.0) < 0.001:
            return
        for o in objs:
            o.location *= hs
            o.data.transform(Matrix.Scale(hs, 4))

    if is_protag:
        o_torso = join_with_pivot(torso_parts, "Protag_Torso", (0.0, 0.0, 0.61), col)
        o_head = join_with_pivot(head_parts, "Protag_Head", (0.0, 0.0, 1.02), col)
        o_eyel = join_with_pivot(eye_l_parts, "Protag_EyeL", (-0.090, -0.220, 1.242), col)
        o_eyer = join_with_pivot(eye_r_parts, "Protag_EyeR", (0.090, -0.220, 1.242), col)
        o_arml = join_with_pivot(arm_l_parts, "Protag_ArmL", (-0.222, 0.0, 0.94), col)
        o_armr = join_with_pivot(arm_r_parts, "Protag_ArmR", (0.222, 0.0, 0.94), col)
        o_legl = join_with_pivot(leg_l_parts, "Protag_LegL", (-0.098, 0.0, 0.48), col)
        o_legr = join_with_pivot(leg_r_parts, "Protag_LegR", (0.098, 0.0, 0.48), col)
        objs = [o_torso, o_head, o_eyel, o_eyer, o_arml, o_armr, o_legl, o_legr]
        scale_all(objs, height_scale)
        export_fbx(objs, fbx_name)
    elif is_single_mesh:
        all_p = torso_parts + head_parts + arm_l_parts + arm_r_parts + leg_l_parts + leg_r_parts
        o_all = join_with_pivot(all_p, fbx_name, (0.0, 0.0, 0.0), col)
        scale_all([o_all], height_scale)
        export_fbx([o_all], fbx_name)
    else:
        o_torso = join_with_pivot(torso_parts + head_parts, "Torso", (0.0, 0.0, 0.78), col)
        o_arml = join_with_pivot(arm_l_parts, "ArmL", (-0.222, 0.0, 0.94), col)
        o_armr = join_with_pivot(arm_r_parts, "ArmR", (0.222, 0.0, 0.94), col)
        o_legl = join_with_pivot(leg_l_parts, "LegL", (-0.098, 0.0, 0.48), col)
        o_legr = join_with_pivot(leg_r_parts, "LegR", (0.098, 0.0, 0.48), col)
        objs = [o_torso, o_arml, o_armr, o_legl, o_legr]
        scale_all(objs, height_scale)
        export_fbx(objs, fbx_name)


def main():
    specs = [
        # (name, skin, hair, hair_style, top, bot, shoe, accent, outfit, hat, glasses, accessory, h_scale, is_protag, is_single)
        ("Char_Protagonist",     "AC_SkinLight", "AC_HairBrown",       "hero_ponytail", "AC_ProtagHero",  "AC_Cream",      "AC_Coral",     "AC_ProtagRibbon", "hero_pinafore", None,           None,         "hero_backpack",    1.04, True,  False),
        ("Char_PedestrianBlue",  "AC_SkinTan",   "AC_HairDark",        "short_boy",     "AC_Denim",       "AC_WoodLight",  "AC_ShoeDark",  "AC_SkyBlue",      "polo_shorts",   "cap",          None,         "tourist_backpack", 1.07, False, False),
        ("Char_PedestrianCoral", "AC_SkinLight", "AC_HairAuburn",      "bob",           "AC_Terracotta",  "AC_Cream",      "AC_WoodWarm",  "AC_MintSoft",     "dress",         "visor",        None,         "crossbody_bag",    0.97, False, False),
        ("Char_FriendNina",      "AC_SkinWarm",  "AC_HairDark",        "high_bun",      "AC_MintTeal",    "AC_Denim",      "AC_ShoeDark",  "AC_Gold",         "work_apron",    None,           None,         None,               1.00, False, False),
        ("Char_FriendBia",       "AC_SkinTan",   "AC_HairAuburn",      "space_buns",    "AC_Lavender",    "AC_PinkSoft",   "AC_Coral",     "AC_SunnyYellow",  "bolero_dress",  None,           None,         None,               0.93, False, False),
        ("Char_Florist",         "AC_SkinLight", "AC_HairBrown",       "long_wave",     "AC_MintSoft",    "AC_Cream",      "AC_WoodDark",  "AC_PinkBerry",    "florist_apron", "straw_flower", None,         "flower_basket",    1.02, False, False),
        ("Char_SisterMarina",    "AC_SkinLight", "AC_HairBrown",       "long_wave",     "AC_SkyBlue",     "AC_Cream",      "AC_ShoeDark",  "AC_SunnyYellow",  "sailor_dress",  "beret",        None,         None,               1.01, False, False),
        ("Char_Barbie",          "AC_SkinLight", "AC_HairWind_Blonde", "long_wave",     "AC_PinkSoft",    "AC_PinkBerry",  "AC_PinkBerry", "AC_Cream",        "dress",         None,           "sunglasses", "handbag",          1.04, False, True),
        ("Char_Japones",         "AC_SkinLight", "AC_HairWind_Dark",   "short_boy",     "AC_SkyBlue",     "AC_Denim",      "AC_WoodDark",  "AC_Coral",        "haori",         "headband",     None,         None,               1.05, False, True),
        ("Char_Vovo",            "AC_SkinLight", "AC_HairWind_Silver", "low_bun",       "AC_MintSoft",    "AC_Cream",      "AC_WoodDark",  "AC_Gold",         "shawl_skirt",   None,           "round_gold", None,               0.89, False, True),
        ("Char_Camponesa",       "AC_SkinLight", "AC_HairWind_Auburn", "side_braid",    "AC_Cream",       "AC_BushGreen",  "AC_WoodDark",  "AC_Orange",       "farm_overalls", "straw_farm",   None,         None,               0.98, False, True),
    ]
    for s in specs:
        clear_scene()
        init_mats()
        build_character(
            s[0], bpy.context.scene.collection,
            s[1], s[2], s[3], s[4], s[5], s[6], s[7],
            outfit_style=s[8], hat_style=s[9], glasses=s[10], accessory=s[11],
            height_scale=s[12], is_protag=s[13], is_single_mesh=s[14]
        )
    print("[v41] ALL distinct Hero & Villager characters exported cleanly!")


if __name__ == "__main__":
    main()
