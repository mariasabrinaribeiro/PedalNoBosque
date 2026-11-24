"""
build_orla_expansion_assets.py
------------------------------
Blender 5.3 procedural 3D asset generator for the Baía do Avesso Expansion:
  1. Char_Protagonist.fbx    - FIXED 100% UPRIGHT articulated protagonist (bake_space_transform=True + clean Y-up joints!)
  2. MarketStall_Plantas.fbx - Natural Plants & Succulents stall
  3. MarketStall_Sushi.fbx   - Cozy Sushi bar stall with red lanterns & sushi platters
  4. MarketStall_Comida.fbx  - Coastal Food & Snacks stall (burgers, pastels, acai, cakes)
  5. MarketStall_Roupas.fbx  - Beachwear & Clothes stall with colorful clothing rack
  6. PetDog.fbx              - Adorable Animal Crossing puppy companion ("Pipoca")
  7. PetCat.fbx              - Adorable Animal Crossing kitty companion ("Mingau")
  8. Surfboard.fbx           - Colorful pastel tropical surfboard for riding ocean waves!
  9. CoastalHouse.fbx        - Charming Animal Crossing coastal house with paintable wall & roof material slots!

Saves BaiaDoAvesso_ExpansionAssets.blend and renders baia_expansion_preview.png.
"""

import math
import os
import bpy
import bmesh
from mathutils import Vector

SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
ORLA3D_DIR = os.path.dirname(SCRIPT_DIR)
MODELS_DIR = os.path.join(ORLA3D_DIR, "Models")
BLEND_OUT = os.path.join(SCRIPT_DIR, "BaiaDoAvesso_ExpansionAssets.blend")
PREVIEW_OUT = os.path.join(SCRIPT_DIR, "baia_expansion_preview.png")

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
    get_mat("AC_StoneWhite", "#F2ECE0", 0.70)
    get_mat("AC_Cream", "#FFF6E5", 0.48)
    get_mat("AC_FlowerWhite", "#FFFDF7", 0.45)
    get_mat("AC_FlowerPink", "#FF7AA8", 0.48)
    get_mat("AC_PinkBerry", "#F26E97", 0.48)
    get_mat("AC_PinkSoft", "#FFA9C2", 0.48)
    get_mat("AC_MintTeal", "#4EC7B6", 0.46)
    get_mat("AC_MintSoft", "#8FE3D6", 0.48)
    get_mat("AC_PurplePlum", "#5B3B6E", 0.50)
    get_mat("AC_Lavender", "#B89CE0", 0.50)
    get_mat("AC_Coral", "#F27B66", 0.48)
    get_mat("AC_SunnyYellow", "#FFD45E", 0.48)
    get_mat("AC_SkyBlue", "#6CC0F0", 0.48)
    get_mat("AC_Gold", "#F5BE4F", 0.36)
    get_mat("AC_Terracotta", "#D47551", 0.70)
    get_mat("AC_BushGreen", "#52B55E", 0.58)
    get_mat("AC_BushLight", "#7CD46E", 0.56)
    get_mat("AC_PalmLeaf", "#4EBF5C", 0.52)
    get_mat("AC_Orange", "#FF9333", 0.44)
    get_mat("AC_Strawberry", "#FF6E8D", 0.42)
    get_mat("AC_Chocolate", "#6E422C", 0.45)
    get_mat("AC_Waffle", "#E3A75B", 0.60)
    get_mat("AC_Glass", "#B8F0FA", 0.12)
    get_mat("AC_BulbWarm", "#FFF0B3", 0.20, "#FFE082", 2.8)
    get_mat("AC_LanternGlow", "#FFE69C", 0.22, "#FFD26B", 3.0)
    get_mat("AC_MetalDark", "#3F4959", 0.35)
    get_mat("AC_MetalSilver", "#D4DCE6", 0.25)

    # Protagonist & Pet colors
    get_mat("AC_SkinWarm", "#C9855E", 0.56)
    get_mat("AC_CheekBlush", "#FF8EA6", 0.55)
    get_mat("AC_EyeDark", "#251D29", 0.15)
    get_mat("AC_EyeShine", "#FFFFFF", 0.10, "#FFFFFF", 0.45)
    get_mat("AC_HairPlum", "#723678", 0.48)
    get_mat("AC_Denim", "#4E78A8", 0.70)
    get_mat("AC_ShoeDark", "#4E3A4C", 0.55)

    # Dedicated Paintable House Material Slots!
    get_mat("AC_HouseWall", "#FFF2DB", 0.58)
    get_mat("AC_HouseRoof", "#F27B66", 0.54)
    get_mat("AC_ProtagShirt", "#F26E97", 0.48)


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
# 1. FIXED 100% UPRIGHT PROTAGONIST (Single baked mesh -> guaranteed upright in Unity!)
#    Plus separate AC_ProtagShirt material slot so buying clothes changes her outfit color!
# ==============================================================================

def build_upright_protagonist(col):
    m_skin = MATERIALS["AC_SkinWarm"]
    m_hair = MATERIALS["AC_HairPlum"]
    m_shirt = MATERIALS["AC_ProtagShirt"]
    m_pants = MATERIALS["AC_Denim"]
    m_shoe = MATERIALS["AC_ShoeDark"]
    m_eye = MATERIALS["AC_EyeDark"]
    m_shine = MATERIALS["AC_EyeShine"]
    m_blush = MATERIALS["AC_CheekBlush"]
    m_yel = MATERIALS["AC_SunnyYellow"]
    m_coral = MATERIALS["AC_Coral"]

    parts = []
    # Shoes & legs
    for sx in (-0.15, 0.15):
        parts.append(make_sphere(f"Shoe_{sx}", 0.13, m_shoe, col, location=(sx, -0.04, 0.09), scale=(0.95, 1.32, 0.72), u_segs=12, v_segs=10))
        parts.append(make_capsule(f"Leg_{sx}", 0.09, 0.36, m_pants, col, location=(sx, 0, 0.28)))

    # Torso & star emblem
    parts.append(make_sphere("Torso", 0.30, m_shirt, col, location=(0, 0, 0.68), scale=(0.96, 0.84, 1.15), u_segs=14, v_segs=12))
    parts.append(make_sphere("StarEmblem", 0.09, m_yel, col, location=(0, -0.24, 0.72), scale=(1.0, 0.35, 1.0), u_segs=10, v_segs=8))

    # Arms nestled naturally
    for side in (-1, 1):
        parts.append(make_capsule(f"Arm_{side}", 0.088, 0.38, m_shirt, col, location=(side * 0.28, 0.0, 0.68), rotation=(0, side * 0.26, 0)))
        parts.append(make_sphere(f"Hand_{side}", 0.085, m_skin, col, location=(side * 0.33, -0.02, 0.52), u_segs=10, v_segs=8))

    # Big cute rounded head
    hz = 1.24
    parts.append(make_sphere("Head", 0.36, m_skin, col, location=(0, 0, hz), scale=(1.04, 0.98, 0.96), u_segs=18, v_segs=14))
    for side in (-1, 1):
        parts.append(make_sphere(f"Ear_{side}", 0.085, m_skin, col, location=(side * 0.36, 0.02, hz - 0.02), scale=(0.65, 0.85, 1.0), u_segs=10, v_segs=8))
        parts.append(make_sphere(f"Eye_{side}", 0.062, m_eye, col, location=(side * 0.14, -0.32, hz + 0.03), scale=(0.85, 0.45, 1.15), u_segs=10, v_segs=8))
        parts.append(make_sphere(f"EyeShine_{side}", 0.022, m_shine, col, location=(side * 0.155, -0.352, hz + 0.055), u_segs=8, v_segs=6))
        parts.append(make_sphere(f"Cheek_{side}", 0.068, m_blush, col, location=(side * 0.21, -0.28, hz - 0.06), scale=(1.1, 0.35, 0.75), u_segs=8, v_segs=6))
    parts.append(make_sphere("Nose", 0.045, m_coral, col, location=(0, -0.35, hz - 0.03), scale=(1.1, 0.8, 0.8), u_segs=8, v_segs=6))
    parts.append(make_sphere("HairCap", 0.385, m_hair, col, location=(0, 0.04, hz + 0.06), scale=(1.04, 1.02, 0.96), u_segs=16, v_segs=12))
    parts.append(make_sphere("BangsL", 0.18, m_hair, col, location=(-0.14, -0.22, hz + 0.23), scale=(1.2, 0.75, 0.75), u_segs=10, v_segs=8))
    parts.append(make_sphere("BangsR", 0.18, m_hair, col, location=(0.14, -0.22, hz + 0.23), scale=(1.2, 0.75, 0.75), u_segs=10, v_segs=8))
    for side in (-1, 1):
        parts.append(make_sphere(f"BobSide_{side}", 0.17, m_hair, col, location=(side * 0.31, 0.05, hz - 0.10), scale=(0.8, 1.0, 1.35), u_segs=10, v_segs=8))
    parts.append(make_sphere("HairClip", 0.075, m_yel, col, location=(0.24, -0.24, hz + 0.24), scale=(1.1, 0.5, 0.9), u_segs=8, v_segs=6))

    return join_and_export(parts, "Char_Protagonist", col)


# ==============================================================================
# 2-5. FOUR NEW THEMED MARKET STALLS (Plantas, Sushi, Comida, Roupas)
# ==============================================================================

def build_stall_shell(parts, col, stripe_mat):
    m_wood = MATERIALS["AC_WoodWarm"]
    m_wood_l = MATERIALS["AC_WoodLight"]
    m_cream = MATERIALS["AC_Cream"]
    m_bulb = MATERIALS["AC_BulbWarm"]

    parts.append(make_box("StallCounter", (2.6, 1.05, 0.82), m_wood_l, col, location=(0, 0, 0.48), bevel=0.05))
    parts.append(make_box("StallTop", (2.82, 1.22, 0.12), m_cream, col, location=(0, 0, 0.94), bevel=0.04))
    for px in (-1.24, 1.24):
        for py in (-0.48, 0.48):
            parts.append(make_cyl(f"SPost_{px}_{py}", 0.08, 0.08, 2.25, m_wood, col, location=(px, py, 1.15), segs=10))
    num_s = 7
    sw = 3.05 / num_s
    for i in range(num_s):
        sx = -1.525 + (i + 0.5) * sw
        mat_s = stripe_mat if i % 2 == 0 else m_cream
        parts.append(make_box(f"SAwning_{i}", (sw * 1.01, 1.78, 0.14), mat_s, col, location=(sx, -0.05, 2.40), rotation=(0.24, 0, 0), bevel=0.03))
        parts.append(make_cyl(f"SScallop_{i}", sw * 0.48, sw * 0.48, 0.10, mat_s, col, location=(sx, -0.88, 2.12), rotation=(math.pi * 0.5, 0, 0), segs=10))
    for lx in (-0.85, 0.0, 0.85):
        parts.append(make_sphere(f"SBulb_{lx}", 0.055, m_bulb, col, location=(lx, -0.78, 2.02), u_segs=8, v_segs=6))


def build_stall_plantas(col):
    parts = []
    build_stall_shell(parts, col, MATERIALS["AC_BushGreen"])
    m_pot = MATERIALS["AC_Terracotta"]
    m_leaf1 = MATERIALS["AC_PalmLeaf"]
    m_leaf2 = MATERIALS["AC_BushLight"]
    # Terracotta pots with ferns, succulents & cute flowers!
    for i in range(5):
        px = -0.96 + i * 0.48
        h = 0.20 + (i % 3) * 0.08
        parts.append(make_cyl(f"PlPot_{i}", 0.11, 0.15, h, m_pot, col, location=(px, -0.06, 1.00 + h * 0.5), segs=10))
        parts.append(make_sphere(f"PlBush_{i}", 0.18 + (i % 2) * 0.05, m_leaf1 if i % 2 == 0 else m_leaf2, col, location=(px, -0.06, 1.18 + h), scale=(1.0, 1.0, 1.15), u_segs=10, v_segs=8))
        if i % 2 == 1:
            parts.append(make_sphere(f"PlBloom_{i}", 0.065, MATERIALS["AC_FlowerWhite"], col, location=(px, -0.14, 1.38 + h), u_segs=8, v_segs=6))
    # Hanging potted fern under the eaves
    parts.append(make_cyl("HangPot", 0.14, 0.18, 0.18, m_pot, col, location=(-1.12, -0.58, 1.80), segs=10))
    parts.append(make_sphere("HangFern", 0.24, m_leaf2, col, location=(-1.12, -0.58, 1.94), scale=(1.2, 1.2, 0.7), u_segs=10, v_segs=8))
    return join_and_export(parts, "MarketStall_Plantas", col)


def build_stall_sushi(col):
    parts = []
    build_stall_shell(parts, col, MATERIALS["AC_Coral"])
    m_wood_d = MATERIALS["AC_WoodDark"]
    m_white = MATERIALS["AC_FlowerWhite"]
    m_salmon = MATERIALS["AC_Orange"]
    m_nori = MATERIALS["AC_MetalDark"]
    m_red = MATERIALS["AC_PinkBerry"]
    m_glow = MATERIALS["AC_LanternGlow"]

    # 3 wooden sushi boats/trays with nigiri & maki rolls!
    for t in range(3):
        tx = -0.82 + t * 0.82
        parts.append(make_box(f"SushiTray_{t}", (0.64, 0.42, 0.06), m_wood_d, col, location=(tx, -0.06, 1.03), bevel=0.015))
        for k in range(3):
            sx = tx - 0.18 + k * 0.18
            if t == 1:
                # Maki roll
                parts.append(make_cyl(f"Maki_{t}_{k}", 0.07, 0.07, 0.10, m_nori, col, location=(sx, -0.06, 1.11), segs=10, bevel=0.008))
                parts.append(make_cyl(f"MakiRice_{t}_{k}", 0.055, 0.055, 0.108, m_white, col, location=(sx, -0.06, 1.11), segs=10, bevel=0.005))
                parts.append(make_cyl(f"MakiCore_{t}_{k}", 0.025, 0.025, 0.115, m_salmon, col, location=(sx, -0.06, 1.11), segs=8, bevel=0.002))
            else:
                # Nigiri
                parts.append(make_box(f"Rice_{t}_{k}", (0.12, 0.22, 0.07), m_white, col, location=(sx, -0.06, 1.10), bevel=0.025))
                parts.append(make_box(f"Salmon_{t}_{k}", (0.13, 0.24, 0.04), m_salmon if k % 2 == 0 else m_red, col, location=(sx, -0.06, 1.15), bevel=0.015))
    # Two glowing paper sushi lanterns hanging on sides!
    for lx in (-1.22, 1.22):
        parts.append(make_sphere(f"SushiLantern_{lx}", 0.18, m_red, col, location=(lx, -0.58, 1.72), scale=(1.0, 1.0, 1.35), u_segs=12, v_segs=10))
        parts.append(make_cyl(f"SushiLanGlow_{lx}", 0.11, 0.11, 0.16, m_glow, col, location=(lx, -0.68, 1.72), segs=10))
    return join_and_export(parts, "MarketStall_Sushi", col)


def build_stall_comida(col):
    parts = []
    build_stall_shell(parts, col, MATERIALS["AC_Orange"])
    m_waffle = MATERIALS["AC_Waffle"]
    m_choco = MATERIALS["AC_Chocolate"]
    m_green = MATERIALS["AC_PalmLeaf"]
    m_pink = MATERIALS["AC_PinkBerry"]
    m_cream = MATERIALS["AC_Cream"]
    m_plum = MATERIALS["AC_PurplePlum"]

    # Coastal snacks: Giant Burgers, Acai Bowls, Golden Pastels & Cupcakes!
    for i in range(2):
        bx = -0.88 + i * 0.42
        parts.append(make_cyl(f"BunBot_{i}", 0.14, 0.14, 0.07, m_waffle, col, location=(bx, -0.08, 1.04), segs=12))
        parts.append(make_cyl(f"Patty_{i}", 0.145, 0.145, 0.06, m_choco, col, location=(bx, -0.08, 1.10), segs=12))
        parts.append(make_cyl(f"Lettuce_{i}", 0.155, 0.155, 0.03, m_green, col, location=(bx, -0.08, 1.14), segs=12))
        parts.append(make_sphere(f"BunTop_{i}", 0.145, m_waffle, col, location=(bx, -0.08, 1.18), scale=(1.0, 1.0, 0.65), u_segs=12, v_segs=8))
    # Acai bowl with banana/strawberry in center
    parts.append(make_cyl("AcaiBowl", 0.15, 0.24, 0.16, m_cream, col, location=(0.15, -0.06, 1.08), segs=14))
    parts.append(make_sphere("AcaiMound", 0.19, m_plum, col, location=(0.15, -0.06, 1.18), scale=(1.0, 1.0, 0.55), u_segs=12, v_segs=8))
    parts.append(make_sphere("AcaiBerry", 0.06, m_pink, col, location=(0.18, -0.10, 1.27), u_segs=8, v_segs=6))
    # Golden coastal pastels on right tray
    parts.append(make_box("PastelTray", (0.68, 0.44, 0.05), m_cream, col, location=(0.88, -0.06, 1.03), bevel=0.015))
    for k in range(3):
        parts.append(make_box(f"Pastel_{k}", (0.16, 0.28, 0.09), MATERIALS["AC_SunnyYellow"], col, location=(0.68 + k * 0.20, -0.06, 1.10), rotation=(0, 0.15, 0.1), bevel=0.03))
    return join_and_export(parts, "MarketStall_Comida", col)


def build_stall_roupas(col):
    parts = []
    build_stall_shell(parts, col, MATERIALS["AC_Lavender"])
    m_silver = MATERIALS["AC_MetalSilver"]
    shirt_mats = [
        MATERIALS["AC_PinkBerry"],
        MATERIALS["AC_MintTeal"],
        MATERIALS["AC_SunnyYellow"],
        MATERIALS["AC_SkyBlue"],
        MATERIALS["AC_Lavender"],
    ]
    # Clothing hanging bar across the stall!
    parts.append(make_cyl("ClothesBar", 0.035, 0.035, 2.35, m_silver, col, location=(0, 0.15, 1.92), rotation=(0, math.pi * 0.5, 0), segs=10))
    for i, sm in enumerate(shirt_mats):
        sx = -0.88 + i * 0.44
        # Cute hanging pastel T-shirts!
        parts.append(make_box(f"ShirtBody_{i}", (0.30, 0.09, 0.44), sm, col, location=(sx, 0.15, 1.62), bevel=0.03))
        parts.append(make_box(f"ShirtSleeves_{i}", (0.44, 0.08, 0.16), sm, col, location=(sx, 0.15, 1.75), bevel=0.025))
    # Folded shirts & sunhat on counter
    for i in range(3):
        parts.append(make_box(f"FoldedCloth_{i}", (0.44, 0.44, 0.11), shirt_mats[i], col, location=(-0.72 + i * 0.58, -0.15, 1.06), bevel=0.03))
    return join_and_export(parts, "MarketStall_Roupas", col)


# ==============================================================================
# 6. ADORABLE ANIMAL CROSSING DOG ("Pipoca")
# ==============================================================================

def build_pet_dog(col):
    m_fur = MATERIALS["AC_WoodLight"]
    m_belly = MATERIALS["AC_Cream"]
    m_ear = MATERIALS["AC_WoodWarm"]
    m_collar = MATERIALS["AC_Coral"]
    m_gold = MATERIALS["AC_Gold"]
    m_eye = MATERIALS["AC_EyeDark"]
    m_shine = MATERIALS["AC_EyeShine"]

    parts = [
        # Plump rounded puppy body
        make_sphere("DogBody", 0.25, m_fur, col, location=(0, 0.04, 0.30), scale=(0.90, 1.38, 0.88), u_segs=14, v_segs=10),
        make_sphere("DogBelly", 0.19, m_belly, col, location=(0, -0.05, 0.26), scale=(0.85, 1.15, 0.72), u_segs=12, v_segs=8),
        # 4 chunky paws
        make_capsule("DogLegFL", 0.068, 0.24, m_fur, col, location=(-0.13, -0.18, 0.12)),
        make_capsule("DogLegFR", 0.068, 0.24, m_fur, col, location=(0.13, -0.18, 0.12)),
        make_capsule("DogLegBL", 0.068, 0.24, m_fur, col, location=(-0.13, 0.22, 0.12)),
        make_capsule("DogLegBR", 0.068, 0.24, m_fur, col, location=(0.13, 0.22, 0.12)),
        # Red collar + golden tag
        make_cyl("DogCollar", 0.17, 0.17, 0.07, m_collar, col, location=(0, -0.22, 0.42), rotation=(0.45, 0, 0), segs=14),
        make_sphere("DogTag", 0.05, m_gold, col, location=(0, -0.35, 0.36), u_segs=8, v_segs=6),
        # Big cute puppy head + snout + floppy ears + happy wagging tail!
        make_sphere("DogHead", 0.25, m_fur, col, location=(0, -0.26, 0.58), scale=(1.05, 0.98, 0.96), u_segs=16, v_segs=12),
        make_sphere("DogSnout", 0.12, m_belly, col, location=(0, -0.46, 0.53), scale=(1.1, 0.9, 0.78), u_segs=12, v_segs=8),
        make_sphere("DogNose", 0.052, m_eye, col, location=(0, -0.56, 0.56), scale=(1.15, 0.85, 0.8), u_segs=10, v_segs=8),
        make_sphere("DogEyeL", 0.044, m_eye, col, location=(-0.10, -0.47, 0.64), u_segs=10, v_segs=8),
        make_sphere("DogEyeR", 0.044, m_eye, col, location=(0.10, -0.47, 0.64), u_segs=10, v_segs=8),
        make_sphere("DogShineL", 0.016, m_shine, col, location=(-0.09, -0.505, 0.655), u_segs=8, v_segs=6),
        make_sphere("DogShineR", 0.016, m_shine, col, location=(0.11, -0.505, 0.655), u_segs=8, v_segs=6),
        make_sphere("DogEarL", 0.12, m_ear, col, location=(-0.25, -0.22, 0.56), scale=(0.55, 0.85, 1.55), rotation=(0.1, 0.25, 0), u_segs=10, v_segs=8),
        make_sphere("DogEarR", 0.12, m_ear, col, location=(0.25, -0.22, 0.56), scale=(0.55, 0.85, 1.55), rotation=(0.1, -0.25, 0), u_segs=10, v_segs=8),
        make_capsule("DogTail", 0.055, 0.28, m_fur, col, location=(0, 0.36, 0.44), rotation=(-0.65, 0, 0)),
    ]
    return join_and_export(parts, "PetDog", col)


# ==============================================================================
# 7. ADORABLE ANIMAL CROSSING CAT ("Mingau")
# ==============================================================================

def build_pet_cat(col):
    m_fur = MATERIALS["AC_FlowerWhite"]
    m_patch = MATERIALS["AC_Lavender"]
    m_pink = MATERIALS["AC_PinkSoft"]
    m_collar = MATERIALS["AC_MintTeal"]
    m_gold = MATERIALS["AC_Gold"]
    m_eye = MATERIALS["AC_EyeDark"]
    m_shine = MATERIALS["AC_EyeShine"]

    parts = [
        make_sphere("CatBody", 0.21, m_fur, col, location=(0, 0.04, 0.27), scale=(0.86, 1.32, 0.88), u_segs=14, v_segs=10),
        make_sphere("CatSpot", 0.14, m_patch, col, location=(0.06, 0.10, 0.36), scale=(1.0, 1.2, 0.6), u_segs=10, v_segs=8),
        make_capsule("CatLegFL", 0.056, 0.22, m_fur, col, location=(-0.11, -0.15, 0.11)),
        make_capsule("CatLegFR", 0.056, 0.22, m_fur, col, location=(0.11, -0.15, 0.11)),
        make_capsule("CatLegBL", 0.056, 0.22, m_fur, col, location=(-0.11, 0.19, 0.11)),
        make_capsule("CatLegBR", 0.056, 0.22, m_fur, col, location=(0.11, 0.19, 0.11)),
        make_cyl("CatCollar", 0.15, 0.15, 0.06, m_collar, col, location=(0, -0.18, 0.38), rotation=(0.45, 0, 0), segs=14),
        make_sphere("CatBell", 0.048, m_gold, col, location=(0, -0.30, 0.33), u_segs=8, v_segs=6),
        # Cute cat head + pointed triangular ears + tiny pink nose + curved fluffy tail!
        make_sphere("CatHead", 0.23, m_fur, col, location=(0, -0.22, 0.54), scale=(1.08, 0.96, 0.92), u_segs=16, v_segs=12),
        make_cyl("CatEarL", 0.09, 0.015, 0.18, m_patch, col, location=(-0.15, -0.20, 0.76), rotation=(0.1, -0.28, 0), segs=4, bevel=0.01),
        make_cyl("CatEarR", 0.09, 0.015, 0.18, m_fur, col, location=(0.15, -0.20, 0.76), rotation=(0.1, 0.28, 0), segs=4, bevel=0.01),
        make_sphere("CatNose", 0.036, m_pink, col, location=(0, -0.44, 0.53), u_segs=8, v_segs=6),
        make_sphere("CatEyeL", 0.044, m_eye, col, location=(-0.095, -0.41, 0.58), scale=(0.9, 0.5, 1.15), u_segs=10, v_segs=8),
        make_sphere("CatEyeR", 0.044, m_eye, col, location=(0.095, -0.41, 0.58), scale=(0.9, 0.5, 1.15), u_segs=10, v_segs=8),
        make_sphere("CatShineL", 0.016, m_shine, col, location=(-0.085, -0.435, 0.595), u_segs=8, v_segs=6),
        make_sphere("CatShineR", 0.016, m_shine, col, location=(0.105, -0.435, 0.595), u_segs=8, v_segs=6),
        make_capsule("CatTail", 0.045, 0.36, m_patch, col, location=(0, 0.34, 0.46), rotation=(-0.38, 0, 0)),
    ]
    return join_and_export(parts, "PetCat", col)


# ==============================================================================
# 8. TROPICAL PASTEL SURFBOARD
# ==============================================================================

def build_surfboard(col):
    parts = [
        # Sleek rounded tropical surfboard aligned along -Y (forward!)
        make_sphere("BoardDeck", 0.42, MATERIALS["AC_SunnyYellow"], col, location=(0, 0, 0.06), scale=(0.92, 2.65, 0.16), u_segs=16, v_segs=10),
        make_sphere("BoardStripe", 0.40, MATERIALS["AC_MintTeal"], col, location=(0, 0, 0.075), scale=(0.35, 2.52, 0.16), u_segs=14, v_segs=8),
        make_sphere("BoardFlower", 0.14, MATERIALS["AC_PinkBerry"], col, location=(0, -0.52, 0.12), scale=(1.0, 1.0, 0.18), u_segs=10, v_segs=6),
        make_sphere("BoardFlowerC", 0.06, MATERIALS["AC_Cream"], col, location=(0, -0.52, 0.14), scale=(1.0, 1.0, 0.25), u_segs=8, v_segs=6),
        make_box("BoardFin", (0.04, 0.28, 0.18), MATERIALS["AC_Coral"], col, location=(0, 0.72, -0.06), bevel=0.01),
    ]
    return join_and_export(parts, "Surfboard", col)


# ==============================================================================
# 9. PAINTABLE ANIMAL CROSSING COASTAL HOUSE (AC_HouseWall + AC_HouseRoof slots!)
# ==============================================================================

def build_coastal_house(col):
    m_wall = MATERIALS["AC_HouseWall"]
    m_roof = MATERIALS["AC_HouseRoof"]
    m_wood = MATERIALS["AC_WoodWarm"]
    m_wood_l = MATERIALS["AC_WoodLight"]
    m_cream = MATERIALS["AC_Cream"]
    m_glass = MATERIALS["AC_Glass"]
    m_stone = MATERIALS["AC_StoneWhite"]
    m_gold = MATERIALS["AC_Gold"]
    m_bush = MATERIALS["AC_BushGreen"]
    m_pink = MATERIALS["AC_FlowerPink"]

    parts = [
        # Cozy stone foundation & wooden front porch facing +Y (north, toward the street & ocean!)
        make_box("HouseBase", (4.4, 3.6, 0.34), m_stone, col, location=(0, 0, 0.17), bevel=0.06),
        make_box("Porch", (2.2, 0.9, 0.20), m_wood_l, col, location=(0, 2.05, 0.12), bevel=0.04),
        # Paintable plump house body!
        make_box("HouseWalls", (4.0, 3.2, 2.25), m_wall, col, location=(0, 0, 1.44), bevel=0.10),
        # Cute pitched paintable cottage roof + cream trim!
        make_box("RoofFront", (4.55, 2.35, 0.24), m_roof, col, location=(0, 0.98, 2.98), rotation=(-0.44, 0, 0), bevel=0.06),
        make_box("RoofBack", (4.55, 2.35, 0.24), m_roof, col, location=(0, -0.98, 2.98), rotation=(0.44, 0, 0), bevel=0.06),
        make_box("RoofRidge", (4.62, 0.30, 0.24), m_cream, col, location=(0, 0, 3.46), bevel=0.06),
        # Cute chimney
        make_box("Chimney", (0.55, 0.55, 1.15), m_stone, col, location=(1.25, -0.45, 3.45), bevel=0.05),
        # Rounded front door facing the street (+Y) + golden doorknob!
        make_box("DoorFrame", (0.98, 0.14, 1.48), m_cream, col, location=(0, 1.62, 1.06), bevel=0.04),
        make_box("DoorLeaf", (0.82, 0.16, 1.36), m_wood, col, location=(0, 1.64, 1.04), bevel=0.04),
        make_sphere("DoorKnob", 0.065, m_gold, col, location=(0.28, 1.74, 1.02), u_segs=8, v_segs=6),
        # Two cozy framed windows with flower boxes on front wall (+Y)
        make_box("WinFrameL", (0.86, 0.14, 0.82), m_cream, col, location=(-1.25, 1.62, 1.48), bevel=0.03),
        make_box("WinGlassL", (0.68, 0.16, 0.64), m_glass, col, location=(-1.25, 1.64, 1.48), bevel=0.02),
        make_box("WinFrameR", (0.86, 0.14, 0.82), m_cream, col, location=(1.25, 1.62, 1.48), bevel=0.03),
        make_box("WinGlassR", (0.68, 0.16, 0.64), m_glass, col, location=(1.25, 1.64, 1.48), bevel=0.02),
        # Window planter flowers
        make_box("WinBoxL", (0.90, 0.24, 0.20), m_wood_l, col, location=(-1.25, 1.74, 0.98), bevel=0.03),
        make_sphere("WinBushL", 0.20, m_bush, col, location=(-1.25, 1.76, 1.12), scale=(1.8, 0.7, 0.6), u_segs=10, v_segs=6),
        make_sphere("WinFlwL", 0.07, m_pink, col, location=(-1.12, 1.84, 1.18), u_segs=8, v_segs=6),
        make_box("WinBoxR", (0.90, 0.24, 0.20), m_wood_l, col, location=(1.25, 1.74, 0.98), bevel=0.03),
        make_sphere("WinBushR", 0.20, m_bush, col, location=(1.25, 1.76, 1.12), scale=(1.8, 0.7, 0.6), u_segs=10, v_segs=6),
        make_sphere("WinFlwR", 0.07, MATERIALS["AC_SunnyYellow"], col, location=(1.36, 1.84, 1.18), u_segs=8, v_segs=6),
    ]
    return join_and_export(parts, "CoastalHouse", col)


def render_preview(col):
    world = bpy.data.worlds.new("ExpansionPreviewWorld")
    bpy.context.scene.world = world
    nt = world.node_tree
    nt.nodes.clear()
    out = nt.nodes.new("ShaderNodeOutputWorld")
    bg = nt.nodes.new("ShaderNodeBackground")
    bg.inputs["Color"].default_value = hex_to_rgba("#CBEBFF")
    bg.inputs["Strength"].default_value = 1.0
    nt.links.new(bg.outputs["Background"], out.inputs["Surface"])

    sun_d = bpy.data.lights.new("Sun", type="SUN")
    sun_d.energy = 3.2
    sun_d.color = hex_to_rgba("#FFF0D0")[:3]
    sun_o = bpy.data.objects.new("Sun", sun_d)
    sun_o.rotation_euler = (math.radians(46), math.radians(15), math.radians(-26))
    link_obj(sun_o, col)

    cam_d = bpy.data.cameras.new("Cam")
    cam_d.lens = 35.0
    cam_o = bpy.data.objects.new("Cam", cam_d)
    cam_o.location = (0.0, -14.5, 6.4)
    direction = Vector((0.0, 0.5, 1.2)) - cam_o.location
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
    col = ensure_collection("BaiaDoAvesso_Expansion")

    protag = build_upright_protagonist(col)
    st_plantas = build_stall_plantas(col)
    st_sushi = build_stall_sushi(col)
    st_comida = build_stall_comida(col)
    st_roupas = build_stall_roupas(col)
    dog = build_pet_dog(col)
    cat = build_pet_cat(col)
    surf = build_surfboard(col)
    house = build_coastal_house(col)

    # Arrange nicely for preview render!
    st_plantas.location = (-5.5, 2.5, 0)
    st_sushi.location = (-2.0, 2.5, 0)
    st_comida.location = (1.5, 2.5, 0)
    st_roupas.location = (5.0, 2.5, 0)
    house.location = (0.0, 6.5, 0)
    house.rotation_euler = (0, 0, math.pi)
    protag.location = (-2.2, -2.2, 0)
    dog.location = (-0.7, -2.4, 0)
    cat.location = (0.6, -2.4, 0)
    surf.location = (2.4, -2.2, 0.1)

    bpy.ops.wm.save_as_mainfile(filepath=BLEND_OUT)
    render_preview(col)
    print("[Expansion Assets Complete]")


if __name__ == "__main__":
    main()
