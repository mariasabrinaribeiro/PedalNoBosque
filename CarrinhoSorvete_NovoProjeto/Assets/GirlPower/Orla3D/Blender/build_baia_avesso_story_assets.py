"""
build_baia_avesso_story_assets.py
---------------------------------
Blender 5.3 procedural 3D asset generator for the "Baía do Avesso" story & landmarks:
  1. PavilionBay.fbx            - Low pastel coastal pavilion, worn mosaic terrace, stone steps to beach, faded mystery signboard!
  2. KioskAwningUpgrade.fbx     - Volumetric striped fabric awning upgrade (mint/coral/cream + scallops + bunting)
  3. CartPaintedSignUpgrade.fbx - Hand-painted wooden crest sign upgrade with sculpted cone & floral details
  4. BasilPot.fbx               - Florist's gifted terracotta basil pot with lush glossy leaves (harvestable!)
  5. FloristCurtain.fbx         - Soft pastel fabric curtain that sways in the ocean breeze
  6. IceCreamConeLemonBasil.fbx - Lemon & fresh basil leaf ice cream cone held in hand!
  7. Char_FriendNina.fbx        - Amiga 1 (at Kiosk Girl Power, helps prepare orders)
  8. Char_FriendBia.fbx         - Amiga 2 (at the Square/Bifurcation, guides to the Feira shortcut)

Saves BaiaDoAvesso_StoryAssets.blend and renders baia_story_preview.png.
"""

import math
import os
import bpy
import bmesh
from mathutils import Vector

SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
ORLA3D_DIR = os.path.dirname(SCRIPT_DIR)
MODELS_DIR = os.path.join(ORLA3D_DIR, "Models")
BLEND_OUT = os.path.join(SCRIPT_DIR, "BaiaDoAvesso_StoryAssets.blend")
PREVIEW_OUT = os.path.join(SCRIPT_DIR, "baia_story_preview.png")

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


def get_mat(name, hex_color, roughness=0.52, emission_hex=None, emission_strength=0.0):
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
    # Core palette matching GirlPowerOrla3DSceneBuilder!
    get_mat("AC_WoodWarm", "#B97A4B", 0.66)
    get_mat("AC_WoodLight", "#D89E6A", 0.62)
    get_mat("AC_WoodDark", "#7B4C2F", 0.68)
    get_mat("AC_StoneWhite", "#F2ECE0", 0.72)
    get_mat("AC_StoneWarm", "#DFD3BE", 0.74)
    get_mat("AC_StoneDark", "#B8A994", 0.78)
    get_mat("AC_Cream", "#FFF6E5", 0.50)
    get_mat("AC_PinkBerry", "#F26E97", 0.48)
    get_mat("AC_PinkSoft", "#FFA9C2", 0.48)
    get_mat("AC_MintTeal", "#4EC7B6", 0.46)
    get_mat("AC_MintSoft", "#8FE3D6", 0.48)
    get_mat("AC_PurplePlum", "#5B3B6E", 0.50)
    get_mat("AC_Lavender", "#B89CE0", 0.52)
    get_mat("AC_Coral", "#F27B66", 0.50)
    get_mat("AC_SunnyYellow", "#FFD45E", 0.48)
    get_mat("AC_SkyBlue", "#6CC0F0", 0.48)
    get_mat("AC_Gold", "#F5BE4F", 0.34)
    get_mat("AC_Terracotta", "#D47551", 0.70)
    get_mat("AC_BushGreen", "#52B55E", 0.58)
    get_mat("AC_BushLight", "#7CD46E", 0.56)
    get_mat("AC_PalmLeaf", "#4EBF5C", 0.52)
    get_mat("AC_PalmLeafTip", "#7BE06C", 0.50)
    get_mat("AC_Waffle", "#E3A75B", 0.60)
    get_mat("AC_FlowerWhite", "#FFFDF7", 0.45)
    get_mat("AC_FlowerPink", "#FF7AA8", 0.48)
    get_mat("AC_FlowerYellow", "#FFE055", 0.48)
    get_mat("AC_BulbWarm", "#FFF0B3", 0.20, "#FFE082", 2.8)

    # Characters
    get_mat("AC_SkinWarm", "#C9855E", 0.56)
    get_mat("AC_SkinLight", "#F4C29F", 0.56)
    get_mat("AC_SkinTan", "#DF9F73", 0.56)
    get_mat("AC_CheekBlush", "#FF8EA6", 0.55)
    get_mat("AC_EyeDark", "#251D29", 0.15)
    get_mat("AC_EyeShine", "#FFFFFF", 0.10, "#FFFFFF", 0.45)
    get_mat("AC_HairBrown", "#6B442A", 0.50)
    get_mat("AC_HairDark", "#332A38", 0.48)
    get_mat("AC_HairAuburn", "#B85337", 0.48)
    get_mat("AC_Denim", "#4E78A8", 0.70)
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


def make_box(name, size, mat, col, location=(0, 0, 0), rotation=(0, 0, 0), bevel=0.04, segs=2):
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
# 1. PAVILHÃO DA BAÍA DO AVESSO (Low roof, worn mosaic, stone steps, faded sign!)
# ==============================================================================

def build_pavilion_bay(col):
    m_stone_w = MATERIALS["AC_StoneWhite"]
    m_stone = MATERIALS["AC_StoneWarm"]
    m_stone_d = MATERIALS["AC_StoneDark"]
    m_wood = MATERIALS["AC_WoodLight"]
    m_wood_d = MATERIALS["AC_WoodWarm"]
    m_mint = MATERIALS["AC_MintSoft"]
    m_lav = MATERIALS["AC_Lavender"]
    m_pink = MATERIALS["AC_PinkSoft"]
    m_cream = MATERIALS["AC_Cream"]
    m_gold = MATERIALS["AC_Gold"]

    parts = []
    # Main stone pavilion terrace (elevated slightly on the sand edge)
    parts.append(make_box("PavBase", (5.4, 4.0, 0.38), m_stone, col, location=(0, 0, 0.14), bevel=0.06))
    parts.append(make_box("PavRim", (5.6, 4.2, 0.12), m_stone_w, col, location=(0, 0, 0.34), bevel=0.04))

    # Worn pastel mosaic tiles arranged in a coastal starburst pattern on the floor!
    mosaic_mats = [m_mint, m_lav, m_pink, m_cream, m_stone_d]
    for gx in range(-4, 5):
        for gy in range(-2, 3):
            # Leave a few gaps to feel weathered/worn ("mosaico gasto")
            if (gx + gy * 3) % 7 == 0:
                continue
            mat_m = mosaic_mats[(abs(gx) + abs(gy)) % len(mosaic_mats)]
            parts.append(make_box(f"Mos_{gx}_{gy}", (0.44, 0.44, 0.03), mat_m, col, location=(gx * 0.50, gy * 0.52, 0.405), bevel=0.01))

    # Stone staircase descending toward the ocean/sand (+Y in Blender = +Z in Unity toward water, so high tide covers lower steps!)
    for step in range(4):
        sy = 2.30 + step * 0.52
        sz = 0.24 - step * 0.13
        parts.append(make_box(f"SeaStep_{step}", (2.4, 0.56, 0.18), m_stone_w if step % 2 == 0 else m_stone, col, location=(0, sy, sz), bevel=0.035))
    # Lower mosaic landing right at the bottom of the steps (revealed at low tide, covered by foam at high tide!)
    parts.append(make_box("LowTideMosaic", (2.8, 1.5, 0.08), m_stone, col, location=(0, 4.55, -0.26), bevel=0.03))
    for mx in (-0.8, 0.0, 0.8):
        for my in (4.2, 4.85):
            parts.append(make_box(f"LowMos_{mx}_{my}", (0.58, 0.48, 0.03), m_mint if (mx == 0) else m_pink, col, location=(mx, my, -0.21), bevel=0.01))

    # Cozy stone steps on the south side (-Y) so player walks smoothly up from the square!
    parts.append(make_box("SouthStep1", (2.6, 0.58, 0.16), m_stone_w, col, location=(0, -2.25, 0.18), bevel=0.03))
    parts.append(make_box("SouthStep2", (2.8, 0.58, 0.14), m_stone, col, location=(0, -2.75, 0.06), bevel=0.03))

    # 6 charming rounded coastal columns supporting the low roof
    for cx in (-2.25, 0.0, 2.25):
        for cy in (-1.55, 1.55):
            if cx == 0.0 and cy < 0:
                continue  # Keep front entrance wide open!
            parts.append(make_cyl(f"ColBase_{cx}_{cy}", 0.22, 0.18, 0.24, m_stone_w, col, location=(cx, cy, 0.50), segs=12))
            parts.append(make_cyl(f"Col_{cx}_{cy}", 0.14, 0.13, 2.25, m_cream, col, location=(cx, cy, 1.72), segs=12))

    # Low wooden beams & gentle pitched pastel roof ("telhado baixo")
    parts.append(make_box("RoofRim", (5.5, 4.1, 0.18), m_wood_d, col, location=(0, 0, 2.86), bevel=0.04))
    parts.append(make_box("RoofFront", (5.8, 2.35, 0.18), m_mint, col, location=(0, -1.08, 3.16), rotation=(0.24, 0, 0), bevel=0.05))
    parts.append(make_box("RoofBack", (5.8, 2.35, 0.18), m_mint, col, location=(0, 1.08, 3.16), rotation=(-0.24, 0, 0), bevel=0.05))
    parts.append(make_box("RoofRidge", (5.86, 0.26, 0.20), m_cream, col, location=(0, 0, 3.46), bevel=0.05))

    # Weathered / faded mystery signboard visible from the square ("letreiro apagado")!
    parts.append(make_box("FadedSignBoard", (3.1, 0.16, 0.68), m_stone_d, col, location=(0, -1.96, 3.36), rotation=(0, 0.04, 0), bevel=0.04))
    parts.append(make_box("FadedSignTrim", (3.22, 0.12, 0.78), m_wood_d, col, location=(0, -1.92, 3.36), rotation=(0, 0.04, 0), bevel=0.03))
    # Subtle weathered seashell emblem on the old signboard
    parts.append(make_sphere("FadedShell", 0.18, m_stone_w, col, location=(-1.18, -2.05, 3.36), scale=(1.0, 0.35, 0.9), u_segs=10, v_segs=8))
    parts.append(make_sphere("FadedCone", 0.16, m_gold, col, location=(1.18, -2.05, 3.36), scale=(0.8, 0.35, 1.1), u_segs=10, v_segs=8))

    # Locked wooden storage chest inside the pavilion (hinting at the sister's key!)
    parts.append(make_box("OldChest", (0.92, 0.56, 0.50), m_wood_d, col, location=(1.55, 0.95, 0.66), bevel=0.04))
    parts.append(make_box("ChestLock", (0.16, 0.08, 0.20), m_gold, col, location=(1.55, 0.65, 0.72), bevel=0.02))

    return join_and_export(parts, "PavilionBay", col)


# ==============================================================================
# 2. KIOSK AWNING UPGRADE (Volumetric festive striped fabric awning!)
# ==============================================================================

def build_kiosk_awning_upgrade(col):
    m_mint = MATERIALS["AC_MintTeal"]
    m_coral = MATERIALS["AC_Coral"]
    m_cream = MATERIALS["AC_Cream"]
    m_gold = MATERIALS["AC_Gold"]
    m_bulb = MATERIALS["AC_BulbWarm"]
    m_yel = MATERIALS["AC_SunnyYellow"]
    m_pink = MATERIALS["AC_PinkSoft"]

    parts = []
    # Rich volumetric billowing fabric stripes resting snugly over the Kiosk roof (centered at kiosk local origin)
    num = 10
    total_w = 4.78
    sw = total_w / num
    mats = [m_mint, m_cream, m_coral, m_cream]
    for i in range(num):
        sx = -total_w * 0.5 + (i + 0.5) * sw
        mat_s = mats[i % len(mats)]
        # Billowing curved awning puff
        parts.append(make_box(f"UpAwning_{i}", (sw * 1.02, 2.70, 0.26), mat_s, col, location=(sx, -0.16, 3.06), rotation=(0.25, 0, 0), bevel=0.08))
        # Plump rounded scalloped front ruffle
        parts.append(make_sphere(f"UpScallop_{i}", sw * 0.54, mat_s, col, location=(sx, -1.42, 2.56), scale=(1.0, 0.55, 1.15), u_segs=12, v_segs=8))
        # Cute golden tassel bead under every second scallop
        if i % 2 == 0:
            parts.append(make_sphere(f"Tassel_{i}", 0.075, m_gold, col, location=(sx, -1.44, 2.26), u_segs=8, v_segs=6))
        else:
            parts.append(make_sphere(f"UpBulb_{i}", 0.085, m_bulb, col, location=(sx, -1.38, 2.34), u_segs=8, v_segs=6))

    # Festive triangular pennant garland along the upper awning edge!
    pennant_mats = [m_yel, m_pink, m_mint, m_coral]
    for i in range(9):
        px = -1.92 + i * 0.48
        parts.append(make_cyl(f"UpPennant_{i}", 0.01, 0.14, 0.26, pennant_mats[i % 4], col, location=(px, -1.52, 2.52), segs=3, bevel=0.0))

    return join_and_export(parts, "KioskAwningUpgrade", col)


# ==============================================================================
# 3. CART PAINTED SIGN UPGRADE (Hand-painted coastal crest sign!)
# ==============================================================================

def build_cart_painted_sign_upgrade(col):
    m_wood = MATERIALS["AC_WoodLight"]
    m_cream = MATERIALS["AC_Cream"]
    m_mint = MATERIALS["AC_MintTeal"]
    m_pink = MATERIALS["AC_PinkBerry"]
    m_yel = MATERIALS["AC_SunnyYellow"]
    m_waffle = MATERIALS["AC_Waffle"]
    m_leaf = MATERIALS["AC_PalmLeaf"]
    m_gold = MATERIALS["AC_Gold"]

    parts = []
    # Ornate curved wooden sign frame mounted on the front/top of the ice cream cart!
    parts.append(make_box("SignBack", (1.68, 0.12, 0.62), m_gold, col, location=(0, 0, 0), bevel=0.04))
    parts.append(make_box("SignPaintBase", (1.52, 0.15, 0.48), m_cream, col, location=(0, -0.02, 0), bevel=0.03))
    # Painted pastel wave stripes on the sign!
    parts.append(make_box("WaveMint", (1.42, 0.05, 0.10), m_mint, col, location=(0, -0.09, -0.14), bevel=0.015))
    parts.append(make_box("WavePink", (1.42, 0.05, 0.08), m_pink, col, location=(0, -0.09, 0.14), bevel=0.015))
    # Sculpted painted lemon-basil cone emblem in center + cute stars on sides!
    parts.append(make_cyl("SignCone", 0.02, 0.10, 0.22, m_waffle, col, location=(-0.54, -0.11, -0.04), segs=10, bevel=0.005))
    parts.append(make_sphere("SignScoop", 0.11, m_yel, col, location=(-0.54, -0.11, 0.10), u_segs=10, v_segs=8))
    parts.append(make_sphere("SignLeaf", 0.06, m_leaf, col, location=(-0.47, -0.13, 0.18), scale=(1.4, 0.5, 0.8), u_segs=8, v_segs=6))
    parts.append(make_sphere("SignFlowerR", 0.10, m_pink, col, location=(0.56, -0.11, 0.02), scale=(1.0, 0.45, 1.0), u_segs=10, v_segs=8))
    parts.append(make_sphere("SignFlowerC", 0.05, m_yel, col, location=(0.56, -0.14, 0.02), u_segs=8, v_segs=6))
    # Wooden mounting brackets
    for sx in (-0.52, 0.52):
        parts.append(make_cyl(f"Mount_{sx}", 0.04, 0.04, 0.34, m_wood, col, location=(sx, 0.04, -0.34), segs=8, bevel=0.01))

    return join_and_export(parts, "CartPaintedSignUpgrade", col)


# ==============================================================================
# 4. BASIL POT (Florist's living gift beside the cart!)
# ==============================================================================

def build_basil_pot(col):
    m_pot = MATERIALS["AC_Terracotta"]
    m_soil = MATERIALS["AC_WoodDark"]
    m_leaf1 = MATERIALS["AC_PalmLeaf"]
    m_leaf2 = MATERIALS["AC_BushLight"]
    m_flower = MATERIALS["AC_FlowerWhite"]
    m_ribbon = MATERIALS["AC_PinkSoft"]

    parts = []
    parts.append(make_cyl("PotBody", 0.20, 0.28, 0.38, m_pot, col, location=(0, 0, 0.19), segs=14, bevel=0.025))
    parts.append(make_cyl("PotRim", 0.30, 0.31, 0.09, m_pot, col, location=(0, 0, 0.37), segs=14, bevel=0.02))
    parts.append(make_cyl("PotRibbon", 0.27, 0.28, 0.06, m_ribbon, col, location=(0, 0, 0.26), segs=14, bevel=0.01))
    parts.append(make_cyl("PotSoil", 0.26, 0.26, 0.05, m_soil, col, location=(0, 0, 0.38), segs=12, bevel=0.01))

    # Lush cluster of glossy rounded basil leaves!
    leaf_specs = [
        (0.0, 0.0, 0.62, 0.16, (1.1, 0.85, 0.6), (0.2, 0.1, 0.0), m_leaf2),
        (-0.15, -0.10, 0.54, 0.14, (1.3, 0.85, 0.5), (0.3, -0.4, 0.6), m_leaf1),
        (0.16, -0.09, 0.55, 0.14, (1.3, 0.85, 0.5), (0.3, 0.4, -0.6), m_leaf1),
        (-0.12, 0.14, 0.56, 0.14, (1.2, 0.85, 0.5), (-0.3, -0.3, -0.5), m_leaf2),
        (0.14, 0.13, 0.54, 0.13, (1.2, 0.85, 0.5), (-0.3, 0.3, 0.5), m_leaf1),
        (0.0, -0.18, 0.48, 0.12, (1.2, 0.8, 0.5), (0.5, 0.0, 0.0), m_leaf2),
        (0.0, 0.02, 0.74, 0.12, (1.0, 0.75, 0.6), (0.1, 0.2, 0.4), m_leaf2),
    ]
    for i, (x, y, z, r, sc, rot, mat) in enumerate(leaf_specs):
        parts.append(make_sphere(f"BasilLeaf_{i}", r, mat, col, location=(x, y, z), scale=sc, rotation=rot, u_segs=10, v_segs=8))
    # Tiny delicate white basil blossoms on top
    parts.append(make_sphere("BasilBloom1", 0.045, m_flower, col, location=(0.04, -0.03, 0.84), u_segs=8, v_segs=6))
    parts.append(make_sphere("BasilBloom2", 0.038, m_flower, col, location=(-0.06, 0.04, 0.80), u_segs=8, v_segs=6))

    return join_and_export(parts, "BasilPot", col)


# ==============================================================================
# 5. FLORIST BREEZE CURTAIN (Soft pastel curtain for the Florist Stand!)
# ==============================================================================

def build_florist_curtain(col):
    m_cream = MATERIALS["AC_Cream"]
    m_lav = MATERIALS["AC_Lavender"]
    parts = []
    # 5 soft pleated fabric strips hanging from origin z=0 down to z=-1.35 (so rotating around X swings gently in the wind!)
    for i in range(5):
        cx = -0.32 + i * 0.16
        mat_c = m_cream if i % 2 == 0 else m_lav
        parts.append(make_box(f"Pleat_{i}", (0.17, 0.06, 1.32), mat_c, col, location=(cx, math.sin(i * 1.1) * 0.04, -0.66), bevel=0.025))
    return join_and_export(parts, "FloristCurtain", col)


# ==============================================================================
# 6. LEMON & BASIL ICE CREAM CONE PROP (Held during first delivery!)
# ==============================================================================

def build_lemon_basil_cone(col):
    parts = [
        make_cyl("ConeWaffle", 0.018, 0.105, 0.25, MATERIALS["AC_Waffle"], col, location=(0, 0, 0.125), segs=12, bevel=0.005),
        make_sphere("LemonScoop1", 0.12, MATERIALS["AC_SunnyYellow"], col, location=(0, 0, 0.29), u_segs=12, v_segs=10),
        make_sphere("LemonScoop2", 0.09, MATERIALS["AC_Cream"], col, location=(0.02, -0.01, 0.39), u_segs=10, v_segs=8),
        # Fresh green basil leaf garnish tucked right into the lemon scoop!
        make_sphere("BasilGarnish1", 0.055, MATERIALS["AC_PalmLeaf"], col, location=(0.08, -0.04, 0.43), scale=(1.5, 0.65, 0.45), rotation=(0.3, -0.5, 0.4), u_segs=8, v_segs=6),
        make_sphere("BasilGarnish2", 0.045, MATERIALS["AC_BushLight"], col, location=(-0.05, -0.05, 0.42), scale=(1.3, 0.65, 0.45), rotation=(0.2, 0.5, -0.4), u_segs=8, v_segs=6),
    ]
    return join_and_export(parts, "IceCreamConeLemonBasil", col)


# ==============================================================================
# 7 & 8. THE TWO FRIENDS: NINA (at Kiosk) & BIA (at Square/Shortcut)
# ==============================================================================

def build_friend_character(col, name, skin_mat, hair_mat, shirt_mat, pants_mat, accent_mat, style="nina"):
    m_eye = MATERIALS["AC_EyeDark"]
    m_shine = MATERIALS["AC_EyeShine"]
    m_blush = MATERIALS["AC_CheekBlush"]
    m_shoe = MATERIALS["AC_ShoeDark"]
    m_cream = MATERIALS["AC_Cream"]

    parts = []
    # Shoes & legs
    for sx in (-0.15, 0.15):
        parts.append(make_sphere(f"Shoe_{sx}", 0.13, m_shoe, col, location=(sx, -0.04, 0.09), scale=(0.95, 1.32, 0.72), u_segs=12, v_segs=10))
        parts.append(make_capsule(f"Leg_{sx}", 0.09, 0.36, pants_mat, col, location=(sx, 0, 0.28)))

    # Cozy torso
    parts.append(make_sphere("Torso", 0.30, shirt_mat, col, location=(0, 0, 0.68), scale=(0.96, 0.84, 1.15), u_segs=14, v_segs=12))

    if style == "nina":
        # Mint ice-cream helper apron + little waving right arm!
        parts.append(make_box("NinaApron", (0.34, 0.10, 0.42), accent_mat, col, location=(0, -0.22, 0.60), bevel=0.04))
        parts.append(make_capsule("ArmL", 0.088, 0.38, shirt_mat, col, location=(-0.28, 0.0, 0.68), rotation=(0, -0.26, 0)))
        parts.append(make_sphere("HandL", 0.085, skin_mat, col, location=(-0.33, -0.02, 0.52), u_segs=10, v_segs=8))
        # Cheerful waving hand!
        parts.append(make_capsule("ArmR", 0.088, 0.38, shirt_mat, col, location=(0.32, -0.04, 0.86), rotation=(0, 2.35, 0)))
        parts.append(make_sphere("HandR", 0.085, skin_mat, col, location=(0.44, -0.06, 1.00), u_segs=10, v_segs=8))
    else:
        # Bia: energetic vest scarf + pointing arm toward the Feira shortcut!
        parts.append(make_cyl("BiaScarf", 0.24, 0.26, 0.12, accent_mat, col, location=(0, 0, 0.90), segs=14, bevel=0.03))
        parts.append(make_capsule("ArmL", 0.088, 0.38, shirt_mat, col, location=(-0.28, 0.0, 0.68), rotation=(0, -0.26, 0)))
        parts.append(make_sphere("HandL", 0.085, skin_mat, col, location=(-0.33, -0.02, 0.52), u_segs=10, v_segs=8))
        # Pointing toward +X (the Feira shortcut!)
        parts.append(make_capsule("ArmR", 0.088, 0.40, shirt_mat, col, location=(0.36, -0.02, 0.78), rotation=(0, 1.45, 0)))
        parts.append(make_sphere("HandR", 0.085, skin_mat, col, location=(0.54, -0.02, 0.79), u_segs=10, v_segs=8))

    # Head + face
    hz = 1.24
    parts.append(make_sphere("Head", 0.36, skin_mat, col, location=(0, 0, hz), scale=(1.04, 0.98, 0.96), u_segs=18, v_segs=14))
    for side in (-1, 1):
        parts.append(make_sphere(f"Ear_{side}", 0.085, skin_mat, col, location=(side * 0.36, 0.02, hz - 0.02), scale=(0.65, 0.85, 1.0), u_segs=10, v_segs=8))
        parts.append(make_sphere(f"Eye_{side}", 0.062, m_eye, col, location=(side * 0.14, -0.32, hz + 0.03), scale=(0.85, 0.45, 1.15), u_segs=10, v_segs=8))
        parts.append(make_sphere(f"EyeShine_{side}", 0.022, m_shine, col, location=(side * 0.155, -0.352, hz + 0.055), u_segs=8, v_segs=6))
        parts.append(make_sphere(f"Cheek_{side}", 0.068, m_blush, col, location=(side * 0.21, -0.28, hz - 0.06), scale=(1.1, 0.35, 0.75), u_segs=8, v_segs=6))
    parts.append(make_sphere("Nose", 0.045, MATERIALS["AC_Coral"], col, location=(0, -0.35, hz - 0.03), scale=(1.1, 0.8, 0.8), u_segs=8, v_segs=6))

    # Distinct cute hairstyles!
    parts.append(make_sphere("HairCap", 0.385, hair_mat, col, location=(0, 0.04, hz + 0.06), scale=(1.04, 1.02, 0.96), u_segs=16, v_segs=12))
    parts.append(make_sphere("BangsL", 0.18, hair_mat, col, location=(-0.14, -0.22, hz + 0.23), scale=(1.2, 0.75, 0.75), u_segs=10, v_segs=8))
    parts.append(make_sphere("BangsR", 0.18, hair_mat, col, location=(0.14, -0.22, hz + 0.23), scale=(1.2, 0.75, 0.75), u_segs=10, v_segs=8))
    if style == "nina":
        # Cute top hair bun with mint scrunchie!
        parts.append(make_sphere("TopBun", 0.20, hair_mat, col, location=(0, 0.08, hz + 0.46), u_segs=12, v_segs=10))
        parts.append(make_cyl("Scrunchie", 0.16, 0.16, 0.07, accent_mat, col, location=(0, 0.08, hz + 0.36), segs=12, bevel=0.02))
    else:
        # Twin pigtails + sunny headband!
        for side in (-1, 1):
            parts.append(make_sphere(f"Pigtail_{side}", 0.18, hair_mat, col, location=(side * 0.42, 0.08, hz + 0.06), scale=(0.95, 0.95, 1.25), u_segs=10, v_segs=8))
        parts.append(make_cyl("Headband", 0.39, 0.39, 0.07, m_cream, col, location=(0, 0.0, hz + 0.22), segs=18, bevel=0.015))

    return join_and_export(parts, name, col)


def render_preview(col):
    world = bpy.data.worlds.new("StoryPreviewWorld")
    bpy.context.scene.world = world
    nt = world.node_tree
    nt.nodes.clear()
    out = nt.nodes.new("ShaderNodeOutputWorld")
    bg = nt.nodes.new("ShaderNodeBackground")
    bg.inputs["Color"].default_value = hex_to_rgba("#F7D4B6")
    bg.inputs["Strength"].default_value = 1.0
    nt.links.new(bg.outputs["Background"], out.inputs["Surface"])

    sun_d = bpy.data.lights.new("Sun", type="SUN")
    sun_d.energy = 3.2
    sun_d.color = hex_to_rgba("#FFEAC2")[:3]
    sun_o = bpy.data.objects.new("Sun", sun_d)
    sun_o.rotation_euler = (math.radians(46), math.radians(15), math.radians(-28))
    link_obj(sun_o, col)

    cam_d = bpy.data.cameras.new("Cam")
    cam_d.lens = 38.0
    cam_o = bpy.data.objects.new("Cam", cam_d)
    cam_o.location = (0.0, -11.5, 5.2)
    direction = Vector((0.0, 0.5, 1.4)) - cam_o.location
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
    col = ensure_collection("BaiaDoAvesso_Story")

    pav = build_pavilion_bay(col)
    awning = build_kiosk_awning_upgrade(col)
    sign = build_cart_painted_sign_upgrade(col)
    basil = build_basil_pot(col)
    curtain = build_florist_curtain(col)
    cone = build_lemon_basil_cone(col)
    nina = build_friend_character(
        col, "Char_FriendNina",
        MATERIALS["AC_SkinTan"], MATERIALS["AC_HairDark"],
        MATERIALS["AC_SunnyYellow"], MATERIALS["AC_Denim"],
        MATERIALS["AC_MintSoft"], style="nina"
    )
    bia = build_friend_character(
        col, "Char_FriendBia",
        MATERIALS["AC_SkinLight"], MATERIALS["AC_HairAuburn"],
        MATERIALS["AC_Lavender"], MATERIALS["AC_Cream"],
        MATERIALS["AC_SunnyYellow"], style="bia"
    )

    # Arrange nicely for visual preview & .blend file inspection!
    pav.location = (0.0, 2.5, 0.0)
    nina.location = (-3.8, -1.5, 0.0)
    bia.location = (-2.2, -1.5, 0.0)
    basil.location = (-0.8, -2.0, 0.0)
    cone.location = (0.2, -2.0, 0.6)
    sign.location = (1.8, -2.0, 0.9)
    curtain.location = (3.6, -1.2, 2.0)
    awning.location = (5.0, 2.0, 0.0)

    bpy.ops.wm.save_as_mainfile(filepath=BLEND_OUT)
    render_preview(col)
    print("[Baia do Avesso Story Assets Complete]")


if __name__ == "__main__":
    main()
