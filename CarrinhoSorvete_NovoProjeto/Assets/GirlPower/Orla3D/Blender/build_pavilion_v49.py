"""
build_pavilion_v49.py
---------------------
Blender 5.3 clean re-export of ONLY the Pavilion 3D models (preserving all v41 character FBXs!):
  1. PavilionBay.fbx            - Elevated mosaic terrace, south/north stairs, ONLY 4 outer corner
                                  columns (ZERO center column blocking the view!), compact OldChest
                                  tucked in outer back-right corner well clear of the table/benches.
  2. PavilionBay_RoofFade.fbx   - Separate fadeable mint roof + beams + front signboard facing -Z
                                  camera so the roof smoothly fades out whenever the player enters!
  3. PavilionFestivalTable.fbx  - Cozy long communal table + cream cloth + mint runner + benches,
                                  sized cleanly inside the terrace with wide perimeter walkway.
  4. PavilionFestivalLights.fbx - Restored signboard & glowing string lights on the FRONT (-Z) eave!

Coordinate reminder (FBX axis_forward='-Z', axis_up='Y', bake_space_transform=True):
  Blender +X -> Unity -X
  Blender +Y -> Unity -Z  (SOUTH / FRONT facing the camera!)
  Blender -Y -> Unity +Z  (NORTH / BACK facing boardwalk & sea!)
  Blender +Z -> Unity +Y  (UP!)
"""

import math
import os
import bpy
import bmesh
from mathutils import Vector

SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
ORLA3D_DIR = os.path.dirname(SCRIPT_DIR)
MODELS_DIR = os.path.join(ORLA3D_DIR, "Models")
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
    get_mat("AC_Gold", "#F5BE4F", 0.34)
    get_mat("AC_BulbWarm", "#FFF0B3", 0.20, "#FFE082", 2.8)
    get_mat("AC_LanternGlow", "#FFE9A8", 0.22, "#FFD56B", 2.4)


def clear_scene():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    MATERIALS.clear()


def ensure_collection(name):
    col = bpy.data.collections.new(name)
    bpy.context.scene.collection.children.link(col)
    return col


def add_mesh_obj(name, bm, mat, col, location=(0, 0, 0), rotation=(0, 0, 0), scale=(1, 1, 1)):
    mesh = bpy.data.meshes.new(name + "_Mesh")
    bm.to_mesh(mesh)
    bm.free()
    obj = bpy.data.objects.new(name, mesh)
    col.objects.link(obj)
    obj.location = location
    obj.rotation_euler = rotation
    obj.scale = scale
    if mat is not None:
        obj.data.materials.append(mat)
    for p in mesh.polygons:
        p.use_smooth = True
    return obj


def make_box(name, size, mat, col, location=(0, 0, 0), rotation=(0, 0, 0), bevel=0.03):
    bm = bmesh.new()
    bmesh.ops.create_cube(bm, size=1.0)
    for v in bm.verts:
        v.co.x *= size[0]
        v.co.y *= size[1]
        v.co.z *= size[2]
    if bevel > 0:
        bmesh.ops.bevel(bm, geom=list(bm.edges), offset=min(bevel, min(size) * 0.35), segments=2, affect="EDGES", clamp_overlap=True)
    return add_mesh_obj(name, bm, mat, col, location=location, rotation=rotation)


def make_cyl(name, r1, r2, depth, mat, col, location=(0, 0, 0), rotation=(0, 0, 0), segs=12, bevel=0.015):
    bm = bmesh.new()
    bmesh.ops.create_cone(bm, cap_ends=True, cap_tris=False, segments=segs, radius1=r1, radius2=r2, depth=depth)
    if bevel > 0:
        edges = [e for e in bm.edges if len(e.link_faces) == 2 and abs(e.link_faces[0].normal.dot(e.link_faces[1].normal)) < 0.7]
        if edges:
            bmesh.ops.bevel(bm, geom=edges, offset=min(bevel, min(r1, r2, depth) * 0.25), segments=1, affect="EDGES", clamp_overlap=True)
    return add_mesh_obj(name, bm, mat, col, location=location, rotation=rotation)


def make_sphere(name, radius, mat, col, location=(0, 0, 0), scale=(1, 1, 1), u_segs=12, v_segs=8):
    bm = bmesh.new()
    bmesh.ops.create_uvsphere(bm, u_segments=u_segs, v_segments=v_segs, radius=radius)
    return add_mesh_obj(name, bm, mat, col, location=location, scale=scale)


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


def build_pavilion_base(col):
    m_stone_w = MATERIALS["AC_StoneWhite"]
    m_stone = MATERIALS["AC_StoneWarm"]
    m_stone_d = MATERIALS["AC_StoneDark"]
    m_wood_d = MATERIALS["AC_WoodWarm"]
    m_mint = MATERIALS["AC_MintSoft"]
    m_lav = MATERIALS["AC_Lavender"]
    m_pink = MATERIALS["AC_PinkSoft"]
    m_cream = MATERIALS["AC_Cream"]
    m_gold = MATERIALS["AC_Gold"]

    parts = []
    # Main elevated stone pavilion terrace (top at z = 0.40)
    parts.append(make_box("PavBase", (5.6, 4.2, 0.36), m_stone, col, location=(0, 0, 0.16), bevel=0.05))
    parts.append(make_box("PavRim", (5.8, 4.4, 0.12), m_stone_w, col, location=(0, 0, 0.34), bevel=0.035))

    # Worn pastel mosaic tiles on the terrace floor (top at z = 0.415)
    mosaic_mats = [m_mint, m_lav, m_pink, m_cream, m_stone_d]
    for gx in range(-4, 5):
        for gy in range(-2, 3):
            if (gx + gy * 3) % 7 == 0:
                continue
            mat_m = mosaic_mats[(abs(gx) + abs(gy)) % len(mosaic_mats)]
            parts.append(make_box(f"Mos_{gx}_{gy}", (0.44, 0.44, 0.025), mat_m, col, location=(gx * 0.50, gy * 0.52, 0.402), bevel=0.01))

    # Front steps on SOUTH (+Y in Blender -> -Z in Unity, facing the camera!)
    parts.append(make_box("FrontStep1", (2.8, 0.56, 0.16), m_stone_w, col, location=(0, 2.38, 0.20), bevel=0.03))
    parts.append(make_box("FrontStep2", (3.0, 0.56, 0.14), m_stone, col, location=(0, 2.86, 0.07), bevel=0.03))

    # Back steps on NORTH (-Y in Blender -> +Z in Unity, leading smoothly toward boardwalk & beach!)
    parts.append(make_box("BackStep1", (2.6, 0.56, 0.16), m_stone_w, col, location=(0, -2.38, 0.20), bevel=0.03))
    parts.append(make_box("BackStep2", (2.8, 0.56, 0.14), m_stone, col, location=(0, -2.86, 0.07), bevel=0.03))

    # ONLY 4 outer corner columns (ZERO center column so the view into the Pavilion is 100% open!)
    for cx in (-2.45, 2.45):
        for cy in (-1.75, 1.75):
            parts.append(make_cyl(f"ColBase_{cx}_{cy}", 0.21, 0.17, 0.24, m_stone_w, col, location=(cx, cy, 0.50), segs=12))
            parts.append(make_cyl(f"Col_{cx}_{cy}", 0.13, 0.12, 2.48, m_cream, col, location=(cx, cy, 1.84), segs=12))

    # Compact wooden storage chest tucked cleanly in the outer back-right corner (Blender (-2.08, -1.32) -> Unity (+2.08, +1.32))
    # 100% clear of the Festival Table & Benches!
    parts.append(make_box("OldChest", (0.64, 0.42, 0.40), m_wood_d, col, location=(-2.08, -1.32, 0.60), bevel=0.035))
    parts.append(make_box("ChestLock", (0.12, 0.06, 0.14), m_gold, col, location=(-2.08, -1.10, 0.64), bevel=0.015))

    return join_and_export(parts, "PavilionBay", col)


def build_pavilion_roof_fade(col):
    m_stone_w = MATERIALS["AC_StoneWhite"]
    m_stone_d = MATERIALS["AC_StoneDark"]
    m_wood_d = MATERIALS["AC_WoodWarm"]
    m_mint = MATERIALS["AC_MintSoft"]
    m_cream = MATERIALS["AC_Cream"]
    m_gold = MATERIALS["AC_Gold"]

    parts = []
    # Airy coastal roof sitting atop the 4 corner columns (z = 3.08..3.68), fades cleanly when player enters!
    parts.append(make_box("RoofRim", (5.65, 4.25, 0.16), m_wood_d, col, location=(0, 0, 3.08), bevel=0.04))
    parts.append(make_box("RoofFront", (5.92, 2.38, 0.16), m_mint, col, location=(0, 1.08, 3.38), rotation=(-0.24, 0, 0), bevel=0.05))
    parts.append(make_box("RoofBack", (5.92, 2.38, 0.16), m_mint, col, location=(0, -1.08, 3.38), rotation=(0.24, 0, 0), bevel=0.05))
    parts.append(make_box("RoofRidge", (5.98, 0.24, 0.18), m_cream, col, location=(0, 0, 3.68), bevel=0.05))

    # Weathered signboard mounted on the FRONT (+Y in Blender -> -Z in Unity, facing the camera!)
    parts.append(make_box("FadedSignBoard", (3.1, 0.16, 0.66), m_stone_d, col, location=(0, 2.02, 3.58), rotation=(0, 0.03, 0), bevel=0.04))
    parts.append(make_box("FadedSignTrim", (3.22, 0.12, 0.76), m_wood_d, col, location=(0, 1.98, 3.58), rotation=(0, 0.03, 0), bevel=0.03))
    parts.append(make_sphere("FadedShell", 0.17, m_stone_w, col, location=(-1.18, 2.11, 3.58), scale=(1.0, 0.35, 0.9), u_segs=10, v_segs=8))
    parts.append(make_sphere("FadedCone", 0.15, m_gold, col, location=(1.18, 2.11, 3.58), scale=(0.8, 0.35, 1.1), u_segs=10, v_segs=8))

    return join_and_export(parts, "PavilionBay_RoofFade", col)


def build_pavilion_festival_table(col):
    m_wood = MATERIALS["AC_WoodWarm"]
    m_wood_l = MATERIALS["AC_WoodLight"]
    m_cream = MATERIALS["AC_Cream"]
    m_mint = MATERIALS["AC_MintSoft"]
    m_pink = MATERIALS["AC_PinkSoft"]

    parts = []
    # Cozy communal table centered on the mosaic floor (origin at floor z=0), compact enough for wide walkway all around!
    parts.append(make_box("TableTop", (2.95, 1.12, 0.10), m_wood_l, col, location=(0, 0, 0.78), bevel=0.03))
    parts.append(make_box("ClothTop", (3.02, 1.20, 0.18), m_cream, col, location=(0, 0, 0.77), bevel=0.05))
    parts.append(make_box("Runner", (3.06, 0.42, 0.20), m_mint, col, location=(0, 0, 0.775), bevel=0.04))
    # Cute scalloped overhang tabs along FRONT (+Y in Blender -> -Z in Unity, facing camera!)
    for i in range(7):
        sx = -1.26 + i * 0.42
        parts.append(make_sphere(f"ClothScallop_{i}", 0.15, m_pink if i % 2 else m_cream, col, location=(sx, 0.60, 0.64), scale=(1.25, 0.35, 0.8), u_segs=10, v_segs=6))
    for lx in (-1.22, 1.22):
        for ly in (-0.42, 0.42):
            parts.append(make_cyl(f"TLeg_{lx}_{ly}", 0.06, 0.06, 0.76, m_wood, col, location=(lx, ly, 0.38), segs=10))
    # Two cozy benches neatly tucked alongside the table (zero overlap with OldChest!)
    for by in (-0.88, 0.88):
        parts.append(make_box(f"TBench_{by}", (2.34, 0.34, 0.08), m_wood_l, col, location=(0, by, 0.40), bevel=0.025))
        for bx in (-0.92, 0.92):
            parts.append(make_box(f"TBenchLeg_{by}_{bx}", (0.10, 0.28, 0.40), m_wood, col, location=(bx, by, 0.20), bevel=0.02))

    return join_and_export(parts, "PavilionFestivalTable", col)


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
    # Restored radiant signboard on FRONT (+Y -> -Z facing camera!)
    parts.append(make_box("RestoredTrim", (3.32, 0.20, 0.82), m_gold, col, location=(0, 2.02, 3.60), bevel=0.05))
    parts.append(make_box("RestoredFace", (3.10, 0.24, 0.64), m_plum, col, location=(0, 2.04, 3.60), bevel=0.04))
    for i in range(9):
        bx = -1.40 + i * 0.35
        parts.append(make_sphere(f"SignBulbTop_{i}", 0.055, m_bulb, col, location=(bx, 2.16, 3.96), u_segs=8, v_segs=6))
        parts.append(make_sphere(f"SignBulbBot_{i}", 0.055, m_bulb, col, location=(bx, 2.16, 3.24), u_segs=8, v_segs=6))

    # Festive draped string lights & colorful pennants along the FRONT (+Y -> -Z) eave!
    pennant_mats = [m_pink, m_yel, m_mint, m_coral]
    for i in range(13):
        u = i / 12.0 - 0.5
        fx = u * 5.2
        sag = -0.28 * (1.0 - (2 * u) ** 2)
        parts.append(make_sphere(f"PavBulbF_{i}", 0.08, m_glow, col, location=(fx, 1.98, 2.98 + sag), u_segs=8, v_segs=6))
        if i % 2 == 0:
            parts.append(make_cyl(f"PavPenF_{i}", 0.01, 0.13, 0.24, pennant_mats[i % 4], col, location=(fx, 1.98, 2.78 + sag), segs=3, bevel=0.0))

    # Hanging glowing paper lanterns at the 4 outer corners of the Pavilion!
    for lx in (-2.30, 2.30):
        for ly in (-1.62, 1.62):
            parts.append(make_sphere(f"PaperLantern_{lx}_{ly}", 0.20, m_glow, col, location=(lx, ly, 2.66), scale=(1.0, 1.0, 1.15), u_segs=12, v_segs=10))
            parts.append(make_cyl(f"LanternCap_{lx}_{ly}", 0.11, 0.11, 0.06, m_coral, col, location=(lx, ly, 2.88), segs=10))

    return join_and_export(parts, "PavilionFestivalLights", col)


def main():
    clear_scene()
    init_palette()
    col = ensure_collection("Pavilion_v49")

    build_pavilion_base(col)
    build_pavilion_roof_fade(col)
    build_pavilion_festival_table(col)
    build_pavilion_festival_lights(col)
    print("[Pavilion v49 Export Complete]")


if __name__ == "__main__":
    main()
