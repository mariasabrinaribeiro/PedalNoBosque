"""
build_orla_living_assets.py
---------------------------
Passo 1 - Living World & Articulated Character Upgrade for:
"Girl Power - Orla de Matinhos 3D" (Animal Crossing: New Horizons Style).

Generates & exports to Assets/GirlPower/Orla3D/Models/:
  1. Char_Protagonist.fbx  (Articulated hierarchy: Body, Head, ArmL, ArmR, LegL, LegR!)
  2. IceCreamConeProp.fbx  (Held 3D strawberry ice cream cone for delivery!)
  3. CloudFluffy.fbx       (Cozy plump Animal Crossing sky cloud)
  4. Seagull3D.fbx         (Articulated coastal seagull: Body, WingL, WingR)
  5. Butterfly3D.fbx       (Articulated pastel butterfly: Body, WingL, WingR)
  6. BeachCrab.fbx         (Cute coral beach crab scuttling on sand)

Also saves Orla_LivingAssets_ACNH.blend + renders orla_living_preview.png.
"""

import math
import os
import bpy
import bmesh
from mathutils import Vector, Euler

SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
ORLA3D_DIR = os.path.dirname(SCRIPT_DIR)
MODELS_DIR = os.path.join(ORLA3D_DIR, "Models")
BLEND_OUT = os.path.join(SCRIPT_DIR, "Orla_LivingAssets_ACNH.blend")
PREVIEW_OUT = os.path.join(SCRIPT_DIR, "orla_living_preview.png")

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
    get_mat("AC_SkinWarm", "#C9855E", 0.56)
    get_mat("AC_CheekBlush", "#FF8EA6", 0.55)
    get_mat("AC_EyeDark", "#251D29", 0.15)
    get_mat("AC_EyeShine", "#FFFFFF", 0.10, "#FFFFFF", 0.45)
    get_mat("AC_HairPlum", "#723678", 0.48)
    get_mat("AC_PinkBerry", "#F26E97", 0.50)
    get_mat("AC_PinkSoft", "#FFA9C2", 0.48)
    get_mat("AC_Denim", "#4E78A8", 0.70)
    get_mat("AC_ShoeDark", "#4E3A4C", 0.55)
    get_mat("AC_SunnyYellow", "#FFD45E", 0.48)
    get_mat("AC_Coral", "#F27B66", 0.48)
    get_mat("AC_Cream", "#FFF6E5", 0.48)
    get_mat("AC_FlowerWhite", "#FFFDF7", 0.45)
    get_mat("AC_SkyBlue", "#6CC0F0", 0.48)
    get_mat("AC_MintSoft", "#8FE3D6", 0.48)
    get_mat("AC_Lavender", "#B89CE0", 0.48)
    get_mat("AC_Waffle", "#E3A75B", 0.60)
    get_mat("AC_Strawberry", "#FF6E8D", 0.42)
    get_mat("AC_Orange", "#FF9333", 0.45)
    get_mat("AC_Starfish", "#FF8566", 0.52)


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


def make_sphere(name, radius, mat, col, location=(0, 0, 0), scale=(1, 1, 1), rotation=(0, 0, 0), u_segs=14, v_segs=10):
    bm = bmesh.new()
    bmesh.ops.create_uvsphere(bm, u_segments=u_segs, v_segments=v_segs, radius=radius)
    return add_mesh_obj(name, bm, mat, col, location=location, rotation=rotation, scale=scale)


def make_capsule(name, radius, height, mat, col, location=(0, 0, 0), rotation=(0, 0, 0), segs=14):
    cyl_h = max(0.01, height - 2.0 * radius)
    bm = bmesh.new()
    bmesh.ops.create_uvsphere(bm, u_segments=segs, v_segments=10, radius=radius)
    for v in bm.verts:
        if v.co.z > 0:
            v.co.z += cyl_h * 0.5
        elif v.co.z < 0:
            v.co.z -= cyl_h * 0.5
    return add_mesh_obj(name, bm, mat, col, location=location, rotation=rotation)


def make_cone(name, r1, r2, depth, mat, col, location=(0, 0, 0), rotation=(0, 0, 0), segs=12):
    bm = bmesh.new()
    bmesh.ops.create_cone(bm, cap_ends=True, cap_tris=False, segments=segs, radius1=r1, radius2=r2, depth=depth)
    return add_mesh_obj(name, bm, mat, col, location=location, rotation=rotation)


def join_part_at_pivot(parts, part_name, pivot_world, parent_obj, target_col):
    """Joins a list of mesh objects into one mesh whose origin is placed cleanly at pivot_world, parented to parent_obj."""
    bpy.ops.object.select_all(action="DESELECT")
    for o in parts:
        o.select_set(True)
    bpy.context.view_layer.objects.active = parts[0]
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    if len(parts) > 1:
        bpy.ops.object.join()
    joined = bpy.context.view_layer.objects.active
    joined.name = part_name
    joined.data.name = part_name + "_Mesh"
    bpy.context.scene.cursor.location = pivot_world
    bpy.ops.object.origin_set(type="ORIGIN_CURSOR")
    link_obj(joined, target_col)
    if parent_obj is not None:
        joined.parent = parent_obj
        joined.matrix_parent_inverse = parent_obj.matrix_world.inverted()
    return joined


def export_hierarchy_fbx(root_obj, filename):
    """Exports root_obj and all its child objects as a clean Unity FBX hierarchy."""
    bpy.ops.object.select_all(action="DESELECT")
    to_select = [root_obj] + list(root_obj.children_recursive)
    for o in to_select:
        o.select_set(True)
    bpy.context.view_layer.objects.active = root_obj
    filepath = os.path.join(MODELS_DIR, filename)
    bpy.ops.export_scene.fbx(
        filepath=filepath,
        use_selection=True,
        global_scale=1.0,
        apply_unit_scale=True,
        apply_scale_options="FBX_SCALE_ALL",
        bake_space_transform=False,
        object_types={"EMPTY", "MESH"},
        use_mesh_modifiers=True,
        mesh_smooth_type="FACE",
        axis_forward="-Z",
        axis_up="Y",
        add_leaf_bones=False,
        bake_anim=False,
    )
    print(f"[FBX Exported] {filepath} ({len(to_select)} nodes)")


# ==============================================================================
# 1. ARTICULATED PROTAGONIST (Body, Head, ArmL, ArmR, LegL, LegR)
# ==============================================================================

def build_articulated_protagonist(col):
    root = bpy.data.objects.new("Char_Protagonist", None)
    root.location = (0, 0, 0)
    link_obj(root, col)

    m_skin = MATERIALS["AC_SkinWarm"]
    m_hair = MATERIALS["AC_HairPlum"]
    m_shirt = MATERIALS["AC_PinkBerry"]
    m_pants = MATERIALS["AC_Denim"]
    m_shoe = MATERIALS["AC_ShoeDark"]
    m_eye = MATERIALS["AC_EyeDark"]
    m_shine = MATERIALS["AC_EyeShine"]
    m_blush = MATERIALS["AC_CheekBlush"]
    m_yel = MATERIALS["AC_SunnyYellow"]
    m_coral = MATERIALS["AC_Coral"]

    # --- BODY (pivot at waist/torso center z=0.68) ---
    body_parts = [
        make_sphere("Torso", 0.30, m_shirt, col, location=(0, 0, 0.68), scale=(0.96, 0.84, 1.15), u_segs=14, v_segs=12),
        make_sphere("StarEmblem", 0.09, m_yel, col, location=(0, -0.24, 0.72), scale=(1.0, 0.35, 1.0), u_segs=10, v_segs=8),
    ]
    join_part_at_pivot(body_parts, "Body", (0, 0, 0.68), root, col)

    # --- HEAD (pivot at neck z=0.96) ---
    hz = 1.24
    head_parts = [
        make_sphere("HeadSphere", 0.36, m_skin, col, location=(0, 0, hz), scale=(1.04, 0.98, 0.96), u_segs=18, v_segs=14),
        make_sphere("EarL", 0.085, m_skin, col, location=(-0.36, 0.02, hz - 0.02), scale=(0.65, 0.85, 1.0), u_segs=10, v_segs=8),
        make_sphere("EarR", 0.085, m_skin, col, location=(0.36, 0.02, hz - 0.02), scale=(0.65, 0.85, 1.0), u_segs=10, v_segs=8),
        make_sphere("Nose", 0.045, m_coral, col, location=(0, -0.35, hz - 0.03), scale=(1.1, 0.8, 0.8), u_segs=8, v_segs=6),
        make_sphere("EyeL", 0.062, m_eye, col, location=(-0.14, -0.32, hz + 0.03), scale=(0.85, 0.45, 1.15), u_segs=10, v_segs=8),
        make_sphere("EyeR", 0.062, m_eye, col, location=(0.14, -0.32, hz + 0.03), scale=(0.85, 0.45, 1.15), u_segs=10, v_segs=8),
        make_sphere("EyeShineL", 0.022, m_shine, col, location=(-0.125, -0.352, hz + 0.055), u_segs=8, v_segs=6),
        make_sphere("EyeShineR", 0.022, m_shine, col, location=(0.155, -0.352, hz + 0.055), u_segs=8, v_segs=6),
        make_sphere("CheekL", 0.068, m_blush, col, location=(-0.21, -0.28, hz - 0.06), scale=(1.1, 0.35, 0.75), u_segs=8, v_segs=6),
        make_sphere("CheekR", 0.068, m_blush, col, location=(0.21, -0.28, hz - 0.06), scale=(1.1, 0.35, 0.75), u_segs=8, v_segs=6),
        make_sphere("HairCap", 0.385, m_hair, col, location=(0, 0.04, hz + 0.06), scale=(1.04, 1.02, 0.96), u_segs=16, v_segs=12),
        make_sphere("HairBangsL", 0.18, m_hair, col, location=(-0.14, -0.22, hz + 0.23), scale=(1.2, 0.75, 0.75), u_segs=10, v_segs=8),
        make_sphere("HairBangsR", 0.18, m_hair, col, location=(0.14, -0.22, hz + 0.23), scale=(1.2, 0.75, 0.75), u_segs=10, v_segs=8),
        make_sphere("BobSideL", 0.17, m_hair, col, location=(-0.31, 0.05, hz - 0.10), scale=(0.8, 1.0, 1.35), u_segs=10, v_segs=8),
        make_sphere("BobSideR", 0.17, m_hair, col, location=(0.31, 0.05, hz - 0.10), scale=(0.8, 1.0, 1.35), u_segs=10, v_segs=8),
        make_sphere("HairClip", 0.075, m_yel, col, location=(0.24, -0.24, hz + 0.24), scale=(1.1, 0.5, 0.9), u_segs=8, v_segs=6),
    ]
    join_part_at_pivot(head_parts, "Head", (0, 0, 0.96), root, col)

    # --- ARMS (pivoted at shoulders z=0.82) ---
    arm_l_parts = [
        make_capsule("ArmL_Mesh", 0.088, 0.38, m_shirt, col, location=(-0.28, 0.0, 0.68), rotation=(0, -0.26, 0)),
        make_sphere("HandL_Mesh", 0.085, m_skin, col, location=(-0.33, -0.02, 0.52), u_segs=10, v_segs=8),
    ]
    join_part_at_pivot(arm_l_parts, "ArmL", (-0.23, 0.0, 0.82), root, col)

    arm_r_parts = [
        make_capsule("ArmR_Mesh", 0.088, 0.38, m_shirt, col, location=(0.28, 0.0, 0.68), rotation=(0, 0.26, 0)),
        make_sphere("HandR_Mesh", 0.085, m_skin, col, location=(0.33, -0.02, 0.52), u_segs=10, v_segs=8),
    ]
    join_part_at_pivot(arm_r_parts, "ArmR", (0.23, 0.0, 0.82), root, col)

    # --- LEGS (pivoted at hips z=0.44) ---
    leg_l_parts = [
        make_capsule("LegL_Mesh", 0.09, 0.36, m_pants, col, location=(-0.15, 0, 0.28)),
        make_sphere("ShoeL_Mesh", 0.13, m_shoe, col, location=(-0.15, -0.04, 0.09), scale=(0.95, 1.32, 0.72), u_segs=12, v_segs=10),
    ]
    join_part_at_pivot(leg_l_parts, "LegL", (-0.15, 0.0, 0.44), root, col)

    leg_r_parts = [
        make_capsule("LegR_Mesh", 0.09, 0.36, m_pants, col, location=(0.15, 0, 0.28)),
        make_sphere("ShoeR_Mesh", 0.13, m_shoe, col, location=(0.15, -0.04, 0.09), scale=(0.95, 1.32, 0.72), u_segs=12, v_segs=10),
    ]
    join_part_at_pivot(leg_r_parts, "LegR", (0.15, 0.0, 0.44), root, col)

    return root


# ==============================================================================
# 2. HELD ICE CREAM CONE PROP
# ==============================================================================

def build_ice_cream_cone_prop(col):
    parts = [
        make_cone("ConeWaffle", 0.018, 0.10, 0.24, MATERIALS["AC_Waffle"], col, location=(0, 0, 0.12), segs=12),
        make_sphere("ScoopMain", 0.115, MATERIALS["AC_Strawberry"], col, location=(0, 0, 0.28), u_segs=12, v_segs=10),
        make_sphere("ScoopTop", 0.085, MATERIALS["AC_PinkSoft"], col, location=(0.02, -0.01, 0.38), u_segs=10, v_segs=8),
        make_sphere("Cherry", 0.032, MATERIALS["AC_PinkBerry"], col, location=(0.01, -0.02, 0.46), u_segs=8, v_segs=6),
    ]
    return join_part_at_pivot(parts, "IceCreamConeProp", (0, 0, 0), None, col)


# ==============================================================================
# 3. FLUFFY ANIMAL CROSSING SKY CLOUD
# ==============================================================================

def build_fluffy_cloud(col):
    m_cloud = MATERIALS["AC_FlowerWhite"]
    m_cream = MATERIALS["AC_Cream"]
    parts = [
        make_sphere("C0", 1.15, m_cloud, col, location=(0.0, 0.0, 0.25), scale=(1.25, 0.85, 0.82), u_segs=14, v_segs=10),
        make_sphere("C1", 0.92, m_cloud, col, location=(-1.15, 0.08, 0.05), scale=(1.1, 0.85, 0.78), u_segs=12, v_segs=10),
        make_sphere("C2", 0.96, m_cloud, col, location=(1.18, -0.05, 0.08), scale=(1.15, 0.85, 0.78), u_segs=12, v_segs=10),
        make_sphere("C3", 0.72, m_cream, col, location=(-1.85, 0.0, -0.10), scale=(1.0, 0.80, 0.65), u_segs=10, v_segs=8),
        make_sphere("C4", 0.76, m_cream, col, location=(1.92, 0.02, -0.08), scale=(1.0, 0.80, 0.65), u_segs=10, v_segs=8),
        make_sphere("C5", 0.82, m_cloud, col, location=(0.45, 0.12, 0.55), scale=(1.05, 0.82, 0.72), u_segs=12, v_segs=10),
    ]
    return join_part_at_pivot(parts, "CloudFluffy", (0, 0, 0), None, col)


# ==============================================================================
# 4. ARTICULATED 3D SEAGULL (Body, WingL, WingR)
# ==============================================================================

def build_seagull(col):
    root = bpy.data.objects.new("Seagull3D", None)
    root.location = (0, 0, 0)
    link_obj(root, col)

    m_white = MATERIALS["AC_FlowerWhite"]
    m_sky = MATERIALS["AC_SkyBlue"]
    m_beak = MATERIALS["AC_Orange"]
    m_eye = MATERIALS["AC_EyeDark"]

    body_parts = [
        make_sphere("GullTorso", 0.22, m_white, col, location=(0, 0, 0), scale=(0.82, 1.55, 0.75), u_segs=12, v_segs=10),
        make_sphere("GullHead", 0.15, m_white, col, location=(0, -0.32, 0.10), u_segs=12, v_segs=10),
        make_cone("GullBeak", 0.045, 0.008, 0.18, m_beak, col, location=(0, -0.49, 0.08), rotation=(math.pi * 0.5, 0, 0), segs=10),
        make_sphere("GullEyeL", 0.024, m_eye, col, location=(-0.10, -0.38, 0.13), u_segs=8, v_segs=6),
        make_sphere("GullEyeR", 0.024, m_eye, col, location=(0.10, -0.38, 0.13), u_segs=8, v_segs=6),
        make_sphere("GullTail", 0.14, m_sky, col, location=(0, 0.36, 0.02), scale=(1.1, 1.3, 0.22), u_segs=10, v_segs=6),
    ]
    join_part_at_pivot(body_parts, "GullBody", (0, 0, 0), root, col)

    # Wings pivoted along torso sides (x = +-0.14)
    wing_l = [
        make_sphere("WingL_Inner", 0.28, m_white, col, location=(-0.36, 0.02, 0.04), scale=(1.2, 0.65, 0.15), u_segs=10, v_segs=6),
        make_sphere("WingL_Tip", 0.24, m_sky, col, location=(-0.68, 0.06, 0.02), scale=(1.15, 0.48, 0.12), u_segs=10, v_segs=6),
    ]
    join_part_at_pivot(wing_l, "WingL", (-0.14, 0.0, 0.04), root, col)

    wing_r = [
        make_sphere("WingR_Inner", 0.28, m_white, col, location=(0.36, 0.02, 0.04), scale=(1.2, 0.65, 0.15), u_segs=10, v_segs=6),
        make_sphere("WingR_Tip", 0.24, m_sky, col, location=(0.68, 0.06, 0.02), scale=(1.15, 0.48, 0.12), u_segs=10, v_segs=6),
    ]
    join_part_at_pivot(wing_r, "WingR", (0.14, 0.0, 0.04), root, col)

    return root


# ==============================================================================
# 5. ARTICULATED PASTEL BUTTERFLY (Body, WingL, WingR)
# ==============================================================================

def build_butterfly(col):
    root = bpy.data.objects.new("Butterfly3D", None)
    root.location = (0, 0, 0)
    link_obj(root, col)

    m_body = MATERIALS["AC_ShoeDark"]
    m_wing1 = MATERIALS["AC_SunnyYellow"]
    m_wing2 = MATERIALS["AC_PinkSoft"]

    body_parts = [
        make_capsule("BFBody", 0.032, 0.22, m_body, col, location=(0, 0, 0), rotation=(math.pi * 0.5, 0, 0)),
    ]
    join_part_at_pivot(body_parts, "ButterflyBody", (0, 0, 0), root, col)

    wing_l = [
        make_sphere("BFL_Top", 0.14, m_wing1, col, location=(-0.15, -0.05, 0.02), scale=(1.1, 0.95, 0.12), u_segs=10, v_segs=6),
        make_sphere("BFL_Bot", 0.10, m_wing2, col, location=(-0.11, 0.09, 0.01), scale=(0.95, 0.95, 0.12), u_segs=10, v_segs=6),
    ]
    join_part_at_pivot(wing_l, "WingL", (-0.02, 0, 0), root, col)

    wing_r = [
        make_sphere("BFR_Top", 0.14, m_wing1, col, location=(0.15, -0.05, 0.02), scale=(1.1, 0.95, 0.12), u_segs=10, v_segs=6),
        make_sphere("BFR_Bot", 0.10, m_wing2, col, location=(0.11, 0.09, 0.01), scale=(0.95, 0.95, 0.12), u_segs=10, v_segs=6),
    ]
    join_part_at_pivot(wing_r, "WingR", (0.02, 0, 0), root, col)

    return root


# ==============================================================================
# 6. CUTE MATINHOS BEACH CRAB
# ==============================================================================

def build_beach_crab(col):
    m_shell = MATERIALS["AC_Starfish"]
    m_claw = MATERIALS["AC_Coral"]
    m_eye = MATERIALS["AC_EyeDark"]
    m_white = MATERIALS["AC_FlowerWhite"]

    parts = [
        make_sphere("CrabShell", 0.22, m_shell, col, location=(0, 0, 0.14), scale=(1.25, 0.92, 0.62), u_segs=12, v_segs=10),
        # Cute eye stalks
        make_sphere("EyeWhiteL", 0.055, m_white, col, location=(-0.10, -0.16, 0.28), u_segs=8, v_segs=6),
        make_sphere("EyeWhiteR", 0.055, m_white, col, location=(0.10, -0.16, 0.28), u_segs=8, v_segs=6),
        make_sphere("EyePupilL", 0.028, m_eye, col, location=(-0.10, -0.20, 0.29), u_segs=8, v_segs=6),
        make_sphere("EyePupilR", 0.028, m_eye, col, location=(0.10, -0.20, 0.29), u_segs=8, v_segs=6),
        # Chunky little claws
        make_sphere("ClawL", 0.10, m_claw, col, location=(-0.26, -0.20, 0.18), scale=(0.9, 1.25, 0.85), u_segs=10, v_segs=8),
        make_sphere("ClawR", 0.10, m_claw, col, location=(0.26, -0.20, 0.18), scale=(0.9, 1.25, 0.85), u_segs=10, v_segs=8),
    ]
    # 6 tiny crab legs
    for side in (-1, 1):
        for i, ly in enumerate((-0.06, 0.04, 0.14)):
            parts.append(
                make_capsule(f"CrabLeg_{side}_{i}", 0.026, 0.22, m_claw, col, location=(side * 0.28, ly, 0.09), rotation=(0, side * 1.1, 0))
            )
    return join_part_at_pivot(parts, "BeachCrab", (0, 0, 0), None, col)


def render_preview(col):
    # Arrange all 6 assets side by side for visual inspection
    world = bpy.data.worlds.new("PreviewWorld")
    bpy.context.scene.world = world
    nt = world.node_tree
    nt.nodes.clear()
    out = nt.nodes.new("ShaderNodeOutputWorld")
    bg = nt.nodes.new("ShaderNodeBackground")
    bg.inputs["Color"].default_value = hex_to_rgba("#CBEBFF")
    bg.inputs["Strength"].default_value = 1.1
    nt.links.new(bg.outputs["Background"], out.inputs["Surface"])

    sun_d = bpy.data.lights.new("Sun", type="SUN")
    sun_d.energy = 3.2
    sun_d.color = hex_to_rgba("#FFF0D0")[:3]
    sun_o = bpy.data.objects.new("Sun", sun_d)
    sun_o.rotation_euler = (math.radians(48), math.radians(15), math.radians(-25))
    link_obj(sun_o, col)

    cam_d = bpy.data.cameras.new("PreviewCam")
    cam_d.lens = 45.0
    cam_o = bpy.data.objects.new("PreviewCam", cam_d)
    cam_o.location = (0.5, -5.6, 2.5)
    direction = Vector((0.5, 0.0, 0.9)) - cam_o.location
    cam_o.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()
    link_obj(cam_o, col)
    bpy.context.scene.camera = cam_o

    scene = bpy.context.scene
    engines = [e.identifier for e in bpy.types.RenderSettings.bl_rna.properties["engine"].enum_items]
    scene.render.engine = "BLENDER_EEVEE_NEXT" if "BLENDER_EEVEE_NEXT" in engines else ("BLENDER_EEVEE" if "BLENDER_EEVEE" in engines else "CYCLES")
    scene.render.resolution_x = 960
    scene.render.resolution_y = 540
    scene.render.image_settings.file_format = "PNG"
    scene.render.filepath = PREVIEW_OUT
    bpy.ops.render.render(write_still=True)


def main():
    clear_scene()
    init_palette()
    col = ensure_collection("LivingAssets")

    # Build & export each asset cleanly at origin (0,0,0), then offset for preview!
    protag = build_articulated_protagonist(col)
    export_hierarchy_fbx(protag, "Char_Protagonist.fbx")

    cone = build_ice_cream_cone_prop(col)
    export_hierarchy_fbx(cone, "IceCreamConeProp.fbx")

    cloud = build_fluffy_cloud(col)
    export_hierarchy_fbx(cloud, "CloudFluffy.fbx")

    gull = build_seagull(col)
    export_hierarchy_fbx(gull, "Seagull3D.fbx")

    bf = build_butterfly(col)
    export_hierarchy_fbx(bf, "Butterfly3D.fbx")

    crab = build_beach_crab(col)
    export_hierarchy_fbx(crab, "BeachCrab.fbx")

    # Arrange nicely for preview & .blend inspection
    protag.location = (-1.6, 0, 0)
    cone.location = (-0.5, -0.2, 0.7)
    crab.location = (0.3, -0.3, 0)
    bf.location = (1.1, 0, 1.0)
    gull.location = (2.1, 0, 1.2)
    cloud.location = (0.6, 1.6, 2.2)
    cloud.scale = (0.65, 0.65, 0.65)

    bpy.ops.wm.save_as_mainfile(filepath=BLEND_OUT)
    render_preview(col)
    print("[Living Assets Complete]")


if __name__ == "__main__":
    main()
