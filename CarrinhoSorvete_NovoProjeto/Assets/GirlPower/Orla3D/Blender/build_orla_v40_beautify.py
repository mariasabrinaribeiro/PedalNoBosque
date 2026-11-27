"""
Orla das Fadas — v40 Beautify & Character Redesign Script (Blender 5.3):
1. Redesigned adorable Animal Crossing chibi characters (smooth sculpted hair & curved bangs,
   soft A-line dresses/tunics, natural tapered arms/legs, clean kawaii faces, delicate fairy wings,
   zero lumpy sphere clusters, zero clipping chest boxes/clipboards, preserving exact Unity pivots).
2. Upgraded OrlaEnvironmentBase.fbx (vibrant green park lawns, cobble patios, uninterrupted coastal
   promenade, steep underwater sand drop past z=8.1m so the ocean shines clearly, and south village
   greenery extending to z=-88m so zero blue sky void ever shows behind the houses).
3. Clean flat-faced RouteSignpost.fbx so 3D sign text never clips inside the wooden arrows.
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
        ("AC_Sand", "#F4DFB4"), ("AC_SandWet", "#DEC69C"), ("AC_Grass", "#62A876"),
        ("AC_GrassLight", "#80C08E"), ("AC_StoneWhite", "#F5EFE4"), ("AC_StoneWarm", "#E6D7BE"),
        ("AC_CobbleA", "#DFCEB4"), ("AC_CobbleB", "#CEB99A"), ("AC_Starfish", "#E28E7C"),
        ("AC_Shell", "#F8ECE0"), ("AC_WoodLight", "#D4AE82"), ("AC_WoodWarm", "#B88E62"),
        ("AC_WoodDark", "#6E4C34"), ("AC_Cream", "#FAF4E8"), ("AC_Gold", "#E6C274"),
        ("AC_ProtagShirt", "#EC5A8E"), ("AC_PinkBerry", "#EC5A8E"), ("AC_PinkSoft", "#F6B4CE"),
        ("AC_MintTeal", "#2FA8B4"), ("AC_MintSoft", "#98D8C8"), ("AC_SkyBlue", "#66B8D4"),
        ("AC_Denim", "#468AA4"), ("AC_Coral", "#E68270"), ("AC_Lavender", "#C6A2E4"),
        ("AC_SunnyYellow", "#F4D472"), ("AC_Orange", "#EA9B52"), ("AC_BushGreen", "#4D9E65"),
        ("AC_BushLight", "#7AC286"), ("AC_FlowerPink", "#EC6FA4"), ("AC_FlowerYellow", "#EED286"),
        ("AC_LanternGlow", "#FFE69C"), ("AC_StrawHat", "#EED2A4"), ("AC_ShoeDark", "#4E3A4C"),
        ("AC_SkinLight", "#F7C9A8"), ("AC_SkinTan", "#E2A67C"), ("AC_SkinWarm", "#CC8B64"),
        ("AC_CheekBlush", "#F594B0"), ("AC_EyeDark", "#221A26"), ("AC_EyeShine", "#FFFFFF"),
        ("AC_HairBrown", "#6B442A"), ("AC_HairDark", "#2E2736"), ("AC_HairAuburn", "#B85C40"),
        ("AC_HairPlum", "#8A4A98"), ("AC_HairWind_Blonde", "#ECC874"),
        ("AC_HairWind_Dark", "#2E2736"), ("AC_HairWind_Silver", "#D6DAE2"),
        ("AC_HairWind_Auburn", "#B85C40"), ("AC_HairWind_Brown", "#6B442A"),
    ]
    for k, hx in palette:
        m = bpy.data.materials.new(name=k)
        bsdf = m.node_tree.nodes.get("Principled BSDF") if m.node_tree else None
        if bsdf:
            bsdf.inputs["Base Color"].default_value = hex_rgba(hx)
            bsdf.inputs["Roughness"].default_value = 0.84
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
    """Smooth revolved profile [(radius, z), ...] for cute organic dresses, torsos, limbs, hair domes & hats."""
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
    """Smooth tapered curved tube along 3D points for flowing bangs, hair locks, ponytails & floppy ears."""
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
                v = p + (side * math.cos(a) + up2 * math.sin(a)) * r
                ring.append(bm.verts.new(v))
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
    """Joins mesh parts into one object while placing its exact origin at pivot_world (for Unity rotation!)."""
    bpy.ops.object.select_all(action="DESELECT")
    for p in parts:
        p.select_set(True)
    bpy.context.view_layer.objects.active = parts[0]
    bpy.ops.object.join()
    obj = bpy.context.active_object
    obj.name = name
    obj.data.name = name
    # Bake world transforms into mesh, then shift origin to pivot_world
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    pv = Vector(pivot_world)
    obj.data.transform(Matrix.Translation(-pv))
    obj.location = pv
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
    print(f"[FBX v40] {out_path}")


# =========================================================================
# 1. CUTE ANIMAL CROSSING CHIBI CHARACTER BUILDER
# =========================================================================
def build_chibi_head_parts(col, skin_mat, hair_mat, hair_style, accent_mat, hat_style=None, z_center=1.24, separate_eyes=False):
    """Builds a charming, clean Animal Crossing head facing -Y with sculpted hair & curved bangs."""
    parts = []
    # Soft chibi head (slightly wider cheeks, smooth rounded silhouette)
    parts.append(add_lathe(
        "HeadBase",
        [
            (0.0, -0.22), (0.11, -0.21), (0.205, -0.15), (0.262, -0.04),
            (0.270, 0.05), (0.252, 0.15), (0.190, 0.23), (0.095, 0.275), (0.0, 0.285),
        ],
        skin_mat, col, loc=(0, 0, z_center), scale=(1.04, 0.96, 0.98), segs=22
    ))
    # Cute little ears nestled cleanly on sides
    for sx in (-1, 1):
        parts.append(add_sphere(f"Ear_{sx}", 0.056, skin_mat, col, loc=(sx * 0.265, 0.01, z_center - 0.01), scale=(0.65, 0.85, 1.05)))
    # Cute tiny button nose
    parts.append(add_sphere("Nose", 0.024, skin_mat, col, loc=(0, -0.258, z_center - 0.032), scale=(1.15, 0.72, 0.85)))
    # Warm rosy cheeks
    for sx in (-1, 1):
        parts.append(add_sphere(f"Cheek_{sx}", 0.046, MATS["AC_CheekBlush"], col,
                                loc=(sx * 0.145, -0.222, z_center - 0.060), scale=(1.15, 0.34, 0.70), rot=(0, 0, sx * 0.24)))
    # Gentle warm smile arc (clean curved strand, never scary!)
    parts.append(add_curved_strand(
        "Smile",
        [(-0.042, -0.236, z_center - 0.098), (0.0, -0.244, z_center - 0.112), (0.042, -0.236, z_center - 0.098)],
        [0.008, 0.010, 0.008], MATS["AC_Coral"], col, segs=8
    ))

    # Glossy expressive Animal Crossing eyes (returned separately for Protagonist blinking!)
    eye_l_parts, eye_r_parts = [], []
    for sx, bucket in ((-1, eye_l_parts), (1, eye_r_parts)):
        ex = sx * 0.096
        ey = -0.236
        ez = z_center + 0.010
        bucket.append(add_sphere(f"Eye_{sx}", 0.042, MATS["AC_EyeDark"], col, loc=(ex, ey, ez), scale=(0.86, 0.44, 1.18), rot=(0, 0, sx * 0.12)))
        bucket.append(add_sphere(f"ShineA_{sx}", 0.015, MATS["AC_EyeShine"], col, loc=(ex - sx * 0.012, ey - 0.014, ez + 0.020), scale=(0.9, 0.55, 1.0)))
        bucket.append(add_sphere(f"ShineB_{sx}", 0.008, MATS["AC_EyeShine"], col, loc=(ex + sx * 0.013, ey - 0.013, ez - 0.016), scale=(0.9, 0.55, 0.9)))
        # Tiny soft brow arch
        parts.append(add_curved_strand(
            f"Brow_{sx}",
            [(ex - sx * 0.036, -0.235, ez + 0.068), (ex, -0.242, ez + 0.076), (ex + sx * 0.034, -0.228, ez + 0.066)],
            [0.007, 0.009, 0.007], hair_mat, col, segs=6
        ))
    if not separate_eyes:
        parts.extend(eye_l_parts)
        parts.extend(eye_r_parts)

    # Smooth sculpted hair cap (NOT lumpy spheres!) — sits cleanly around top/back of head
    hair_cap_r = 0.296
    parts.append(add_lathe(
        "HairCap",
        [
            (0.285, -0.05), (0.302, 0.04), (0.292, 0.14), (0.235, 0.235),
            (0.140, 0.290), (0.0, 0.308),
        ],
        hair_mat, col, loc=(0, 0.030, z_center + 0.018), scale=(1.04, 1.00, 0.98), segs=24, cap_bot=True
    ))
    # Back hair curtain cradling the nape seamlessly
    parts.append(add_sphere("HairBack", 0.278, hair_mat, col, loc=(0, 0.085, z_center - 0.02), scale=(1.02, 0.86, 0.94), u=20, v=14))

    # Style-specific sculpted bangs & silhouette (smooth curved locks!)
    if hair_style == "ponytail":
        # Sweeping side-parted bangs
        for pts, rads in [
            ([(-0.04, -0.15, z_center + 0.26), (-0.13, -0.25, z_center + 0.16), (-0.21, -0.22, z_center + 0.02), (-0.24, -0.12, z_center - 0.10)], [0.058, 0.055, 0.044, 0.022]),
            ([(0.02, -0.16, z_center + 0.26), (0.10, -0.26, z_center + 0.16), (0.19, -0.23, z_center + 0.04), (0.23, -0.13, z_center - 0.08)], [0.056, 0.054, 0.042, 0.022]),
            ([(-0.08, -0.18, z_center + 0.24), (0.00, -0.265, z_center + 0.155), (0.07, -0.255, z_center + 0.09)], [0.042, 0.046, 0.024]),
        ]:
            parts.append(add_curved_strand("Bang", pts, rads, hair_mat, col, segs=10))
        # Cute high bouncy ponytail + ribbon bow
        parts.append(add_curved_strand(
            "Ponytail",
            [(0.0, 0.26, z_center + 0.10), (0.0, 0.37, z_center + 0.04), (0.0, 0.42, z_center - 0.12), (0.0, 0.36, z_center - 0.26)],
            [0.062, 0.105, 0.095, 0.032], hair_mat, col, segs=14
        ))
        parts.append(add_sphere("TieCenter", 0.044, accent_mat, col, loc=(0, 0.27, z_center + 0.11)))
        for sx in (-1, 1):
            parts.append(add_sphere(f"Bow_{sx}", 0.058, accent_mat, col, loc=(sx * 0.072, 0.285, z_center + 0.125), scale=(1.2, 0.55, 0.80), rot=(0, -sx * 0.25, 0)))

    elif hair_style == "short_boy":
        # Clean tousled coastal short hair bangs
        for sx, dx in ((-1, -0.15), (0, -0.02), (1, 0.13)):
            parts.append(add_curved_strand(
                f"BoyBang_{sx}",
                [(dx * 0.6, -0.14, z_center + 0.26), (dx, -0.255, z_center + 0.16), (dx * 1.15, -0.245, z_center + 0.085)],
                [0.052, 0.050, 0.022], hair_mat, col, segs=8
            ))
        for sx in (-1, 1):
            parts.append(add_curved_strand(
                f"Sideburn_{sx}",
                [(sx * 0.24, -0.05, z_center + 0.14), (sx * 0.26, -0.06, z_center + 0.02)],
                [0.045, 0.026], hair_mat, col, segs=8
            ))

    elif hair_style == "bob":
        # Sleek shoulder-length Animal Crossing bob (ZERO lumpy meatballs!)
        for sx in (-1, 1):
            parts.append(add_curved_strand(
                f"BobSide_{sx}",
                [(sx * 0.18, -0.06, z_center + 0.22), (sx * 0.285, -0.07, z_center + 0.04), (sx * 0.275, -0.05, z_center - 0.16), (sx * 0.20, -0.02, z_center - 0.24)],
                [0.075, 0.088, 0.082, 0.045], hair_mat, col, segs=12
            ))
            parts.append(add_curved_strand(
                f"BobBackSide_{sx}",
                [(sx * 0.15, 0.12, z_center + 0.16), (sx * 0.22, 0.15, z_center - 0.04), (sx * 0.18, 0.14, z_center - 0.22)],
                [0.085, 0.095, 0.060], hair_mat, col, segs=12
            ))
        # Soft parted bangs
        for sx in (-1, 1):
            parts.append(add_curved_strand(
                f"BobBang_{sx}",
                [(0.0, -0.14, z_center + 0.26), (sx * 0.11, -0.255, z_center + 0.16), (sx * 0.21, -0.215, z_center + 0.05)],
                [0.055, 0.052, 0.026], hair_mat, col, segs=10
            ))
        # Cute slender headband
        parts.append(add_lathe(
            "Headband",
            [(0.285, -0.02), (0.312, 0.0), (0.285, 0.02)],
            accent_mat, col, loc=(0, -0.02, z_center + 0.12), scale=(1.01, 0.96, 1.0), rot=(0.45, 0, 0), segs=24
        ))

    elif hair_style == "bun":
        # Sleek updo + high tidy top bun + cute hair tie
        for sx in (-1, 1):
            parts.append(add_curved_strand(
                f"BunBang_{sx}",
                [(0.0, -0.15, z_center + 0.26), (sx * 0.11, -0.255, z_center + 0.16), (sx * 0.20, -0.22, z_center + 0.06)],
                [0.052, 0.048, 0.024], hair_mat, col, segs=10
            ))
        parts.append(add_sphere("TopBun", 0.135, hair_mat, col, loc=(0, 0.11, z_center + 0.30), scale=(1.05, 1.0, 0.88), u=18, v=12))
        parts.append(add_lathe(
            "BunTie", [(0.115, -0.025), (0.140, 0.0), (0.115, 0.025)],
            accent_mat, col, loc=(0, 0.095, z_center + 0.245), rot=(0.25, 0, 0), segs=18
        ))

    elif hair_style == "long_wave":
        # Long flowing wavy coastal locks (for Barbie / Florist / Marina)
        for sx in (-1, 1):
            parts.append(add_curved_strand(
                f"LongSide_{sx}",
                [(sx * 0.19, -0.04, z_center + 0.20), (sx * 0.29, -0.04, z_center + 0.02), (sx * 0.28, 0.04, z_center - 0.18), (sx * 0.23, 0.08, z_center - 0.36)],
                [0.072, 0.085, 0.078, 0.036], hair_mat, col, segs=12
            ))
            parts.append(add_curved_strand(
                f"LongBang_{sx}",
                [(0.0, -0.14, z_center + 0.26), (sx * 0.11, -0.255, z_center + 0.16), (sx * 0.22, -0.205, z_center + 0.04)],
                [0.052, 0.050, 0.025], hair_mat, col, segs=10
            ))
        parts.append(add_curved_strand(
            "LongBackC",
            [(0.0, 0.16, z_center + 0.12), (0.0, 0.24, z_center - 0.08), (0.0, 0.22, z_center - 0.34)],
            [0.145, 0.155, 0.085], hair_mat, col, segs=14
        ))

    # Optional cute hats (cleanly fitted!)
    if hat_style == "cap":
        parts.append(add_lathe(
            "CapDome",
            [(0.288, 0.0), (0.292, 0.07), (0.245, 0.16), (0.14, 0.215), (0.0, 0.228)],
            accent_mat, col, loc=(0, 0.01, z_center + 0.125), scale=(1.02, 1.02, 0.92), rot=(-0.10, 0, 0), segs=22
        ))
        parts.append(add_sphere("CapBrim", 0.22, accent_mat, col, loc=(0, -0.23, z_center + 0.145), scale=(1.08, 0.85, 0.14), rot=(0.12, 0, 0)))
        parts.append(add_sphere("CapBtn", 0.026, MATS["AC_Cream"], col, loc=(0, 0.02, z_center + 0.335)))
    elif hat_style == "straw":
        parts.append(add_lathe(
            "StrawBrim",
            [(0.22, 0.02), (0.36, 0.01), (0.44, -0.02), (0.45, -0.038), (0.34, -0.02), (0.22, 0.0)],
            MATS["AC_StrawHat"], col, loc=(0, 0.02, z_center + 0.19), rot=(-0.10, 0, 0), segs=28
        ))
        parts.append(add_lathe(
            "StrawCrown",
            [(0.24, 0.0), (0.235, 0.08), (0.205, 0.155), (0.12, 0.185), (0.0, 0.190)],
            MATS["AC_StrawHat"], col, loc=(0, 0.02, z_center + 0.195), rot=(-0.10, 0, 0), segs=24
        ))
        parts.append(add_lathe(
            "StrawRibbon",
            [(0.248, 0.0), (0.248, 0.048)],
            accent_mat, col, loc=(0, 0.02, z_center + 0.205), rot=(-0.10, 0, 0), segs=24, cap_bot=False, cap_top=False
        ))

    return parts, eye_l_parts, eye_r_parts


def build_chibi_character(
    fbx_name, col,
    skin_key, hair_key, hair_style,
    top_key, bottom_key, shoe_key, accent_key,
    is_dress=True, hat_style=None, is_protag=False, is_single_mesh=False
):
    skin = MATS[skin_key]
    hair = MATS[hair_key]
    top = MATS[top_key]
    bot = MATS[bottom_key]
    shoe = MATS[shoe_key]
    accent = MATS[accent_key]
    cream = MATS["AC_Cream"]

    # 1. TORSO (Soft cute A-line dress or cozy layered coastal tunic — ZERO floating white boxes!)
    torso_parts = []
    # Smooth neck connecting cleanly into collar
    torso_parts.append(add_lathe("Neck", [(0.072, 0.96), (0.068, 1.06)], skin, col, segs=14))
    if is_dress:
        # Soft bell-shaped Animal Crossing sundress (z = 0.44 .. 0.99)
        torso_parts.append(add_lathe(
            "Dress",
            [(0.0, 0.44), (0.235, 0.44), (0.248, 0.48), (0.215, 0.62), (0.182, 0.78), (0.188, 0.92), (0.145, 0.99), (0.0, 1.00)],
            top, col, scale=(1.04, 0.90, 1.0), segs=22
        ))
        # Clean hem trim + waist sash
        torso_parts.append(add_lathe("Sash", [(0.194, 0.71), (0.198, 0.765), (0.190, 0.775)], accent, col, scale=(1.04, 0.91, 1.0), segs=22))
        torso_parts.append(add_lathe("HemTrim", [(0.244, 0.445), (0.253, 0.475)], cream, col, scale=(1.04, 0.90, 1.0), segs=22))
        # Soft Peter-Pan rounded collar
        for sx in (-1, 1):
            torso_parts.append(add_sphere(f"Collar_{sx}", 0.068, cream, col, loc=(sx * 0.065, -0.095, 0.965), scale=(1.15, 0.85, 0.32), rot=(0.25, 0, sx * 0.25)))
    else:
        # Cozy coastal summer shirt + tailored shorts
        torso_parts.append(add_lathe(
            "Shorts",
            [(0.0, 0.44), (0.192, 0.45), (0.196, 0.59), (0.180, 0.66)],
            bot, col, scale=(1.04, 0.90, 1.0), segs=20
        ))
        torso_parts.append(add_lathe(
            "Shirt",
            [(0.196, 0.60), (0.195, 0.76), (0.198, 0.92), (0.150, 0.99), (0.0, 1.00)],
            top, col, scale=(1.04, 0.90, 1.0), segs=22
        ))
        torso_parts.append(add_lathe("CollarRing", [(0.135, 0.955), (0.148, 0.985), (0.115, 1.005)], cream, col, scale=(1.04, 0.92, 1.0), segs=20))
        torso_parts.append(add_lathe("Belt", [(0.198, 0.605), (0.200, 0.640)], accent, col, scale=(1.04, 0.90, 1.0), segs=20))

    # 2. HEAD
    head_parts, eye_l_parts, eye_r_parts = build_chibi_head_parts(
        col, skin, hair, hair_style, accent, hat_style=hat_style, z_center=1.24, separate_eyes=is_protag
    )

    # 3. NATURAL SOFTLY TAPERED ARMS WITH PUFF SLEEVES & ROUNDED MITTEN HANDS
    arm_l_parts, arm_r_parts = [], []
    for sx, bucket in ((-1, arm_l_parts), (1, arm_r_parts)):
        ax = sx * 0.215
        # Soft puff sleeve at shoulder
        bucket.append(add_sphere(f"Sleeve_{sx}", 0.080, top, col, loc=(ax, 0.0, 0.91), scale=(0.95, 0.92, 1.15)))
        # Soft gently angled forearm + cute rounded hand
        bucket.append(add_curved_strand(
            f"ArmLimb_{sx}",
            [(ax, 0.0, 0.89), (ax + sx * 0.025, -0.015, 0.74), (ax + sx * 0.040, -0.025, 0.61)],
            [0.052, 0.044, 0.038], skin, col, segs=12
        ))
        bucket.append(add_sphere(f"Hand_{sx}", 0.046, skin, col, loc=(ax + sx * 0.042, -0.028, 0.585), scale=(0.95, 1.05, 0.95)))

    # 4. NATURAL SOFTLY TAPERED LEGS + SOCKS + CUTE ROUNDED SNEAKERS
    leg_l_parts, leg_r_parts = [], []
    leg_mat = skin if is_dress else bot
    for sx, bucket in ((-1, leg_l_parts), (1, leg_r_parts)):
        lx = sx * 0.095
        bucket.append(add_lathe(
            f"Leg_{sx}",
            [(0.048, 0.14), (0.054, 0.30), (0.064, 0.48)],
            leg_mat, col, loc=(lx, 0.0, 0.0), segs=14
        ))
        # Cute little white sock cuff
        bucket.append(add_lathe(f"Sock_{sx}", [(0.052, 0.09), (0.054, 0.175)], cream, col, loc=(lx, 0.0, 0.0), segs=14))
        # Cute rounded Animal Crossing sneaker/Mary-Jane (bottom cleanly at z = 0.0!)
        bucket.append(add_sphere(f"Shoe_{sx}", 0.068, shoe, col, loc=(lx, -0.035, 0.058), scale=(0.96, 1.48, 0.86), u=16, v=10))
        bucket.append(add_sphere(f"Sole_{sx}", 0.067, cream, col, loc=(lx, -0.035, 0.018), scale=(0.98, 1.50, 0.26), u=16, v=8))

    # Assemble & export with exact Unity bone/joint names & pivots!
    if is_protag:
        o_torso = join_with_pivot(torso_parts, "Protag_Torso", (0.0, 0.0, 0.61), col)
        o_head = join_with_pivot(head_parts, "Protag_Head", (0.0, 0.0, 1.02), col)
        o_eyel = join_with_pivot(eye_l_parts, "Protag_EyeL", (-0.096, -0.236, 1.25), col)
        o_eyer = join_with_pivot(eye_r_parts, "Protag_EyeR", (0.096, -0.236, 1.25), col)
        o_arml = join_with_pivot(arm_l_parts, "Protag_ArmL", (-0.215, 0.0, 0.94), col)
        o_armr = join_with_pivot(arm_r_parts, "Protag_ArmR", (0.215, 0.0, 0.94), col)
        o_legl = join_with_pivot(leg_l_parts, "Protag_LegL", (-0.095, 0.0, 0.48), col)
        o_legr = join_with_pivot(leg_r_parts, "Protag_LegR", (0.095, 0.0, 0.48), col)
        export_fbx([o_torso, o_head, o_eyel, o_eyer, o_arml, o_armr, o_legl, o_legr], fbx_name)
    elif is_single_mesh:
        all_p = torso_parts + head_parts + arm_l_parts + arm_r_parts + leg_l_parts + leg_r_parts
        o_all = join_with_pivot(all_p, fbx_name, (0.0, 0.0, 0.0), col)
        export_fbx([o_all], fbx_name)
    else:
        o_torso = join_with_pivot(torso_parts + head_parts, "Torso", (0.0, 0.0, 0.78), col)
        o_arml = join_with_pivot(arm_l_parts, "ArmL", (-0.215, 0.0, 0.94), col)
        o_armr = join_with_pivot(arm_r_parts, "ArmR", (0.215, 0.0, 0.94), col)
        o_legl = join_with_pivot(leg_l_parts, "LegL", (-0.095, 0.0, 0.48), col)
        o_legr = join_with_pivot(leg_r_parts, "LegR", (0.095, 0.0, 0.48), col)
        export_fbx([o_torso, o_arml, o_armr, o_legl, o_legr], fbx_name)


def build_cute_fairy(col):
    """Adorable miniature Fairy Waitress with delicate slender butterfly/fairy wings (NOT giant moth ears!)."""
    body_parts = []
    # Delicate flared petal dress
    body_parts.append(add_lathe(
        "FairyDress",
        [(0.0, 0.34), (0.22, 0.36), (0.25, 0.44), (0.18, 0.62), (0.155, 0.78), (0.125, 0.86), (0.0, 0.88)],
        MATS["AC_PinkSoft"], col, scale=(1.02, 0.92, 1.0), segs=20
    ))
    body_parts.append(add_lathe("FairySash", [(0.165, 0.64), (0.172, 0.69)], MATS["AC_Gold"], col, segs=20))
    body_parts.append(add_lathe("FairyNeck", [(0.058, 0.84), (0.055, 0.92)], MATS["AC_SkinWarm"], col, segs=12))
    # Petite tucked legs & fairy slippers
    for sx in (-1, 1):
        body_parts.append(add_curved_strand(
            f"FLeg_{sx}", [(sx * 0.065, 0.0, 0.38), (sx * 0.072, 0.04, 0.16)], [0.038, 0.030], MATS["AC_SkinWarm"], col, segs=10
        ))
        body_parts.append(add_sphere(f"FShoe_{sx}", 0.042, MATS["AC_PinkBerry"], col, loc=(sx * 0.072, 0.02, 0.13), scale=(0.9, 1.35, 0.75)))
        body_parts.append(add_curved_strand(
            f"FArm_{sx}", [(sx * 0.165, 0.0, 0.80), (sx * 0.21, -0.05, 0.66), (sx * 0.19, -0.09, 0.56)], [0.042, 0.034, 0.030], MATS["AC_SkinWarm"], col, segs=10
        ))
    head_p, _, _ = build_chibi_head_parts(col, MATS["AC_SkinWarm"], MATS["AC_HairPlum"], "bob", MATS["AC_Gold"], z_center=1.08, separate_eyes=False)
    body_parts.extend(head_p)
    # Tiny golden star hair clip
    body_parts.append(add_sphere("StarClip", 0.038, MATS["AC_LanternGlow"], col, loc=(0.15, -0.14, 1.24), scale=(1.0, 0.55, 1.0)))

    # Delicate, graceful slender fairy wings (0.36m each — proportional & charming!)
    wing_l_parts, wing_r_parts = [], []
    for sx, bucket in ((-1, wing_l_parts), (1, wing_r_parts)):
        # Upper teardrop wing lobe
        bucket.append(add_sphere(
            f"WingUp_{sx}", 0.14, MATS["AC_MintSoft"], col,
            loc=(sx * 0.21, 0.17, 0.88), scale=(1.35, 0.14, 0.68), rot=(0.15, sx * 0.34, -sx * 0.22), u=16, v=10
        ))
        bucket.append(add_sphere(
            f"WingGlow_{sx}", 0.085, MATS["AC_LanternGlow"], col,
            loc=(sx * 0.22, 0.165, 0.88), scale=(1.15, 0.18, 0.55), rot=(0.15, sx * 0.34, -sx * 0.22), u=12, v=8
        ))
        # Lower petite wing lobe
        bucket.append(add_sphere(
            f"WingLow_{sx}", 0.095, MATS["AC_Lavender"], col,
            loc=(sx * 0.15, 0.17, 0.72), scale=(1.15, 0.14, 0.62), rot=(0.10, -sx * 0.25, -sx * 0.18), u=14, v=8
        ))

    o_body = join_with_pivot(body_parts, "Fairy_Body", (0.0, 0.0, 0.0), col)
    o_wl = join_with_pivot(wing_l_parts, "Fairy_WingL", (-0.06, 0.15, 0.82), col)
    o_wr = join_with_pivot(wing_r_parts, "Fairy_WingR", (0.06, 0.15, 0.82), col)
    export_fbx([o_body, o_wl, o_wr], "Char_FairyWaitress")


def build_cute_pets(col):
    """Adorable rounded Animal Crossing Pipoca puppy & Mingau kitty preserving all exact pivot names."""
    # --- PET DOG (Pipoca) ---
    body = [
        add_sphere("DogTorso", 0.20, MATS["AC_Cream"], col, loc=(0, 0.02, 0.27), scale=(0.94, 1.42, 0.86), u=20, v=14),
        add_sphere("DogPatch", 0.12, MATS["AC_WoodDark"], col, loc=(0.07, 0.10, 0.33), scale=(0.85, 1.05, 0.65)),
        add_lathe("DogCollar", [(0.135, -0.025), (0.145, 0.0), (0.135, 0.025)], MATS["AC_ProtagShirt"], col, loc=(0, -0.17, 0.34), rot=(1.20, 0, 0), segs=18),
        add_sphere("DogBell", 0.032, MATS["AC_Gold"], col, loc=(0, -0.27, 0.25)),
    ]
    head = [
        add_sphere("DogHead", 0.195, MATS["AC_Cream"], col, loc=(0, -0.27, 0.45), scale=(1.04, 1.02, 0.96), u=20, v=14),
        add_sphere("DogSnout", 0.095, MATS["AC_Cream"], col, loc=(0, -0.43, 0.41), scale=(1.08, 0.92, 0.76)),
        add_sphere("DogNose", 0.036, MATS["AC_EyeDark"], col, loc=(0, -0.505, 0.43), scale=(1.15, 0.78, 0.85)),
        add_sphere("DogEyePatch", 0.088, MATS["AC_WoodDark"], col, loc=(0.085, -0.36, 0.47), scale=(0.95, 0.68, 0.95)),
        add_sphere("DogTongue", 0.026, MATS["AC_Coral"], col, loc=(0, -0.45, 0.35), scale=(0.9, 0.55, 1.0)),
    ]
    for sx in (-1, 1):
        head.append(add_sphere(f"DEye_{sx}", 0.030, MATS["AC_EyeDark"], col, loc=(sx * 0.082, -0.435, 0.475), scale=(0.9, 0.6, 1.05)))
        head.append(add_sphere(f"DShine_{sx}", 0.011, MATS["AC_EyeShine"], col, loc=(sx * 0.075, -0.452, 0.486)))
    ear_l = [add_curved_strand("DEarL", [(-0.16, -0.26, 0.53), (-0.22, -0.26, 0.43), (-0.21, -0.27, 0.33)], [0.058, 0.068, 0.048], MATS["AC_WoodDark"], col, segs=10)]
    ear_r = [add_curved_strand("DEarR", [(0.16, -0.26, 0.53), (0.22, -0.26, 0.43), (0.21, -0.27, 0.33)], [0.058, 0.068, 0.048], MATS["AC_WoodDark"], col, segs=10)]
    tail = [add_curved_strand("DTail", [(0, 0.25, 0.34), (0, 0.35, 0.42), (0, 0.37, 0.52)], [0.045, 0.052, 0.034], MATS["AC_WoodDark"], col, segs=10)]
    legs = {}
    for ln, lx, ly in [("Pipoca_LegFL", -0.11, -0.14), ("Pipoca_LegFR", 0.11, -0.14), ("Pipoca_LegBL", -0.11, 0.15), ("Pipoca_LegBR", 0.11, 0.15)]:
        lp = [
            add_lathe(ln + "_S", [(0.044, 0.04), (0.052, 0.19)], MATS["AC_Cream"], col, loc=(lx, ly, 0.0), segs=12),
            add_sphere(ln + "_P", 0.050, MATS["AC_WoodWarm"], col, loc=(lx, ly - 0.015, 0.038), scale=(1.0, 1.25, 0.72)),
        ]
        legs[ln] = join_with_pivot(lp, ln, (lx, ly, 0.20), col)
    o_b = join_with_pivot(body, "Pipoca_Body", (0, 0, 0.27), col)
    o_h = join_with_pivot(head, "Pipoca_Head", (0, -0.22, 0.39), col)
    o_el = join_with_pivot(ear_l, "Pipoca_EarL", (-0.16, -0.26, 0.53), col)
    o_er = join_with_pivot(ear_r, "Pipoca_EarR", (0.16, -0.26, 0.53), col)
    o_t = join_with_pivot(tail, "Pipoca_Tail", (0, 0.25, 0.34), col)
    export_fbx([o_b, o_h, o_el, o_er, o_t, legs["Pipoca_LegFL"], legs["Pipoca_LegFR"], legs["Pipoca_LegBL"], legs["Pipoca_LegBR"]], "PetDog")

    # --- PET CAT (Mingau) ---
    clear_scene()
    init_mats()
    col = bpy.context.scene.collection
    c_body = [
        add_sphere("CatTorso", 0.175, MATS["AC_Cream"], col, loc=(0, 0.02, 0.25), scale=(0.90, 1.38, 0.84), u=20, v=14),
        add_lathe("CatCollar", [(0.118, -0.02), (0.128, 0.0), (0.118, 0.02)], MATS["AC_MintTeal"], col, loc=(0, -0.15, 0.31), rot=(1.2, 0, 0), segs=16),
        add_sphere("CatBell", 0.028, MATS["AC_Gold"], col, loc=(0, -0.23, 0.23)),
    ]
    c_head = [
        add_sphere("CatHead", 0.178, MATS["AC_Cream"], col, loc=(0, -0.24, 0.43), scale=(1.08, 0.96, 0.94), u=20, v=14),
        add_sphere("CatNose", 0.024, MATS["AC_PinkBerry"], col, loc=(0, -0.405, 0.41), scale=(1.1, 0.6, 0.8)),
    ]
    for sx in (-1, 1):
        c_head.append(add_lathe(f"CEar_{sx}", [(0.062, 0.0), (0.042, 0.07), (0.0, 0.135)], MATS["AC_ShoeDark"], col, loc=(sx * 0.105, -0.22, 0.545), rot=(0.1, sx * 0.28, 0), segs=12))
        c_head.append(add_sphere(f"CEye_{sx}", 0.030, MATS["AC_EyeDark"], col, loc=(sx * 0.072, -0.392, 0.448), scale=(0.92, 0.55, 1.1)))
        c_head.append(add_sphere(f"CShine_{sx}", 0.011, MATS["AC_EyeShine"], col, loc=(sx * 0.066, -0.406, 0.458)))
    c_tail = [add_curved_strand("CTail", [(0, 0.22, 0.30), (0, 0.32, 0.42), (0, 0.28, 0.55)], [0.036, 0.034, 0.026], MATS["AC_ShoeDark"], col, segs=10)]
    c_legs = {}
    for ln, lx, ly in [("Cat_LegFL", -0.09, -0.13), ("Cat_LegFR", 0.09, -0.13), ("Cat_LegBL", -0.09, 0.14), ("Cat_LegBR", 0.09, 0.14)]:
        lp = [
            add_lathe(ln + "_S", [(0.036, 0.03), (0.044, 0.18)], MATS["AC_Cream"], col, loc=(lx, ly, 0.0), segs=12),
            add_sphere(ln + "_P", 0.042, MATS["AC_ShoeDark"], col, loc=(lx, ly - 0.012, 0.032), scale=(0.95, 1.2, 0.72)),
        ]
        c_legs[ln] = join_with_pivot(lp, ln, (lx, ly, 0.19), col)
    oc_b = join_with_pivot(c_body, "Cat_Body", (0, 0, 0.25), col)
    oc_h = join_with_pivot(c_head, "Cat_Head", (0, -0.20, 0.38), col)
    oc_t = join_with_pivot(c_tail, "Cat_Tail", (0, 0.22, 0.30), col)
    export_fbx([oc_b, oc_h, oc_t, c_legs["Cat_LegFL"], c_legs["Cat_LegFR"], c_legs["Cat_LegBL"], c_legs["Cat_LegBR"]], "PetCat")


# =========================================================================
# 2. BEAUTIFIED ENVIRONMENT BASE (Vibrant Zoning + Open Shoreline + Zero Void)
# =========================================================================
def build_subdivided_strip(name, x0, x1, y0, y1, z_top, thickness, segs_x, segs_y, mat, col):
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
            v0, v1, b0, b1 = top_grid[ix][iy_edge], top_grid[ix + 1][iy_edge], bot_row[ix], bot_row[ix + 1]
            bm.faces.new((v1, v0, b0, b1) if flip else (v0, v1, b1, b0))
    return new_mesh_obj(name, bm, mat, col, smooth=False)


def build_framed_patch(name, x0, x1, y0, y1, z_top, mat, border_mat, col, border_w=0.22):
    """Creates a clean framed tropical lawn or cobble patio pad slightly proud of the plaza (zero Z-fighting!)."""
    out = []
    sx = max(4, int((x1 - x0) / 2.2))
    sy = max(4, int((y1 - y0) / 2.2))
    out.append(build_subdivided_strip(name + "_Fill", x0 + border_w, x1 - border_w, y0 + border_w, y1 - border_w, z_top, 0.05, sx, sy, mat, col))
    # Subtle low stone edging around the patch
    out.append(build_subdivided_strip(name + "_N", x0, x1, y1 - border_w, y1, z_top + 0.010, 0.06, sx, 1, border_mat, col))
    out.append(build_subdivided_strip(name + "_S", x0, x1, y0, y0 + border_w, z_top + 0.010, 0.06, sx, 1, border_mat, col))
    out.append(build_subdivided_strip(name + "_W", x0, x0 + border_w, y0 + border_w, y1 - border_w, z_top + 0.010, 0.06, 1, sy, border_mat, col))
    out.append(build_subdivided_strip(name + "_E", x1 - border_w, x1, y0 + border_w, y1 - border_w, z_top + 0.010, 0.06, 1, sy, border_mat, col))
    return out


def build_beautified_environment(col):
    parts = []
    x0, x1 = -120.0, 144.0
    segs_x = 96

    # 1. Crisp Light-Stone Promenade (z = -3.75 .. +3.50, y = 0.00)
    parts.append(build_subdivided_strip("Surf_Calcadao", x0, x1, -3.75, 3.50, 0.0, 0.22, segs_x, 4, MATS["AC_StoneWhite"], col))

    # Subtle south promenade border lip with wide open passages
    for bname, bx0, bx1 in [
        ("Surf_BordaPraca_FarW", -120.0, -11.5),
        ("Surf_BordaPraca_RestW", -10.0, -5.2),
        ("Surf_BordaPraca_East",   2.8,  144.0),
    ]:
        parts.append(build_subdivided_strip(bname, bx0, bx1, -3.86, -3.72, 0.012, 0.07, max(4, int((bx1 - bx0) / 3.0)), 1, MATS["AC_StoneWarm"], col))

    # 2. Warm Plaza Base (z = -24.2 .. -3.75, y = -0.03)
    parts.append(build_subdivided_strip("Surf_PracaContinua", x0, x1, -24.2, -3.75, -0.03, 0.22, segs_x, 8, MATS["AC_StoneWarm"], col))

    # 3. SOUTH COASTAL VILLAGE BACKDROP (z = -88.0 .. -24.2) -> ZERO blue sky void behind houses ever!
    parts.append(build_subdivided_strip("Surf_RuaVilaSul", x0, x1, -33.5, -24.2, -0.04, 0.22, segs_x, 4, MATS["AC_CobbleA"], col))
    parts.append(build_subdivided_strip("Surf_GramadoSul", x0, x1, -88.0, -33.5, -0.05, 0.22, segs_x, 12, MATS["AC_Grass"], col))

    # 4. VIBRANT GREEN PARK LAWNS & COBBLE COURTYARDS (breaking the monochrome beige!)
    # Horta & Pomar lush green garden lawn
    parts.extend(build_framed_patch("Lawn_Horta", -11.2, 1.0, -23.0, -12.8, -0.008, MATS["AC_Grass"], MATS["AC_StoneWhite"], col))
    # Pavilion lush park lawn (east)
    parts.extend(build_framed_patch("Lawn_Pavilhao", 27.6, 37.8, -18.6, -7.8, -0.008, MATS["AC_Grass"], MATS["AC_StoneWhite"], col))
    # West coastal park lawn pocket behind rest area
    parts.extend(build_framed_patch("Lawn_Oeste", -18.2, -12.2, -11.2, -4.4, -0.008, MATS["AC_GrassLight"], MATS["AC_StoneWhite"], col))
    # Café das Fadas warm cobblestone patio
    parts.extend(build_framed_patch("Patio_Cafe", 2.8, 10.2, -17.6, -5.6, -0.012, MATS["AC_CobbleA"], MATS["AC_StoneWhite"], col))
    # Feira Central warm cobblestone fairground courtyard
    parts.extend(build_framed_patch("Patio_Feira", 10.8, 27.0, -16.6, -10.8, -0.012, MATS["AC_CobbleA"], MATS["AC_StoneWhite"], col))
    # Lush coastal park lawns flanking far West & far East edges (replacing endless empty beige!)
    parts.append(build_subdivided_strip("ParkFlank_West", x0, -18.6, -24.2, 3.35, -0.006, 0.16, 20, 8, MATS["AC_Grass"], col))
    parts.append(build_subdivided_strip("ParkFlank_East", 38.4, x1, -24.2, 3.35, -0.006, 0.16, 20, 8, MATS["AC_Grass"], col))

    # 5. Low coastal mureta segments flanking the 3 wide beach accesses
    for mname, mx0, mx1 in [
        ("Mureta_FarWest", -116.0, -7.2),
        ("Mureta_MidWest",   -3.3,  5.3),
        ("Mureta_MidEast",    9.7, 17.8),
        ("Mureta_FarEast",   22.2, 142.0),
    ]:
        ms = max(4, int((mx1 - mx0) / 3.0))
        parts.append(build_subdivided_strip(mname, mx0, mx1, 3.40, 3.68, 0.13, 0.19, ms, 1, MATS["AC_StoneWhite"], col))
        parts.append(build_subdivided_strip(mname + "_Cap", mx0 - 0.02, mx1 + 0.02, 3.37, 3.71, 0.16, 0.04, ms, 1, MATS["AC_StoneWarm"], col))

    # 6. WARM GOLDEN SAND BEACH sloping cleanly & steeply BELOW the ocean plane (y=-0.18) right at z ~ 8.15m!
    # Ends cleanly at z = 14.0 (at y = -1.85m) so ZERO sand geometry ever hides or clips the turquoise ocean!
    bm = bmesh.new()
    grid = []
    for ix in range(segs_x + 1):
        u = ix / segs_x
        x = x0 + (x1 - x0) * u
        coast_curve = math.sin(x * 0.045) * 0.65 + math.cos(x * 0.022) * 0.30
        ys = [
            3.35,
            5.60 + coast_curve * 0.30,
            7.85 + coast_curve * 0.72,
            8.75 + coast_curve * 0.80,
            10.80 + coast_curve * 0.85,
            14.20,
        ]
        zs = [-0.03, -0.10, -0.18, -0.48, -1.15, -1.85]
        grid.append([bm.verts.new((x, ys[r], zs[r])) for r in range(len(ys))])
    for ix in range(segs_x):
        for r in range(len(grid[0]) - 1):
            bm.faces.new((grid[ix][r], grid[ix + 1][r], grid[ix + 1][r + 1], grid[ix][r + 1]))
    parts.append(new_mesh_obj("Surf_AreiaCreme", bm, MATS["AC_Sand"], col, smooth=True))

    # Subtle coastal shells & starfish on dry sand
    for idx, (sx, sy) in enumerate([(-3.6, 5.4), (4.2, 6.0), (13.4, 5.8), (20.8, 6.2)]):
        parts.append(add_sphere(f"Starfish_{idx}", 0.14, MATS["AC_Starfish"], col, loc=(sx, sy, -0.08), scale=(1.1, 1.1, 0.24), u=10, v=6))
        parts.append(add_sphere(f"Shell_{idx}", 0.10, MATS["AC_Shell"], col, loc=(sx + 0.55, sy + 0.32, -0.09), scale=(1.2, 0.9, 0.42), u=8, v=6))

    env_obj = join_with_pivot(parts, "OrlaEnvironmentBase", (0.0, 0.0, 0.0), col)
    export_fbx([env_obj], "OrlaEnvironmentBase")


# =========================================================================
# 3. CLEAN FLAT-FACED ROUTE SIGNPOST (Zero text clipping!)
# =========================================================================
def build_clean_route_signpost(col):
    parts = [
        # Rounded wooden base & vertical post (centered at x=0, y=0)
        add_lathe("SignBase", [(0.16, 0.0), (0.14, 0.12), (0.08, 0.16)], MATS["AC_WoodDark"], col, segs=14),
        add_lathe("SignPost", [(0.068, 0.08), (0.064, 1.82), (0.0, 1.88)], MATS["AC_WoodWarm"], col, segs=14),
        # Upper plank (Coral — flat front face at Y = -0.036 so 3D text at Z=-0.065 sits cleanly in front!)
        add_rounded_box("BoardTop", (1.28, 0.065, 0.34), MATS["AC_Coral"], col, loc=(0.14, 0.0, 1.54), bevel=0.025, segs=2),
        # Lower plank (MintTeal — flat front face at Y = -0.036!)
        add_rounded_box("BoardBot", (1.28, 0.065, 0.34), MATS["AC_MintTeal"], col, loc=(0.14, 0.0, 1.12), bevel=0.025, segs=2),
    ]
    sp = join_with_pivot(parts, "RouteSignpost", (0.0, 0.0, 0.0), col)
    export_fbx([sp], "RouteSignpost")


def main():
    # 1. Protagonist + All Visible Villagers & Tourists (each exported cleanly in an isolated scene!)
    char_specs = [
        # (fbx_name, skin, hair, style, top, bottom, shoe, accent, is_dress, hat, is_protag, is_single)
        ("Char_Protagonist",     "AC_SkinLight", "AC_HairBrown",       "ponytail",  "AC_ProtagShirt", "AC_Denim",     "AC_PinkBerry", "AC_MintTeal",    True,  None,    True,  False),
        ("Char_PedestrianBlue",  "AC_SkinTan",   "AC_HairDark",        "short_boy", "AC_SkyBlue",     "AC_Denim",     "AC_ShoeDark",  "AC_SunnyYellow", False, "cap",   False, False),
        ("Char_PedestrianCoral", "AC_SkinLight", "AC_HairAuburn",      "bob",       "AC_Coral",       "AC_Cream",     "AC_ShoeDark",  "AC_MintTeal",    True,  None,    False, False),
        ("Char_FriendBia",       "AC_SkinTan",   "AC_HairAuburn",      "bob",       "AC_Lavender",    "AC_PinkSoft",  "AC_Coral",     "AC_SunnyYellow", True,  None,    False, False),
        ("Char_FriendNina",      "AC_SkinWarm",  "AC_HairDark",        "bun",       "AC_MintTeal",    "AC_Denim",     "AC_ShoeDark",  "AC_Gold",        False, None,    False, False),
        ("Char_Florist",         "AC_SkinLight", "AC_HairBrown",       "long_wave", "AC_MintSoft",    "AC_Cream",     "AC_WoodDark",  "AC_PinkBerry",   True,  "straw", False, False),
        ("Char_SisterMarina",    "AC_SkinLight", "AC_HairBrown",       "long_wave", "AC_Coral",       "AC_Cream",     "AC_ShoeDark",  "AC_SunnyYellow", True,  "straw", False, False),
        ("Char_Barbie",          "AC_SkinLight", "AC_HairWind_Blonde", "long_wave", "AC_PinkSoft",    "AC_PinkBerry", "AC_PinkBerry", "AC_Cream",       True,  None,    False, True),
        ("Char_Japones",         "AC_SkinLight", "AC_HairWind_Dark",   "short_boy", "AC_SkyBlue",     "AC_Denim",     "AC_WoodDark",  "AC_Coral",       False, None,    False, True),
        ("Char_Vovo",            "AC_SkinLight", "AC_HairWind_Silver", "bun",       "AC_MintSoft",    "AC_PinkSoft",  "AC_WoodDark",  "AC_Gold",        True,  None,    False, True),
        ("Char_Camponesa",       "AC_SkinLight", "AC_HairWind_Auburn", "ponytail",  "AC_Cream",       "AC_MintTeal",  "AC_WoodDark",  "AC_Orange",      True,  "straw", False, True),
    ]
    for spec in char_specs:
        clear_scene()
        init_mats()
        build_chibi_character(*spec[:1], bpy.context.scene.collection, *spec[1:])

    # 2. Cute Miniature Fairy Waitress
    clear_scene()
    init_mats()
    build_cute_fairy(bpy.context.scene.collection)

    # 3. Cute Rounded Pets (Pipoca & Mingau)
    clear_scene()
    init_mats()
    build_cute_pets(bpy.context.scene.collection)

    # 4. Beautified Continuous Environment Base
    clear_scene()
    init_mats()
    build_beautified_environment(bpy.context.scene.collection)

    # 5. Clean Flat-Faced RouteSignpost
    clear_scene()
    init_mats()
    build_clean_route_signpost(bpy.context.scene.collection)

    print("[v40] ALL character models, pets, terrain & signpost exported successfully!")


if __name__ == "__main__":
    main()
