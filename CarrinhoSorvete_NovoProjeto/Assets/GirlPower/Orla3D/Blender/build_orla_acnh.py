"""
build_orla_acnh.py
------------------
Procedural 3D Asset & Diorama Generator in Blender for:
"Girl Power - Orla de Matinhos 3D" (Animal Crossing: New Horizons Cozy Aesthetic).

Generates:
  1. Complete assembled Blender scene: Orla_Matinhos_ACNH.blend
  2. High-quality modular FBX models for Unity (Assets/GirlPower/Orla3D/Models/*.fbx)
  3. Beauty render preview: orla_blender_preview.png

Compatible with Blender 4.x and 5.x.
Run via:
  blender --background --python build_orla_acnh.py
"""

import math
import os
import random
import bpy
import bmesh
from mathutils import Vector, Matrix, Euler

# ==============================================================================
# CONFIGURATION & PATHS
# ==============================================================================

SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
ORLA3D_DIR = os.path.dirname(SCRIPT_DIR)
MODELS_DIR = os.path.join(ORLA3D_DIR, "Models")
BLEND_OUT = os.path.join(SCRIPT_DIR, "Orla_Matinhos_ACNH.blend")
PREVIEW_OUT = os.path.join(SCRIPT_DIR, "orla_blender_preview.png")

os.makedirs(MODELS_DIR, exist_ok=True)


# ==============================================================================
# MATERIAL PALETTE (Animal Crossing: New Horizons Cozy Coastal Palette)
# ==============================================================================

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


MATERIALS = {}


def get_mat(name, hex_color, roughness=0.58, metallic=0.0, emission_hex=None, emission_strength=0.0, alpha=1.0):
    if name in MATERIALS:
        return MATERIALS[name]
    mat = bpy.data.materials.new(name=name)
    if hasattr(mat, "use_nodes") and not mat.use_nodes:
        mat.use_nodes = True
    nt = mat.node_tree
    bsdf = None
    for node in nt.nodes:
        if node.type == "BSDF_PRINCIPLED":
            bsdf = node
            break
    if bsdf is None:
        bsdf = nt.nodes.new("ShaderNodeBsdfPrincipled")
    rgba = hex_to_rgba(hex_color, alpha)
    if "Base Color" in bsdf.inputs:
        bsdf.inputs["Base Color"].default_value = rgba
    if "Roughness" in bsdf.inputs:
        bsdf.inputs["Roughness"].default_value = roughness
    if "Metallic" in bsdf.inputs:
        bsdf.inputs["Metallic"].default_value = metallic
    if "Specular IOR Level" in bsdf.inputs:
        bsdf.inputs["Specular IOR Level"].default_value = 0.35
    if "Alpha" in bsdf.inputs and alpha < 1.0:
        bsdf.inputs["Alpha"].default_value = alpha
    if emission_hex and emission_strength > 0.0:
        em_rgba = hex_to_rgba(emission_hex, 1.0)
        if "Emission Color" in bsdf.inputs:
            bsdf.inputs["Emission Color"].default_value = em_rgba
        if "Emission Strength" in bsdf.inputs:
            bsdf.inputs["Emission Strength"].default_value = emission_strength
    mat.diffuse_color = rgba
    MATERIALS[name] = mat
    return mat


def init_palette():
    # Coastal nature
    get_mat("AC_Sand", "#F7E2B8", roughness=0.82)
    get_mat("AC_SandWet", "#E2C491", roughness=0.55)
    get_mat("AC_Grass", "#70C46B", roughness=0.78)
    get_mat("AC_GrassLight", "#91DA7B", roughness=0.76)
    get_mat("AC_Ocean", "#35B8C8", roughness=0.18)
    get_mat("AC_OceanShallow", "#6BE0DC", roughness=0.22)
    get_mat("AC_Foam", "#F5FCFF", roughness=0.45)
    get_mat("AC_HillDistant", "#66A38A", roughness=0.85)
    get_mat("AC_HillFar", "#7A9AB0", roughness=0.88)

    # Pavements & Stone
    get_mat("AC_StoneWhite", "#F2ECE0", roughness=0.72)
    get_mat("AC_StoneWarm", "#DFD3BE", roughness=0.74)
    get_mat("AC_StoneDark", "#B8A994", roughness=0.78)
    get_mat("AC_CobbleA", "#D8B99B", roughness=0.75)
    get_mat("AC_CobbleB", "#C59F82", roughness=0.76)
    get_mat("AC_PlazaTile", "#EFE1CA", roughness=0.72)
    get_mat("AC_PlazaAccent", "#E8C9A6", roughness=0.72)
    get_mat("AC_Asphalt", "#636E7B", roughness=0.85)
    get_mat("AC_RoadLine", "#F6E2A2", roughness=0.75)

    # Wood & Architectural
    get_mat("AC_WoodWarm", "#B97A4B", roughness=0.68)
    get_mat("AC_WoodLight", "#D89E6A", roughness=0.65)
    get_mat("AC_WoodDark", "#7B4C2F", roughness=0.70)
    get_mat("AC_Cream", "#FFF6E5", roughness=0.52)
    get_mat("AC_PinkBerry", "#F26E97", roughness=0.50)
    get_mat("AC_PinkSoft", "#FFA9C2", roughness=0.50)
    get_mat("AC_MintTeal", "#4EC7B6", roughness=0.48)
    get_mat("AC_MintSoft", "#8FE3D6", roughness=0.50)
    get_mat("AC_PurplePlum", "#5B3B6E", roughness=0.52)
    get_mat("AC_Lavender", "#B89CE0", roughness=0.55)
    get_mat("AC_Coral", "#F27B66", roughness=0.52)
    get_mat("AC_SunnyYellow", "#FFD45E", roughness=0.50)
    get_mat("AC_SkyBlue", "#6CC0F0", roughness=0.48)
    get_mat("AC_Glass", "#B8F0FA", roughness=0.10, alpha=0.62)
    get_mat("AC_MetalDark", "#3F4959", roughness=0.35, metallic=0.65)
    get_mat("AC_MetalSilver", "#D4DCE6", roughness=0.25, metallic=0.75)
    get_mat("AC_Gold", "#F5BE4F", roughness=0.32, metallic=0.45)

    # Emissive Warm Lights
    get_mat("AC_LanternGlow", "#FFE69C", roughness=0.25, emission_hex="#FFD26B", emission_strength=3.2)
    get_mat("AC_BulbWarm", "#FFF0B3", roughness=0.20, emission_hex="#FFE082", emission_strength=2.8)

    # Foliage & Fruits & Flowers
    get_mat("AC_PalmTrunk", "#C48A5A", roughness=0.76)
    get_mat("AC_PalmRing", "#9F6A40", roughness=0.78)
    get_mat("AC_PalmLeaf", "#4EBF5C", roughness=0.56)
    get_mat("AC_PalmLeafTip", "#7BE06C", roughness=0.54)
    get_mat("AC_Coconut", "#6D4127", roughness=0.52)
    get_mat("AC_CoconutGreen", "#5DBB53", roughness=0.45)
    get_mat("AC_BushGreen", "#52B55E", roughness=0.68)
    get_mat("AC_BushLight", "#7CD46E", roughness=0.65)
    get_mat("AC_Terracotta", "#D47551", roughness=0.72)
    get_mat("AC_Orange", "#FF9333", roughness=0.44)
    get_mat("AC_Strawberry", "#FF6E8D", roughness=0.42)
    get_mat("AC_Chocolate", "#6E422C", roughness=0.45)
    get_mat("AC_Vanilla", "#FFF3D1", roughness=0.45)
    get_mat("AC_Waffle", "#E3A75B", roughness=0.62)
    get_mat("AC_FlowerPink", "#FF7AA8", roughness=0.50)
    get_mat("AC_FlowerYellow", "#FFE055", roughness=0.50)
    get_mat("AC_FlowerBlue", "#75BFFF", roughness=0.50)
    get_mat("AC_FlowerWhite", "#FFFDF7", roughness=0.50)
    get_mat("AC_Starfish", "#FF8566", roughness=0.60)
    get_mat("AC_Shell", "#FFE8D6", roughness=0.45)

    # Characters (Chibi Animal Crossing style)
    get_mat("AC_SkinWarm", "#C9855E", roughness=0.58)
    get_mat("AC_SkinLight", "#F4C29F", roughness=0.58)
    get_mat("AC_SkinTan", "#DF9F73", roughness=0.58)
    get_mat("AC_CheekBlush", "#FF8EA6", roughness=0.55)
    get_mat("AC_EyeDark", "#251D29", roughness=0.15)
    get_mat("AC_EyeShine", "#FFFFFF", roughness=0.10, emission_hex="#FFFFFF", emission_strength=0.4)
    get_mat("AC_HairPlum", "#723678", roughness=0.48)
    get_mat("AC_HairBrown", "#6B442A", roughness=0.52)
    get_mat("AC_HairDark", "#332A38", roughness=0.50)
    get_mat("AC_HairAuburn", "#B85337", roughness=0.50)
    get_mat("AC_Denim", "#4E78A8", roughness=0.72)
    get_mat("AC_StrawHat", "#F2CF7E", roughness=0.75)
    get_mat("AC_ShoeDark", "#4E3A4C", roughness=0.55)


# ==============================================================================
# SCENE & COLLECTION HELPERS
# ==============================================================================

def clear_scene():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    MATERIALS.clear()


def ensure_collection(name, parent=None):
    if name in bpy.data.collections:
        return bpy.data.collections[name]
    col = bpy.data.collections.new(name)
    if parent is None:
        bpy.context.scene.collection.children.link(col)
    else:
        parent.children.link(col)
    return col


def link_obj(obj, col):
    for c in list(obj.users_collection):
        c.objects.unlink(obj)
    col.objects.link(obj)
    return obj


def set_smooth(mesh, smooth=True):
    for poly in mesh.polygons:
        poly.use_smooth = smooth


def add_mesh_obj(name, bm, mat, col, location=(0, 0, 0), rotation=(0, 0, 0), scale=(1, 1, 1), smooth=True):
    mesh = bpy.data.meshes.new(name + "_Mesh")
    bm.normal_update()
    bm.to_mesh(mesh)
    bm.free()
    set_smooth(mesh, smooth)
    if mat is not None:
        mesh.materials.append(mat)
    obj = bpy.data.objects.new(name, mesh)
    obj.location = location
    obj.rotation_euler = rotation
    obj.scale = scale
    link_obj(obj, col)
    return obj


def make_box(name, size, mat, col, location=(0, 0, 0), rotation=(0, 0, 0), bevel=0.04, segments=2):
    bm = bmesh.new()
    bmesh.ops.create_cube(bm, size=1.0)
    bmesh.ops.scale(bm, vec=Vector(size), verts=bm.verts)
    if bevel > 0.0:
        max_b = min(size[0], size[1], size[2]) * 0.42
        b = min(bevel, max_b)
        bmesh.ops.bevel(
            bm,
            geom=list(bm.edges),
            offset=b,
            segments=segments,
            affect="EDGES",
            clamp_overlap=True,
        )
    return add_mesh_obj(name, bm, mat, col, location=location, rotation=rotation, smooth=True)


def make_cylinder(name, r1, r2, depth, mat, col, location=(0, 0, 0), rotation=(0, 0, 0), segs=16, bevel=0.015):
    bm = bmesh.new()
    bmesh.ops.create_cone(
        bm,
        cap_ends=True,
        cap_tris=False,
        segments=segs,
        radius1=r1,
        radius2=r2,
        depth=depth,
    )
    if bevel > 0.0:
        cap_edges = [e for e in bm.edges if abs(e.verts[0].co.z - e.verts[1].co.z) < 1e-4]
        if cap_edges:
            bmesh.ops.bevel(
                bm,
                geom=cap_edges,
                offset=min(bevel, min(r1, r2, depth * 0.4) * 0.45) if min(r1, r2) > 0.005 else 0.0,
                segments=2,
                affect="EDGES",
                clamp_overlap=True,
            )
    return add_mesh_obj(name, bm, mat, col, location=location, rotation=rotation, smooth=True)


def make_sphere(name, radius, mat, col, location=(0, 0, 0), scale=(1, 1, 1), rotation=(0, 0, 0), u_segs=16, v_segs=12):
    bm = bmesh.new()
    bmesh.ops.create_uvsphere(bm, u_segments=u_segs, v_segments=v_segs, radius=radius)
    return add_mesh_obj(name, bm, mat, col, location=location, rotation=rotation, scale=scale, smooth=True)


def make_capsule(name, radius, height, mat, col, location=(0, 0, 0), rotation=(0, 0, 0), segs=14):
    """Vertical rounded capsule centered at origin of total height (including caps)."""
    cyl_h = max(0.01, height - 2.0 * radius)
    bm = bmesh.new()
    bmesh.ops.create_uvsphere(bm, u_segments=segs, v_segments=10, radius=radius)
    for v in bm.verts:
        if v.co.z > 0:
            v.co.z += cyl_h * 0.5
        elif v.co.z < 0:
            v.co.z -= cyl_h * 0.5
    return add_mesh_obj(name, bm, mat, col, location=location, rotation=rotation, smooth=True)


def make_dome(name, radius, height, mat, col, location=(0, 0, 0), segs=16, rings=7):
    """Cozy parasol / rounded awning dome (open underside closed with flat cap)."""
    bm = bmesh.new()
    ring_verts = []
    for r in range(rings + 1):
        t = r / rings  # 0 at apex, 1 at rim
        ang = t * (math.pi * 0.5)
        rad = math.sin(ang) * radius
        z = math.cos(ang) * height
        if r == 0:
            v_top = bm.verts.new((0, 0, height))
            ring_verts.append([v_top])
        else:
            row = []
            for s in range(segs):
                a = (s / segs) * math.tau
                # Slight scalloped edge at rim
                scallop = 1.0 - 0.04 * abs(math.sin(a * segs * 0.25)) if r == rings else 1.0
                row.append(bm.verts.new((math.cos(a) * rad * scallop, math.sin(a) * rad * scallop, z)))
            ring_verts.append(row)
    # Apex triangles
    top_v = ring_verts[0][0]
    first_row = ring_verts[1]
    for s in range(segs):
        bm.faces.new((top_v, first_row[s], first_row[(s + 1) % segs]))
    # Quads
    for r in range(1, rings):
        row_a = ring_verts[r]
        row_b = ring_verts[r + 1]
        for s in range(segs):
            s2 = (s + 1) % segs
            bm.faces.new((row_a[s], row_b[s], row_b[s2], row_a[s2]))
    # Rim underside
    bottom_center = bm.verts.new((0, 0, height * 0.08))
    rim = ring_verts[-1]
    for s in range(segs):
        s2 = (s + 1) % segs
        bm.faces.new((bottom_center, rim[s2], rim[s]))
    return add_mesh_obj(name, bm, mat, col, location=location, smooth=True)


def join_collection_to_single_mesh(col, final_name, target_col):
    """Joins all mesh objects in col into one clean multi-material mesh with origin at (0,0,0)."""
    objs = [o for o in col.objects if o.type == "MESH"]
    if not objs:
        return None
    bpy.ops.object.select_all(action="DESELECT")
    for o in objs:
        o.select_set(True)
    bpy.context.view_layer.objects.active = objs[0]
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    if len(objs) > 1:
        bpy.ops.object.join()
    joined = bpy.context.view_layer.objects.active
    joined.name = final_name
    joined.data.name = final_name + "_Mesh"
    # Ensure origin at world 0,0,0
    bpy.context.scene.cursor.location = (0, 0, 0)
    bpy.ops.object.origin_set(type="ORIGIN_CURSOR")
    link_obj(joined, target_col)
    # Remove temp collection
    if col.name in bpy.data.collections and col != target_col:
        bpy.data.collections.remove(col)
    return joined


def export_fbx(obj, filename):
    """Exports a single object cleanly to FBX formatted for Unity."""
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    filepath = os.path.join(MODELS_DIR, filename)
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


# ==============================================================================
# ASSET BUILDERS (All authored at origin (0,0,0), Z-up in Blender, facing -Y)
# Note: In Unity after FBX export (-Z forward, Y up), Blender -Y faces Unity -Z (toward camera!)
# ==============================================================================

def build_palm_tree(target_col):
    tmp = ensure_collection("_tmp_PalmTree")
    m_trunk = MATERIALS["AC_PalmTrunk"]
    m_ring = MATERIALS["AC_PalmRing"]
    m_leaf = MATERIALS["AC_PalmLeaf"]
    m_tip = MATERIALS["AC_PalmLeafTip"]
    m_coco = MATERIALS["AC_Coconut"]

    # Gently arched segmented coconut trunk
    segs = 9
    height = 3.85
    curve_x = 0.55
    prev_pos = Vector((0, 0, 0))
    top_pos = Vector((0, 0, height))
    for i in range(segs):
        t0 = i / segs
        t1 = (i + 1) / segs
        tm = (t0 + t1) * 0.5
        p0 = Vector((math.sin(t0 * 1.15) * curve_x, 0.0, t0 * height))
        p1 = Vector((math.sin(t1 * 1.15) * curve_x, 0.0, t1 * height))
        pm = (p0 + p1) * 0.5
        seg_vec = p1 - p0
        seg_len = seg_vec.length * 1.08
        r_bot = 0.24 * (1.0 - t0 * 0.45)
        r_top = 0.22 * (1.0 - t1 * 0.45)
        tilt_y = math.atan2(seg_vec.x, seg_vec.z)
        make_cylinder(
            f"TrunkSeg_{i}", r_bot, r_top, seg_len,
            m_trunk if i % 2 == 0 else m_ring,
            tmp, location=pm, rotation=(0, tilt_y, 0), segs=12, bevel=0.02
        )
        top_pos = p1

    # Plump coconut cluster under fronds
    for k in range(3):
        ang = k * (math.tau / 3.0) + 0.4
        cpos = top_pos + Vector((math.cos(ang) * 0.21, math.sin(ang) * 0.21, -0.18))
        make_sphere(f"Coconut_{k}", 0.16, m_coco, tmp, location=cpos, scale=(1.0, 1.0, 1.12), u_segs=12, v_segs=10)

    # Lush Animal Crossing plump arched palm fronds (8 fronds around crown)
    num_fronds = 8
    for f in range(num_fronds):
        yaw = (f / num_fronds) * math.tau + (0.18 if f % 2 else 0.0)
        pitch_base = 0.25 if f % 2 == 0 else 0.52
        frond_len = 1.95 if f % 2 == 0 else 1.72
        # Build each frond from 5 plump tapering arched leaflets
        for s in range(5):
            u = (s + 0.5) / 5.0
            dist = u * frond_len
            arch_z = math.sin(u * math.pi * 0.72) * 0.55 - (u * u) * 0.78 + (0.12 if f % 2 else 0.0)
            lx = math.cos(yaw) * dist
            ly = math.sin(yaw) * dist
            pos = top_pos + Vector((lx, ly, arch_z + 0.06))
            width = 0.36 * math.sin(u * math.pi * 0.85 + 0.22)
            length = (frond_len / 5.0) * 0.82
            # Slope angle along frond
            droop = -0.35 + u * 1.15 - (0.15 if f % 2 else 0.0)
            rot = Euler((0.0, droop, yaw), "XYZ")
            mat_leaf = m_tip if s >= 3 else m_leaf
            make_sphere(
                f"Frond_{f}_{s}", 1.0, mat_leaf, tmp,
                location=pos,
                rotation=rot,
                scale=(length, width, 0.09),
                u_segs=10, v_segs=8
            )

    # Little top tuft crown
    make_sphere("PalmCrown", 0.22, m_leaf, tmp, location=top_pos + Vector((0, 0, 0.06)), scale=(1.0, 1.0, 0.7))

    return join_collection_to_single_mesh(tmp, "PalmTree", target_col)


def build_kiosk_girl_power(target_col):
    tmp = ensure_collection("_tmp_Kiosk")
    m_wood = MATERIALS["AC_WoodWarm"]
    m_wood_d = MATERIALS["AC_WoodDark"]
    m_mint = MATERIALS["AC_MintTeal"]
    m_pink = MATERIALS["AC_PinkBerry"]
    m_pink_s = MATERIALS["AC_PinkSoft"]
    m_cream = MATERIALS["AC_Cream"]
    m_plum = MATERIALS["AC_PurplePlum"]
    m_gold = MATERIALS["AC_Gold"]
    m_glass = MATERIALS["AC_Glass"]
    m_bulb = MATERIALS["AC_BulbWarm"]
    m_bush = MATERIALS["AC_BushGreen"]
    m_pot = MATERIALS["AC_Terracotta"]
    m_waffle = MATERIALS["AC_Waffle"]

    # Raised wooden deck base
    make_box("Deck", (4.6, 2.5, 0.22), m_wood, tmp, location=(0, 0, 0.11), bevel=0.05)
    make_box("DeckTrim", (4.72, 2.62, 0.08), m_wood_d, tmp, location=(0, 0, 0.04), bevel=0.03)

    # Main counter body (mint teal planks + pink horizontal ribbon + cream marble countertop)
    make_box("CounterBody", (3.8, 1.15, 1.05), m_mint, tmp, location=(0, -0.35, 0.74), bevel=0.06)
    make_box("CounterPinkStripe", (3.86, 1.19, 0.20), m_pink, tmp, location=(0, -0.35, 0.64), bevel=0.03)
    make_box("CounterTop", (4.08, 1.38, 0.16), m_cream, tmp, location=(0, -0.35, 1.30), bevel=0.05)

    # Vertical counter plank accents (cozy woodwork)
    for i in range(-5, 6):
        make_box(f"Plank_{i}", (0.05, 0.06, 0.96), m_wood_d, tmp, location=(i * 0.34, -0.92, 0.74), bevel=0.01)

    # Back wall & menu board inside kiosk
    make_box("BackWall", (3.9, 0.18, 2.25), m_wood, tmp, location=(0, 0.88, 1.34), bevel=0.04)
    make_box("MenuBoard", (1.45, 0.08, 0.82), m_plum, tmp, location=(-0.85, 0.76, 1.92), bevel=0.03)
    make_box("MenuLine1", (1.10, 0.04, 0.07), m_cream, tmp, location=(-0.85, 0.72, 2.12), bevel=0.01)
    make_box("MenuLine2", (0.90, 0.04, 0.06), m_pink_s, tmp, location=(-0.95, 0.72, 1.94), bevel=0.01)
    make_box("MenuLine3", (0.98, 0.04, 0.06), MATERIALS["AC_SunnyYellow"], tmp, location=(-0.91, 0.72, 1.76), bevel=0.01)

    # Back shelf with syrup bottles
    make_box("BackShelf", (1.25, 0.28, 0.08), m_wood_d, tmp, location=(0.95, 0.68, 1.62), bevel=0.02)
    bottle_mats = [m_pink, m_mint, MATERIALS["AC_SunnyYellow"], MATERIALS["AC_SkyBlue"]]
    for idx, bm_mat in enumerate(bottle_mats):
        bx = 0.52 + idx * 0.28
        make_cylinder(f"Bottle_{idx}", 0.07, 0.07, 0.28, bm_mat, tmp, location=(bx, 0.68, 1.80), segs=10, bevel=0.02)
        make_cylinder(f"BottleCap_{idx}", 0.04, 0.04, 0.08, m_cream, tmp, location=(bx, 0.68, 1.96), segs=10, bevel=0.01)

    # Glass ice-cream display case with 4 pastel tubs
    make_box("DisplayBase", (1.72, 0.62, 0.12), m_cream, tmp, location=(0, -0.32, 1.43), bevel=0.03)
    tub_mats = [MATERIALS["AC_Strawberry"], MATERIALS["AC_Vanilla"], MATERIALS["AC_Chocolate"], MATERIALS["AC_MintSoft"]]
    for idx, tm in enumerate(tub_mats):
        tx = -0.57 + idx * 0.38
        make_box(f"IceTub_{idx}", (0.31, 0.42, 0.18), tm, tmp, location=(tx, -0.32, 1.56), bevel=0.04)
        make_sphere(f"ScoopMound_{idx}", 0.13, tm, tmp, location=(tx, -0.32, 1.66), scale=(1.0, 1.2, 0.65), u_segs=10, v_segs=8)
    # Glass hood
    make_box("GlassHood", (1.74, 0.64, 0.42), m_glass, tmp, location=(0, -0.32, 1.66), bevel=0.03)

    # 4 sturdy chunky corner posts
    for px in (-1.92, 1.92):
        for py in (-0.82, 0.82):
            make_cylinder(f"Post_{px}_{py}", 0.12, 0.11, 2.55, m_wood_d, tmp, location=(px, py, 1.48), segs=12, bevel=0.02)

    # Sloped striped scalloped awning roof (Animal Crossing chunky awning!)
    num_stripes = 10
    stripe_w = 4.50 / num_stripes
    for i in range(num_stripes):
        sx = -2.25 + (i + 0.5) * stripe_w
        mat_s = m_pink if i % 2 == 0 else m_cream
        # Main sloped awning segment
        make_box(f"AwningSlope_{i}", (stripe_w * 1.01, 2.45, 0.18), mat_s, tmp, location=(sx, -0.10, 2.95), rotation=(0.24, 0, 0), bevel=0.04)
        # Cute rounded front scallop tab
        make_cylinder(f"Scallop_{i}", stripe_w * 0.48, stripe_w * 0.48, 0.14, mat_s, tmp, location=(sx, -1.28, 2.56), rotation=(math.pi * 0.5, 0, 0), segs=12, bevel=0.02)

    # Warm fairy light bulbs under awning front edge
    for i in range(9):
        lx = -1.80 + i * 0.45
        make_sphere(f"AwningBulb_{i}", 0.065, m_bulb, tmp, location=(lx, -1.16, 2.46), u_segs=10, v_segs=8)

    # Top header wooden plaque (for GIRL POWER sign)
    make_box("HeaderBack", (2.75, 0.18, 0.72), m_plum, tmp, location=(0, -0.58, 3.55), bevel=0.05)
    make_box("HeaderTrimTop", (2.82, 0.22, 0.08), m_gold, tmp, location=(0, -0.58, 3.90), bevel=0.02)
    make_box("HeaderTrimBot", (2.82, 0.22, 0.08), m_gold, tmp, location=(0, -0.58, 3.20), bevel=0.02)

    # Cute 3D giant ice-cream cone ornaments flanking the header!
    for side in (-1.06, 1.06):
        make_cylinder(f"ConeOrn_{side}", 0.02, 0.13, 0.32, m_waffle, tmp, location=(side, -0.68, 3.42), segs=10, bevel=0.01)
        make_sphere(f"ScoopOrn_{side}", 0.15, m_pink_s, tmp, location=(side, -0.68, 3.63), u_segs=12, v_segs=10)
        make_sphere(f"CherryOrn_{side}", 0.045, m_pink, tmp, location=(side, -0.68, 3.78), u_segs=8, v_segs=6)

    # Two cozy potted bush planters flanking the kiosk
    for side in (-2.42, 2.42):
        make_cylinder(f"Pot_{side}", 0.26, 0.34, 0.42, m_pot, tmp, location=(side, -0.55, 0.22), segs=12, bevel=0.03)
        make_sphere(f"Bush_{side}", 0.38, m_bush, tmp, location=(side, -0.55, 0.62), scale=(1.0, 1.0, 0.9), u_segs=12, v_segs=10)
        make_sphere(f"BushFlowerA_{side}", 0.07, MATERIALS["AC_FlowerPink"], tmp, location=(side - 0.14, -0.82, 0.72), u_segs=8, v_segs=6)
        make_sphere(f"BushFlowerB_{side}", 0.06, MATERIALS["AC_FlowerYellow"], tmp, location=(side + 0.16, -0.78, 0.64), u_segs=8, v_segs=6)

    return join_collection_to_single_mesh(tmp, "KioskGirlPower", target_col)


def build_ice_cream_cart(target_col):
    tmp = ensure_collection("_tmp_IceCreamCart")
    m_mint = MATERIALS["AC_MintTeal"]
    m_pink = MATERIALS["AC_PinkBerry"]
    m_pink_s = MATERIALS["AC_PinkSoft"]
    m_cream = MATERIALS["AC_Cream"]
    m_wood = MATERIALS["AC_WoodWarm"]
    m_wood_d = MATERIALS["AC_WoodDark"]
    m_silver = MATERIALS["AC_MetalSilver"]
    m_gold = MATERIALS["AC_Gold"]
    m_waffle = MATERIALS["AC_Waffle"]

    # Chunky cart body
    make_box("CartBody", (1.68, 0.96, 0.86), m_mint, tmp, location=(0, 0, 0.78), bevel=0.07)
    make_box("CartStripe", (1.72, 1.00, 0.18), m_pink, tmp, location=(0, 0, 0.60), bevel=0.03)
    make_box("CartTop", (1.84, 1.08, 0.14), m_cream, tmp, location=(0, 0, 1.26), bevel=0.05)

    # Freezer lids
    make_box("LidL", (0.52, 0.62, 0.08), m_silver, tmp, location=(-0.38, 0, 1.35), bevel=0.02)
    make_box("LidR", (0.52, 0.62, 0.08), m_silver, tmp, location=(0.24, 0, 1.35), bevel=0.02)

    # Cute front emblem medallion with miniature ice cream cone
    make_cylinder("EmblemDisk", 0.24, 0.24, 0.06, m_cream, tmp, location=(0, -0.49, 0.76), rotation=(math.pi * 0.5, 0, 0), segs=16, bevel=0.015)
    make_cylinder("EmblemCone", 0.015, 0.08, 0.18, m_waffle, tmp, location=(0, -0.53, 0.69), segs=10, bevel=0.005)
    make_sphere("EmblemScoop", 0.095, m_pink_s, tmp, location=(0, -0.53, 0.83), u_segs=10, v_segs=8)

    # Giant cute ice cream display cone on right corner of cart
    make_cylinder("BigCone", 0.03, 0.16, 0.34, m_waffle, tmp, location=(0.66, -0.18, 1.48), segs=12, bevel=0.01)
    make_sphere("BigScoop1", 0.18, m_pink_s, tmp, location=(0.66, -0.18, 1.72), u_segs=12, v_segs=10)
    make_sphere("BigScoop2", 0.14, MATERIALS["AC_Vanilla"], tmp, location=(0.56, -0.14, 1.88), u_segs=12, v_segs=10)

    # Wooden push handle on left side
    for hy in (-0.32, 0.32):
        make_cylinder(f"HandleArm_{hy}", 0.04, 0.04, 0.45, m_wood_d, tmp, location=(-1.02, hy, 1.12), rotation=(0, 1.25, 0), segs=10, bevel=0.01)
    make_cylinder("HandleGrip", 0.055, 0.055, 0.76, m_pink, tmp, location=(-1.22, 0, 1.18), rotation=(math.pi * 0.5, 0, 0), segs=12, bevel=0.02)

    # Big chunky spoked wheels (left & right front/back) + small caster wheel
    for wx in (-0.52, 0.50):
        for wy in (-0.50, 0.50):
            make_cylinder(f"WheelTire_{wx}_{wy}", 0.32, 0.32, 0.11, m_wood_d, tmp, location=(wx, wy, 0.32), rotation=(math.pi * 0.5, 0, 0), segs=16, bevel=0.025)
            make_cylinder(f"WheelRim_{wx}_{wy}", 0.22, 0.22, 0.125, m_cream, tmp, location=(wx, wy, 0.32), rotation=(math.pi * 0.5, 0, 0), segs=14, bevel=0.01)
            make_cylinder(f"WheelHub_{wx}_{wy}", 0.09, 0.09, 0.15, m_gold, tmp, location=(wx, wy, 0.32), rotation=(math.pi * 0.5, 0, 0), segs=10, bevel=0.01)

    # Parasol pole & plump striped umbrella dome
    make_cylinder("UmbrellaPole", 0.055, 0.055, 1.55, m_wood, tmp, location=(0, 0, 2.02), segs=12, bevel=0.01)
    # Build 8 alternating colored umbrella wedges
    segs = 8
    radius = 1.28
    dome_h = 0.54
    z0 = 2.48
    for i in range(segs):
        a0 = (i / segs) * math.tau
        a1 = ((i + 1) / segs) * math.tau
        am = (a0 + a1) * 0.5
        bm = bmesh.new()
        rings = 5
        rows = []
        v_top = bm.verts.new((0, 0, z0 + dome_h))
        for r in range(1, rings + 1):
            t = r / rings
            ang = t * (math.pi * 0.5)
            rad = math.sin(ang) * radius
            zz = z0 + math.cos(ang) * dome_h
            row = [
                bm.verts.new((math.cos(a0) * rad, math.sin(a0) * rad, zz)),
                bm.verts.new((math.cos(am) * rad * 1.03, math.sin(am) * rad * 1.03, zz)),
                bm.verts.new((math.cos(a1) * rad, math.sin(a1) * rad, zz)),
            ]
            rows.append(row)
        bm.faces.new((v_top, rows[0][0], rows[0][1]))
        bm.faces.new((v_top, rows[0][1], rows[0][2]))
        for r in range(rings - 1):
            bm.faces.new((rows[r][0], rows[r + 1][0], rows[r + 1][1], rows[r][1]))
            bm.faces.new((rows[r][1], rows[r + 1][1], rows[r + 1][2], rows[r][2]))
        # Solidify slightly for thickness
        bmesh.ops.solidify(bm, geom=list(bm.faces), thickness=0.04)
        add_mesh_obj(f"UmbrellaWedge_{i}", bm, m_pink if i % 2 == 0 else m_cream, tmp, smooth=True)
    make_sphere("UmbrellaFinial", 0.09, m_gold, tmp, location=(0, 0, z0 + dome_h + 0.05), u_segs=10, v_segs=8)

    return join_collection_to_single_mesh(tmp, "IceCreamCart", target_col)


def build_florist_stand(target_col):
    tmp = ensure_collection("_tmp_FloristStand")
    m_wood = MATERIALS["AC_WoodLight"]
    m_wood_d = MATERIALS["AC_WoodWarm"]
    m_lav = MATERIALS["AC_Lavender"]
    m_cream = MATERIALS["AC_Cream"]
    m_mint = MATERIALS["AC_MintSoft"]
    m_pot = MATERIALS["AC_Terracotta"]
    m_bush = MATERIALS["AC_BushGreen"]
    m_bulb = MATERIALS["AC_BulbWarm"]
    flower_mats = [
        MATERIALS["AC_FlowerPink"],
        MATERIALS["AC_FlowerYellow"],
        MATERIALS["AC_FlowerBlue"],
        MATERIALS["AC_FlowerWhite"],
    ]

    # Cozy pergola platform & wooden stand
    make_box("FloristBase", (4.3, 2.3, 0.20), m_wood, tmp, location=(0, 0, 0.10), bevel=0.05)
    make_box("FloristCounter", (3.6, 1.0, 0.96), m_mint, tmp, location=(0, -0.32, 0.68), bevel=0.06)
    make_box("FloristTop", (3.85, 1.18, 0.14), m_cream, tmp, location=(0, -0.32, 1.20), bevel=0.04)

    # Posts & charming garden trellis back wall
    for px in (-1.82, 1.82):
        for py in (-0.75, 0.78):
            make_cylinder(f"FPost_{px}_{py}", 0.10, 0.10, 2.50, m_wood_d, tmp, location=(px, py, 1.44), segs=12, bevel=0.02)
    for hx in (-1.2, -0.6, 0.0, 0.6, 1.2):
        make_box(f"TrellisV_{hx}", (0.07, 0.07, 2.15), m_wood, tmp, location=(hx, 0.78, 1.35), bevel=0.015)
    for hz in (0.7, 1.25, 1.80, 2.35):
        make_box(f"TrellisH_{hz}", (3.60, 0.07, 0.07), m_wood, tmp, location=(0, 0.78, hz), bevel=0.015)

    # Lavender & cream striped awning
    num_stripes = 9
    sw = 4.25 / num_stripes
    for i in range(num_stripes):
        sx = -2.125 + (i + 0.5) * sw
        mat_s = m_lav if i % 2 == 0 else m_cream
        make_box(f"FAwning_{i}", (sw * 1.01, 2.25, 0.16), mat_s, tmp, location=(sx, -0.05, 2.86), rotation=(0.22, 0, 0), bevel=0.04)
        make_cylinder(f"FScallop_{i}", sw * 0.48, sw * 0.48, 0.12, mat_s, tmp, location=(sx, -1.12, 2.52), rotation=(math.pi * 0.5, 0, 0), segs=12, bevel=0.02)

    # Fairy lights
    for i in range(8):
        make_sphere(f"FBulb_{i}", 0.06, m_bulb, tmp, location=(-1.58 + i * 0.45, -1.04, 2.42), u_segs=8, v_segs=6)

    # Sign plaque
    make_box("FloristPlaque", (2.65, 0.16, 0.62), MATERIALS["AC_PurplePlum"], tmp, location=(0, -0.52, 3.44), bevel=0.05)

    # Lush flower pots on counter & tiered display steps!
    for i in range(6):
        fx = -1.35 + i * 0.54
        make_cylinder(f"CounterPot_{i}", 0.12, 0.16, 0.22, m_pot, tmp, location=(fx, -0.35, 1.38), segs=10, bevel=0.02)
        make_sphere(f"CounterFoliage_{i}", 0.19, m_bush, tmp, location=(fx, -0.35, 1.56), scale=(1.0, 1.0, 0.85), u_segs=10, v_segs=8)
        fmat = flower_mats[i % len(flower_mats)]
        for k in range(3):
            ang = k * (math.tau / 3.0) + i
            make_sphere(
                f"CounterBloom_{i}_{k}", 0.075, fmat, tmp,
                location=(fx + math.cos(ang) * 0.10, -0.35 + math.sin(ang) * 0.10, 1.69),
                u_segs=8, v_segs=6
            )

    # Side flower buckets on ground
    for side_idx, sx in enumerate((-2.25, 2.25)):
        make_cylinder(f"BigPot_{side_idx}", 0.26, 0.34, 0.44, m_pot, tmp, location=(sx, -0.45, 0.22), segs=12, bevel=0.03)
        make_sphere(f"BigBush_{side_idx}", 0.36, m_bush, tmp, location=(sx, -0.45, 0.64), u_segs=12, v_segs=10)
        for k in range(5):
            ang = k * (math.tau / 5.0)
            make_sphere(
                f"BigBloom_{side_idx}_{k}", 0.085, flower_mats[(k + side_idx) % len(flower_mats)], tmp,
                location=(sx + math.cos(ang) * 0.22, -0.45 + math.sin(ang) * 0.22, 0.80 + (k % 2) * 0.08),
                u_segs=8, v_segs=6
            )

    return join_collection_to_single_mesh(tmp, "FloristStand", target_col)


def build_market_stall(target_col, name, stripe_mat, theme="coco"):
    tmp = ensure_collection(f"_tmp_{name}")
    m_wood = MATERIALS["AC_WoodWarm"]
    m_wood_l = MATERIALS["AC_WoodLight"]
    m_cream = MATERIALS["AC_Cream"]
    m_bulb = MATERIALS["AC_BulbWarm"]

    # Wooden table & legs
    make_box("StallCounter", (2.6, 1.05, 0.82), m_wood_l, tmp, location=(0, 0, 0.48), bevel=0.05)
    make_box("StallTop", (2.82, 1.22, 0.12), m_cream, tmp, location=(0, 0, 0.94), bevel=0.04)
    for px in (-1.24, 1.24):
        for py in (-0.48, 0.48):
            make_cylinder(f"SPost_{px}_{py}", 0.08, 0.08, 2.25, m_wood, tmp, location=(px, py, 1.15), segs=10, bevel=0.015)

    # Striped cozy roof
    num_s = 7
    sw = 3.05 / num_s
    for i in range(num_s):
        sx = -1.525 + (i + 0.5) * sw
        mat_s = stripe_mat if i % 2 == 0 else m_cream
        make_box(f"SAwning_{i}", (sw * 1.01, 1.78, 0.14), mat_s, tmp, location=(sx, -0.05, 2.40), rotation=(0.24, 0, 0), bevel=0.03)
        make_cylinder(f"SScallop_{i}", sw * 0.48, sw * 0.48, 0.10, mat_s, tmp, location=(sx, -0.88, 2.12), rotation=(math.pi * 0.5, 0, 0), segs=10, bevel=0.015)

    # Warm bulb
    for lx in (-0.85, 0.0, 0.85):
        make_sphere(f"SBulb_{lx}", 0.055, m_bulb, tmp, location=(lx, -0.78, 2.02), u_segs=8, v_segs=6)

    # Theme merchandise on counter!
    if theme == "coco":
        m_coco_g = MATERIALS["AC_CoconutGreen"]
        m_pink = MATERIALS["AC_PinkBerry"]
        for row in range(2):
            for i in range(5 - row):
                cx = -0.85 + i * 0.34 + row * 0.17
                make_sphere(f"GreenCoco_{row}_{i}", 0.16, m_coco_g, tmp, location=(cx, -0.10, 1.13 + row * 0.24), scale=(1.0, 1.0, 1.08), u_segs=10, v_segs=8)
                if row == 1:
                    make_cylinder(f"Straw_{i}", 0.015, 0.015, 0.25, m_pink, tmp, location=(cx + 0.04, -0.10, 1.54), rotation=(0, 0.28, 0), segs=6, bevel=0.0)
    elif theme == "artesanato":
        # Folded colorful cangas & coastal hats
        colors = [MATERIALS["AC_PinkBerry"], MATERIALS["AC_SkyBlue"], MATERIALS["AC_SunnyYellow"], MATERIALS["AC_MintTeal"]]
        for idx, cm in enumerate(colors):
            bx = -0.92 + idx * 0.56
            for stack in range(3):
                make_box(f"Canga_{idx}_{stack}", (0.42, 0.55, 0.09), cm, tmp, location=(bx, -0.05, 1.05 + stack * 0.095), rotation=(0, 0, (stack - 1) * 0.08), bevel=0.03)
    else:
        # Tropical fruits (oranges, strawberries, pineapples)
        for i in range(4):
            fx = -0.88 + i * 0.58
            make_box(f"FruitBox_{i}", (0.48, 0.56, 0.16), m_wood, tmp, location=(fx, -0.05, 1.08), bevel=0.02)
            fmat = MATERIALS["AC_Orange"] if i % 2 == 0 else MATERIALS["AC_SunnyYellow"]
            for k in range(4):
                ox = fx + (-0.11 if k % 2 == 0 else 0.11)
                oy = -0.15 if k < 2 else 0.05
                make_sphere(f"Fruit_{i}_{k}", 0.10, fmat, tmp, location=(ox, oy, 1.22), u_segs=10, v_segs=8)

    return join_collection_to_single_mesh(tmp, name, target_col)


def build_coastal_bench(target_col):
    tmp = ensure_collection("_tmp_Bench")
    m_wood = MATERIALS["AC_WoodLight"]
    m_wood_d = MATERIALS["AC_WoodDark"]
    m_metal = MATERIALS["AC_MetalDark"]

    # Seat slats (chunky rounded)
    for sy in (-0.22, 0.0, 0.22):
        make_box(f"Seat_{sy}", (1.76, 0.18, 0.07), m_wood, tmp, location=(0, sy, 0.46), bevel=0.025)
    # Backrest slats
    for sz in (0.72, 0.94):
        make_box(f"Back_{sz}", (1.76, 0.07, 0.18), m_wood, tmp, location=(0, 0.34, sz), rotation=(-0.18, 0, 0), bevel=0.025)
    # Chunky side frames & armrests
    for sx in (-0.74, 0.74):
        make_box(f"LegF_{sx}", (0.10, 0.10, 0.48), m_metal, tmp, location=(sx, -0.24, 0.24), bevel=0.02)
        make_box(f"LegB_{sx}", (0.10, 0.10, 1.02), m_metal, tmp, location=(sx, 0.30, 0.51), bevel=0.02)
        make_box(f"Arm_{sx}", (0.12, 0.64, 0.08), m_wood_d, tmp, location=(sx, 0.03, 0.66), bevel=0.03)
        make_box(f"ArmPost_{sx}", (0.08, 0.08, 0.22), m_metal, tmp, location=(sx, -0.24, 0.56), bevel=0.015)

    return join_collection_to_single_mesh(tmp, "CoastalBench", target_col)


def build_street_lamp(target_col):
    tmp = ensure_collection("_tmp_StreetLamp")
    m_metal = MATERIALS["AC_MetalDark"]
    m_gold = MATERIALS["AC_Gold"]
    m_glow = MATERIALS["AC_LanternGlow"]

    # Base pedestal + tapered lamp pole
    make_cylinder("LampBase", 0.22, 0.16, 0.45, m_metal, tmp, location=(0, 0, 0.225), segs=12, bevel=0.03)
    make_cylinder("LampCollar", 0.17, 0.12, 0.18, m_gold, tmp, location=(0, 0, 0.50), segs=12, bevel=0.02)
    make_cylinder("LampPole", 0.09, 0.065, 2.75, m_metal, tmp, location=(0, 0, 1.92), segs=12, bevel=0.01)

    # Charming curved bracket arm
    make_box("LampArm", (0.62, 0.08, 0.08), m_metal, tmp, location=(0.28, 0, 3.24), bevel=0.025)
    make_sphere("PoleFinial", 0.11, m_gold, tmp, location=(0, 0, 3.36), u_segs=10, v_segs=8)

    # Glowing coastal lantern housing
    lx = 0.54
    make_cylinder("LanternCap", 0.24, 0.06, 0.18, m_metal, tmp, location=(lx, 0, 3.12), segs=8, bevel=0.015)
    make_cylinder("LanternGlass", 0.15, 0.19, 0.34, m_glow, tmp, location=(lx, 0, 2.86), segs=8, bevel=0.02)
    make_cylinder("LanternBottom", 0.10, 0.16, 0.10, m_metal, tmp, location=(lx, 0, 2.66), segs=8, bevel=0.015)

    return join_collection_to_single_mesh(tmp, "StreetLamp", target_col)


def build_planter_bush(target_col):
    tmp = ensure_collection("_tmp_PlanterBush")
    m_stone = MATERIALS["AC_StoneWhite"]
    m_bush = MATERIALS["AC_BushGreen"]
    m_bush_l = MATERIALS["AC_BushLight"]
    m_pink = MATERIALS["AC_FlowerPink"]
    m_yel = MATERIALS["AC_FlowerYellow"]

    make_box("PlanterBox", (1.65, 0.78, 0.34), m_stone, tmp, location=(0, 0, 0.17), bevel=0.05)
    # Plump overlapping Animal Crossing foliage puffs
    puffs = [
        (-0.46, 0.0, 0.54, 0.40, m_bush),
        (0.0, 0.04, 0.62, 0.48, m_bush_l),
        (0.46, -0.02, 0.54, 0.40, m_bush),
        (-0.22, -0.14, 0.50, 0.32, m_bush_l),
        (0.24, 0.12, 0.52, 0.32, m_bush),
    ]
    for i, (x, y, z, r, m) in enumerate(puffs):
        make_sphere(f"Puff_{i}", r, m, tmp, location=(x, y, z), scale=(1.0, 0.92, 0.86), u_segs=12, v_segs=10)
    # Tiny cute flowers tucked into the bush
    blooms = [
        (-0.48, -0.30, 0.64, m_pink),
        (-0.12, -0.34, 0.78, m_yel),
        (0.22, -0.30, 0.72, m_pink),
        (0.52, -0.26, 0.62, m_yel),
        (0.04, 0.22, 0.92, m_pink),
    ]
    for i, (x, y, z, m) in enumerate(blooms):
        make_sphere(f"Bloom_{i}", 0.08, m, tmp, location=(x, y, z), u_segs=8, v_segs=6)

    return join_collection_to_single_mesh(tmp, "PlanterBush", target_col)


def build_festoon_lights(target_col):
    tmp = ensure_collection("_tmp_Festoon")
    m_wire = MATERIALS["AC_WoodDark"]
    m_bulb = MATERIALS["AC_BulbWarm"]
    pennant_mats = [
        MATERIALS["AC_PinkBerry"],
        MATERIALS["AC_SunnyYellow"],
        MATERIALS["AC_MintTeal"],
        MATERIALS["AC_SkyBlue"],
        MATERIALS["AC_Coral"],
    ]

    span = 4.5
    steps = 11
    for i in range(steps):
        u0 = i / steps - 0.5
        u1 = (i + 1) / steps - 0.5
        um = (u0 + u1) * 0.5
        x0, x1 = u0 * span, u1 * span
        z0 = -0.38 * (1.0 - (2 * u0) ** 2)
        z1 = -0.38 * (1.0 - (2 * u1) ** 2)
        zm = (z0 + z1) * 0.5
        dx = x1 - x0
        dz = z1 - z0
        seg_len = math.hypot(dx, dz) * 1.05
        angle = math.atan2(dz, dx)
        make_cylinder(f"Cord_{i}", 0.022, 0.022, seg_len, m_wire, tmp, location=((x0 + x1) * 0.5, 0, zm), rotation=(0, math.pi * 0.5 - angle, 0), segs=6, bevel=0.0)
        # Cute pennant triangle + warm light bulb
        if i % 2 == 0:
            make_cylinder(f"Pennant_{i}", 0.01, 0.14, 0.24, pennant_mats[i % len(pennant_mats)], tmp, location=(um * span, 0, zm - 0.15), segs=3, bevel=0.0)
        make_sphere(f"FestBulb_{i}", 0.075, m_bulb, tmp, location=(um * span, 0, zm - 0.07), u_segs=8, v_segs=6)

    return join_collection_to_single_mesh(tmp, "FestoonLights", target_col)


def build_crate(target_col, name, kind="oranges"):
    tmp = ensure_collection(f"_tmp_{name}")
    m_wood = MATERIALS["AC_WoodWarm"]
    m_wood_l = MATERIALS["AC_WoodLight"]

    # Chunky wooden crate
    make_box("CrateBottom", (0.86, 0.86, 0.12), m_wood, tmp, location=(0, 0, 0.06), bevel=0.02)
    for side in (-0.39, 0.39):
        make_box(f"SideX_{side}", (0.08, 0.86, 0.58), m_wood_l, tmp, location=(side, 0, 0.35), bevel=0.02)
        make_box(f"SideY_{side}", (0.86, 0.08, 0.58), m_wood_l, tmp, location=(0, side, 0.35), bevel=0.02)
    for cx in (-0.39, 0.39):
        for cy in (-0.39, 0.39):
            make_box(f"Corner_{cx}_{cy}", (0.12, 0.12, 0.66), m_wood, tmp, location=(cx, cy, 0.33), bevel=0.02)

    if kind == "oranges":
        m_or = MATERIALS["AC_Orange"]
        m_lf = MATERIALS["AC_PalmLeaf"]
        for ix in (-0.20, 0.0, 0.20):
            for iy in (-0.20, 0.0, 0.20):
                make_sphere(f"Or_{ix}_{iy}", 0.13, m_or, tmp, location=(ix, iy, 0.56), u_segs=10, v_segs=8)
        for ix in (-0.11, 0.11):
            for iy in (-0.11, 0.11):
                make_sphere(f"OrTop_{ix}_{iy}", 0.125, m_or, tmp, location=(ix, iy, 0.72), u_segs=10, v_segs=8)
        make_sphere("OrLeaf", 0.09, m_lf, tmp, location=(0.14, -0.05, 0.83), scale=(1.4, 0.7, 0.3), u_segs=8, v_segs=6)
    else:
        m_bush = MATERIALS["AC_BushGreen"]
        flowers = [MATERIALS["AC_FlowerPink"], MATERIALS["AC_FlowerYellow"], MATERIALS["AC_FlowerBlue"], MATERIALS["AC_FlowerWhite"]]
        make_sphere("FlowerBed", 0.40, m_bush, tmp, location=(0, 0, 0.58), scale=(1.05, 1.05, 0.58), u_segs=12, v_segs=10)
        for i in range(7):
            ang = i * (math.tau / 7.0)
            rad = 0.22 if i > 0 else 0.0
            make_sphere(
                f"CrateBloom_{i}", 0.11, flowers[i % len(flowers)], tmp,
                location=(math.cos(ang) * rad, math.sin(ang) * rad, 0.76 + (0.06 if i == 0 else 0.0)),
                u_segs=8, v_segs=6
            )

    return join_collection_to_single_mesh(tmp, name, target_col)


def build_route_signpost(target_col):
    tmp = ensure_collection("_tmp_Signpost")
    m_wood = MATERIALS["AC_WoodWarm"]
    m_wood_l = MATERIALS["AC_WoodLight"]
    m_mint = MATERIALS["AC_MintTeal"]
    m_coral = MATERIALS["AC_Coral"]

    make_cylinder("SignPost", 0.09, 0.08, 1.85, m_wood, tmp, location=(0, 0, 0.92), segs=10, bevel=0.02)
    make_sphere("SignCap", 0.11, m_wood_l, tmp, location=(0, 0, 1.88), u_segs=10, v_segs=8)
    # Upper board (Feira)
    make_box("BoardTop", (1.28, 0.10, 0.34), m_coral, tmp, location=(0.14, 0, 1.54), rotation=(0, 0, 0.08), bevel=0.03)
    # Lower board (Orla)
    make_box("BoardBot", (1.28, 0.10, 0.34), m_mint, tmp, location=(0.14, 0, 1.12), rotation=(0, 0, -0.06), bevel=0.03)

    return join_collection_to_single_mesh(tmp, "RouteSignpost", target_col)


def build_signboard_new(target_col):
    tmp = ensure_collection("_tmp_SignboardNew")
    m_plum = MATERIALS["AC_PurplePlum"]
    m_gold = MATERIALS["AC_Gold"]
    m_cream = MATERIALS["AC_Cream"]
    m_pink = MATERIALS["AC_PinkBerry"]

    make_box("SignFrame", (3.20, 0.22, 0.88), m_gold, tmp, location=(0, 0, 0), bevel=0.06)
    make_box("SignFace", (2.98, 0.25, 0.68), m_plum, tmp, location=(0, -0.02, 0), bevel=0.04)
    # Little decorative stars on corners
    for sx in (-1.38, 1.38):
        make_sphere(f"Star_{sx}", 0.10, m_cream, tmp, location=(sx, -0.14, 0.24), u_segs=8, v_segs=6)
        make_sphere(f"Dot_{sx}", 0.08, m_pink, tmp, location=(sx, -0.14, -0.24), u_segs=8, v_segs=6)

    return join_collection_to_single_mesh(tmp, "SignboardNew", target_col)


def build_beach_umbrella(target_col):
    tmp = ensure_collection("_tmp_BeachUmbrella")
    m_wood = MATERIALS["AC_WoodLight"]
    m_coral = MATERIALS["AC_Coral"]
    m_cream = MATERIALS["AC_Cream"]
    m_gold = MATERIALS["AC_Gold"]

    make_cylinder("UPole", 0.05, 0.05, 2.35, m_wood, tmp, location=(0, 0, 1.15), rotation=(0.10, 0.08, 0), segs=10, bevel=0.01)
    # 8-wedge umbrella dome tilted gently
    segs = 8
    radius = 1.48
    dome_h = 0.56
    z0 = 1.98
    for i in range(segs):
        a0 = (i / segs) * math.tau
        a1 = ((i + 1) / segs) * math.tau
        am = (a0 + a1) * 0.5
        bm = bmesh.new()
        rings = 5
        rows = []
        v_top = bm.verts.new((0, 0, z0 + dome_h))
        for r in range(1, rings + 1):
            t = r / rings
            ang = t * (math.pi * 0.5)
            rad = math.sin(ang) * radius
            zz = z0 + math.cos(ang) * dome_h
            rows.append([
                bm.verts.new((math.cos(a0) * rad, math.sin(a0) * rad, zz)),
                bm.verts.new((math.cos(am) * rad * 1.03, math.sin(am) * rad * 1.03, zz)),
                bm.verts.new((math.cos(a1) * rad, math.sin(a1) * rad, zz)),
            ])
        bm.faces.new((v_top, rows[0][0], rows[0][1]))
        bm.faces.new((v_top, rows[0][1], rows[0][2]))
        for r in range(rings - 1):
            bm.faces.new((rows[r][0], rows[r + 1][0], rows[r + 1][1], rows[r][1]))
            bm.faces.new((rows[r][1], rows[r + 1][1], rows[r + 1][2], rows[r][2]))
        bmesh.ops.solidify(bm, geom=list(bm.faces), thickness=0.035)
        obj = add_mesh_obj(f"BUWedge_{i}", bm, m_coral if i % 2 == 0 else m_cream, tmp, smooth=True)
        obj.rotation_euler = (0.10, 0.08, 0)
    make_sphere("UFinial", 0.09, m_gold, tmp, location=(0.18, -0.22, z0 + dome_h + 0.04), u_segs=8, v_segs=6)

    return join_collection_to_single_mesh(tmp, "BeachUmbrella", target_col)


def build_beach_chair(target_col):
    tmp = ensure_collection("_tmp_BeachChair")
    m_wood = MATERIALS["AC_WoodLight"]
    m_aqua = MATERIALS["AC_SkyBlue"]
    m_cream = MATERIALS["AC_Cream"]

    # Lounge chair frame & striped fabric
    make_box("ChairFrame", (0.76, 1.55, 0.08), m_wood, tmp, location=(0, 0, 0.34), bevel=0.025)
    make_box("ChairFabric", (0.64, 1.10, 0.06), m_aqua, tmp, location=(0, -0.16, 0.38), bevel=0.02)
    make_box("ChairBack", (0.68, 0.65, 0.07), m_aqua, tmp, location=(0, 0.56, 0.62), rotation=(-0.72, 0, 0), bevel=0.025)
    make_box("ChairPillow", (0.48, 0.22, 0.12), m_cream, tmp, location=(0, 0.64, 0.78), rotation=(-0.72, 0, 0), bevel=0.05)
    for lx in (-0.32, 0.32):
        for ly in (-0.58, 0.56):
            make_cylinder(f"CLeg_{lx}_{ly}", 0.04, 0.04, 0.36, m_wood, tmp, location=(lx, ly, 0.18), segs=8, bevel=0.01)

    return join_collection_to_single_mesh(tmp, "BeachChair", target_col)


def build_lifeguard_tower(target_col):
    tmp = ensure_collection("_tmp_LifeguardTower")
    m_wood = MATERIALS["AC_WoodLight"]
    m_cream = MATERIALS["AC_Cream"]
    m_pink = MATERIALS["AC_PinkBerry"]
    m_sky = MATERIALS["AC_SkyBlue"]

    # Stilts
    for sx in (-0.75, 0.75):
        for sy in (-0.75, 0.75):
            make_cylinder(f"LGStilt_{sx}_{sy}", 0.09, 0.08, 1.65, m_wood, tmp, location=(sx, sy, 0.82), segs=10, bevel=0.015)
    # Platform & railing
    make_box("LGDeck", (1.95, 1.95, 0.16), m_wood, tmp, location=(0, 0, 1.62), bevel=0.04)
    make_box("LGHouse", (1.42, 1.38, 1.25), m_cream, tmp, location=(0, 0.08, 2.30), bevel=0.06)
    make_box("LGWindow", (1.10, 1.44, 0.52), m_sky, tmp, location=(0, 0.08, 2.48), bevel=0.03)
    # Pitched cozy roof
    make_box("LGRoofL", (1.22, 1.88, 0.16), m_pink, tmp, location=(-0.50, 0.08, 3.16), rotation=(0, -0.38, 0), bevel=0.04)
    make_box("LGRoofR", (1.22, 1.88, 0.16), m_pink, tmp, location=(0.50, 0.08, 3.16), rotation=(0, 0.38, 0), bevel=0.04)
    # Cute ladder in front
    for lx in (-0.28, 0.28):
        make_cylinder(f"LGLadder_{lx}", 0.04, 0.04, 1.72, m_wood, tmp, location=(lx, -1.02, 0.84), rotation=(0.20, 0, 0), segs=8, bevel=0.01)
    for r in range(5):
        make_cylinder(f"LGRung_{r}", 0.03, 0.03, 0.58, m_wood, tmp, location=(0, -1.14 + r * 0.065, 0.26 + r * 0.31), rotation=(0, math.pi * 0.5, 0), segs=8, bevel=0.008)

    return join_collection_to_single_mesh(tmp, "LifeguardTower", target_col)


def build_chibi_character(target_col, name, skin_mat, hair_mat, shirt_mat, pants_mat, hat="none", star_emblem=False, apron=False):
    """
    Adorable Animal Crossing: New Horizons chibi character!
    Total height ~1.58m, big friendly rounded head, rosy cheeks, bright eyes, chunky limbs.
    Faces -Y in Blender (which exports to -Z in Unity, facing the camera!).
    """
    tmp = ensure_collection(f"_tmp_{name}")
    m_eye = MATERIALS["AC_EyeDark"]
    m_shine = MATERIALS["AC_EyeShine"]
    m_blush = MATERIALS["AC_CheekBlush"]
    m_shoe = MATERIALS["AC_ShoeDark"]
    m_cream = MATERIALS["AC_Cream"]
    m_yel = MATERIALS["AC_SunnyYellow"]

    # Chunky little shoes
    for sx in (-0.15, 0.15):
        make_sphere(f"Shoe_{sx}", 0.13, m_shoe, tmp, location=(sx, -0.04, 0.09), scale=(0.95, 1.32, 0.72), u_segs=12, v_segs=10)
        make_capsule(f"Leg_{sx}", 0.09, 0.36, pants_mat, tmp, location=(sx, 0, 0.28))

    # Plump cozy torso (pear-shaped Animal Crossing villager body!)
    make_sphere("Torso", 0.30, shirt_mat, tmp, location=(0, 0, 0.68), scale=(0.96, 0.84, 1.15), u_segs=14, v_segs=12)

    if star_emblem:
        make_sphere("StarEmblem", 0.09, m_yel, tmp, location=(0, -0.24, 0.72), scale=(1.0, 0.35, 1.0), u_segs=10, v_segs=8)
    if apron:
        make_box("ApronFront", (0.34, 0.10, 0.42), MATERIALS["AC_MintSoft"], tmp, location=(0, -0.22, 0.60), bevel=0.04)
        make_sphere("ApronFlower", 0.055, MATERIALS["AC_FlowerPink"], tmp, location=(0.08, -0.27, 0.56), u_segs=8, v_segs=6)

    # Rounded chibi arms nestled naturally at torso sides
    for side in (-1, 1):
        make_capsule(f"Arm_{side}", 0.088, 0.38, shirt_mat, tmp, location=(side * 0.28, 0.0, 0.68), rotation=(0, side * 0.28, 0))
        make_sphere(f"Hand_{side}", 0.085, skin_mat, tmp, location=(side * 0.33, -0.02, 0.52), u_segs=10, v_segs=8)

    # Big iconic Animal Crossing rounded head!
    head_z = 1.24
    make_sphere("Head", 0.36, skin_mat, tmp, location=(0, 0, head_z), scale=(1.04, 0.98, 0.96), u_segs=18, v_segs=14)
    # Cute rounded ears
    for side in (-1, 1):
        make_sphere(f"Ear_{side}", 0.085, skin_mat, tmp, location=(side * 0.36, 0.02, head_z - 0.02), scale=(0.65, 0.85, 1.0), u_segs=10, v_segs=8)
    # Little button nose
    make_sphere("Nose", 0.045, MATERIALS["AC_Coral"], tmp, location=(0, -0.35, head_z - 0.03), scale=(1.1, 0.8, 0.8), u_segs=8, v_segs=6)
    # Expressive glossy eyes + white sparkle highlight + rosy cheeks
    for side in (-1, 1):
        make_sphere(f"Eye_{side}", 0.062, m_eye, tmp, location=(side * 0.14, -0.32, head_z + 0.03), scale=(0.85, 0.45, 1.15), u_segs=10, v_segs=8)
        make_sphere(f"EyeShine_{side}", 0.022, m_shine, tmp, location=(side * 0.155, -0.352, head_z + 0.055), u_segs=8, v_segs=6)
        make_sphere(f"Cheek_{side}", 0.068, m_blush, tmp, location=(side * 0.21, -0.28, head_z - 0.06), scale=(1.1, 0.35, 0.75), u_segs=8, v_segs=6)

    # Fluffy rounded hair cap + side tufts
    make_sphere("HairCap", 0.385, hair_mat, tmp, location=(0, 0.04, head_z + 0.06), scale=(1.04, 1.02, 0.96), u_segs=16, v_segs=12)
    make_sphere("HairBangsL", 0.18, hair_mat, tmp, location=(-0.14, -0.22, head_z + 0.23), scale=(1.2, 0.75, 0.75), u_segs=10, v_segs=8)
    make_sphere("HairBangsR", 0.18, hair_mat, tmp, location=(0.14, -0.22, head_z + 0.23), scale=(1.2, 0.75, 0.75), u_segs=10, v_segs=8)
    if hat == "hairclip":
        # Cute bob side locks + sunny star hairclip for Girl Power protagonist!
        for side in (-1, 1):
            make_sphere(f"BobSide_{side}", 0.17, hair_mat, tmp, location=(side * 0.31, 0.05, head_z - 0.10), scale=(0.8, 1.0, 1.35), u_segs=10, v_segs=8)
        make_sphere("HairClip", 0.075, m_yel, tmp, location=(0.24, -0.24, head_z + 0.24), scale=(1.1, 0.5, 0.9), u_segs=8, v_segs=6)
    elif hat == "straw":
        # Wide-brimmed florist straw sunhat with pink ribbon & flower!
        m_straw = MATERIALS["AC_StrawHat"]
        make_cylinder("HatBrim", 0.58, 0.56, 0.06, m_straw, tmp, location=(0, 0.02, head_z + 0.27), rotation=(-0.10, 0, 0), segs=20, bevel=0.02)
        make_sphere("HatCrown", 0.34, m_straw, tmp, location=(0, 0.02, head_z + 0.36), scale=(1.0, 1.0, 0.65), u_segs=14, v_segs=10)
        make_cylinder("HatRibbon", 0.355, 0.355, 0.08, MATERIALS["AC_PinkBerry"], tmp, location=(0, 0.02, head_z + 0.32), rotation=(-0.10, 0, 0), segs=18, bevel=0.01)
        make_sphere("HatFlower", 0.09, MATERIALS["AC_FlowerYellow"], tmp, location=(-0.28, -0.24, head_z + 0.34), u_segs=8, v_segs=6)
    elif hat == "cap":
        make_sphere("CapDome", 0.39, MATERIALS["AC_SkyBlue"], tmp, location=(0, 0.02, head_z + 0.14), scale=(1.02, 1.02, 0.80), u_segs=14, v_segs=10)
        make_box("CapVisor", (0.42, 0.32, 0.06), m_cream, tmp, location=(0, -0.34, head_z + 0.18), rotation=(0.15, 0, 0), bevel=0.025)
    elif hat == "visor":
        make_cylinder("VisorBand", 0.38, 0.38, 0.08, MATERIALS["AC_PinkSoft"], tmp, location=(0, 0.0, head_z + 0.20), segs=16, bevel=0.015)
        make_box("VisorBrim", (0.46, 0.28, 0.05), m_cream, tmp, location=(0, -0.34, head_z + 0.19), rotation=(0.12, 0, 0), bevel=0.02)

    return join_collection_to_single_mesh(tmp, name, target_col)


def build_environment_base(target_col):
    """
    Builds the continuous Matinhos Orla coastal terrain & walkways (spanning X from -14 to +38):
      - South (lower Y in Blender = closer to camera): Boardwalk terrace, Wide Portuguese-stone Orla Promenade
      - Center: Restinga green median strip, Plazas (Kiosk at x=0, Bifurcation at x=6.8, Florist at x=24), Cobblestone Feira lane (x=9..21)
      - North (higher Y in Blender = further from camera): Whitewashed Coastal Mureta (seawall), Golden Beach Sand slope,
        Sparkling Ocean Foam ribbon, Shallow Turquoise Sea, Deep Coastal Ocean, Distant Matinhos green hills & cute sailboats!
    Coordinate mapping matches GirlPowerFirstDeliveryBuilder.cs:
      Blender X = Unity X (left to right along the coast, Kiosk at X=0, Florist at X=24)
      Blender Y = Unity Z (depth toward the ocean! Y=-3.5 boardwalk, Y=-1.8 promenade, Y=0 garden, Y=+1.3 feira, Y=+3.6 mureta, Y=+6 beach, Y=+14 ocean)
    """
    tmp = ensure_collection("_tmp_EnvironmentBase")
    cx = 12.0
    span_x = 62.0

    # 1. Lower coastal asphalt street edge & curb (foreground framing)
    make_box("AsphaltRoad", (span_x, 4.5, 0.20), MATERIALS["AC_Asphalt"], tmp, location=(cx, -5.8, -0.18), bevel=0.02)
    for dx in range(-14, 42, 4):
        make_box(f"RoadDash_{dx}", (1.8, 0.16, 0.02), MATERIALS["AC_RoadLine"], tmp, location=(dx, -5.6, -0.075), bevel=0.005)
    make_box("StreetCurb", (span_x, 0.34, 0.18), MATERIALS["AC_StoneWarm"], tmp, location=(cx, -3.55, -0.05), bevel=0.04)

    # 2. Wide Orla Promenade (Portuguese stone petit-pave aesthetic) at Y = -3.4 .. -0.6
    make_box("OrlaPromenade", (span_x, 2.95, 0.16), MATERIALS["AC_StoneWhite"], tmp, location=(cx, -2.02, -0.06), bevel=0.04)
    # Decorative wavy petit-pave accent stones along promenade
    for i in range(-12, 38):
        wave_y = -2.02 + math.sin(i * 0.72) * 0.42
        make_box(f"PetitPave_{i}", (0.72, 0.24, 0.02), MATERIALS["AC_StoneWarm"], tmp, location=(i * 1.0, wave_y, 0.022), rotation=(0, 0, math.cos(i * 0.72) * 0.35), bevel=0.01)

    # 3. Lush Restinga Coastal Grass Strip (Y = -0.6 .. +3.6)
    make_box("CoastalLawn", (span_x, 4.35, 0.18), MATERIALS["AC_Grass"], tmp, location=(cx, 1.55, -0.08), bevel=0.04)
    # Soft lighter grass patches only on open lawn (outside paved plazas & feira lane!)
    rng = random.Random(42)
    for i in range(36):
        gx = rng.uniform(-10.0, 35.0)
        gy = rng.uniform(-0.3, 3.2)
        in_kiosk_plaza = (-4.3 <= gx <= 4.3 and -0.8 <= gy <= 3.4)
        in_bifurc_plaza = (4.3 <= gx <= 9.3 and -1.2 <= gy <= 2.8)
        in_florist_plaza = (20.5 <= gx <= 27.5 and -0.8 <= gy <= 3.4)
        in_feira_lane = (8.0 <= gx <= 21.8 and 0.4 <= gy <= 2.5)
        if in_kiosk_plaza or in_bifurc_plaza or in_florist_plaza or in_feira_lane:
            continue
        make_sphere(f"GrassPatch_{i}", rng.uniform(0.45, 0.90), MATERIALS["AC_GrassLight"], tmp, location=(gx, gy, 0.005), scale=(1.3, 0.9, 0.025), u_segs=10, v_segs=6)

    # 4. Welcoming Paved Plazas (Kiosk at X=0, Bifurcation at X=6.8, Florist at X=24)
    plazas = [
        ("PlazaKiosk", 0.0, 1.25, 7.6, 3.8),
        ("PlazaBifurcation", 6.8, 0.75, 4.4, 3.5),
        ("PlazaFlorist", 24.0, 1.25, 6.4, 3.8),
    ]
    for pname, px, py, pw, pd in plazas:
        make_box(pname, (pw, pd, 0.06), MATERIALS["AC_PlazaTile"], tmp, location=(px, py, 0.005), bevel=0.03)
        make_box(pname + "_Border", (pw + 0.22, pd + 0.22, 0.04), MATERIALS["AC_PlazaAccent"], tmp, location=(px, py, -0.005), bevel=0.02)

    # 5. Cobblestone Feira Shortcut connecting Bifurcation (X=8.6) to Florist Plaza (X=21.2) at Y = 1.45
    make_box("FeiraLaneBase", (13.4, 1.75, 0.05), MATERIALS["AC_CobbleA"], tmp, location=(14.9, 1.45, 0.004), bevel=0.025)
    for row in range(3):
        for col_i in range(18):
            cx_s = 8.6 + col_i * 0.74 + (0.35 if row % 2 else 0.0)
            cy_s = 0.92 + row * 0.52
            make_box(f"Cobble_{row}_{col_i}", (0.52, 0.36, 0.022), MATERIALS["AC_CobbleB"], tmp, location=(cx_s, cy_s, 0.026), bevel=0.012)

    # 6. Whitewashed Coastal Seawall (Mureta da Orla) at Y = 3.72
    make_box("CoastalMureta", (span_x, 0.42, 0.56), MATERIALS["AC_StoneWhite"], tmp, location=(cx, 3.72, 0.16), bevel=0.06)
    make_box("MuretaCap", (span_x, 0.50, 0.10), MATERIALS["AC_StoneWarm"], tmp, location=(cx, 3.72, 0.46), bevel=0.03)

    # 7. Golden Beach Sand Slope (Y = 3.9 .. 10.6)
    make_box("BeachSandDry", (span_x + 12, 5.4, 0.40), MATERIALS["AC_Sand"], tmp, location=(cx, 6.5, -0.26), rotation=(-0.06, 0, 0), bevel=0.08)
    make_box("BeachSandWet", (span_x + 12, 2.2, 0.30), MATERIALS["AC_SandWet"], tmp, location=(cx, 9.8, -0.44), rotation=(-0.04, 0, 0), bevel=0.06)

    # Cute scattered starfish & shells on the beach!
    for idx, (sx, sy) in enumerate([(-4.0, 5.8), (3.5, 7.2), (12.8, 6.4), (19.5, 7.5), (27.0, 6.1)]):
        make_sphere(f"Starfish_{idx}", 0.18, MATERIALS["AC_Starfish"], tmp, location=(sx, sy, -0.08), scale=(1.1, 1.1, 0.28), u_segs=10, v_segs=6)
        make_sphere(f"Shell_{idx}", 0.12, MATERIALS["AC_Shell"], tmp, location=(sx + 0.6, sy + 0.5, -0.10), scale=(1.2, 0.9, 0.45), u_segs=8, v_segs=6)

    # 8. Ocean Foam Ribbon, Shallow Turquoise & Deep Sea
    for i in range(-18, 44, 2):
        fy = 10.45 + math.sin(i * 0.55) * 0.35
        make_sphere(f"FoamPuff_{i}", 1.25, MATERIALS["AC_Foam"], tmp, location=(i, fy, -0.34), scale=(1.25, 0.48, 0.08), u_segs=10, v_segs=6)
    make_box("OceanShallow", (span_x + 36, 9.5, 0.20), MATERIALS["AC_OceanShallow"], tmp, location=(cx, 15.0, -0.46), bevel=0.02)
    make_box("OceanDeep", (span_x + 80, 48.0, 0.20), MATERIALS["AC_Ocean"], tmp, location=(cx, 42.0, -0.50), bevel=0.02)

    # 9. Distant cozy Matinhos coastal islands / hills & tiny sailboats on the horizon!
    hills = [
        (-22.0, 34.0, 5.5, 3.2, MATERIALS["AC_HillDistant"]),
        (-13.0, 38.0, 8.0, 4.6, MATERIALS["AC_HillFar"]),
        (36.0, 36.0, 7.5, 4.2, MATERIALS["AC_HillDistant"]),
        (46.0, 34.0, 9.0, 5.2, MATERIALS["AC_HillFar"]),
    ]
    for i, (hx, hy, hr, hh, hm) in enumerate(hills):
        make_sphere(f"CoastalHill_{i}", 1.0, hm, tmp, location=(hx, hy, -0.6), scale=(hr, hr * 0.65, hh), u_segs=16, v_segs=10)

    # 3 cute sailboats in the bay
    for idx, (bx, by) in enumerate([(2.5, 19.5), (14.5, 22.5), (27.5, 18.8)]):
        make_box(f"BoatHull_{idx}", (1.35, 0.52, 0.34), MATERIALS["AC_Cream"], tmp, location=(bx, by, -0.22), bevel=0.08)
        make_box(f"BoatStripe_{idx}", (1.38, 0.54, 0.10), MATERIALS["AC_PinkBerry"] if idx % 2 == 0 else MATERIALS["AC_Coral"], tmp, location=(bx, by, -0.14), bevel=0.02)
        make_cylinder(f"BoatMast_{idx}", 0.035, 0.035, 1.55, MATERIALS["AC_WoodWarm"], tmp, location=(bx, by, 0.55), segs=8, bevel=0.0)
        make_box(f"BoatSail_{idx}", (0.78, 0.06, 1.15), MATERIALS["AC_Cream"], tmp, location=(bx + 0.34, by, 0.68), rotation=(0, -0.12, 0.18), bevel=0.02)

    return join_collection_to_single_mesh(tmp, "OrlaEnvironmentBase", target_col)


# ==============================================================================
# SCENE ASSEMBLY & BEAUTY PREVIEW RENDER
# ==============================================================================

def place_instance(master_obj, name, col, location, rot_z_deg=0.0, scale=1.0):
    inst = master_obj.copy()
    inst.name = name
    inst.location = location
    inst.rotation_euler = (0.0, 0.0, math.radians(rot_z_deg))
    if isinstance(scale, (int, float)):
        inst.scale = (scale, scale, scale)
    else:
        inst.scale = scale
    link_obj(inst, col)
    return inst


def setup_world_and_lighting(col):
    # Cozy Golden-Hour Tropical Sky (Animal Crossing: New Horizons sunset warmth)
    world = bpy.data.worlds.new("OrlaACNH_World")
    bpy.context.scene.world = world
    if hasattr(world, "use_nodes") and not world.use_nodes:
        world.use_nodes = True
    nt = world.node_tree
    nt.nodes.clear()
    out = nt.nodes.new("ShaderNodeOutputWorld")
    bg = nt.nodes.new("ShaderNodeBackground")
    bg.inputs["Color"].default_value = hex_to_rgba("#F8C8A4", 1.0)
    bg.inputs["Strength"].default_value = 0.95
    nt.links.new(bg.outputs["Background"], out.inputs["Surface"])

    # Warm Golden-Hour Sun Light
    sun_data = bpy.data.lights.new("Sun_GoldenHour", type="SUN")
    sun_data.energy = 3.4
    sun_data.color = hex_to_rgba("#FFE2B2")[:3]
    sun_data.angle = math.radians(4.5)
    sun_obj = bpy.data.objects.new("Sun_GoldenHour", sun_data)
    sun_obj.rotation_euler = (math.radians(48), math.radians(16), math.radians(-32))
    link_obj(sun_obj, col)

    # Soft Pastel Fill Light (lifts shadows into cozy lavender-aqua)
    fill_data = bpy.data.lights.new("Fill_SkyBounce", type="AREA")
    fill_data.energy = 260.0
    fill_data.size = 18.0
    fill_data.color = hex_to_rgba("#BFE3FF")[:3]
    fill_obj = bpy.data.objects.new("Fill_SkyBounce", fill_data)
    fill_obj.location = (6.0, -8.0, 10.0)
    fill_obj.rotation_euler = (math.radians(45), 0, 0)
    link_obj(fill_obj, col)

    # Warm Point Lights at Kiosk & Streetlamps
    for idx, pos in enumerate([(0.0, 1.5, 2.4), (6.8, -0.5, 2.8), (12.2, -0.3, 2.8), (24.0, 1.5, 2.4)]):
        pt_data = bpy.data.lights.new(f"WarmGlow_{idx}", type="POINT")
        pt_data.energy = 65.0
        pt_data.shadow_soft_size = 0.6
        pt_data.color = hex_to_rgba("#FFD57A")[:3]
        pt_obj = bpy.data.objects.new(f"WarmGlow_{idx}", pt_data)
        pt_obj.location = pos
        link_obj(pt_obj, col)


def setup_camera_and_render(col):
    cam_data = bpy.data.cameras.new("ACNH_PreviewCam")
    cam_data.lens = 36.0  # Wide cozy Animal Crossing coastal diorama view!
    cam_obj = bpy.data.objects.new("ACNH_PreviewCam", cam_data)
    # Framing Kiosk, Ice Cream Cart, Protagonist, Palms, Feira Stalls, Beach & Ocean!
    cam_obj.location = (3.8, -13.8, 9.2)
    direction = Vector((4.8, 2.6, 0.9)) - cam_obj.location
    cam_obj.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()
    link_obj(cam_obj, col)
    bpy.context.scene.camera = cam_obj

    scene = bpy.context.scene
    # Pick best available fast render engine (EEVEE Next / EEVEE / Cycles)
    engines = [e.identifier for e in bpy.types.RenderSettings.bl_rna.properties["engine"].enum_items]
    print("[Available Render Engines]", engines)
    if "BLENDER_EEVEE_NEXT" in engines:
        scene.render.engine = "BLENDER_EEVEE_NEXT"
    elif "BLENDER_EEVEE" in engines:
        scene.render.engine = "BLENDER_EEVEE"
    else:
        scene.render.engine = "CYCLES"
        scene.cycles.device = "CPU"
        scene.cycles.samples = 32

    scene.render.resolution_x = 1280
    scene.render.resolution_y = 720
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.render.filepath = PREVIEW_OUT

    # Cozy vibrant color management
    try:
        scene.view_settings.view_transform = "AgX"
        scene.view_settings.look = "AgX - Medium High Contrast"
    except Exception:
        pass
    scene.view_settings.exposure = -0.10

    print(f"[Rendering Preview] {PREVIEW_OUT} with {scene.render.engine}...")
    bpy.ops.render.render(write_still=True)
    print("[Preview Render Complete]")


def main():
    clear_scene()
    init_palette()

    col_masters = ensure_collection("01_AssetMasters")
    col_scene = ensure_collection("02_OrlaScene_Assembled")
    col_lighting = ensure_collection("03_LightingAndCamera")

    # 1. Build all modular 3D assets at origin (0,0,0)
    assets = {}
    assets["OrlaEnvironmentBase"] = build_environment_base(col_masters)
    assets["PalmTree"] = build_palm_tree(col_masters)
    assets["KioskGirlPower"] = build_kiosk_girl_power(col_masters)
    assets["IceCreamCart"] = build_ice_cream_cart(col_masters)
    assets["FloristStand"] = build_florist_stand(col_masters)
    assets["MarketStall_Coco"] = build_market_stall(col_masters, "MarketStall_Coco", MATERIALS["AC_MintTeal"], "coco")
    assets["MarketStall_Artesanato"] = build_market_stall(col_masters, "MarketStall_Artesanato", MATERIALS["AC_Coral"], "artesanato")
    assets["MarketStall_Frutas"] = build_market_stall(col_masters, "MarketStall_Frutas", MATERIALS["AC_SunnyYellow"], "frutas")
    assets["CoastalBench"] = build_coastal_bench(col_masters)
    assets["StreetLamp"] = build_street_lamp(col_masters)
    assets["PlanterBush"] = build_planter_bush(col_masters)
    assets["FestoonLights"] = build_festoon_lights(col_masters)
    assets["CrateOranges"] = build_crate(col_masters, "CrateOranges", "oranges")
    assets["CrateFlowers"] = build_crate(col_masters, "CrateFlowers", "flowers")
    assets["RouteSignpost"] = build_route_signpost(col_masters)
    assets["SignboardNew"] = build_signboard_new(col_masters)
    assets["BeachUmbrella"] = build_beach_umbrella(col_masters)
    assets["BeachChair"] = build_beach_chair(col_masters)
    assets["LifeguardTower"] = build_lifeguard_tower(col_masters)

    # Chibi Characters
    assets["Char_Protagonist"] = build_chibi_character(
        col_masters, "Char_Protagonist",
        MATERIALS["AC_SkinWarm"], MATERIALS["AC_HairPlum"],
        MATERIALS["AC_PinkBerry"], MATERIALS["AC_Denim"],
        hat="hairclip", star_emblem=True
    )
    assets["Char_Florist"] = build_chibi_character(
        col_masters, "Char_Florist",
        MATERIALS["AC_SkinLight"], MATERIALS["AC_HairBrown"],
        MATERIALS["AC_Cream"], MATERIALS["AC_WoodLight"],
        hat="straw", apron=True
    )
    assets["Char_PedestrianBlue"] = build_chibi_character(
        col_masters, "Char_PedestrianBlue",
        MATERIALS["AC_SkinTan"], MATERIALS["AC_HairDark"],
        MATERIALS["AC_SkyBlue"], MATERIALS["AC_Denim"],
        hat="cap"
    )
    assets["Char_PedestrianCoral"] = build_chibi_character(
        col_masters, "Char_PedestrianCoral",
        MATERIALS["AC_SkinLight"], MATERIALS["AC_HairAuburn"],
        MATERIALS["AC_Coral"], MATERIALS["AC_Cream"],
        hat="visor"
    )

    # 2. Export every modular asset to FBX for Unity!
    for key, obj in assets.items():
        if obj is not None:
            export_fbx(obj, f"{key}.fbx")

    # 3. Hide AssetMasters from viewport & render, and assemble the full Orla Scene!
    col_masters.hide_viewport = True
    col_masters.hide_render = True

    place_instance(assets["OrlaEnvironmentBase"], "Orla_Terreno_Base", col_scene, (0, 0, 0))

    # Kiosk & Florist
    place_instance(assets["KioskGirlPower"], "Quiosque_GirlPower", col_scene, (0.0, 2.35, 0.0))
    place_instance(assets["FloristStand"], "Banca_Florista", col_scene, (24.0, 2.35, 0.0))

    # Ice Cream Cart & Protagonist & Florist
    place_instance(assets["IceCreamCart"], "Carrinho_Sorvete", col_scene, (1.15, -1.75, 0.0), rot_z_deg=-6)
    place_instance(assets["Char_Protagonist"], "Protagonista", col_scene, (-1.25, -1.75, 0.0), rot_z_deg=20)
    place_instance(assets["Char_Florist"], "Florista_NPC", col_scene, (24.0, 0.25, 0.0), rot_z_deg=-15)

    # Coconut Palms along the coastal mureta
    palm_specs = [
        (-5.2, 3.15, 12, 1.05),
        (4.3, 3.15, -18, 0.98),
        (8.6, 3.15, 160, 0.95),
        (17.8, 3.15, -14, 1.04),
        (27.8, 3.15, 155, 1.08),
    ]
    for i, (px, py, rot, sc) in enumerate(palm_specs):
        place_instance(assets["PalmTree"], f"Coqueiro_{i+1}", col_scene, (px, py, 0.0), rot_z_deg=rot, scale=sc)

    # Feirinha da Orla stalls & festoons
    place_instance(assets["MarketStall_Coco"], "Barraca_AguaDeCoco", col_scene, (11.1, 2.85, 0.0))
    place_instance(assets["MarketStall_Artesanato"], "Barraca_Artesanato", col_scene, (15.6, 2.85, 0.0))
    place_instance(assets["MarketStall_Frutas"], "Barraca_Frutas", col_scene, (20.1, 2.85, 0.0))
    for i, fx in enumerate((11.1, 15.6, 20.1)):
        place_instance(assets["FestoonLights"], f"Varal_Luzes_{i+1}", col_scene, (fx, 2.05, 2.75))

    # Crates & Pedestrians along the Feira shortcut
    place_instance(assets["CrateOranges"], "Caixa_Laranjas", col_scene, (10.2, 1.35, 0.0), rot_z_deg=12)
    place_instance(assets["CrateFlowers"], "Caixa_Flores", col_scene, (14.2, 1.15, 0.0), rot_z_deg=-8)
    place_instance(assets["Char_PedestrianBlue"], "Pedestre_Azul", col_scene, (17.2, 1.60, 0.0), rot_z_deg=-25)
    place_instance(assets["Char_PedestrianCoral"], "Pedestre_Coral", col_scene, (19.6, 1.15, 0.0), rot_z_deg=22)

    # Route signpost at bifurcation
    place_instance(assets["RouteSignpost"], "Placa_Bifurcacao", col_scene, (6.4, 0.35, 0.0), rot_z_deg=-10)

    # Planter bushes along central coastal garden median
    for i, bx in enumerate((-4.4, 10.6, 13.8, 17.0, 20.1, 27.6)):
        place_instance(assets["PlanterBush"], f"Jardineira_{i+1}", col_scene, (bx, -0.20, 0.0))

    # Streetlamps
    lamp_positions = [(-3.0, 1.85), (3.0, 1.85), (12.2, -0.35), (18.6, -0.35), (21.2, 1.85), (26.8, 1.85)]
    for i, (lx, ly) in enumerate(lamp_positions):
        place_instance(assets["StreetLamp"], f"Poste_{i+1}", col_scene, (lx, ly, 0.0), rot_z_deg=180 if lx > 15 else 0)

    # Coastal benches facing the sea
    for i, bx in enumerate((-1.6, 4.0, 9.2, 14.4, 19.6, 25.0)):
        place_instance(assets["CoastalBench"], f"Banco_Orla_{i+1}", col_scene, (bx, -3.10, 0.0), rot_z_deg=180)

    # Beach umbrellas, lounge chairs & lifeguard towers on the golden sand!
    for i, ux in enumerate((-2.5, 5.2, 13.5, 21.8)):
        place_instance(assets["BeachUmbrella"], f"GuardaSol_{i+1}", col_scene, (ux, 6.2, -0.10), rot_z_deg=i * 35)
        place_instance(assets["BeachChair"], f"CadeiraPraia_{i+1}", col_scene, (ux + 1.1, 6.1, -0.10), rot_z_deg=165)
    place_instance(assets["LifeguardTower"], "PostoSalvaVidas_1", col_scene, (9.4, 7.0, -0.14), rot_z_deg=180)
    place_instance(assets["LifeguardTower"], "PostoSalvaVidas_2", col_scene, (26.2, 6.8, -0.14), rot_z_deg=175)

    # 4. Lighting, Camera & Preview Render
    setup_world_and_lighting(col_lighting)

    # Save the editable .blend file BEFORE rendering (so even if headless GPU differs, .blend is saved!)
    bpy.ops.wm.save_as_mainfile(filepath=BLEND_OUT)
    print(f"[Blend Saved] {BLEND_OUT}")

    setup_camera_and_render(col_lighting)


if __name__ == "__main__":
    main()
