"""
build_baia_chapters_assets.py
-----------------------------
Blender 5.3 procedural 3D asset generator for Chapters 2, 3 & Finale of "Baía do Avesso":
  1. FloristCard.fbx           - Folded pastel invitation card with lavender wax seal (Chapter 2 Wind Gust)
  2. Char_SisterMarina.fbx     - Marina, the Florist's sister (coral dress, sunny bow, matching the Polaroid photo!)
  3. PavilionFestivalTable.fbx - Long cozy communal wooden table draped with pastel cloth (Chapter 3)
  4. PavilionFestivalLights.fbx- Glowing fairy lights, pennants & restored golden signboard for the Pavilion (Chapter 3)
  5. FestivalDishesProp.fbx    - Dishes, flower pots, ice cream bowls & lanterns brought by neighbors (Finale!)

Saves BaiaDoAvesso_ChaptersAssets.blend and renders baia_chapters_preview.png.
"""

import math
import os
import bpy
import bmesh
from mathutils import Vector

SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
ORLA3D_DIR = os.path.dirname(SCRIPT_DIR)
MODELS_DIR = os.path.join(ORLA3D_DIR, "Models")
BLEND_OUT = os.path.join(SCRIPT_DIR, "BaiaDoAvesso_ChaptersAssets.blend")
PREVIEW_OUT = os.path.join(SCRIPT_DIR, "baia_chapters_preview.png")

os.makedirs(MODELS_DIR, exist_ok=True)
MATERIALS = {}


def srgb_to_linear(c):
    if c <= 0.04045:
        return c / 12.92
    return ((c + 0.055) / 1.055) ** 2.4


def hex_to_rgba(hex_str, alpha=1.0):
    hex_str = hex_str.lstrip("#")
    r = srgb_to_linear(int(hex_str[0:2], 16) / 255.0)
    g = srgb_to_linear(int(hex_str[2:4], 16) / 255.0)
    b = srgb_to_linear(int(hex_str[4:6], 16) / 255.0)
    return (r, g, b, alpha)


def get_mat(name, hex_color, roughness=0.50, emission_hex=None, emission_strength=0.0):
    if name in MATERIALS:
        return MATERIALS[name]
    mat = bpy.data.materials.new(name=name)
    nt = mat.node_tree
    bsdf = next((n for n in nt.nodes if n.type == "BSDF_PRINCIPLED"), None)
    if bsdf is None:
        bsdf = nt.nodes.new("ShaderNodeBsdfPrincipled")
    rgba = hex_to_rgba(hex_color, 1.0)
    if "Base Color" in bsdf.inputs:
        bsdf.inputs["Base Color"].default_value = rgba
    if "Roughness" in bsdf.inputs:
        bsdf.inputs["Roughness"].default_value = roughness
    if emission_hex and emission_strength > 0.0:
        if "Emission Color" in bsdf.inputs:
            bsdf.inputs["Emission Color"].default_value = hex_to_rgba(emission_hex)
        if "Emission Strength" in bsdf.inputs:
            bsdf.inputs["Emission Strength"].default_value = emission_strength
    mat.diffuse_color = rgba
    MATERIALS[name] = mat
    return mat


def init_palette():
    get_mat("AC_WoodWarm", "#B97A4B", 0.66)
    get_mat("AC_WoodLight", "#D89E6A", 0.62)
    get_mat("AC_WoodDark", "#7B4C2F", 0.68)
    get_mat("AC_Cream", "#FFF6E5", 0.48)
    get_mat("AC_FlowerWhite", "#FFFDF7", 0.45)
    get_mat("AC_PinkBerry", "#F26E97", 0.48)
    get_mat("AC_PinkSoft", "#FFA9C2", 0.48)
    get_mat("AC_MintTeal", "#4EC7B6", 0.46)
    get_mat("AC_MintSoft", "#8FE3D6", 0.48)
    get_mat("AC_PurplePlum", "#5B3B6E", 0.50)
    get_mat("AC_Lavender", "#B89CE0", 0.50)
    get_mat("AC_Coral", "#F27B66", 0.48)
    get_mat("AC_SunnyYellow", "#FFD45E", 0.48)
    get_mat("AC_SkyBlue", "#6CC0F0", 0.48)
    get_mat("AC_Gold", "#F5BE4F", 0.34)
    get_mat("AC_Terracotta", "#D47551", 0.70)
    get_mat("AC_BushGreen", "#52B55E", 0.58)
    get_mat("AC_PalmLeaf", "#4EBF5C", 0.52)
    get_mat("AC_Orange", "#FF9333", 0.44)
    get_mat("AC_Strawberry", "#FF6E8D", 0.42)
    get_mat("AC_Waffle", "#E3A75B", 0.60)
    get_mat("AC_BulbWarm", "#FFF0B3", 0.20, "#FFE082", 3.0)
    get_mat("AC_LanternGlow", "#FFE69C", 0.22, "#FFD26B", 3.2)

    get_mat("AC_SkinLight", "#F4C29F", 0.56)
    get_mat("AC_CheekBlush", "#FF8EA6", 0.55)
    get_mat("AC_EyeDark", "#251D29", 0.15)
    get_mat("AC_EyeShine", "#FFFFFF", 0.10, "#FFFFFF", 0.45)
    get_mat("AC_HairBrown", "#6B442A", 0.50)
    get_mat("AC_ShoeDark", "#4E3A4C", 0.55)


def clear_scene():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    MATERIALS.clear()


def ensure_collection(name):
    if name in bpy.data.collections:
        return bpy.data.collections[name]
    col = bpy.data.collections.new(name)
    bpy.context.scene.collection.children.link(col)
    return col


def link_obj(obj, col):
    for c in list(obj.users_collection):
        c.objects.unlink(obj)
    col.objects.link(obj)
    return obj


def add_mesh_obj(name, bm, mat, col, location=(0, 0, 0), rotation=(0, 0, 0), scale=(1, 1, 1)):
    mesh = bpy.data.meshes.new(name + "_Mesh")
    bm.normal_update()
    bm.to_mesh(mesh)
    bm.free()
    for p in mesh.polygons:
        p.use_smooth = True
    if mat is not None:
        mesh.materials.append(mat)
    obj = bpy.data.objects.new(name, mesh)
    obj.location = location
    obj.rotation_euler = rotation
    obj.scale = scale
    link_obj(obj, col)
    return obj


def make_box(name, size, mat, col, location=(0, 0, 0), rotation=(0, 0, 0), bevel=0.03, segs=2):
    bm = bmesh.new()
    bmesh.ops.create_cube(bm, size=1.0)
    bmesh.ops.scale(bm, vec=Vector(size), verts=bm.verts)
    if bevel > 0.0:
        b = min(bevel, min(size[0], size[1], size[2]) * 0.42)
        bmesh.ops.bevel(bm, geom=list(bm.edges), offset=b, segments=segs, affect="EDGES", clamp_overlap=True)
    return add_mesh_obj(name, bm, mat, col, location=location, rotation=rotation)


def make_cyl(name, r1, r2, depth, mat, col, location=(0, 0, 0), rotation=(0, 0, 0), segs=14, bevel=0.015):
    bm = bmesh.new()
    bmesh.ops.create_cone(bm, cap_ends=True, cap_tris=False, segments=segs, radius1=r1, radius2=r2, depth=depth)
    if bevel > 0.0 and min(r1, r2) > 0.01:
        cap_edges = [e for e in bm.edges if abs(e.verts[0].co.z - e.verts[1].co.z) < 1e-4]
        if cap_edges:
            bmesh.ops.bevel(bm, geom=cap_edges, offset=min(bevel, min(r1, r2, depth * 0.4) * 0.42), segments=2, affect="EDGES", clamp_overlap=True)
    return add_mesh_obj(name, bm, mat, col, location=location, rotation=rotation)


def make_sphere(name, radius, mat, col, location=(0, 0, 0), scale=(1, 1, 1), rotation=(0, 0, 0), u_segs=14, v_segs=10):
    bm = bmesh.new()
    bmesh.ops.create_uvsphere(bm, u_segments=u_segs, v_segments=v_segs, radius=radius)
    return add_mesh_obj(name, bm, mat, col, location=location, rotation=rotation, scale=scale)


def make_capsule(name, radius, height, mat, col, location=(0, 0, 0), rotation=(0, 0, 0), segs=12):
    cyl_h = max(0.01, height - 2.0 * radius)
    bm = bmesh.new()
    bmesh.ops.create_uvsphere(bm, u_segments=segs, v_segments=10, radius=radius)
    for v in bm.verts:
        if v.co.z > 0:
            v.co.z += cyl_h * 0.5
        elif v.co.z < 0:
            v.co.z -= cyl_h * 0.5
    return add_mesh_obj(name, bm, mat, col, location=location, rotation=rotation)


def join_and_export(parts, name, col):
    bpy.ops.object.select_all(action="DESELECT")
    for o in parts:
        o.select_set(True)
    bpy.context.view_layer.objects.active = parts[0]
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    if len(parts) > 1:
        bpy.ops.object.join()
    joined = bpy.context.view_layer.objects.active
    joined.name = name
    joined.data.name = name + "_Mesh"
    bpy.context.scene.cursor.location = (0, 0, 0)
    bpy.ops.object.origin_set(type="ORIGIN_CURSOR")
    link_obj(joined, col)

    filepath = os.path.join(MODELS_DIR, f"{name}.fbx")
    bpy.ops.export_scene.fbx(
        filepath=filepath,
        use_selection=True,
        global_scale=1.0,
        apply_unit_scale=True,
        apply_scale_options="FBX_SCALE_ALL",
        bake_space_transform=True,
        object_types={"MESH"},
        use_mesh_modifiers=True,
        mesh_smooth_type="FACE",
        axis_forward="-Z",
        axis_up="Y",
        add_leaf_bones=False,
        bake_anim=False,
    )
    print(f"[FBX Exported] {filepath}")
    return joined


# ==============================================================================
# 1. FLORIST INVITATION CARD (Chapter 2 Wind Gust Collectible!)
# ==============================================================================

def build_florist_card(col):
    parts = [
        # Cozy cream envelope card angled slightly off the ground
        make_box("CardBody", (0.58, 0.40, 0.045), MATERIALS["AC_Cream"], col, location=(0, 0, 0.24), rotation=(0.25, 0.14, 0.18), bevel=0.015),
        # Lavender envelope flap accent
        make_box("CardFlap", (0.52, 0.22, 0.02), MATERIALS["AC_Lavender"], col, location=(0, 0.06, 0.28), rotation=(0.25, 0.14, 0.18), bevel=0.008),
        # Wax seal heart/flower dot + golden glow sparkle!
        make_sphere("WaxSeal", 0.075, MATERIALS["AC_PinkBerry"], col, location=(0, -0.02, 0.28), scale=(1.0, 1.0, 0.45), u_segs=10, v_segs=8),
        make_sphere("CardGlint", 0.065, MATERIALS["AC_BulbWarm"], col, location=(0.24, -0.14, 0.42), u_segs=8, v_segs=6),
    ]
    return join_and_export(parts, "FloristCard", col)


# ==============================================================================
# 2. MARINA — THE FLORIST'S SISTER (Matching the Polaroid photo: coral dress, yellow bow & golden key!)
# ==============================================================================

def build_sister_marina(col):
    m_skin = MATERIALS["AC_SkinLight"]
    m_hair = MATERIALS["AC_HairBrown"]
    m_dress = MATERIALS["AC_Coral"]
    m_cream = MATERIALS["AC_Cream"]
    m_bow = MATERIALS["AC_SunnyYellow"]
    m_gold = MATERIALS["AC_Gold"]
    m_shoe = MATERIALS["AC_ShoeDark"]
    m_eye = MATERIALS["AC_EyeDark"]
    m_shine = MATERIALS["AC_EyeShine"]
    m_blush = MATERIALS["AC_CheekBlush"]

    parts = []
    for sx in (-0.15, 0.15):
        parts.append(make_sphere(f"Shoe_{sx}", 0.13, m_shoe, col, location=(sx, -0.04, 0.09), scale=(0.95, 1.32, 0.72), u_segs=12, v_segs=10))
        parts.append(make_capsule(f"Leg_{sx}", 0.09, 0.36, m_cream, col, location=(sx, 0, 0.28)))

    # Coral painter's dress + soft cream collar
    parts.append(make_sphere("Torso", 0.31, m_dress, col, location=(0, 0, 0.68), scale=(0.98, 0.86, 1.16), u_segs=14, v_segs=12))
    parts.append(make_cyl("SkirtFlare", 0.33, 0.25, 0.24, m_dress, col, location=(0, 0, 0.46), segs=14, bevel=0.03))
    parts.append(make_cyl("Collar", 0.22, 0.24, 0.09, m_cream, col, location=(0, 0, 0.90), segs=14, bevel=0.02))

    # Left arm resting, right arm holding the iconic Golden Pavilion Key!
    parts.append(make_capsule("ArmL", 0.088, 0.38, m_dress, col, location=(-0.28, 0.0, 0.68), rotation=(0, -0.26, 0)))
    parts.append(make_sphere("HandL", 0.085, m_skin, col, location=(-0.33, -0.02, 0.52), u_segs=10, v_segs=8))
    parts.append(make_capsule("ArmR", 0.088, 0.38, m_dress, col, location=(0.28, -0.08, 0.72), rotation=(0.45, 0.25, 0)))
    parts.append(make_sphere("HandR", 0.085, m_skin, col, location=(0.33, -0.22, 0.64), u_segs=10, v_segs=8))

    # Cute Golden Key in Marina's right hand!
    parts.append(make_cyl("KeyRing", 0.09, 0.09, 0.035, m_gold, col, location=(0.36, -0.32, 0.72), rotation=(math.pi * 0.5, 0, 0), segs=12, bevel=0.008))
    parts.append(make_cyl("KeyShaft", 0.025, 0.025, 0.24, m_gold, col, location=(0.36, -0.32, 0.55), segs=8, bevel=0.005))
    parts.append(make_box("KeyTooth", (0.09, 0.03, 0.07), m_gold, col, location=(0.40, -0.32, 0.46), bevel=0.008))

    # Head + face + warm brown wavy hair + sunny yellow side bow (matching the Polaroid photo!)
    hz = 1.24
    parts.append(make_sphere("Head", 0.36, m_skin, col, location=(0, 0, hz), scale=(1.04, 0.98, 0.96), u_segs=18, v_segs=14))
    for side in (-1, 1):
        parts.append(make_sphere(f"Ear_{side}", 0.085, m_skin, col, location=(side * 0.36, 0.02, hz - 0.02), scale=(0.65, 0.85, 1.0), u_segs=10, v_segs=8))
        parts.append(make_sphere(f"Eye_{side}", 0.062, m_eye, col, location=(side * 0.14, -0.32, hz + 0.03), scale=(0.85, 0.45, 1.15), u_segs=10, v_segs=8))
        parts.append(make_sphere(f"EyeShine_{side}", 0.022, m_shine, col, location=(side * 0.155, -0.352, hz + 0.055), u_segs=8, v_segs=6))
        parts.append(make_sphere(f"Cheek_{side}", 0.068, m_blush, col, location=(side * 0.21, -0.28, hz - 0.06), scale=(1.1, 0.35, 0.75), u_segs=8, v_segs=6))
    parts.append(make_sphere("Nose", 0.045, m_dress, col, location=(0, -0.35, hz - 0.03), scale=(1.1, 0.8, 0.8), u_segs=8, v_segs=6))

    parts.append(make_sphere("HairCap", 0.385, m_hair, col, location=(0, 0.04, hz + 0.06), scale=(1.04, 1.02, 0.96), u_segs=16, v_segs=12))
    parts.append(make_sphere("BangsL", 0.18, m_hair, col, location=(-0.14, -0.22, hz + 0.23), scale=(1.2, 0.75, 0.75), u_segs=10, v_segs=8))
    parts.append(make_sphere("BangsR", 0.18, m_hair, col, location=(0.14, -0.22, hz + 0.23), scale=(1.2, 0.75, 0.75), u_segs=10, v_segs=8))
    for side in (-1, 1):
        parts.append(make_sphere(f"HairLong_{side}", 0.18, m_hair, col, location=(side * 0.32, 0.10, hz - 0.14), scale=(0.85, 1.0, 1.5), u_segs=10, v_segs=8))
    # Sunny yellow hair bow!
    parts.append(make_sphere("BowCenter", 0.07, m_bow, col, location=(0.24, -0.18, hz + 0.28), u_segs=8, v_segs=6))
    parts.append(make_sphere("BowL", 0.11, m_bow, col, location=(0.14, -0.17, hz + 0.31), scale=(1.1, 0.55, 0.8), u_segs=8, v_segs=6))
    parts.append(make_sphere("BowR", 0.11, m_bow, col, location=(0.34, -0.17, hz + 0.26), scale=(1.1, 0.55, 0.8), u_segs=8, v_segs=6))

    return join_and_export(parts, "Char_SisterMarina", col)


# ==============================================================================
# 3. PAVILION FESTIVAL LONG TABLE (Mesa Comprida!)
# ==============================================================================

def build_pavilion_festival_table(col):
    m_wood = MATERIALS["AC_WoodWarm"]
    m_wood_l = MATERIALS["AC_WoodLight"]
    m_cream = MATERIALS["AC_Cream"]
    m_mint = MATERIALS["AC_MintSoft"]
    m_pink = MATERIALS["AC_PinkSoft"]

    parts = []
    # Long wooden communal table standing cleanly on the Pavilion mosaic floor (origin at floor z=0)
    parts.append(make_box("TableTop", (3.25, 1.24, 0.10), m_wood_l, col, location=(0, 0, 0.78), bevel=0.03))
    # Cozy draped pastel tablecloth + mint table runner
    parts.append(make_box("ClothTop", (3.32, 1.32, 0.18), m_cream, col, location=(0, 0, 0.77), bevel=0.05))
    parts.append(make_box("Runner", (3.36, 0.44, 0.20), m_mint, col, location=(0, 0, 0.775), bevel=0.04))
    # Cute scalloped overhang tabs along front
    for i in range(7):
        sx = -1.35 + i * 0.45
        parts.append(make_sphere(f"ClothScallop_{i}", 0.16, m_pink if i % 2 else m_cream, col, location=(sx, -0.65, 0.64), scale=(1.3, 0.35, 0.8), u_segs=10, v_segs=6))
    # Sturdy wooden legs
    for lx in (-1.36, 1.36):
        for ly in (-0.46, 0.46):
            parts.append(make_cyl(f"TLeg_{lx}_{ly}", 0.065, 0.065, 0.76, m_wood, col, location=(lx, ly, 0.38), segs=10))
    # Two cozy wooden benches alongside the long table!
    for by in (-0.98, 0.98):
        parts.append(make_box(f"TBench_{by}", (2.65, 0.38, 0.08), m_wood_l, col, location=(0, by, 0.40), bevel=0.025))
        for bx in (-1.05, 1.05):
            parts.append(make_box(f"TBenchLeg_{by}_{bx}", (0.10, 0.30, 0.40), m_wood, col, location=(bx, by, 0.20), bevel=0.02))

    return join_and_export(parts, "PavilionFestivalTable", col)


# ==============================================================================
# 4. PAVILION FESTIVAL LIGHTS & RESTORED SIGNBOARD (Luzes & Letreiro Restaurado!)
# ==============================================================================

def build_pavilion_festival_lights(col):
    m_bulb = MATERIALS["AC_BulbWarm"]
    m_glow = MATERIALS["AC_LanternGlow"]
    m_gold = MATERIALS["AC_Gold"]
    m_coral = MATERIALS["AC_Coral"]
    m_mint = MATERIALS["AC_MintTeal"]
    m_pink = MATERIALS["AC_PinkBerry"]
    m_yel = MATERIALS["AC_SunnyYellow"]
    m_plum = MATERIALS["AC_PurplePlum"]

    parts = []
    # Restored radiant signboard replacing the faded weathered one (aligned to PavilionBay origin!)
    parts.append(make_box("RestoredTrim", (3.32, 0.20, 0.84), m_gold, col, location=(0, -1.96, 3.38), bevel=0.05))
    parts.append(make_box("RestoredFace", (3.10, 0.24, 0.66), m_plum, col, location=(0, -1.98, 3.38), bevel=0.04))
    # Glowing marquee bulbs framing the restored signboard!
    for i in range(9):
        bx = -1.40 + i * 0.35
        parts.append(make_sphere(f"SignBulbTop_{i}", 0.055, m_bulb, col, location=(bx, -2.10, 3.74), u_segs=8, v_segs=6))
        parts.append(make_sphere(f"SignBulbBot_{i}", 0.055, m_bulb, col, location=(bx, -2.10, 3.02), u_segs=8, v_segs=6))

    # Draped festive glowing string lights & colorful pennants along the front & sides of the Pavilion eaves!
    pennant_mats = [m_pink, m_yel, m_mint, m_coral]
    for i in range(13):
        u = i / 12.0 - 0.5
        fx = u * 5.2
        sag = -0.34 * (1.0 - (2 * u) ** 2)
        parts.append(make_sphere(f"PavBulbF_{i}", 0.085, m_glow, col, location=(fx, -1.92, 2.74 + sag), u_segs=8, v_segs=6))
        if i % 2 == 0:
            parts.append(make_cyl(f"PavPenF_{i}", 0.01, 0.14, 0.26, pennant_mats[i % 4], col, location=(fx, -1.92, 2.54 + sag), segs=3, bevel=0.0))

    # Hanging glowing paper lanterns at the 4 corners of the Pavilion!
    for lx in (-2.15, 2.15):
        for ly in (-1.45, 1.45):
            parts.append(make_sphere(f"PaperLantern_{lx}_{ly}", 0.22, m_glow, col, location=(lx, ly, 2.38), scale=(1.0, 1.0, 1.15), u_segs=12, v_segs=10))
            parts.append(make_cyl(f"LanternCap_{lx}_{ly}", 0.12, 0.12, 0.06, m_coral, col, location=(lx, ly, 2.62), segs=10))

    return join_and_export(parts, "PavilionFestivalLights", col)


# ==============================================================================
# 5. FESTIVAL DISHES, FLOWER POTS & DECORATIONS (Trazidos pelos moradores!)
# ==============================================================================

def build_festival_dishes(col):
    m_cream = MATERIALS["AC_Cream"]
    m_white = MATERIALS["AC_FlowerWhite"]
    m_pot = MATERIALS["AC_Terracotta"]
    m_bush = MATERIALS["AC_BushGreen"]
    m_pink = MATERIALS["AC_PinkBerry"]
    m_yel = MATERIALS["AC_SunnyYellow"]
    m_mint = MATERIALS["AC_MintSoft"]
    m_or = MATERIALS["AC_Orange"]
    m_straw = MATERIALS["AC_Strawberry"]
    m_glow = MATERIALS["AC_LanternGlow"]
    m_lav = MATERIALS["AC_Lavender"]

    parts = []
    ty = 0.88  # Sits right on top of PavilionFestivalTable (z=0.87)

    # Centerpiece: Florist's festive bouquet pot + glowing table candle lantern!
    parts.append(make_cyl("CenterPot", 0.16, 0.22, 0.26, m_pot, col, location=(0, 0, ty + 0.12), segs=12))
    parts.append(make_sphere("CenterFoliage", 0.28, m_bush, col, location=(0, 0, ty + 0.34), scale=(1.1, 1.0, 0.85), u_segs=12, v_segs=10))
    for k, bm in enumerate([m_pink, m_yel, m_lav, m_white, m_pink]):
        ang = k * (math.tau / 5.0)
        parts.append(make_sphere(f"CenterBloom_{k}", 0.085, bm, col, location=(math.cos(ang) * 0.18, math.sin(ang) * 0.18, ty + 0.48), u_segs=8, v_segs=6))

    # Left side: Giant Ice Cream Sundae Bowl (brought by Nina & Protagonist!)
    parts.append(make_cyl("SundaeBowl", 0.14, 0.24, 0.18, m_mint, col, location=(-0.92, -0.05, ty + 0.08), segs=14))
    parts.append(make_sphere("SundaeScoop1", 0.14, m_yel, col, location=(-0.98, -0.05, ty + 0.22), u_segs=10, v_segs=8))
    parts.append(make_sphere("SundaeScoop2", 0.14, m_straw, col, location=(-0.84, -0.02, ty + 0.22), u_segs=10, v_segs=8))
    parts.append(make_sphere("SundaeScoop3", 0.12, m_cream, col, location=(-0.91, -0.05, ty + 0.34), u_segs=10, v_segs=8))
    parts.append(make_sphere("SundaeLeaf", 0.06, MATERIALS["AC_PalmLeaf"], col, location=(-0.86, -0.05, ty + 0.43), scale=(1.5, 0.7, 0.4), u_segs=8, v_segs=6))

    # Right side: Tropical Fruit Platter (brought by Beto & Clara from the Feira!)
    parts.append(make_cyl("FruitPlate", 0.30, 0.32, 0.05, m_white, col, location=(0.92, 0.02, ty + 0.02), segs=16))
    for k in range(5):
        ang = k * (math.tau / 5.0)
        parts.append(make_sphere(f"PlateFruit_{k}", 0.095, m_or if k % 2 == 0 else m_straw, col, location=(0.92 + math.cos(ang) * 0.14, 0.02 + math.sin(ang) * 0.14, ty + 0.10), u_segs=10, v_segs=8))
    parts.append(make_sphere("PlateTop", 0.10, m_yel, col, location=(0.92, 0.02, ty + 0.19), u_segs=10, v_segs=8))

    # Two warm table candle lanterns at far ends + pastel cake!
    for lx in (-1.36, 1.36):
        parts.append(make_cyl(f"TableLantern_{lx}", 0.09, 0.09, 0.22, m_glow, col, location=(lx, 0.15, ty + 0.11), segs=10))
    # Cute berry celebration cake!
    parts.append(make_cyl("CakeBase", 0.22, 0.22, 0.20, m_cream, col, location=(-0.44, 0.22, ty + 0.10), segs=14))
    parts.append(make_cyl("CakeIcing", 0.23, 0.23, 0.07, m_pink, col, location=(-0.44, 0.22, ty + 0.20), segs=14))
    parts.append(make_sphere("CakeBerry", 0.055, m_straw, col, location=(-0.44, 0.22, ty + 0.27), u_segs=8, v_segs=6))

    return join_and_export(parts, "FestivalDishesProp", col)


def render_preview(col):
    world = bpy.data.worlds.new("ChaptersPreviewWorld")
    bpy.context.scene.world = world
    nt = world.node_tree
    nt.nodes.clear()
    out = nt.nodes.new("ShaderNodeOutputWorld")
    bg = nt.nodes.new("ShaderNodeBackground")
    bg.inputs["Color"].default_value = hex_to_rgba("#F2C4A8")
    bg.inputs["Strength"].default_value = 0.95
    nt.links.new(bg.outputs["Background"], out.inputs["Surface"])

    sun_d = bpy.data.lights.new("Sun", type="SUN")
    sun_d.energy = 3.0
    sun_d.color = hex_to_rgba("#FFE5BC")[:3]
    sun_o = bpy.data.objects.new("Sun", sun_d)
    sun_o.rotation_euler = (math.radians(46), math.radians(15), math.radians(-26))
    link_obj(sun_o, col)

    cam_d = bpy.data.cameras.new("Cam")
    cam_d.lens = 38.0
    cam_o = bpy.data.objects.new("Cam", cam_d)
    cam_o.location = (0.0, -9.2, 4.4)
    direction = Vector((0.0, 0.0, 1.3)) - cam_o.location
    cam_o.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()
    link_obj(cam_o, col)
    bpy.context.scene.camera = cam_o

    scene = bpy.context.scene
    engines = [e.identifier for e in bpy.types.RenderSettings.bl_rna.properties["engine"].enum_items]
    scene.render.engine = "BLENDER_EEVEE_NEXT" if "BLENDER_EEVEE_NEXT" in engines else ("BLENDER_EEVEE" if "BLENDER_EEVEE" in engines else "CYCLES")
    scene.render.resolution_x = 1280
    scene.render.resolution_y = 720
    scene.render.image_settings.file_format = "PNG"
    scene.render.filepath = PREVIEW_OUT
    bpy.ops.render.render(write_still=True)


def main():
    clear_scene()
    init_palette()
    col = ensure_collection("BaiaDoAvesso_Chapters")

    card = build_florist_card(col)
    marina = build_sister_marina(col)
    table = build_pavilion_festival_table(col)
    lights = build_pavilion_festival_lights(col)
    dishes = build_festival_dishes(col)

    # Arrange for preview & .blend inspection
    marina.location = (-2.6, -1.2, 0.0)
    card.location = (-1.4, -1.8, 0.2)
    table.location = (0.8, 0.2, 0.0)
    dishes.location = (0.8, 0.2, 0.0)
    lights.location = (0.8, 0.8, 0.0)

    bpy.ops.wm.save_as_mainfile(filepath=BLEND_OUT)
    render_preview(col)
    print("[Baia Chapters Assets Complete]")


if __name__ == "__main__":
    main()
