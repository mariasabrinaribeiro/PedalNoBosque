"""
build_baia_hero_polish.py
-------------------------
Blender 5.3 Hero Asset Polish for Baia do Avesso:
  1. IceCreamCart.fbx        - Hero turquoise cart with spoked rounded wheels, beveled counter,
                               glass display case with 4 pastel flavor tubs, pink-and-white
                               scalloped umbrella, front readable plaque & warm brass handle!
  2. CoastalVegetationA.fbx  - Varied low coastal grass tufts + wildflower cluster (organic silhouette)
  3. CoastalVegetationB.fbx  - Multi-height soft restinga shrubs + wooden edge planter (marks entries/curves)

Exports clean Y-up FBX models to Assets/GirlPower/Orla3D/Models,
saves BaiaDoAvesso_HeroPolish.blend, and renders baia_hero_preview.png.
"""

import math
import os
import bpy
import bmesh
from mathutils import Vector

SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
ORLA3D_DIR = os.path.dirname(SCRIPT_DIR)
MODELS_DIR = os.path.join(ORLA3D_DIR, "Models")
BLEND_OUT = os.path.join(SCRIPT_DIR, "BaiaDoAvesso_HeroPolish.blend")
PREVIEW_OUT = os.path.join(SCRIPT_DIR, "baia_hero_preview.png")

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


def get_mat(name, hex_color, roughness=0.50):
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
    mat.diffuse_color = rgba
    MATERIALS[name] = mat
    return mat


def init_palette():
    get_mat("AC_MintTeal", "#4EC7B6", 0.42)
    get_mat("AC_MintSoft", "#8FE3D6", 0.45)
    get_mat("AC_PinkBerry", "#F26392", 0.42)
    get_mat("AC_PinkSoft", "#FF9EC0", 0.44)
    get_mat("AC_Cream", "#FFF6E6", 0.46)
    get_mat("AC_WoodWarm", "#C28A5B", 0.62)
    get_mat("AC_WoodLight", "#D9AC7C", 0.58)
    get_mat("AC_WoodDark", "#7F5438", 0.64)
    get_mat("AC_Gold", "#F0BE59", 0.34)
    get_mat("AC_MetalDark", "#424C5C", 0.36)
    get_mat("AC_MetalSilver", "#D8E0EA", 0.25)
    get_mat("AC_Glass", "#CBEFF7", 0.14)
    get_mat("AC_SunnyYellow", "#F7DA78", 0.46)
    get_mat("AC_Strawberry", "#F47B96", 0.42)
    get_mat("AC_Vanilla", "#FFF2CF", 0.46)
    get_mat("AC_Chocolate", "#754C35", 0.45)
    get_mat("AC_Waffle", "#DFA864", 0.56)
    get_mat("AC_Grass", "#6DA876", 0.62)
    get_mat("AC_GrassLight", "#87BC85", 0.60)
    get_mat("AC_BushGreen", "#5D9969", 0.60)
    get_mat("AC_BushLight", "#7AB37B", 0.58)
    get_mat("AC_FlowerPink", "#F598B5", 0.48)
    get_mat("AC_FlowerYellow", "#F7DF7E", 0.48)
    get_mat("AC_FlowerWhite", "#FFFDF7", 0.48)


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


def make_cyl(name, radius, depth, mat, col, location=(0, 0, 0), rotation=(0, 0, 0), segs=16, r_top=None):
    bm = bmesh.new()
    rt = radius if r_top is None else r_top
    bmesh.ops.create_cone(bm, cap_ends=True, cap_tris=False, segments=segs, radius1=radius, radius2=rt, depth=depth)
    return add_mesh_obj(name, bm, mat, col, location=location, rotation=rotation)


def make_sphere(name, radius, mat, col, location=(0, 0, 0), scale=(1, 1, 1), u_segs=14, v_segs=10):
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


# 1. HERO ICE CREAM CART (Turquoise body + Pink & White Scalloped Umbrella + Flavor Display + Spoked Wheels!)
def build_hero_ice_cream_cart(col):
    m_mint = MATERIALS["AC_MintTeal"]
    m_pink = MATERIALS["AC_PinkBerry"]
    m_pink_s = MATERIALS["AC_PinkSoft"]
    m_cream = MATERIALS["AC_Cream"]
    m_wood_l = MATERIALS["AC_WoodLight"]
    m_wood_w = MATERIALS["AC_WoodWarm"]
    m_gold = MATERIALS["AC_Gold"]
    m_dark = MATERIALS["AC_MetalDark"]
    m_silver = MATERIALS["AC_MetalSilver"]
    m_glass = MATERIALS["AC_Glass"]

    parts = [
        # Lower chassis frame & beveled turquoise cart body
        make_box("Chassis", (1.62, 0.94, 0.10), m_wood_w, col, location=(0, 0, 0.36), bevel=0.03),
        make_box("CartBody", (1.58, 0.92, 0.78), m_mint, col, location=(0, 0, 0.78), bevel=0.07),
        # Iconic hero pink waist stripe & wooden trim moulding
        make_box("PinkBand", (1.63, 0.96, 0.14), m_pink, col, location=(0, 0, 0.74), bevel=0.03),
        make_box("FrontPlaqueBack", (1.10, 0.08, 0.42), m_cream, col, location=(0, -0.49, 0.80), bevel=0.03),
        make_box("FrontPlaqueFrame", (1.16, 0.06, 0.48), m_gold, col, location=(0, -0.47, 0.80), bevel=0.03),
        # Cute 3D ice cream cone crest on the front plaque!
        make_cyl("CrestCone", 0.015, 0.18, MATERIALS["AC_Waffle"], col, location=(-0.38, -0.55, 0.75), segs=10, r_top=0.07),
        make_sphere("CrestScoop", 0.085, m_pink, col, location=(-0.38, -0.55, 0.86), u_segs=10, v_segs=8),
        # Proportional cream counter top
        make_box("CounterTop", (1.76, 1.08, 0.11), m_cream, col, location=(0, 0, 1.21), bevel=0.04),
        # Open-front Artisan Flavor Vitrine so all 4 pastel ice cream tubs shine clearly!
        make_box("VitrineBase", (1.22, 0.66, 0.09), m_wood_l, col, location=(-0.12, -0.02, 1.30), bevel=0.025),
        make_box("VitrineBackGlass", (1.20, 0.05, 0.28), m_glass, col, location=(-0.12, 0.28, 1.46), bevel=0.012),
        make_box("VitrineFrontLip", (1.20, 0.05, 0.11), m_glass, col, location=(-0.12, -0.32, 1.38), bevel=0.012),
        make_cyl("VitrinePostFL", 0.020, 0.28, m_silver, col, location=(-0.70, -0.30, 1.46), segs=8),
        make_cyl("VitrinePostFR", 0.020, 0.28, m_silver, col, location=(0.46, -0.30, 1.46), segs=8),
        make_cyl("VitrinePostBL", 0.020, 0.28, m_silver, col, location=(-0.70, 0.26, 1.46), segs=8),
        make_cyl("VitrinePostBR", 0.020, 0.28, m_silver, col, location=(0.46, 0.26, 1.46), segs=8),
        make_box("VitrineTopRail", (1.22, 0.05, 0.04), m_silver, col, location=(-0.12, -0.30, 1.60), bevel=0.01),
        make_box("VitrineBackRail", (1.22, 0.05, 0.04), m_silver, col, location=(-0.12, 0.26, 1.60), bevel=0.01),
        # Push handle bar on right side
        make_cyl("HandleArmF", 0.028, 0.42, m_silver, col, location=(0.94, -0.26, 1.18), rotation=(0, 1.22, 0), segs=10),
        make_cyl("HandleArmB", 0.028, 0.42, m_silver, col, location=(0.94, 0.26, 1.18), rotation=(0, 1.22, 0), segs=10),
        make_cyl("HandleGrip", 0.042, 0.62, m_pink, col, location=(1.12, 0, 1.26), rotation=(math.pi / 2, 0, 0), segs=12),
        # Cone holder cylinder on counter right
        make_cyl("ConeHolder", 0.075, 0.22, m_gold, col, location=(0.66, -0.16, 1.37), segs=12),
        make_cyl("DisplayCone", 0.015, 0.18, MATERIALS["AC_Waffle"], col, location=(0.66, -0.16, 1.50), segs=10, r_top=0.065),
        make_sphere("DisplayScoop", 0.080, m_pink_s, col, location=(0.66, -0.16, 1.60), u_segs=10, v_segs=8),
    ]

    # 4 Prominent Pastel Flavor Tubs inside the open-front vitrine (Lemon, Strawberry, Vanilla, Chocolate)
    flavors = [MATERIALS["AC_SunnyYellow"], MATERIALS["AC_Strawberry"], MATERIALS["AC_Vanilla"], MATERIALS["AC_Chocolate"]]
    for i, fm in enumerate(flavors):
        tx = -0.52 + i * 0.265
        parts.append(make_box(f"Tub_{i}", (0.23, 0.46, 0.12), m_silver, col, location=(tx, -0.02, 1.39), bevel=0.02))
        parts.append(make_sphere(f"Scoop_{i}", 0.125, fm, col, location=(tx, -0.02, 1.48), scale=(0.92, 1.45, 0.72), u_segs=12, v_segs=8))

    # Rounded Vintage Spoked Wheels (spokes clearly visible on outer rim face!)
    for wy in (-0.54, 0.54):
        wx = -0.42
        outer_y = wy + (-0.052 if wy < 0 else 0.052)
        parts.append(make_cyl(f"Tire_{wy}", 0.34, 0.09, m_dark, col, location=(wx, wy, 0.34), rotation=(math.pi / 2, 0, 0), segs=22))
        parts.append(make_cyl(f"Rim_{wy}", 0.27, 0.095, m_cream, col, location=(wx, wy, 0.34), rotation=(math.pi / 2, 0, 0), segs=20))
        parts.append(make_cyl(f"Hub_{wy}", 0.085, 0.14, m_pink, col, location=(wx, wy, 0.34), rotation=(math.pi / 2, 0, 0), segs=14))
        for sp in range(4):
            ang = sp * math.pi / 4.0
            parts.append(make_box(f"Spoke_{wy}_{sp}", (0.50, 0.024, 0.032), m_gold, col, location=(wx, outer_y, 0.34), rotation=(0, ang, 0), bevel=0.004))

    # Right support legs + small caster wheels
    for wy in (-0.34, 0.34):
        parts.append(make_cyl(f"Leg_{wy}", 0.035, 0.26, m_silver, col, location=(0.56, wy, 0.22), segs=10))
        parts.append(make_cyl(f"Caster_{wy}", 0.11, 0.07, m_dark, col, location=(0.56, wy, 0.11), rotation=(math.pi / 2, 0, 0), segs=14))

    # Iconic Pink & White Striped Scalloped Umbrella — RAISED (+0.48m) & OFFSET BACK (+0.34Y)
    # so the counter, open flavor vitrine, scoops & customer service side stay 100% unobstructed on mobile camera!
    umb_y = 0.34
    parts.append(make_cyl("UmbPole", 0.042, 2.24, m_wood_l, col, location=(0.0, umb_y, 2.08), segs=12))
    parts.append(make_sphere("UmbFinial", 0.085, m_gold, col, location=(0.0, umb_y, 3.30), u_segs=12, v_segs=8))
    parts.append(make_sphere("UmbCap", 0.28, m_pink, col, location=(0.0, umb_y, 3.21), scale=(1.0, 1.0, 0.34), u_segs=14, v_segs=8))
    segs = 12
    for i in range(segs):
        ang = (i / segs) * math.pi * 2.0
        um = m_pink if i % 2 == 0 else m_cream
        px = math.cos(ang) * 0.44
        py = umb_y + math.sin(ang) * 0.44
        parts.append(make_box(f"UmbPanel_{i}", (0.72, 0.34, 0.065), um, col,
                              location=(px, py, 3.14),
                              rotation=(0.0, 0.22, ang), bevel=0.02))
        rx = math.cos(ang) * 0.78
        ry = umb_y + math.sin(ang) * 0.78
        parts.append(make_sphere(f"Scallop_{i}", 0.12, um, col, location=(rx, ry, 2.99), scale=(0.85, 0.85, 0.68), u_segs=10, v_segs=6))

    return join_and_export(parts, "IceCreamCart", col)


# 2. VARIED COASTAL GRASS & WILDFLOWER CLUSTER (Soft coastal green!)
def build_coastal_veg_a(col):
    m_g1 = MATERIALS["AC_Grass"]
    m_g2 = MATERIALS["AC_GrassLight"]
    m_b1 = MATERIALS["AC_BushGreen"]
    m_fl1 = MATERIALS["AC_FlowerWhite"]
    m_fl2 = MATERIALS["AC_FlowerYellow"]

    parts = [
        make_sphere("LowBushA", 0.36, m_b1, col, location=(-0.28, 0.08, 0.24), scale=(1.15, 0.95, 0.72), u_segs=12, v_segs=8),
        make_sphere("LowBushB", 0.28, m_g1, col, location=(0.26, -0.08, 0.20), scale=(1.0, 1.1, 0.76), u_segs=12, v_segs=8),
        make_sphere("GrassTuft1", 0.22, m_g2, col, location=(0.04, 0.24, 0.16), scale=(0.8, 0.8, 1.15), u_segs=10, v_segs=6),
        make_sphere("GrassTuft2", 0.18, m_g2, col, location=(-0.48, -0.18, 0.14), scale=(0.85, 0.85, 1.05), u_segs=10, v_segs=6),
        make_sphere("Flw1", 0.065, m_fl1, col, location=(-0.18, -0.02, 0.44), u_segs=8, v_segs=6),
        make_sphere("Flw2", 0.06, m_fl2, col, location=(0.30, -0.10, 0.38), u_segs=8, v_segs=6),
    ]
    return join_and_export(parts, "CoastalVegetationA", col)


# 3. VARIED WOODEN PLANTER WITH MULTI-HEIGHT SHRUBS (Marks entrances & curves!)
def build_coastal_veg_b(col):
    m_wood_l = MATERIALS["AC_WoodLight"]
    m_wood_w = MATERIALS["AC_WoodWarm"]
    m_b1 = MATERIALS["AC_BushGreen"]
    m_b2 = MATERIALS["AC_BushLight"]
    m_g2 = MATERIALS["AC_GrassLight"]
    m_fl = MATERIALS["AC_FlowerPink"]

    parts = [
        make_box("PlanterBase", (1.36, 0.62, 0.34), m_wood_l, col, location=(0, 0, 0.17), bevel=0.04),
        make_box("PlanterRim", (1.44, 0.70, 0.08), m_wood_w, col, location=(0, 0, 0.35), bevel=0.025),
        make_sphere("ShrubTall", 0.34, m_b1, col, location=(-0.34, 0.02, 0.56), scale=(1.05, 0.92, 1.05), u_segs=12, v_segs=8),
        make_sphere("ShrubMid", 0.28, m_b2, col, location=(0.14, -0.04, 0.50), scale=(1.1, 0.95, 0.82), u_segs=12, v_segs=8),
        make_sphere("ShrubLow", 0.22, m_g2, col, location=(0.48, 0.04, 0.45), scale=(0.95, 0.9, 0.78), u_segs=10, v_segs=8),
        make_sphere("Blossom1", 0.065, m_fl, col, location=(-0.26, -0.18, 0.74), u_segs=8, v_segs=6),
        make_sphere("Blossom2", 0.060, MATERIALS["AC_FlowerWhite"], col, location=(0.20, -0.16, 0.64), u_segs=8, v_segs=6),
    ]
    return join_and_export(parts, "CoastalVegetationB", col)


def render_preview(col):
    world = bpy.data.worlds.new("HeroPreviewWorld")
    bpy.context.scene.world = world
    nt = world.node_tree
    nt.nodes.clear()
    out = nt.nodes.new("ShaderNodeOutputWorld")
    bg = nt.nodes.new("ShaderNodeBackground")
    bg.inputs["Color"].default_value = hex_to_rgba("#CBE8F6")
    bg.inputs["Strength"].default_value = 1.0
    nt.links.new(bg.outputs["Background"], out.inputs["Surface"])

    sun_d = bpy.data.lights.new("Sun", type="SUN")
    sun_d.energy = 2.8
    sun_o = bpy.data.objects.new("Sun", sun_d)
    sun_o.rotation_euler = (math.radians(42), math.radians(16), math.radians(-28))
    link_obj(sun_o, col)

    cam_d = bpy.data.cameras.new("Cam")
    cam_d.lens = 40.0
    cam_o = bpy.data.objects.new("Cam", cam_d)
    cam_o.location = (0.0, -6.8, 3.4)
    direction = Vector((0.0, 0.0, 1.35)) - cam_o.location
    cam_o.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()
    link_obj(cam_o, col)
    bpy.context.scene.camera = cam_o

    scene = bpy.context.scene
    engines = [e.identifier for e in bpy.types.RenderSettings.bl_rna.properties["engine"].enum_items]
    scene.render.engine = "BLENDER_EEVEE_NEXT" if "BLENDER_EEVEE_NEXT" in engines else ("BLENDER_EEVEE" if "BLENDER_EEVEE" in engines else "CYCLES")
    scene.render.resolution_x = 1024
    scene.render.resolution_y = 576
    scene.render.image_settings.file_format = "PNG"
    scene.render.filepath = PREVIEW_OUT
    bpy.ops.render.render(write_still=True)


def main():
    clear_scene()
    init_palette()
    col = ensure_collection("BaiaDoAvesso_HeroPolish")

    cart = build_hero_ice_cream_cart(col)
    veg_a = build_coastal_veg_a(col)
    veg_b = build_coastal_veg_b(col)

    cart.location = (0.0, 0.0, 0.0)
    veg_a.location = (-2.1, 0.3, 0.0)
    veg_b.location = (2.2, 0.3, 0.0)

    bpy.ops.wm.save_as_mainfile(filepath=BLEND_OUT)
    render_preview(col)
    print("[Hero Blender Polish Complete]")


if __name__ == "__main__":
    main()
