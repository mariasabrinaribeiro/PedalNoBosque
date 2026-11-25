"""
Orla das Fadas — Art Direction Character, Fairy & Pet Pack (Blender 5.3)
Standardizes EVERY character, friend, resident, fairy, and pet to Unity's native +Z forward axis
(Blender -Y front -> export axis_forward='Z', axis_up='Y', bake_space_transform=True -> det = +1.0,
zero flipped normals, zero inverted vectors in Unity!).

Art Direction Implemented:
1. Protagonist (Char_Protagonist.fbx):
   - Legs ~15% longer, head ~8% smaller, expressive eyes & smile preserved
   - Recognizable back hairstyle (sculpted 2-tier high ponytail + crisp turquoise ribbon bow)
   - Short crossbody satchel (visible front & back) + exclusive signature palette
2. Two Friends (Char_FriendNina.fbx & Char_FriendBia.fbx) — distinct even in pure black silhouette:
   - Nina: tall slender silhouette, high tight top-bun hair, crisp straight boxy jacket & straight pants
   - Bia: ~20% shorter compact silhouette, voluminous curly cloud hair, rounded puffy dress & sleeves
3. Residents (Char_Florist.fbx, Char_PedestrianBlue.fbx, Char_PedestrianCoral.fbx, Char_SisterMarina.fbx):
   - Modular height & width proportions + clear back-readable accessories & articulated legs/arms
   - Florist Luna: botanical apron (with X-straps & bow on back) + woven seedling basket with sprouts/flowers
4. Fairies & Pets (Char_FairyWaitress.fbx, PetDog.fbx, PetCat.fbx):
   - High-contrast silhouettes & bright warm wing trims that pop against BOTH turquoise ocean and beige pavement
   - Fully articulated joints for Pipoca (head/ears/tail/4 legs), Mingau (head/tail/4 legs), and Fairy wings
"""

import bpy
import bmesh
import math
import os
from mathutils import Vector

SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
MODELS_DIR = os.path.normpath(os.path.join(SCRIPT_DIR, "..", "Models"))
BLEND_OUT = os.path.join(SCRIPT_DIR, "OrlaDasFadas_ArtDirectionChars.blend")
os.makedirs(MODELS_DIR, exist_ok=True)


def clear_scene():
    bpy.ops.wm.read_factory_settings(use_empty=True)


def hex_rgba(h):
    h = h.lstrip("#")
    return (int(h[0:2], 16) / 255.0, int(h[2:4], 16) / 255.0, int(h[4:6], 16) / 255.0, 1.0)


MATS = {}


def make_mat(name, hx, rough=0.50):
    m = bpy.data.materials.new(name=name)
    m.use_nodes = True
    bsdf = m.node_tree.nodes.get("Principled BSDF") if m.node_tree else None
    if bsdf:
        bsdf.inputs["Base Color"].default_value = hex_rgba(hx)
        bsdf.inputs["Roughness"].default_value = rough
    MATS[name] = m
    return m


def init_mats():
    MATS.clear()
    for k, hx in [
        ("AC_ProtagShirt", "#EC6FA4"),
        ("AC_PinkBerry", "#EC6FA4"),
        ("AC_PinkSoft", "#F4AECB"),
        ("AC_MintTeal", "#2AC6CC"),
        ("AC_MintSoft", "#8EDFE2"),
        ("AC_Lavender", "#C68CE0"),
        ("AC_PurplePlum", "#3A949A"),
        ("AC_Denim", "#4694A8"),
        ("AC_SkyBlue", "#68C4D6"),
        ("AC_Coral", "#D89886"),
        ("AC_SunnyYellow", "#F4D678"),
        ("AC_Cream", "#FAF2E4"),
        ("AC_StoneWhite", "#F2E9D6"),
        ("AC_SkinLight", "#F4C29F"),
        ("AC_SkinWarm", "#C9855E"),
        ("AC_SkinTan", "#DF9F73"),
        ("AC_CheekBlush", "#F494B4"),
        ("AC_EyeDark", "#251D29"),
        ("AC_EyeShine", "#FFFFFF"),
        ("AC_HairBrown", "#6B442A"),
        ("AC_HairDark", "#332A38"),
        ("AC_HairAuburn", "#B45C42"),
        ("AC_HairPlum", "#8A4A98"),
        ("AC_HairWind_Brown", "#6B442A"),
        ("AC_HairWind_Auburn", "#B4563C"),
        ("AC_HairWind_Dark", "#2E2736"),
        ("AC_ShoeDark", "#4E3A4C"),
        ("AC_WoodLight", "#C9996B"),
        ("AC_WoodWarm", "#A6764E"),
        ("AC_WoodDark", "#744C30"),
        ("AC_StrawHat", "#EED2A4"),
        ("AC_Gold", "#E8C374"),
        ("AC_LanternGlow", "#FFE69C"),
        ("AC_BushGreen", "#629C80"),
        ("AC_BushLight", "#7EB496"),
        ("AC_FlowerPink", "#EC6FA4"),
        ("AC_FlowerYellow", "#EED286"),
        ("AC_MetalDark", "#475160"),
        ("AC_Terracotta", "#C98868"),
    ]:
        make_mat(k, hx)


def add_box(name, size, mat, col, loc=(0, 0, 0), rot=(0, 0, 0), bevel=0.02):
    mesh = bpy.data.meshes.new(name)
    bm = bmesh.new()
    bmesh.ops.create_cube(bm, size=1.0)
    bmesh.ops.scale(bm, vec=Vector(size), verts=bm.verts)
    if bevel > 0.0:
        b = min(bevel, min(size) * 0.35)
        bmesh.ops.bevel(bm, geom=list(bm.edges), offset=b, segments=2, affect="EDGES", clamp_overlap=True)
    for f in bm.faces:
        f.smooth = True
    bm.to_mesh(mesh)
    bm.free()
    obj = bpy.data.objects.new(name, mesh)
    obj.location = loc
    obj.rotation_euler = rot
    obj.data.materials.append(mat)
    col.objects.link(obj)
    return obj


def add_sphere(name, r, mat, col, loc=(0, 0, 0), scale=(1, 1, 1), rot=(0, 0, 0), u=14, v=10):
    mesh = bpy.data.meshes.new(name)
    bm = bmesh.new()
    bmesh.ops.create_uvsphere(bm, u_segments=u, v_segments=v, radius=r)
    for f in bm.faces:
        f.smooth = True
    bm.to_mesh(mesh)
    bm.free()
    obj = bpy.data.objects.new(name, mesh)
    obj.location = loc
    obj.rotation_euler = rot
    obj.scale = scale
    obj.data.materials.append(mat)
    col.objects.link(obj)
    return obj


def add_cyl(name, r, h, mat, col, loc=(0, 0, 0), rot=(0, 0, 0), segs=12, r_top=None):
    mesh = bpy.data.meshes.new(name)
    bm = bmesh.new()
    bmesh.ops.create_cone(
        bm, cap_ends=True, cap_tris=False, segments=segs,
        radius1=r, radius2=(r if r_top is None else r_top), depth=h
    )
    for f in bm.faces:
        f.smooth = True
    bm.to_mesh(mesh)
    bm.free()
    obj = bpy.data.objects.new(name, mesh)
    obj.location = loc
    obj.rotation_euler = rot
    obj.data.materials.append(mat)
    col.objects.link(obj)
    return obj


def add_smile(name, mat, col, loc, width=0.115, dip=0.023, thickness=0.0065):
    curve = bpy.data.curves.new(name, type="CURVE")
    curve.dimensions = "3D"
    curve.resolution_u = 12
    curve.bevel_depth = thickness
    curve.bevel_resolution = 3
    curve.use_fill_caps = True
    spline = curve.splines.new("BEZIER")
    spline.bezier_points.add(2)
    spline.bezier_points[0].co = (-width * 0.5, 0.0, 0.0)
    spline.bezier_points[1].co = (0.0, 0.0, -dip)
    spline.bezier_points[2].co = (width * 0.5, 0.0, 0.0)
    for point in spline.bezier_points:
        point.handle_left_type = "AUTO"
        point.handle_right_type = "AUTO"
    curve.materials.append(mat)
    obj = bpy.data.objects.new(name, curve)
    obj.location = loc
    col.objects.link(obj)
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.convert(target="MESH")
    return bpy.context.view_layer.objects.active


def join_around_pivot(parts, name, pivot_world, col):
    bpy.ops.object.select_all(action="DESELECT")
    for p in parts:
        p.select_set(True)
    bpy.context.view_layer.objects.active = parts[0]
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    if len(parts) > 1:
        bpy.ops.object.join()
    obj = bpy.context.view_layer.objects.active
    obj.name = name
    px, py, pz = pivot_world
    for v in obj.data.vertices:
        v.co.x -= px
        v.co.y -= py
        v.co.z -= pz
    obj.location = pivot_world
    return obj


def export_standardized_unity_fbx(objs, filename):
    """
    Exports flat sibling meshes with bake_space_transform=True and axis_forward='Z', axis_up='Y'.
    Blender front (-Y) maps cleanly to Unity +Z (transform.forward), Blender +Z maps to Unity +Y,
    with determinant = +1.0 (zero mirrored normals, zero -90 X tilt, zero inverted vectors!).
    """
    bpy.ops.object.select_all(action="DESELECT")
    for o in objs:
        o.select_set(True)
    bpy.context.view_layer.objects.active = objs[0]
    out_path = os.path.join(MODELS_DIR, f"{filename}.fbx")
    import time
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
                axis_forward="Z",
                axis_up="Y",
                add_leaf_bones=False,
                bake_anim=False,
            )
            break
        except RuntimeError:
            if attempt == 3:
                raise
            time.sleep(0.35)
    print(f"[FBX Standardized +Z Forward] {out_path}")


# =========================================================================
# 1. PROTAGONISTA (Discreetly longer legs + slightly taller silhouette for crisp
#    stride readability on mobile, back-readable ponytail+bow, short satchel, +Z forward!)
# =========================================================================
def build_protagonist(col):
    exported = []
    dz = 0.13  # +18% longer legs (hip z=0.54) + smaller balanced head -> feet, stride & hands at 0.94m counter read clearly!

    # Torso lifted cleanly over longer legs (hip pivot z=0.54 -> neck z=1.02)
    torso_parts = [
        # Signature Berry-Pink coastal top + Cream sailor-style collar & hem trim
        add_cyl("T_Shirt", 0.192, 0.42, MATS["AC_ProtagShirt"], col, loc=(0, 0, 0.68 + dz), r_top=0.158),
        add_sphere("T_Collar", 0.136, MATS["AC_Cream"], col, loc=(0, 0, 0.88 + dz), scale=(1.10, 1.02, 0.36)),
        add_cyl("T_ShirtHem", 0.202, 0.048, MATS["AC_Cream"], col, loc=(0, 0, 0.49 + dz), r_top=0.198),
        # Deep coastal denim shorts
        add_cyl("T_Shorts", 0.202, 0.17, MATS["AC_Denim"], col, loc=(0, 0, 0.44 + dz), r_top=0.192),
        # SHORT CROSSBODY SATCHEL (Bolsa Curta): diagonal strap visible front & back + golden pouch at hip!
        add_box("T_StrapFront", (0.045, 0.028, 0.44), MATS["AC_WoodDark"], col,
                loc=(0.02, -0.175, 0.67 + dz), rot=(0.12, math.radians(-34), 0.0), bevel=0.008),
        add_box("T_StrapBack", (0.045, 0.028, 0.44), MATS["AC_WoodDark"], col,
                loc=(0.02, 0.175, 0.67 + dz), rot=(-0.12, math.radians(-34), 0.0), bevel=0.008),
        add_box("T_StrapShoulder", (0.06, 0.24, 0.032), MATS["AC_WoodDark"], col,
                loc=(-0.11, 0.0, 0.865 + dz), rot=(0.0, 0.0, 0.0), bevel=0.008),
        add_box("T_SatchelBody", (0.125, 0.195, 0.165), MATS["AC_Gold"], col,
                loc=(0.215, 0.04, 0.48 + dz), rot=(0.0, math.radians(10), math.radians(-12)), bevel=0.025),
        add_box("T_SatchelFlap", (0.135, 0.20, 0.068), MATS["AC_Cream"], col,
                loc=(0.22, 0.04, 0.535 + dz), rot=(0.0, math.radians(10), math.radians(-12)), bevel=0.015),
        add_sphere("T_SatchelClasp", 0.030, MATS["AC_MintTeal"], col, loc=(0.28, 0.03, 0.505 + dz)),
    ]
    torso = join_around_pivot(torso_parts, "Protag_Torso", (0, 0, 0.48 + dz), col)
    exported.append(torso)

    # Head ~7% smaller (r=0.248) so silhouette balances head, torso & elongated stride in game camera!
    head_parts = [
        add_sphere("H_Skull", 0.248, MATS["AC_SkinLight"], col, loc=(0, 0, 1.13 + dz), scale=(1.01, 0.97, 0.99), u=18, v=14),
        add_sphere("H_CheekL", 0.045, MATS["AC_CheekBlush"], col, loc=(-0.130, -0.206, 1.078 + dz), scale=(1.2, 0.42, 0.82)),
        add_sphere("H_CheekR", 0.045, MATS["AC_CheekBlush"], col, loc=(0.130, -0.206, 1.078 + dz), scale=(1.2, 0.42, 0.82)),
        add_sphere("H_Nose", 0.026, MATS["AC_SkinLight"], col, loc=(0, -0.248, 1.098 + dz)),
        add_sphere("H_HairMain", 0.272, MATS["AC_HairBrown"], col, loc=(0, 0.035, 1.172 + dz), scale=(1.03, 1.01, 0.97), u=16, v=12),
        add_sphere("H_BangL", 0.112, MATS["AC_HairBrown"], col, loc=(-0.118, -0.184, 1.268 + dz), scale=(1.2, 0.72, 0.72)),
        add_sphere("H_BangR", 0.112, MATS["AC_HairBrown"], col, loc=(0.118, -0.184, 1.268 + dz), scale=(1.2, 0.72, 0.72)),
        # Iconic back-readable high double-tier sweeping ponytail + wide MintTeal ribbon bow!
        add_sphere("H_PonytailUpper", 0.146, MATS["AC_HairBrown"], col, loc=(0, 0.295, 1.205 + dz), scale=(0.95, 1.14, 1.16)),
        add_sphere("H_PonytailTail", 0.132, MATS["AC_HairBrown"], col, loc=(0, 0.36, 1.01 + dz), scale=(0.86, 0.94, 1.52), rot=(math.radians(16), 0, 0)),
        add_sphere("H_RibbonKnot", 0.058, MATS["AC_MintTeal"], col, loc=(0, 0.246, 1.28 + dz)),
        add_sphere("H_RibbonBowL", 0.092, MATS["AC_MintTeal"], col, loc=(-0.112, 0.265, 1.30 + dz), scale=(1.28, 0.56, 0.80), rot=(0, 0, math.radians(-18))),
        add_sphere("H_RibbonBowR", 0.092, MATS["AC_MintTeal"], col, loc=(0.112, 0.265, 1.30 + dz), scale=(1.28, 0.56, 0.80), rot=(0, 0, math.radians(18))),
        add_cyl("H_RibbonTailL", 0.026, 0.155, MATS["AC_Gold"], col, loc=(-0.055, 0.275, 1.185 + dz), rot=(math.radians(14), math.radians(12), 0), r_top=0.018),
        add_cyl("H_RibbonTailR", 0.026, 0.155, MATS["AC_Gold"], col, loc=(0.055, 0.275, 1.185 + dz), rot=(math.radians(14), math.radians(-12), 0), r_top=0.018),
        add_smile("H_Smile", MATS["AC_EyeDark"], col, loc=(0, -0.245, 1.048 + dz), width=0.112, dip=0.023),
        add_sphere("H_Tongue", 0.020, MATS["AC_Coral"], col, loc=(0, -0.258, 1.026 + dz), scale=(1.0, 0.36, 0.50), u=10, v=8),
    ]
    head = join_around_pivot(head_parts, "Protag_Head", (0, 0, 0.89 + dz), col)
    exported.append(head)

    # Separate expressive Eyes for natural blinking!
    for side, sx in [("L", -0.090), ("R", 0.090)]:
        ep = [
            add_sphere(f"EyeBall_{side}", 0.042, MATS["AC_EyeDark"], col, loc=(sx, -0.220, 1.13 + dz), scale=(0.90, 0.45, 1.15)),
            add_sphere(f"EyeShine_{side}", 0.016, MATS["AC_EyeShine"], col, loc=(sx - 0.012, -0.240, 1.147 + dz)),
        ]
        eye = join_around_pivot(ep, f"Protag_Eye{side}", (sx, -0.220, 1.13 + dz), col)
        exported.append(eye)

    # Articulated Arms (pivoted at shoulders z=0.82 + dz -> hands naturally reach 0.94m counters & carry visible cone!)
    for side, sx in [("L", -0.232), ("R", 0.232)]:
        ap = [
            add_sphere(f"Sleeve_{side}", 0.080, MATS["AC_ProtagShirt"], col, loc=(sx, 0, 0.79 + dz)),
            add_cyl(f"ArmTube_{side}", 0.048, 0.28, MATS["AC_SkinLight"], col, loc=(sx, 0, 0.625 + dz)),
            add_sphere(f"Hand_{side}", 0.060, MATS["AC_SkinLight"], col, loc=(sx, 0, 0.48 + dz)),
        ]
        arm = join_around_pivot(ap, f"Protag_Arm{side}", (sx, 0, 0.82 + dz), col)
        exported.append(arm)

    # Articulated Legs (+18% longer: hip z=0.54, LegTube h=0.42, crisp socks & berry sneakers clearly visible when walking!)
    for side, sx in [("L", -0.105), ("R", 0.105)]:
        lp = [
            add_cyl(f"LegTube_{side}", 0.056, 0.42, MATS["AC_SkinLight"], col, loc=(sx, 0, 0.33)),
            add_cyl(f"Sock_{side}", 0.062, 0.10, MATS["AC_Cream"], col, loc=(sx, 0, 0.125)),
            add_sphere(f"Shoe_{side}", 0.086, MATS["AC_ProtagShirt"], col, loc=(sx, -0.042, 0.056), scale=(0.94, 1.44, 0.66)),
            add_sphere(f"ShoeSole_{side}", 0.088, MATS["AC_Cream"], col, loc=(sx, -0.042, 0.022), scale=(0.96, 1.46, 0.28)),
        ]
        leg = join_around_pivot(lp, f"Protag_Leg{side}", (sx, 0, 0.54), col)
        exported.append(leg)

    export_standardized_unity_fbx(exported, "Char_Protagonist")
    return exported


# =========================================================================
# 2. TWO FRIENDS — DOMINANT CONTRASTING SILHOUETTES EVEN WITHOUT COLOR!
# =========================================================================
def build_friend_nina(col):
    """Nina: TALL & SLENDER silhouette, high tight top-bun hair, crisp straight boxy jacket."""
    exported = []
    # Tall slender articulated legs (hip z=0.52)
    for side, sx in [("L", -0.095), ("R", 0.095)]:
        lp = [
            add_cyl(f"N_Leg_{side}", 0.056, 0.44, MATS["AC_Denim"], col, loc=(sx, 0, 0.28), r_top=0.062),
            add_sphere(f"N_Shoe_{side}", 0.076, MATS["AC_ShoeDark"], col, loc=(sx, -0.035, 0.05), scale=(0.92, 1.38, 0.62)),
        ]
        exported.append(join_around_pivot(lp, f"Leg{side}", (sx, 0, 0.50), col))

    # Crisp straight boxy jacket + tall slender torso + high tight top-bun head
    body = [
        add_box("N_JacketMain", (0.38, 0.24, 0.50), MATS["AC_MintTeal"], col, loc=(0, 0, 0.77), bevel=0.022),
        add_box("N_JacketCollarL", (0.09, 0.05, 0.16), MATS["AC_Cream"], col, loc=(-0.09, -0.125, 0.94), rot=(0, 0, math.radians(14)), bevel=0.01),
        add_box("N_JacketCollarR", (0.09, 0.05, 0.16), MATS["AC_Cream"], col, loc=(0.09, -0.125, 0.94), rot=(0, 0, math.radians(-14)), bevel=0.01),
        add_box("N_InnerShirt", (0.14, 0.04, 0.42), MATS["AC_Cream"], col, loc=(0, -0.115, 0.76), bevel=0.01),
        add_box("N_BackPleat", (0.28, 0.04, 0.06), MATS["AC_Gold"], col, loc=(0, 0.125, 0.64), bevel=0.01),
        # Head (proportional oval on tall neck)
        add_cyl("N_Neck", 0.055, 0.10, MATS["AC_SkinWarm"], col, loc=(0, 0, 1.06)),
        add_sphere("N_Head", 0.225, MATS["AC_SkinWarm"], col, loc=(0, 0, 1.26), scale=(0.95, 0.94, 1.05), u=16, v=12),
        add_sphere("N_EyeL", 0.036, MATS["AC_EyeDark"], col, loc=(-0.082, -0.198, 1.27), scale=(0.88, 0.45, 1.15)),
        add_sphere("N_EyeR", 0.036, MATS["AC_EyeDark"], col, loc=(0.082, -0.198, 1.27), scale=(0.88, 0.45, 1.15)),
        add_sphere("N_ShineL", 0.013, MATS["AC_EyeShine"], col, loc=(-0.092, -0.214, 1.285)),
        add_sphere("N_ShineR", 0.013, MATS["AC_EyeShine"], col, loc=(0.072, -0.214, 1.285)),
        add_sphere("N_CheekL", 0.040, MATS["AC_CheekBlush"], col, loc=(-0.118, -0.184, 1.225), scale=(1.1, 0.4, 0.75)),
        add_sphere("N_CheekR", 0.040, MATS["AC_CheekBlush"], col, loc=(0.118, -0.184, 1.225), scale=(1.1, 0.4, 0.75)),
        add_smile("N_Smile", MATS["AC_EyeDark"], col, loc=(0, -0.208, 1.19), width=0.095, dip=0.020),
        # Sleek pulled-back hair + HIGH TIGHT VERTICAL TOP BUN (unmistakable silhouette!)
        add_sphere("N_HairCap", 0.238, MATS["AC_HairDark"], col, loc=(0, 0.022, 1.30), scale=(0.98, 0.98, 0.96)),
        add_sphere("N_TopBun", 0.135, MATS["AC_HairDark"], col, loc=(0, 0.06, 1.56), scale=(0.92, 0.92, 1.18)),
        add_cyl("N_BunBand", 0.105, 0.045, MATS["AC_Gold"], col, loc=(0, 0.05, 1.46)),
    ]
    exported.append(join_around_pivot(body, "Torso", (0, 0, 0.77), col))

    # Straight jacket sleeves/arms
    for side, sx in [("L", -0.245), ("R", 0.245)]:
        ap = [
            add_box(f"N_Sleeve_{side}", (0.11, 0.12, 0.26), MATS["AC_MintTeal"], col, loc=(sx, 0, 0.87), bevel=0.02),
            add_cyl(f"N_Forearm_{side}", 0.045, 0.16, MATS["AC_SkinWarm"], col, loc=(sx, 0, 0.67)),
            add_sphere(f"N_Hand_{side}", 0.054, MATS["AC_SkinWarm"], col, loc=(sx, 0, 0.58)),
        ]
        exported.append(join_around_pivot(ap, f"Arm{side}", (sx, 0, 0.97), col))

    export_standardized_unity_fbx(exported, "Char_FriendNina")
    return exported


def build_friend_bia(col):
    """Bia: SHORTER & COMPACT silhouette, voluminous curly cloud hair, rounded puffy balloon dress."""
    exported = []
    # Shorter legs (hip z=0.31)
    for side, sx in [("L", -0.095), ("R", 0.095)]:
        lp = [
            add_cyl(f"B_Leg_{side}", 0.055, 0.24, MATS["AC_SkinTan"], col, loc=(sx, 0, 0.18)),
            add_sphere(f"B_Shoe_{side}", 0.078, MATS["AC_Coral"], col, loc=(sx, -0.03, 0.052), scale=(1.05, 1.25, 0.72)),
        ]
        exported.append(join_around_pivot(lp, f"Leg{side}", (sx, 0, 0.30), col))

    # Rounded puffy balloon sweater-dress + VOLUMINOUS CURLY HAIR HALO (unmistakable round silhouette!)
    body = [
        add_sphere("B_RoundSkirt", 0.305, MATS["AC_Lavender"], col, loc=(0, 0, 0.44), scale=(1.12, 1.08, 0.88), u=18, v=14),
        add_sphere("B_RoundSweater", 0.255, MATS["AC_PinkSoft"], col, loc=(0, 0, 0.66), scale=(1.08, 1.02, 0.92), u=16, v=12),
        add_cyl("B_WaistRibbon", 0.268, 0.055, MATS["AC_SunnyYellow"], col, loc=(0, 0, 0.55)),
        add_sphere("B_BackBow", 0.085, MATS["AC_SunnyYellow"], col, loc=(0, 0.26, 0.55), scale=(1.45, 0.55, 0.82)),
        # Head (shorter height overall: z=0.98)
        add_sphere("B_Head", 0.228, MATS["AC_SkinTan"], col, loc=(0, 0, 0.98), scale=(1.02, 0.98, 0.98), u=16, v=12),
        add_sphere("B_EyeL", 0.038, MATS["AC_EyeDark"], col, loc=(-0.082, -0.202, 0.985), scale=(0.92, 0.45, 1.12)),
        add_sphere("B_EyeR", 0.038, MATS["AC_EyeDark"], col, loc=(0.082, -0.202, 0.985), scale=(0.92, 0.45, 1.12)),
        add_sphere("B_ShineL", 0.014, MATS["AC_EyeShine"], col, loc=(-0.092, -0.220, 1.00)),
        add_sphere("B_ShineR", 0.014, MATS["AC_EyeShine"], col, loc=(0.072, -0.220, 1.00)),
        add_sphere("B_CheekL", 0.046, MATS["AC_CheekBlush"], col, loc=(-0.125, -0.188, 0.938), scale=(1.2, 0.42, 0.80)),
        add_sphere("B_CheekR", 0.046, MATS["AC_CheekBlush"], col, loc=(0.125, -0.188, 0.938), scale=(1.2, 0.42, 0.80)),
        add_smile("B_Smile", MATS["AC_EyeDark"], col, loc=(0, -0.214, 0.905), width=0.105, dip=0.023),
        # BIG VOLUMINOUS CURLY CLOUD HAIR (clearly wide/round from front, side, and back!)
        add_sphere("B_HairCore", 0.285, MATS["AC_HairAuburn"], col, loc=(0, 0.045, 1.04), scale=(1.15, 1.08, 1.05)),
        add_sphere("B_CurlTopL", 0.165, MATS["AC_HairAuburn"], col, loc=(-0.19, 0.03, 1.16)),
        add_sphere("B_CurlTopR", 0.165, MATS["AC_HairAuburn"], col, loc=(0.19, 0.03, 1.16)),
        add_sphere("B_CurlSideL", 0.175, MATS["AC_HairAuburn"], col, loc=(-0.27, 0.05, 0.97)),
        add_sphere("B_CurlSideR", 0.175, MATS["AC_HairAuburn"], col, loc=(0.27, 0.05, 0.97)),
        add_sphere("B_CurlBackL", 0.175, MATS["AC_HairAuburn"], col, loc=(-0.15, 0.24, 0.96)),
        add_sphere("B_CurlBackR", 0.175, MATS["AC_HairAuburn"], col, loc=(0.15, 0.24, 0.96)),
        add_sphere("B_CurlBackC", 0.185, MATS["AC_HairAuburn"], col, loc=(0.0, 0.27, 1.06)),
        add_sphere("B_HairClip", 0.055, MATS["AC_SunnyYellow"], col, loc=(0.17, -0.15, 1.14), scale=(1.2, 0.55, 0.85)),
    ]
    exported.append(join_around_pivot(body, "Torso", (0, 0, 0.62), col))

    # Puffy rounded sleeves/arms
    for side, sx in [("L", -0.275), ("R", 0.275)]:
        ap = [
            add_sphere(f"B_PuffSleeve_{side}", 0.125, MATS["AC_PinkSoft"], col, loc=(sx, 0, 0.72)),
            add_cyl(f"B_Arm_{side}", 0.048, 0.15, MATS["AC_SkinTan"], col, loc=(sx, 0, 0.57)),
            add_sphere(f"B_Hand_{side}", 0.056, MATS["AC_SkinTan"], col, loc=(sx, 0, 0.49)),
        ]
        exported.append(join_around_pivot(ap, f"Arm{side}", (sx, 0, 0.74), col))

    export_standardized_unity_fbx(exported, "Char_FriendBia")
    return exported


# =========================================================================
# 3. MODULAR RESIDENTS (Height & Width Variations + Back-Readable Silhouettes +
#    Florist Luna's Botanical Apron & Seedling Basket!)
# =========================================================================
def build_modular_resident(
    col, filename, skin_key, hair_key, top_key, bot_key, accent_key,
    h_scale=1.0, w_scale=1.0, role="blue"
):
    m_skin = MATS[skin_key]
    m_hair = MATS[hair_key]
    m_top = MATS[top_key]
    m_bot = MATS[bot_key]
    m_acc = MATS[accent_key]

    hip_z = 0.40 * h_scale
    leg_h = 0.31 * h_scale
    torso_z = hip_z + 0.26 * h_scale
    shoulder_z = hip_z + 0.43 * h_scale
    head_z = hip_z + 0.72 * h_scale

    exported = []

    # Articulated legs for GirlPowerOrla3DNpcAI walking cycles
    for side, sx in [("L", -0.105 * w_scale), ("R", 0.105 * w_scale)]:
        leg_mat = m_bot if role in ("blue", "florist") else m_skin
        lp = [
            add_cyl(f"{filename}_Leg_{side}", 0.058 * w_scale, leg_h, leg_mat, col, loc=(sx, 0, hip_z * 0.56)),
            add_sphere(f"{filename}_Shoe_{side}", 0.078 * w_scale, MATS["AC_ShoeDark"], col,
                       loc=(sx, -0.035, 0.052), scale=(0.95, 1.38, 0.65)),
        ]
        exported.append(join_around_pivot(lp, f"Leg{side}", (sx, 0, hip_z), col))

    body = [
        add_cyl("R_Torso", 0.195 * w_scale, 0.42 * h_scale, m_top, col, loc=(0, 0, torso_z), r_top=0.165 * w_scale),
        add_cyl("R_Hip", 0.205 * w_scale, 0.16 * h_scale, m_bot, col, loc=(0, 0, hip_z + 0.04), r_top=0.195 * w_scale),
        add_sphere("R_Head", 0.238 * (0.94 + 0.06 * w_scale), m_skin, col, loc=(0, 0, head_z), scale=(0.98, 0.96, 1.02), u=16, v=12),
        add_sphere("EyeL", 0.036, MATS["AC_EyeDark"], col, loc=(-0.084, -0.212, head_z + 0.01), scale=(0.9, 0.45, 1.15)),
        add_sphere("EyeR", 0.036, MATS["AC_EyeDark"], col, loc=(0.084, -0.212, head_z + 0.01), scale=(0.9, 0.45, 1.15)),
        add_sphere("ShineL", 0.013, MATS["AC_EyeShine"], col, loc=(-0.094, -0.230, head_z + 0.025)),
        add_sphere("ShineR", 0.013, MATS["AC_EyeShine"], col, loc=(0.074, -0.230, head_z + 0.025)),
        add_sphere("CheekL", 0.042, MATS["AC_CheekBlush"], col, loc=(-0.122, -0.198, head_z - 0.035), scale=(1.1, 0.4, 0.78)),
        add_sphere("CheekR", 0.042, MATS["AC_CheekBlush"], col, loc=(0.122, -0.198, head_z - 0.035), scale=(1.1, 0.4, 0.78)),
        add_smile("R_Smile", MATS["AC_EyeDark"], col, loc=(0, -0.224, head_z - 0.072), width=0.10, dip=0.021),
        add_sphere("R_HairBase", 0.252 * (0.94 + 0.06 * w_scale), m_hair, col, loc=(0, 0.03, head_z + 0.045), scale=(1.02, 1.0, 0.96)),
    ]

    # Role-specific back-readable silhouettes & props!
    if role == "florist":
        # Wide-brim straw sun hat with floral ribbon (instantly recognizable from back & top!)
        body.extend([
            add_cyl("F_HatBrim", 0.42, 0.04, MATS["AC_StrawHat"], col, loc=(0, 0.02, head_z + 0.16), segs=18),
            add_cyl("F_HatCrown", 0.22, 0.16, MATS["AC_StrawHat"], col, loc=(0, 0.02, head_z + 0.25), r_top=0.19, segs=16),
            add_cyl("F_HatRibbon", 0.232, 0.055, MATS["AC_PinkBerry"], col, loc=(0, 0.02, head_z + 0.195), segs=16),
            add_sphere("F_HatFlowerA", 0.062, MATS["AC_FlowerYellow"], col, loc=(-0.19, 0.14, head_z + 0.21)),
            add_sphere("F_HatFlowerB", 0.052, MATS["AC_FlowerPink"], col, loc=(-0.12, 0.20, head_z + 0.21)),
            # Long braided ponytail down the back
            add_sphere("F_Braid1", 0.082, m_hair, col, loc=(0, 0.24, head_z - 0.06)),
            add_sphere("F_Braid2", 0.074, m_hair, col, loc=(0, 0.26, head_z - 0.18)),
            add_sphere("F_Braid3", 0.062, m_hair, col, loc=(0, 0.27, head_z - 0.29)),
            # Botanical Green Apron (front bib + pocket + X-straps & bow on the back!)
            add_box("F_ApronFront", (0.28, 0.05, 0.44), MATS["AC_BushGreen"], col, loc=(0, -0.19, torso_z - 0.06), bevel=0.015),
            add_box("F_ApronPocket", (0.18, 0.045, 0.12), MATS["AC_Cream"], col, loc=(0, -0.215, torso_z - 0.14), bevel=0.01),
            add_box("F_ApronStrapL", (0.042, 0.03, 0.36), MATS["AC_BushGreen"], col, loc=(-0.05, 0.185, torso_z + 0.06), rot=(0, math.radians(24), 0), bevel=0.008),
            add_box("F_ApronStrapR", (0.042, 0.03, 0.36), MATS["AC_BushGreen"], col, loc=(0.05, 0.185, torso_z + 0.06), rot=(0, math.radians(-24), 0), bevel=0.008),
            add_cyl("F_ApronWaistTie", 0.215, 0.05, MATS["AC_BushGreen"], col, loc=(0, 0, hip_z + 0.12)),
            add_sphere("F_ApronBackBow", 0.088, MATS["AC_BushLight"], col, loc=(0, 0.225, hip_z + 0.12), scale=(1.45, 0.55, 0.85)),
            # WOVEN SEEDLING BASKET (Cesto de Mudas) held prominently at her right side — readable from front & back!
            add_cyl("F_BasketBody", 0.165, 0.21, MATS["AC_WoodLight"], col, loc=(0.30, -0.04, hip_z + 0.12), r_top=0.195, segs=14),
            add_cyl("F_BasketRim", 0.205, 0.042, MATS["AC_WoodWarm"], col, loc=(0.30, -0.04, hip_z + 0.225), segs=14),
            add_cyl("F_BasketSoil", 0.175, 0.04, MATS["AC_WoodDark"], col, loc=(0.30, -0.04, hip_z + 0.21), segs=12),
            # 3 bright green sprouts & colorful flowers peeking out of the basket!
            add_sphere("F_Sprout1", 0.085, MATS["AC_BushGreen"], col, loc=(0.25, -0.08, hip_z + 0.29), scale=(1.0, 1.0, 1.3)),
            add_sphere("F_Sprout2", 0.090, MATS["AC_BushLight"], col, loc=(0.35, -0.02, hip_z + 0.31), scale=(1.0, 1.0, 1.35)),
            add_sphere("F_Sprout3", 0.080, MATS["AC_BushGreen"], col, loc=(0.29, 0.04, hip_z + 0.28), scale=(1.0, 1.0, 1.25)),
            add_sphere("F_BasketFlower1", 0.052, MATS["AC_FlowerPink"], col, loc=(0.24, -0.09, hip_z + 0.37)),
            add_sphere("F_BasketFlower2", 0.056, MATS["AC_FlowerYellow"], col, loc=(0.36, -0.02, hip_z + 0.39)),
        ])
    elif role == "blue":
        # Tall broad coastal hiker: cap + crisp rectangular daypack on back (distinctive back silhouette!)
        body.extend([
            add_sphere("B_CapCrown", 0.26, m_acc, col, loc=(0, 0.01, head_z + 0.10), scale=(1.02, 1.06, 0.72)),
            add_box("B_CapVisor", (0.24, 0.18, 0.032), m_acc, col, loc=(0, -0.22, head_z + 0.10), bevel=0.01),
            add_box("B_Backpack", (0.28, 0.17, 0.36), MATS["AC_Gold"], col, loc=(0, 0.23, torso_z + 0.02), bevel=0.035),
            add_box("B_PackPocket", (0.22, 0.07, 0.15), MATS["AC_Cream"], col, loc=(0, 0.31, torso_z - 0.05), bevel=0.02),
        ])
    elif role == "coral":
        # Shorter wide-coat resident with voluminous rounded curly-bob hair (unmistakable silhouette even in grayscale!)
        body.extend([
            add_cyl("C_WideCoat", 0.30 * w_scale, 0.42 * h_scale, m_top, col, loc=(0, 0, torso_z - 0.05), r_top=0.19 * w_scale),
            add_cyl("C_CoatTrim", 0.31 * w_scale, 0.06 * h_scale, MATS["AC_Cream"], col, loc=(0, 0, hip_z + 0.02), r_top=0.30 * w_scale),
            add_sphere("C_HairCloudL", 0.175, m_hair, col, loc=(-0.21, 0.06, head_z + 0.02), scale=(0.92, 0.98, 1.05)),
            add_sphere("C_HairCloudR", 0.175, m_hair, col, loc=( 0.21, 0.06, head_z + 0.02), scale=(0.92, 0.98, 1.05)),
            add_sphere("C_HairCloudBack", 0.215, m_hair, col, loc=(0.0, 0.19, head_z - 0.01), scale=(1.18, 0.82, 1.04)),
            add_sphere("C_BeltBag", 0.115, MATS["AC_MintTeal"], col, loc=(0, 0.26, hip_z + 0.10), scale=(1.45, 0.65, 0.85)),
        ])
    elif role == "marina":
        # Tall graceful sister: flowing coral dress + long wavy hair + BIG sunny yellow bow on back!
        body.extend([
            add_cyl("M_Dress", 0.31 * w_scale, 0.56 * h_scale, m_top, col, loc=(0, 0, hip_z - 0.06), r_top=0.165 * w_scale),
            add_sphere("M_LongHair", 0.20, m_hair, col, loc=(0, 0.22, head_z - 0.16), scale=(1.22, 0.62, 1.65)),
            add_sphere("M_SunBowL", 0.105, MATS["AC_SunnyYellow"], col, loc=(-0.12, 0.26, head_z + 0.08), scale=(1.25, 0.5, 0.78)),
            add_sphere("M_SunBowR", 0.105, MATS["AC_SunnyYellow"], col, loc=(0.12, 0.26, head_z + 0.08), scale=(1.25, 0.5, 0.78)),
            add_box("M_StrawPurse", (0.14, 0.16, 0.14), MATS["AC_StrawHat"], col, loc=(-0.24, 0.04, hip_z + 0.04), bevel=0.025),
        ])

    exported.append(join_around_pivot(body, "Torso", (0, 0, torso_z), col))

    # Articulated arms
    for side, sx in [("L", -0.235 * w_scale), ("R", 0.235 * w_scale)]:
        ap = [
            add_sphere(f"{filename}_Sleeve_{side}", 0.078 * w_scale, m_top, col, loc=(sx, 0, shoulder_z - 0.03)),
            add_cyl(f"{filename}_Arm_{side}", 0.048 * w_scale, 0.25 * h_scale, m_skin, col, loc=(sx, 0, shoulder_z - 0.17 * h_scale)),
            add_sphere(f"{filename}_Hand_{side}", 0.058 * w_scale, m_skin, col, loc=(sx, 0, shoulder_z - 0.30 * h_scale)),
        ]
        exported.append(join_around_pivot(ap, f"Arm{side}", (sx, 0, shoulder_z), col))

    export_standardized_unity_fbx(exported, filename)
    return exported


# =========================================================================
# 4. FAIRY WAITRESS (High-contrast against ocean & pavement + glowing golden wing rims + +Z forward!)
# =========================================================================
def build_fairy_waitress(col):
    body_parts = [
        # Vibrant Berry-Pink & Lavender petal dress with Gold sash -> pops against BOTH turquoise sea & beige tiles!
        add_cyl("DressSkirt", 0.28, 0.44, MATS["AC_PinkBerry"], col, loc=(0, 0, 0.44), r_top=0.135),
        add_cyl("DressUnder", 0.31, 0.18, MATS["AC_Lavender"], col, loc=(0, 0, 0.28), r_top=0.25),
        add_cyl("GoldSash", 0.158, 0.075, MATS["AC_Gold"], col, loc=(0, 0, 0.63)),
        add_cyl("Apron", 0.21, 0.25, MATS["AC_Cream"], col, loc=(0, -0.06, 0.49), r_top=0.14),
        add_cyl("Bodice", 0.145, 0.32, MATS["AC_PinkBerry"], col, loc=(0, 0, 0.80), r_top=0.125),
        # Graceful floating legs
        add_cyl("LegL", 0.046, 0.32, MATS["AC_SkinWarm"], col, loc=(-0.08, 0.02, 0.16), rot=(0.18, 0, 0)),
        add_cyl("LegR", 0.046, 0.32, MATS["AC_SkinWarm"], col, loc=(0.08, 0.04, 0.14), rot=(0.24, 0, 0)),
        add_sphere("SlipperL", 0.062, MATS["AC_Gold"], col, loc=(-0.08, 0.05, 0.03), scale=(0.9, 1.35, 0.6)),
        add_sphere("SlipperR", 0.062, MATS["AC_Gold"], col, loc=(0.08, 0.08, 0.01), scale=(0.9, 1.35, 0.6)),
        # Expressive head + dark plum wavy hair (strong silhouette contrast!) + sunny flower crown
        add_sphere("Head", 0.20, MATS["AC_SkinWarm"], col, loc=(0, 0, 1.13), scale=(0.96, 0.94, 1.05), u=16, v=12),
        add_sphere("EyeL", 0.033, MATS["AC_EyeDark"], col, loc=(-0.075, -0.175, 1.14), scale=(0.9, 0.45, 1.2)),
        add_sphere("EyeR", 0.033, MATS["AC_EyeDark"], col, loc=(0.075, -0.175, 1.14), scale=(0.9, 0.45, 1.2)),
        add_sphere("ShineL", 0.012, MATS["AC_EyeShine"], col, loc=(-0.085, -0.190, 1.155)),
        add_sphere("ShineR", 0.012, MATS["AC_EyeShine"], col, loc=(0.065, -0.190, 1.155)),
        add_sphere("CheekL", 0.038, MATS["AC_CheekBlush"], col, loc=(-0.11, -0.16, 1.09), scale=(1.1, 0.4, 0.7)),
        add_sphere("CheekR", 0.038, MATS["AC_CheekBlush"], col, loc=(0.11, -0.16, 1.09), scale=(1.1, 0.4, 0.7)),
        add_smile("F_Smile", MATS["AC_EyeDark"], col, loc=(0, -0.184, 1.06), width=0.082, dip=0.017),
        add_sphere("HairTop", 0.22, MATS["AC_HairPlum"], col, loc=(0, 0.04, 1.19), scale=(1.03, 1.03, 0.95)),
        add_sphere("HairFlow", 0.165, MATS["AC_HairPlum"], col, loc=(0, 0.20, 1.00), scale=(1.18, 0.72, 1.50)),
        add_sphere("CrownF1", 0.065, MATS["AC_LanternGlow"], col, loc=(-0.14, -0.06, 1.29)),
        add_sphere("CrownF2", 0.072, MATS["AC_FlowerPink"], col, loc=(0.0, -0.14, 1.32)),
        add_sphere("CrownF3", 0.065, MATS["AC_LanternGlow"], col, loc=(0.14, -0.06, 1.29)),
        # Arms holding golden serving tray
        add_cyl("ArmL", 0.040, 0.28, MATS["AC_SkinWarm"], col, loc=(-0.19, -0.08, 0.81), rot=(0.75, 0, 0.25)),
        add_cyl("ArmR", 0.040, 0.28, MATS["AC_SkinWarm"], col, loc=(0.19, -0.08, 0.81), rot=(0.75, 0, -0.25)),
        add_cyl("Tray", 0.24, 0.032, MATS["AC_Gold"], col, loc=(0, -0.34, 0.87), segs=16),
        add_cyl("TrayCup", 0.055, 0.14, MATS["AC_MintTeal"], col, loc=(-0.08, -0.34, 0.96), segs=10),
        add_sphere("TrayIce", 0.068, MATS["AC_LanternGlow"], col, loc=(0.08, -0.34, 0.97)),
    ]
    body = join_around_pivot(body_parts, "Fairy_Body", (0, 0, 0.0), col)

    # HIGH-CONTRAST FAIRY WINGS: Glowing Golden Rim + Rich Lavender/Pink Inner Membrane (pops on sea AND pavement!)
    wing_l_parts = [
        add_sphere("WingL_RimUpper", 0.29, MATS["AC_LanternGlow"], col, loc=(-0.37, 0.225, 0.99), scale=(1.55, 0.11, 0.98)),
        add_sphere("WingL_CoreUpper", 0.24, MATS["AC_Lavender"], col, loc=(-0.35, 0.215, 0.98), scale=(1.42, 0.15, 0.84)),
        add_sphere("WingL_RimLower", 0.205, MATS["AC_Gold"], col, loc=(-0.28, 0.215, 0.72), scale=(1.32, 0.10, 0.88)),
        add_sphere("WingL_CoreLower", 0.165, MATS["AC_PinkBerry"], col, loc=(-0.27, 0.205, 0.72), scale=(1.18, 0.14, 0.74)),
        add_sphere("WingL_Gem", 0.068, MATS["AC_LanternGlow"], col, loc=(-0.44, 0.20, 1.00), scale=(1.0, 0.45, 1.0)),
    ]
    wing_l = join_around_pivot(wing_l_parts, "Fairy_WingL", (-0.07, 0.16, 0.87), col)

    wing_r_parts = [
        add_sphere("WingR_RimUpper", 0.29, MATS["AC_LanternGlow"], col, loc=(0.37, 0.225, 0.99), scale=(1.55, 0.11, 0.98)),
        add_sphere("WingR_CoreUpper", 0.24, MATS["AC_Lavender"], col, loc=(0.35, 0.215, 0.98), scale=(1.42, 0.15, 0.84)),
        add_sphere("WingR_RimLower", 0.205, MATS["AC_Gold"], col, loc=(0.28, 0.215, 0.72), scale=(1.32, 0.10, 0.88)),
        add_sphere("WingR_CoreLower", 0.165, MATS["AC_PinkBerry"], col, loc=(0.27, 0.205, 0.72), scale=(1.18, 0.14, 0.74)),
        add_sphere("WingR_Gem", 0.068, MATS["AC_LanternGlow"], col, loc=(0.44, 0.20, 1.00), scale=(1.0, 0.45, 1.0)),
    ]
    wing_r = join_around_pivot(wing_r_parts, "Fairy_WingR", (0.07, 0.16, 0.87), col)

    export_standardized_unity_fbx([body, wing_l, wing_r], "Char_FairyWaitress")
    return [body, wing_l, wing_r]


# =========================================================================
# 5. HIGH-CONTRAST ARTICULATED PETS (Pipoca Dog & Mingau Cat — +Z Forward!)
# =========================================================================
def build_pipoca(col):
    """Pipoca: Rich warm caramel/chocolate patches + bright Berry collar so body pops on beige sand & blue sea!"""
    exported = []
    body_parts = [
        add_sphere("P_Torso", 0.225, MATS["AC_Cream"], col, loc=(0, 0.02, 0.29), scale=(0.94, 1.40, 0.92), u=16, v=12),
        add_sphere("P_SaddlePatch", 0.175, MATS["AC_WoodDark"], col, loc=(0.02, 0.05, 0.36), scale=(1.12, 1.25, 0.72)),
        add_sphere("P_RumpPatch", 0.135, MATS["AC_WoodWarm"], col, loc=(-0.05, 0.20, 0.33), scale=(1.0, 1.0, 0.8)),
        add_cyl("P_Collar", 0.158, 0.062, MATS["AC_ProtagShirt"], col, loc=(0, -0.18, 0.35), rot=(math.radians(32), 0, 0)),
        add_sphere("P_Bell", 0.042, MATS["AC_Gold"], col, loc=(0, -0.29, 0.265)),
    ]
    exported.append(join_around_pivot(body_parts, "Pipoca_Body", (0, 0, 0.27), col))

    head_parts = [
        add_sphere("P_Skull", 0.205, MATS["AC_Cream"], col, loc=(0, -0.28, 0.49), scale=(1.04, 1.0, 0.96), u=16, v=12),
        add_sphere("P_Snout", 0.108, MATS["AC_Cream"], col, loc=(0, -0.445, 0.445), scale=(1.05, 0.98, 0.80)),
        add_sphere("P_Nose", 0.046, MATS["AC_EyeDark"], col, loc=(0, -0.535, 0.47), scale=(1.12, 0.82, 0.82)),
        add_smile("P_Smile", MATS["AC_EyeDark"], col, loc=(0, -0.535, 0.392), width=0.11, dip=0.022, thickness=0.006),
        add_sphere("P_Tongue", 0.025, MATS["AC_Coral"], col, loc=(0, -0.552, 0.368), scale=(1.0, 0.38, 0.50), u=10, v=8),
        add_sphere("P_EyeL", 0.038, MATS["AC_EyeDark"], col, loc=(-0.09, -0.435, 0.53)),
        add_sphere("P_EyeR", 0.038, MATS["AC_EyeDark"], col, loc=(0.09, -0.435, 0.53)),
        add_sphere("P_ShineL", 0.015, MATS["AC_EyeShine"], col, loc=(-0.10, -0.464, 0.545)),
        add_sphere("P_ShineR", 0.015, MATS["AC_EyeShine"], col, loc=(0.08, -0.464, 0.545)),
        add_sphere("P_PatchEye", 0.105, MATS["AC_WoodDark"], col, loc=(-0.095, -0.37, 0.535), scale=(0.95, 0.65, 0.95)),
    ]
    exported.append(join_around_pivot(head_parts, "Pipoca_Head", (0, -0.22, 0.39), col))

    for side, sx in [("L", -0.175), ("R", 0.175)]:
        ep = [add_sphere(f"EarMesh_{side}", 0.090, MATS["AC_WoodDark"], col, loc=(sx * 1.15, -0.26, 0.46), scale=(0.56, 0.88, 1.50))]
        exported.append(join_around_pivot(ep, f"Pipoca_Ear{side}", (sx, -0.26, 0.56), col))

    tp = [
        add_sphere("TailMesh", 0.076, MATS["AC_WoodDark"], col, loc=(0, 0.35, 0.42), scale=(0.75, 1.55, 0.85)),
        add_sphere("TailTip", 0.056, MATS["AC_Cream"], col, loc=(0, 0.44, 0.46)),
    ]
    exported.append(join_around_pivot(tp, "Pipoca_Tail", (0, 0.27, 0.35), col))

    for lname, lx, ly in [("FL", -0.115, -0.15), ("FR", 0.115, -0.15), ("BL", -0.115, 0.16), ("BR", 0.115, 0.16)]:
        lp = [
            add_cyl(f"PLeg_{lname}", 0.050, 0.19, MATS["AC_Cream"], col, loc=(lx, ly, 0.105)),
            add_sphere(f"PPaw_{lname}", 0.058, MATS["AC_WoodWarm"], col, loc=(lx, ly - 0.02, 0.036), scale=(1.0, 1.24, 0.65)),
        ]
        exported.append(join_around_pivot(lp, f"Pipoca_Leg{lname}", (lx, ly, 0.20), col))

    export_standardized_unity_fbx(exported, "PetDog")
    return exported


def build_mingau_cat(col):
    """Mingau: Articulated cat with high-contrast charcoal-plum ears/back/tail + bright MintTeal collar!"""
    exported = []
    body_parts = [
        add_sphere("C_Torso", 0.195, MATS["AC_Cream"], col, loc=(0, 0.02, 0.26), scale=(0.88, 1.38, 0.88), u=16, v=12),
        add_sphere("C_BackSpot", 0.145, MATS["AC_ShoeDark"], col, loc=(0, 0.07, 0.33), scale=(1.05, 1.15, 0.68)),
        add_cyl("C_Collar", 0.135, 0.052, MATS["AC_MintTeal"], col, loc=(0, -0.155, 0.31), rot=(math.radians(28), 0, 0)),
        add_sphere("C_Bell", 0.036, MATS["AC_Gold"], col, loc=(0, -0.245, 0.24)),
    ]
    exported.append(join_around_pivot(body_parts, "Cat_Body", (0, 0, 0.24), col))

    head_parts = [
        add_sphere("C_Skull", 0.185, MATS["AC_Cream"], col, loc=(0, -0.24, 0.45), scale=(1.08, 0.96, 0.92), u=16, v=12),
        add_cyl("C_EarL", 0.068, 0.13, MATS["AC_ShoeDark"], col, loc=(-0.115, -0.23, 0.61), rot=(0, math.radians(-16), 0), r_top=0.012, segs=8),
        add_cyl("C_EarR", 0.068, 0.13, MATS["AC_ShoeDark"], col, loc=(0.115, -0.23, 0.61), rot=(0, math.radians(16), 0), r_top=0.012, segs=8),
        add_sphere("C_Nose", 0.028, MATS["AC_PinkBerry"], col, loc=(0, -0.415, 0.445), scale=(1.1, 0.7, 0.7)),
        add_sphere("CatEyeL", 0.036, MATS["AC_EyeDark"], col, loc=(-0.082, -0.388, 0.475)),
        add_sphere("CatEyeR", 0.036, MATS["AC_EyeDark"], col, loc=(0.082, -0.388, 0.475)),
        add_sphere("CatShineL", 0.014, MATS["AC_EyeShine"], col, loc=(-0.092, -0.414, 0.488)),
        add_sphere("CatShineR", 0.014, MATS["AC_EyeShine"], col, loc=(0.072, -0.414, 0.488)),
        add_smile("C_Smile", MATS["AC_EyeDark"], col, loc=(0, -0.412, 0.405), width=0.082, dip=0.016, thickness=0.0055),
        add_sphere("C_ForeheadPatch", 0.09, MATS["AC_ShoeDark"], col, loc=(0.06, -0.26, 0.56), scale=(1.1, 0.9, 0.65)),
    ]
    exported.append(join_around_pivot(head_parts, "Cat_Head", (0, -0.19, 0.36), col))

    # Graceful curved upright cat tail (high contrast!)
    tp = [
        add_cyl("CTail_Low", 0.036, 0.24, MATS["AC_ShoeDark"], col, loc=(0, 0.30, 0.40), rot=(math.radians(-26), 0, 0), r_top=0.032),
        add_sphere("CTail_Tip", 0.048, MATS["AC_Lavender"], col, loc=(0, 0.36, 0.52)),
    ]
    exported.append(join_around_pivot(tp, "Cat_Tail", (0, 0.24, 0.30), col))

    for lname, lx, ly in [("FL", -0.095, -0.13), ("FR", 0.095, -0.13), ("BL", -0.095, 0.14), ("BR", 0.095, 0.14)]:
        lp = [
            add_cyl(f"CLeg_{lname}", 0.042, 0.17, MATS["AC_Cream"], col, loc=(lx, ly, 0.095)),
            add_sphere(f"CPaw_{lname}", 0.048, MATS["AC_ShoeDark"], col, loc=(lx, ly - 0.015, 0.032), scale=(1.0, 1.2, 0.65)),
        ]
        exported.append(join_around_pivot(lp, f"Cat_Leg{lname}", (lx, ly, 0.18), col))

    export_standardized_unity_fbx(exported, "PetCat")
    return exported


def main():
    builders = [
        lambda c: build_protagonist(c),
        lambda c: build_friend_nina(c),
        lambda c: build_friend_bia(c),
        lambda c: build_modular_resident(c, "Char_Florist", "AC_SkinLight", "AC_HairBrown", "AC_Cream", "AC_WoodLight", "AC_StrawHat", h_scale=1.02, w_scale=0.96, role="florist"),
        lambda c: build_modular_resident(c, "Char_PedestrianBlue", "AC_SkinTan", "AC_HairDark", "AC_SkyBlue", "AC_Denim", "AC_Denim", h_scale=1.18, w_scale=1.00, role="blue"),
        lambda c: build_modular_resident(c, "Char_PedestrianCoral", "AC_SkinLight", "AC_HairAuburn", "AC_Coral", "AC_Cream", "AC_MintTeal", h_scale=0.88, w_scale=1.14, role="coral"),
        lambda c: build_modular_resident(c, "Char_SisterMarina", "AC_SkinLight", "AC_HairBrown", "AC_Coral", "AC_Cream", "AC_SunnyYellow", h_scale=1.06, w_scale=0.94, role="marina"),
        lambda c: build_fairy_waitress(c),
        lambda c: build_pipoca(c),
        lambda c: build_mingau_cat(c),
    ]

    # Export each character from a completely clean scene so EVERY joint has exact names (zero .001/.002 suffixes!)
    for fn in builders:
        clear_scene()
        init_mats()
        col = bpy.data.collections.new("SingleExport")
        bpy.context.scene.collection.children.link(col)
        fn(col)

    # Also save a combined .blend showcase file with all 10 characters lined up for easy visual inspection
    clear_scene()
    init_mats()
    col_all = bpy.data.collections.new("OrlaArtDirection_Lineup")
    bpy.context.scene.collection.children.link(col_all)
    # Re-import the exported FBXs into the lineup .blend so the lineup matches the exact Unity FBX exports!
    for idx, fname in enumerate([
        "Char_Protagonist", "Char_FriendNina", "Char_FriendBia", "Char_Florist",
        "Char_PedestrianBlue", "Char_PedestrianCoral", "Char_SisterMarina",
        "Char_FairyWaitress", "PetDog", "PetCat"
    ]):
        before = set(bpy.data.objects)
        bpy.ops.import_scene.fbx(filepath=os.path.join(MODELS_DIR, f"{fname}.fbx"), axis_forward="Z", axis_up="Y")
        for o in set(bpy.data.objects) - before:
            o.location.x += idx * 1.45

    # Also add architectural scale reference props side-by-side (Counter 0.94m, Bench 0.48m, Doorway 2.15m) for proportion checks!
    col_ref = bpy.data.collections.new("Ref_Escala_Cenario")
    bpy.context.scene.collection.children.link(col_ref)
    add_box("Ref_Balcao_094m", (1.60, 0.72, 0.94), MATS["AC_WoodLight"], col_ref, loc=(-1.9, 0.0, 0.47))
    add_box("Ref_Banco_048m", (1.60, 0.52, 0.48), MATS["AC_WoodWarm"], col_ref, loc=(-3.9, 0.0, 0.24))
    add_box("Ref_Porta_215m", (1.10, 0.14, 2.15), MATS["AC_Cream"], col_ref, loc=(-5.6, 0.0, 1.075))

    bpy.ops.wm.save_as_mainfile(filepath=BLEND_OUT)
    print(f"[Orla das Fadas Art Direction Characters Saved & Exported (+Z Forward, Clean Joint Names)] {BLEND_OUT}")


if __name__ == "__main__":
    main()
