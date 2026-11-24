"""
build_baia_detective_assets.py
------------------------------
Blender 5.3 procedural 3D asset generator for the Baia do Avesso Detective Pipoca & Police system:
  1. Char_Thief.fbx         - Sneaky street thief with bandit mask, beanie, striped shirt & loot sack
  2. Char_PoliceOfficer.fbx - Friendly coastal Police Officer with navy uniform, gold badge & cap
  3. PolicePatrolCar.fbx    - Cute coastal patrol car with flashing red & blue beacon lightbar
  4. DetectiveHatPipoca.fbx - Adorable Sherlock-style detective cap for Pipoca!

Exports clean Y-up FBX models to Assets/GirlPower/Orla3D/Models,
saves BaiaDoAvesso_DetectiveAssets.blend, and renders baia_detective_preview.png.
"""

import math
import os
import bpy
import bmesh
from mathutils import Vector

SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
ORLA3D_DIR = os.path.dirname(SCRIPT_DIR)
MODELS_DIR = os.path.join(ORLA3D_DIR, "Models")
BLEND_OUT = os.path.join(SCRIPT_DIR, "BaiaDoAvesso_DetectiveAssets.blend")
PREVIEW_OUT = os.path.join(SCRIPT_DIR, "baia_detective_preview.png")

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
    get_mat("AC_SkinLight", "#F4C29F", 0.56)
    get_mat("AC_SkinTan", "#DF9F73", 0.56)
    get_mat("AC_EyeDark", "#251D29", 0.15)
    get_mat("AC_EyeShine", "#FFFFFF", 0.10, "#FFFFFF", 0.45)
    get_mat("AC_MetalDark", "#3F4959", 0.35)
    get_mat("AC_MetalSilver", "#D4DCE6", 0.25)
    get_mat("AC_Denim", "#3B5E94", 0.60)
    get_mat("AC_SkyBlue", "#6CC0F0", 0.40)
    get_mat("AC_Cream", "#FFF6E5", 0.48)
    get_mat("AC_Coral", "#F25A52", 0.42, "#FF544A", 1.2)
    get_mat("AC_Gold", "#F5BE4F", 0.32)
    get_mat("AC_WoodWarm", "#B97A4B", 0.65)
    get_mat("AC_WoodDark", "#7B4C2F", 0.68)
    get_mat("AC_Waffle", "#D8A668", 0.64)
    get_mat("AC_Glass", "#B8F0FA", 0.15)
    get_mat("AC_ShoeDark", "#332A38", 0.50)
    get_mat("AC_PurplePlum", "#4A345C", 0.52)


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


def make_box(name, size, mat, col, location=(0, 0, 0), rotation=(0, 0, 0), bevel=0.025, segs=2):
    bm = bmesh.new()
    bmesh.ops.create_cube(bm, size=1.0)
    bmesh.ops.scale(bm, vec=Vector(size), verts=bm.verts)
    if bevel > 0.0:
        b = min(bevel, min(size[0], size[1], size[2]) * 0.42)
        bmesh.ops.bevel(bm, geom=list(bm.edges), offset=b, segments=segs, affect="EDGES", clamp_overlap=True)
    return add_mesh_obj(name, bm, mat, col, location=location, rotation=rotation)


def make_cyl(name, radius, depth, mat, col, location=(0, 0, 0), rotation=(0, 0, 0), segs=14, r_top=None):
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


# 1. STREET THIEF (LADRAO DE RUA)
def build_thief(col):
    parts = [
        # Boots & Legs
        make_sphere("ShoeL", 0.11, MATERIALS["AC_ShoeDark"], col, location=(-0.13, -0.02, 0.08), scale=(0.9, 1.35, 0.7)),
        make_sphere("ShoeR", 0.11, MATERIALS["AC_ShoeDark"], col, location=(0.13, -0.02, 0.08), scale=(0.9, 1.35, 0.7)),
        make_cyl("LegL", 0.078, 0.30, MATERIALS["AC_MetalDark"], col, location=(-0.13, 0, 0.25), segs=12),
        make_cyl("LegR", 0.078, 0.30, MATERIALS["AC_MetalDark"], col, location=(0.13, 0, 0.25), segs=12),
        # Striped bandit torso
        make_cyl("Torso", 0.24, 0.52, MATERIALS["AC_MetalDark"], col, location=(0, 0, 0.62), segs=16, r_top=0.20),
        make_cyl("Stripe1", 0.248, 0.09, MATERIALS["AC_Cream"], col, location=(0, 0, 0.52), segs=16),
        make_cyl("Stripe2", 0.228, 0.09, MATERIALS["AC_Cream"], col, location=(0, 0, 0.70), segs=16),
        # Sneaky arms
        make_cyl("ArmL", 0.068, 0.34, MATERIALS["AC_MetalDark"], col, location=(-0.29, -0.06, 0.66), rotation=(0.35, 0.28, 0), segs=10),
        make_cyl("ArmR", 0.068, 0.34, MATERIALS["AC_MetalDark"], col, location=(0.29, -0.06, 0.66), rotation=(0.35, -0.28, 0), segs=10),
        make_sphere("HandL", 0.072, MATERIALS["AC_SkinTan"], col, location=(-0.34, -0.15, 0.52)),
        make_sphere("HandR", 0.072, MATERIALS["AC_SkinTan"], col, location=(0.34, -0.15, 0.52)),
        # Head + Bandit Domino Mask + Dark Beanie
        make_sphere("Head", 0.30, MATERIALS["AC_SkinTan"], col, location=(0, 0, 1.12), scale=(1.0, 0.96, 1.0)),
        make_box("BanditMask", (0.44, 0.14, 0.14), MATERIALS["AC_ShoeDark"], col, location=(0, -0.24, 1.13), bevel=0.03),
        make_sphere("EyeL", 0.042, MATERIALS["AC_Cream"], col, location=(-0.10, -0.295, 1.13), u_segs=8, v_segs=6),
        make_sphere("EyeR", 0.042, MATERIALS["AC_Cream"], col, location=(0.10, -0.295, 1.13), u_segs=8, v_segs=6),
        make_sphere("PupilL", 0.022, MATERIALS["AC_EyeDark"], col, location=(-0.10, -0.325, 1.13), u_segs=8, v_segs=6),
        make_sphere("PupilR", 0.022, MATERIALS["AC_EyeDark"], col, location=(0.10, -0.325, 1.13), u_segs=8, v_segs=6),
        make_sphere("Beanie", 0.32, MATERIALS["AC_PurplePlum"], col, location=(0, 0.02, 1.24), scale=(1.0, 1.0, 0.78)),
        make_cyl("BeanieCuff", 0.325, 0.09, MATERIALS["AC_ShoeDark"], col, location=(0, 0.01, 1.22), segs=16),
        # Tan loot sack slung over the back!
        make_sphere("LootSack", 0.28, MATERIALS["AC_Waffle"], col, location=(0, 0.32, 0.72), scale=(0.95, 0.85, 1.18)),
        make_cyl("SackTie", 0.11, 0.08, MATERIALS["AC_Coral"], col, location=(0, 0.28, 0.98), segs=10),
    ]
    return join_and_export(parts, "Char_Thief", col)


# 2. COASTAL POLICE OFFICER (POLICIAL DA BAIA)
def build_police_officer(col):
    parts = [
        make_sphere("ShoeL", 0.11, MATERIALS["AC_ShoeDark"], col, location=(-0.13, -0.02, 0.08), scale=(0.9, 1.35, 0.7)),
        make_sphere("ShoeR", 0.11, MATERIALS["AC_ShoeDark"], col, location=(0.13, -0.02, 0.08), scale=(0.9, 1.35, 0.7)),
        make_cyl("LegL", 0.08, 0.30, MATERIALS["AC_MetalDark"], col, location=(-0.13, 0, 0.25), segs=12),
        make_cyl("LegR", 0.08, 0.30, MATERIALS["AC_MetalDark"], col, location=(0.13, 0, 0.25), segs=12),
        # Crisp Navy Police Uniform + Belt + Golden Star Badge
        make_cyl("Torso", 0.25, 0.54, MATERIALS["AC_Denim"], col, location=(0, 0, 0.63), segs=16, r_top=0.22),
        make_cyl("Belt", 0.255, 0.08, MATERIALS["AC_ShoeDark"], col, location=(0, 0, 0.45), segs=16),
        make_box("Buckle", (0.10, 0.06, 0.08), MATERIALS["AC_Gold"], col, location=(0, -0.24, 0.45), bevel=0.01),
        make_sphere("Badge", 0.055, MATERIALS["AC_Gold"], col, location=(0.11, -0.22, 0.74), u_segs=8, v_segs=6),
        make_cyl("ArmL", 0.07, 0.34, MATERIALS["AC_Denim"], col, location=(-0.30, 0, 0.68), rotation=(0, 0.22, 0), segs=10),
        make_cyl("ArmR", 0.07, 0.34, MATERIALS["AC_Denim"], col, location=(0.30, -0.05, 0.72), rotation=(-0.45, -0.22, 0), segs=10),
        make_sphere("HandL", 0.072, MATERIALS["AC_SkinLight"], col, location=(-0.34, 0, 0.52)),
        make_sphere("HandR", 0.072, MATERIALS["AC_SkinLight"], col, location=(0.34, -0.18, 0.84)),
        # Head + Friendly Eyes + Police Officer Visor Cap
        make_sphere("Head", 0.30, MATERIALS["AC_SkinLight"], col, location=(0, 0, 1.13)),
        make_sphere("EyeL", 0.042, MATERIALS["AC_EyeDark"], col, location=(-0.10, -0.27, 1.14), u_segs=8, v_segs=6),
        make_sphere("EyeR", 0.042, MATERIALS["AC_EyeDark"], col, location=(0.10, -0.27, 1.14), u_segs=8, v_segs=6),
        make_cyl("CapBase", 0.31, 0.14, MATERIALS["AC_Denim"], col, location=(0, 0, 1.36), segs=16, r_top=0.35),
        make_box("CapVisor", (0.34, 0.20, 0.045), MATERIALS["AC_ShoeDark"], col, location=(0, -0.28, 1.30), rotation=(0.16, 0, 0), bevel=0.015),
        make_sphere("CapStar", 0.055, MATERIALS["AC_Gold"], col, location=(0, -0.33, 1.38), u_segs=8, v_segs=6),
    ]
    return join_and_export(parts, "Char_PoliceOfficer", col)


# 3. COASTAL POLICE PATROL CAR (VIATURA DA POLICIA)
def build_police_patrol_car(col):
    parts = [
        # Rounded body + cabin + stripe
        make_box("Body", (1.68, 3.10, 0.72), MATERIALS["AC_Cream"], col, location=(0, 0, 0.62), bevel=0.14),
        make_box("BlueStripe", (1.72, 3.14, 0.22), MATERIALS["AC_Denim"], col, location=(0, 0, 0.54), bevel=0.04),
        make_box("Cabin", (1.46, 1.65, 0.66), MATERIALS["AC_Cream"], col, location=(0, 0.15, 1.24), bevel=0.12),
        make_box("Windshield", (1.30, 0.12, 0.46), MATERIALS["AC_Glass"], col, location=(0, -0.68, 1.22), rotation=(0.18, 0, 0), bevel=0.03),
        make_box("SideGlass", (1.50, 1.18, 0.42), MATERIALS["AC_Glass"], col, location=(0, 0.15, 1.22), bevel=0.03),
        # Flashing Red & Blue Roof Siren Bar!
        make_box("SirenBase", (0.94, 0.28, 0.08), MATERIALS["AC_MetalSilver"], col, location=(0, 0.08, 1.60), bevel=0.02),
        make_box("SirenRed", (0.36, 0.24, 0.16), MATERIALS["AC_Coral"], col, location=(-0.24, 0.08, 1.70), bevel=0.04),
        make_box("SirenBlue", (0.36, 0.24, 0.16), MATERIALS["AC_SkyBlue"], col, location=(0.24, 0.08, 1.70), bevel=0.04),
        # Headlights & bumpers
        make_sphere("LightL", 0.13, MATERIALS["AC_Gold"], col, location=(-0.56, -1.54, 0.66), u_segs=10, v_segs=8),
        make_sphere("LightR", 0.13, MATERIALS["AC_Gold"], col, location=(0.56, -1.54, 0.66), u_segs=10, v_segs=8),
        make_box("BumperF", (1.74, 0.20, 0.18), MATERIALS["AC_MetalSilver"], col, location=(0, -1.58, 0.36), bevel=0.04),
        make_box("BumperB", (1.74, 0.20, 0.18), MATERIALS["AC_MetalSilver"], col, location=(0, 1.58, 0.36), bevel=0.04),
    ]
    for wx in (-0.82, 0.82):
        for wy in (-0.98, 0.98):
            parts.append(make_cyl("Tire", 0.34, 0.24, MATERIALS["AC_ShoeDark"], col, location=(wx, wy, 0.34), rotation=(0, math.pi / 2, 0), segs=14))
            parts.append(make_cyl("Hub", 0.17, 0.26, MATERIALS["AC_MetalSilver"], col, location=(wx, wy, 0.34), rotation=(0, math.pi / 2, 0), segs=12))
    return join_and_export(parts, "PolicePatrolCar", col)


# 4. DETECTIVE PIPOCA CAP (CHAPEU DE DETETIVE DO PIPOCA)
def build_detective_hat_pipoca(col):
    parts = [
        make_sphere("Crown", 0.22, MATERIALS["AC_WoodWarm"], col, location=(0, 0, 0.08), scale=(1.0, 1.15, 0.72), u_segs=12, v_segs=8),
        make_box("VisorF", (0.28, 0.18, 0.04), MATERIALS["AC_WoodDark"], col, location=(0, -0.22, 0.02), rotation=(0.18, 0, 0), bevel=0.015),
        make_box("VisorB", (0.26, 0.16, 0.04), MATERIALS["AC_WoodDark"], col, location=(0, 0.22, 0.02), rotation=(-0.18, 0, 0), bevel=0.015),
        make_cyl("Band", 0.225, 0.045, MATERIALS["AC_Gold"], col, location=(0, 0, 0.06), segs=14),
    ]
    return join_and_export(parts, "DetectiveHatPipoca", col)


def render_preview(col):
    world = bpy.data.worlds.new("DetectivePreviewWorld")
    bpy.context.scene.world = world
    nt = world.node_tree
    nt.nodes.clear()
    out = nt.nodes.new("ShaderNodeOutputWorld")
    bg = nt.nodes.new("ShaderNodeBackground")
    bg.inputs["Color"].default_value = hex_to_rgba("#CBEBFF")
    bg.inputs["Strength"].default_value = 1.0
    nt.links.new(bg.outputs["Background"], out.inputs["Surface"])

    sun_d = bpy.data.lights.new("Sun", type="SUN")
    sun_d.energy = 3.0
    sun_o = bpy.data.objects.new("Sun", sun_d)
    sun_o.rotation_euler = (math.radians(44), math.radians(18), math.radians(-26))
    link_obj(sun_o, col)

    cam_d = bpy.data.cameras.new("Cam")
    cam_d.lens = 38.0
    cam_o = bpy.data.objects.new("Cam", cam_d)
    cam_o.location = (0.0, -8.5, 3.2)
    direction = Vector((0.0, 0.2, 0.8)) - cam_o.location
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
    col = ensure_collection("BaiaDoAvesso_Detective")

    thief = build_thief(col)
    officer = build_police_officer(col)
    car = build_police_patrol_car(col)
    hat = build_detective_hat_pipoca(col)

    thief.location = (-2.6, 0.0, 0.0)
    officer.location = (-0.9, 0.0, 0.0)
    car.location = (1.8, 0.4, 0.0)
    car.rotation_euler = (0, 0, -0.35)
    hat.location = (-1.75, -1.2, 0.25)

    bpy.ops.wm.save_as_mainfile(filepath=BLEND_OUT)
    render_preview(col)
    print("[Detective Blender Assets Complete]")


if __name__ == "__main__":
    main()
