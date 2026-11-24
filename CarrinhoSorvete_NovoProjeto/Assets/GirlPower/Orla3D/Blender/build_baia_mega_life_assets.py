"""
build_baia_mega_life_assets.py
------------------------------
Blender 5.3 procedural 3D asset generator for the Baia do Avesso Mega Life & Sports Expansion:
  1.  Skateboard.fbx           - Pastel cruiser skateboard with trucks & wheels
  2.  Scooter.fbx              - Mint coastal patinete with handlebars & deck
  3.  RollerSkates.fbx         - Pair of cute pastel roller skates for feet
  4.  HawaiianCanoe.fbx        - Va'a Hawaiian outrigger canoe + ama + paddle
  5.  BeachTennisCourt.fbx     - Beach Tennis net, posts, boundary lines, rackets & ball
  6.  CampingTent.fbx          - Family beach tent + picnic blanket + cooler + campfire
  7.  RestaurantKioskTable.fbx - Kiosk dining table + umbrella + 4 stools + basket of French Fries & drinks
  8.  RestaurantFriesKitchen.fbx - Coastal Restaurant "Batata & Mar" counter + fryer + menu board
  9.  MarketStall_Doces.fbx    - Sweets, Cupcakes & Candy beach stall
  10. MarketStall_SucosRefri.fbx - Cold Water, Sodas & Fresh Juices stall
  11. GardenPlanterBed.fbx     - Raised coastal herb & berry garden bed + watering can
  12. FishingPierDock.fbx      - Fishing Pier deck + rod stand + wooden fish bucket
  13. VillageNoticeBoard.fbx   - Coastal Village Quest Notice Board with pinned colorful notes
  14. WardrobeHatProp.fbx      - Straw sun hat + stylish sunglasses accessory for Protagonist
  15. PetBandanaProp.fbx       - Matching colorful bandana collar for Pipoca
  16. FishProp.fbx             - Cute tropical swimming fish for shallow bay & fishing

Exports clean Y-up FBX meshes directly into Assets/GirlPower/Orla3D/Models,
saves BaiaDoAvesso_MegaLifeAssets.blend, and renders baia_megalife_preview.png.
"""

import math
import os
import bpy
import bmesh
from mathutils import Vector

SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
ORLA3D_DIR = os.path.dirname(SCRIPT_DIR)
MODELS_DIR = os.path.join(ORLA3D_DIR, "Models")
BLEND_OUT = os.path.join(SCRIPT_DIR, "BaiaDoAvesso_MegaLifeAssets.blend")
PREVIEW_OUT = os.path.join(SCRIPT_DIR, "baia_megalife_preview.png")

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
    get_mat("AC_StoneWhite", "#E2D9CA", 0.70)
    get_mat("AC_StoneDark", "#A4947E", 0.72)
    get_mat("AC_Cream", "#FFF6E5", 0.48)
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
    get_mat("AC_Waffle", "#E3A75B", 0.56)
    get_mat("AC_Glass", "#B8F0FA", 0.14)
    get_mat("AC_LanternGlow", "#FFE69C", 0.22, "#FFD26B", 2.6)
    get_mat("AC_MetalDark", "#3F4959", 0.35)
    get_mat("AC_MetalSilver", "#D4DCE6", 0.25)
    get_mat("AC_StrawHat", "#F2CF7E", 0.62)
    get_mat("AC_FlowerPink", "#FF7AA8", 0.48)
    get_mat("AC_FlowerYellow", "#FFE055", 0.48)
    get_mat("AC_FlowerBlue", "#75BFFF", 0.48)
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


# 1. SKATEBOARD
def build_skateboard(col):
    parts = [
        make_box("Deck", (0.34, 1.02, 0.045), MATERIALS["AC_PinkBerry"], col, location=(0, 0, 0.11), bevel=0.02),
        make_box("Grip", (0.28, 0.84, 0.015), MATERIALS["AC_MetalDark"], col, location=(0, 0, 0.138), bevel=0.005),
        make_box("Stripe", (0.10, 0.92, 0.012), MATERIALS["AC_SunnyYellow"], col, location=(0, 0, 0.088), bevel=0.004),
        make_box("Nose", (0.31, 0.18, 0.04), MATERIALS["AC_PinkBerry"], col, location=(0, 0.56, 0.145), rotation=(0.35, 0, 0), bevel=0.015),
        make_box("Tail", (0.31, 0.18, 0.04), MATERIALS["AC_PinkBerry"], col, location=(0, -0.56, 0.145), rotation=(-0.35, 0, 0), bevel=0.015),
    ]
    for ty in (-0.34, 0.34):
        parts.append(make_cyl("Axle", 0.025, 0.34, MATERIALS["AC_MetalSilver"], col, location=(0, ty, 0.055), rotation=(0, math.pi / 2, 0), segs=10))
        for tx in (-0.17, 0.17):
            parts.append(make_cyl("Wheel", 0.055, 0.055, MATERIALS["AC_MintTeal"], col, location=(tx, ty, 0.055), rotation=(0, math.pi / 2, 0), segs=12))
    return join_and_export(parts, "Skateboard", col)


# 2. SCOOTER (PATINETE)
def build_scooter(col):
    parts = [
        make_box("Deck", (0.24, 0.88, 0.045), MATERIALS["AC_MintTeal"], col, location=(0, 0, 0.11), bevel=0.02),
        make_box("DeckStrip", (0.16, 0.72, 0.015), MATERIALS["AC_Cream"], col, location=(0, 0, 0.136), bevel=0.005),
        make_cyl("Stem", 0.030, 0.96, MATERIALS["AC_MetalSilver"], col, location=(0, 0.38, 0.58), rotation=(-0.14, 0, 0), segs=12),
        make_cyl("Handlebar", 0.026, 0.54, MATERIALS["AC_Coral"], col, location=(0, 0.45, 1.04), rotation=(0, math.pi / 2, 0), segs=10),
        make_cyl("GripL", 0.036, 0.12, MATERIALS["AC_PinkBerry"], col, location=(-0.23, 0.45, 1.04), rotation=(0, math.pi / 2, 0), segs=10),
        make_cyl("GripR", 0.036, 0.12, MATERIALS["AC_PinkBerry"], col, location=(0.23, 0.45, 1.04), rotation=(0, math.pi / 2, 0), segs=10),
        make_cyl("WheelF", 0.095, 0.055, MATERIALS["AC_SunnyYellow"], col, location=(0, 0.46, 0.095), rotation=(0, math.pi / 2, 0), segs=14),
        make_cyl("WheelB", 0.095, 0.055, MATERIALS["AC_SunnyYellow"], col, location=(0, -0.44, 0.095), rotation=(0, math.pi / 2, 0), segs=14),
    ]
    return join_and_export(parts, "Scooter", col)


# 3. ROLLER SKATES (PATINS NO PE)
def build_roller_skates(col):
    parts = []
    for sx in (-0.15, 0.15):
        parts.append(make_box("Boot", (0.15, 0.32, 0.14), MATERIALS["AC_Lavender"], col, location=(sx, 0, 0.15), bevel=0.03))
        parts.append(make_box("Ankle", (0.14, 0.15, 0.14), MATERIALS["AC_PinkSoft"], col, location=(sx, -0.07, 0.25), bevel=0.03))
        parts.append(make_box("Sole", (0.16, 0.34, 0.035), MATERIALS["AC_Cream"], col, location=(sx, 0, 0.075), bevel=0.01))
        for wy in (-0.10, 0.10):
            for wx in (-0.075, 0.075):
                parts.append(make_cyl("W", 0.042, 0.035, MATERIALS["AC_SunnyYellow"], col, location=(sx + wx, wy, 0.042), rotation=(0, math.pi / 2, 0), segs=10))
        parts.append(make_sphere("Stopper", 0.038, MATERIALS["AC_Coral"], col, location=(sx, 0.18, 0.055), u_segs=8, v_segs=6))
    return join_and_export(parts, "RollerSkates", col)


# 4. HAWAIIAN CANOE (CANOA HAVAIANA / VA'A)
def build_hawaiian_canoe(col):
    parts = [
        # Long sleek hull
        make_sphere("Hull", 0.42, MATERIALS["AC_Coral"], col, location=(0, 0, 0.24), scale=(0.95, 3.6, 0.65), u_segs=16, v_segs=10),
        make_box("HullTrim", (0.76, 2.25, 0.08), MATERIALS["AC_SunnyYellow"], col, location=(0, 0, 0.40), bevel=0.03),
        make_box("Seat1", (0.64, 0.26, 0.06), MATERIALS["AC_WoodWarm"], col, location=(0, 0.32, 0.42), bevel=0.02),
        make_box("Seat2", (0.64, 0.26, 0.06), MATERIALS["AC_WoodWarm"], col, location=(0, -0.45, 0.42), bevel=0.02),
        # Outrigger cross-beams (iako)
        make_cyl("Iako1", 0.038, 1.42, MATERIALS["AC_WoodLight"], col, location=(0.68, 0.55, 0.42), rotation=(0, math.pi / 2, 0), segs=10),
        make_cyl("Iako2", 0.038, 1.42, MATERIALS["AC_WoodLight"], col, location=(0.68, -0.55, 0.42), rotation=(0, math.pi / 2, 0), segs=10),
        # Outrigger float (ama)
        make_sphere("Ama", 0.16, MATERIALS["AC_MintTeal"], col, location=(1.34, 0, 0.16), scale=(0.9, 6.2, 0.8), u_segs=12, v_segs=8),
        # Paddle resting across the bow
        make_cyl("PaddleShaft", 0.024, 1.55, MATERIALS["AC_WoodDark"], col, location=(-0.38, 0.15, 0.54), rotation=(0.15, 0.28, 0.0), segs=10),
        make_box("PaddleBlade", (0.20, 0.03, 0.46), MATERIALS["AC_WoodLight"], col, location=(-0.56, 0.24, 0.02), rotation=(0.15, 0.28, 0), bevel=0.01),
    ]
    return join_and_export(parts, "HawaiianCanoe", col)


# 5. BEACH TENNIS COURT
def build_beach_tennis_court(col):
    parts = [
        # Left & Right wooden net posts
        make_cyl("PostL", 0.075, 2.05, MATERIALS["AC_WoodWarm"], col, location=(-3.4, 0, 1.02), segs=12),
        make_cyl("PostR", 0.075, 2.05, MATERIALS["AC_WoodWarm"], col, location=(3.4, 0, 1.02), segs=12),
        # Top & bottom net tape
        make_box("TopTape", (6.8, 0.06, 0.09), MATERIALS["AC_SunnyYellow"], col, location=(0, 0, 1.88), bevel=0.015),
        make_box("BotTape", (6.8, 0.05, 0.06), MATERIALS["AC_Cream"], col, location=(0, 0, 1.05), bevel=0.01),
    ]
    # Stylized net grid slats
    for x in range(-6, 7):
        parts.append(make_box(f"NetV_{x}", (0.025, 0.02, 0.80), MATERIALS["AC_Cream"], col, location=(x * 0.52, 0, 1.46), bevel=0.0))
    for z in (1.25, 1.46, 1.67):
        parts.append(make_box(f"NetH_{z}", (6.7, 0.02, 0.022), MATERIALS["AC_Cream"], col, location=(0, 0, z), bevel=0.0))
    # Bright court boundary ribbons on the sand
    parts.append(make_box("LineN", (6.2, 0.12, 0.025), MATERIALS["AC_Coral"], col, location=(0, 2.4, 0.02), bevel=0.005))
    parts.append(make_box("LineS", (6.2, 0.12, 0.025), MATERIALS["AC_Coral"], col, location=(0, -2.4, 0.02), bevel=0.005))
    parts.append(make_box("LineW", (0.12, 4.9, 0.025), MATERIALS["AC_Coral"], col, location=(-3.1, 0, 0.02), bevel=0.005))
    parts.append(make_box("LineE", (0.12, 4.9, 0.025), MATERIALS["AC_Coral"], col, location=(3.1, 0, 0.02), bevel=0.005))
    # Two Beach Tennis rackets leaning by right post + yellow ball
    parts.append(make_cyl("RacketHandle1", 0.026, 0.24, MATERIALS["AC_MetalDark"], col, location=(3.65, -0.35, 0.14), rotation=(0.25, 0, 0), segs=8))
    parts.append(make_box("RacketHead1", (0.28, 0.04, 0.36), MATERIALS["AC_PinkBerry"], col, location=(3.65, -0.42, 0.42), rotation=(0.25, 0, 0), bevel=0.015))
    parts.append(make_cyl("RacketHandle2", 0.026, 0.24, MATERIALS["AC_MetalDark"], col, location=(3.88, -0.25, 0.14), rotation=(-0.22, 0.15, 0), segs=8))
    parts.append(make_box("RacketHead2", (0.28, 0.04, 0.36), MATERIALS["AC_MintTeal"], col, location=(3.90, -0.18, 0.42), rotation=(-0.22, 0.15, 0), bevel=0.015))
    parts.append(make_sphere("Ball", 0.075, MATERIALS["AC_SunnyYellow"], col, location=(1.4, -0.9, 0.08), u_segs=10, v_segs=8))
    return join_and_export(parts, "BeachTennisCourt", col)


# 6. FAMILY CAMPING TENT & PICNIC SET
def build_camping_tent(col):
    parts = [
        # Cozy dome/A-frame beach camping tent
        make_box("TentWallL", (0.10, 2.25, 1.75), MATERIALS["AC_SkyBlue"], col, location=(-0.62, 0, 0.78), rotation=(0, 0.56, 0), bevel=0.03),
        make_box("TentWallR", (0.10, 2.25, 1.75), MATERIALS["AC_SkyBlue"], col, location=(0.62, 0, 0.78), rotation=(0, -0.56, 0), bevel=0.03),
        make_box("TentBack", (2.15, 0.10, 1.35), MATERIALS["AC_MintSoft"], col, location=(0, 1.08, 0.68), bevel=0.03),
        make_box("TentFloor", (2.25, 2.30, 0.08), MATERIALS["AC_Cream"], col, location=(0, 0, 0.05), bevel=0.02),
        make_box("TentRidge", (0.16, 2.35, 0.16), MATERIALS["AC_SunnyYellow"], col, location=(0, 0, 1.54), bevel=0.04),
        # Family picnic blanket in front
        make_box("Blanket", (2.15, 1.45, 0.035), MATERIALS["AC_PinkSoft"], col, location=(0, -1.95, 0.025), bevel=0.01),
        make_box("BlanketStripe", (2.16, 0.35, 0.04), MATERIALS["AC_Cream"], col, location=(0, -1.95, 0.028), bevel=0.01),
        # Cooler box + cozy campfire stones
        make_box("Cooler", (0.62, 0.42, 0.44), MATERIALS["AC_Coral"], col, location=(1.42, -0.85, 0.24), bevel=0.04),
        make_box("CoolerLid", (0.66, 0.46, 0.10), MATERIALS["AC_Cream"], col, location=(1.42, -0.85, 0.48), bevel=0.03),
        make_sphere("FireGlow", 0.22, MATERIALS["AC_LanternGlow"], col, location=(-1.45, -1.55, 0.20), u_segs=10, v_segs=8),
    ]
    for a in range(6):
        ang = a * math.pi / 3.0
        parts.append(make_sphere(f"Stone_{a}", 0.13, MATERIALS["AC_StoneDark"], col, location=(-1.45 + math.cos(ang) * 0.34, -1.55 + math.sin(ang) * 0.34, 0.10), u_segs=8, v_segs=6))
    return join_and_export(parts, "CampingTent", col)


# 7. RESTAURANT KIOSK TABLE WITH FRENCH FRIES & DRINKS
def build_restaurant_kiosk_table(col):
    parts = [
        # Wooden round table + central post
        make_cyl("TableTop", 0.88, 0.09, MATERIALS["AC_WoodLight"], col, location=(0, 0, 0.78), segs=18),
        make_cyl("TableLeg", 0.11, 0.78, MATERIALS["AC_WoodDark"], col, location=(0, 0, 0.39), segs=12),
        make_cyl("TableBase", 0.44, 0.06, MATERIALS["AC_WoodWarm"], col, location=(0, 0, 0.04), segs=14),
        # Striped kiosk sun umbrella overhead
        make_cyl("UmbPole", 0.045, 2.35, MATERIALS["AC_WoodWarm"], col, location=(0, 0, 1.35), segs=10),
        make_cyl("UmbCanopy", 1.38, 0.38, MATERIALS["AC_Coral"], col, location=(0, 0, 2.42), segs=12, r_top=0.14),
        make_cyl("UmbRing", 1.40, 0.10, MATERIALS["AC_Cream"], col, location=(0, 0, 2.24), segs=12),
        # Red basket of golden French Fries (Batata Frita) right in the center!
        make_cyl("FryBasket", 0.22, 0.12, MATERIALS["AC_Coral"], col, location=(0.24, 0.12, 0.88), segs=12, r_top=0.25),
        # Two cold drink cups with straws + small sauce bowl
        make_cyl("Drink1", 0.08, 0.20, MATERIALS["AC_MintTeal"], col, location=(-0.32, 0.24, 0.92), segs=10),
        make_cyl("Drink2", 0.08, 0.20, MATERIALS["AC_Orange"], col, location=(-0.22, -0.34, 0.92), segs=10),
        make_cyl("Ketchup", 0.07, 0.06, MATERIALS["AC_Strawberry"], col, location=(0.28, -0.22, 0.85), segs=10),
    ]
    # Crispy golden fries sticking out of the basket
    for f in range(9):
        ang = f * 0.72
        rx = 0.24 + math.cos(ang) * (0.04 + (f % 3) * 0.045)
        ry = 0.12 + math.sin(ang) * (0.04 + (f % 2) * 0.045)
        parts.append(make_box(f"Fry_{f}", (0.036, 0.036, 0.22), MATERIALS["AC_SunnyYellow"], col, location=(rx, ry, 0.99), rotation=(0.18 * math.sin(ang), 0.18 * math.cos(ang), ang), bevel=0.005))
    # 4 cozy round stools around the table
    for s in range(4):
        ang = s * math.pi * 0.5 + 0.4
        sx, sy = math.cos(ang) * 1.15, math.sin(ang) * 1.15
        parts.append(make_cyl(f"StoolSeat_{s}", 0.24, 0.07, MATERIALS["AC_Cream"], col, location=(sx, sy, 0.48), segs=12))
        parts.append(make_cyl(f"StoolLeg_{s}", 0.07, 0.46, MATERIALS["AC_WoodWarm"], col, location=(sx, sy, 0.23), segs=10))
    return join_and_export(parts, "RestaurantKioskTable", col)


# 8. RESTAURANT FRIES KITCHEN ("BATATA & MAR")
def build_restaurant_fries_kitchen(col):
    parts = [
        make_box("BaseDeck", (4.1, 2.3, 0.16), MATERIALS["AC_WoodWarm"], col, location=(0, 0, 0.08), bevel=0.04),
        make_box("CounterFront", (3.6, 1.5, 1.05), MATERIALS["AC_Cream"], col, location=(0, -0.2, 0.65), bevel=0.06),
        make_box("CounterTop", (3.8, 1.7, 0.12), MATERIALS["AC_WoodLight"], col, location=(0, -0.2, 1.22), bevel=0.04),
        make_box("BackWall", (3.6, 0.22, 2.35), MATERIALS["AC_MintSoft"], col, location=(0, 0.88, 1.32), bevel=0.05),
        # Awning & golden fries sign
        make_box("RoofMain", (4.3, 2.5, 0.22), MATERIALS["AC_Coral"], col, location=(0, 0.0, 2.66), rotation=(0.14, 0, 0), bevel=0.06),
        make_box("SignBoard", (2.6, 0.14, 0.58), MATERIALS["AC_WoodDark"], col, location=(0, -0.95, 2.98), bevel=0.04),
        # Posts
        make_cyl("PostL", 0.09, 2.5, MATERIALS["AC_WoodDark"], col, location=(-1.72, -0.82, 1.35), segs=10),
        make_cyl("PostR", 0.09, 2.5, MATERIALS["AC_WoodDark"], col, location=(1.72, -0.82, 1.35), segs=10),
        # Fryer station & display trays of fries & burgers
        make_box("Fryer", (0.78, 0.62, 0.36), MATERIALS["AC_MetalSilver"], col, location=(-0.95, 0.15, 1.45), bevel=0.03),
        make_box("Tray1", (0.58, 0.42, 0.06), MATERIALS["AC_SunnyYellow"], col, location=(0.15, -0.35, 1.32), bevel=0.02),
        make_box("Tray2", (0.58, 0.42, 0.06), MATERIALS["AC_Orange"], col, location=(0.95, -0.35, 1.32), bevel=0.02),
    ]
    return join_and_export(parts, "RestaurantFriesKitchen", col)


# 9. MARKET STALL - DOCES & CUPCAKES
def build_stall_doces(col):
    parts = [
        make_box("Base", (2.5, 1.15, 0.95), MATERIALS["AC_PinkSoft"], col, location=(0, 0, 0.48), bevel=0.05),
        make_box("Top", (2.66, 1.28, 0.10), MATERIALS["AC_Cream"], col, location=(0, 0, 1.0), bevel=0.03),
        make_cyl("PoleL", 0.06, 2.15, MATERIALS["AC_WoodLight"], col, location=(-1.12, -0.45, 1.18), segs=10),
        make_cyl("PoleR", 0.06, 2.15, MATERIALS["AC_WoodLight"], col, location=(1.12, -0.45, 1.18), segs=10),
        make_cyl("PoleBL", 0.06, 2.25, MATERIALS["AC_WoodLight"], col, location=(-1.12, 0.45, 1.24), segs=10),
        make_cyl("PoleBR", 0.06, 2.25, MATERIALS["AC_WoodLight"], col, location=(1.12, 0.45, 1.24), segs=10),
        make_box("Awning", (2.8, 1.55, 0.16), MATERIALS["AC_PinkBerry"], col, location=(0, 0, 2.34), rotation=(0.18, 0, 0), bevel=0.05),
        make_box("AwningTrim", (2.82, 0.20, 0.18), MATERIALS["AC_Cream"], col, location=(0, -0.72, 2.18), bevel=0.03),
        # Cute candy jars & giant cupcake display
        make_cyl("Jar1", 0.16, 0.34, MATERIALS["AC_Glass"], col, location=(-0.72, -0.10, 1.22), segs=12),
        make_sphere("Candy1", 0.12, MATERIALS["AC_SunnyYellow"], col, location=(-0.72, -0.10, 1.20), u_segs=8, v_segs=6),
        make_cyl("CupcakeBase", 0.20, 0.18, MATERIALS["AC_Waffle"], col, location=(0.0, -0.05, 1.14), segs=12, r_top=0.24),
        make_sphere("Frosting", 0.24, MATERIALS["AC_Strawberry"], col, location=(0.0, -0.05, 1.32), scale=(1, 1, 0.78), u_segs=12, v_segs=8),
        make_cyl("Jar2", 0.16, 0.34, MATERIALS["AC_Glass"], col, location=(0.74, -0.10, 1.22), segs=12),
        make_sphere("Candy2", 0.12, MATERIALS["AC_MintTeal"], col, location=(0.74, -0.10, 1.20), u_segs=8, v_segs=6),
    ]
    return join_and_export(parts, "MarketStall_Doces", col)


# 10. MARKET STALL - SUCOS, AGUA & REFRI
def build_stall_sucos_refri(col):
    parts = [
        make_box("Base", (2.5, 1.15, 0.95), MATERIALS["AC_SkyBlue"], col, location=(0, 0, 0.48), bevel=0.05),
        make_box("Top", (2.66, 1.28, 0.10), MATERIALS["AC_Cream"], col, location=(0, 0, 1.0), bevel=0.03),
        make_cyl("PoleL", 0.06, 2.15, MATERIALS["AC_WoodWarm"], col, location=(-1.12, -0.45, 1.18), segs=10),
        make_cyl("PoleR", 0.06, 2.15, MATERIALS["AC_WoodWarm"], col, location=(1.12, -0.45, 1.18), segs=10),
        make_cyl("PoleBL", 0.06, 2.25, MATERIALS["AC_WoodWarm"], col, location=(-1.12, 0.45, 1.24), segs=10),
        make_cyl("PoleBR", 0.06, 2.25, MATERIALS["AC_WoodWarm"], col, location=(1.12, 0.45, 1.24), segs=10),
        make_box("Awning", (2.8, 1.55, 0.16), MATERIALS["AC_Orange"], col, location=(0, 0, 2.34), rotation=(0.18, 0, 0), bevel=0.05),
        make_box("AwningTrim", (2.82, 0.20, 0.18), MATERIALS["AC_SunnyYellow"], col, location=(0, -0.72, 2.18), bevel=0.03),
        # Juice dispenser, soda cans & water bottles
        make_cyl("Dispenser", 0.22, 0.42, MATERIALS["AC_SunnyYellow"], col, location=(-0.65, -0.05, 1.26), segs=12),
        make_cyl("WaterBottle", 0.10, 0.34, MATERIALS["AC_Glass"], col, location=(0.05, -0.10, 1.22), segs=10),
        make_cyl("SodaCan1", 0.08, 0.22, MATERIALS["AC_Coral"], col, location=(0.55, -0.10, 1.16), segs=10),
        make_cyl("SodaCan2", 0.08, 0.22, MATERIALS["AC_BushGreen"], col, location=(0.76, -0.10, 1.16), segs=10),
    ]
    return join_and_export(parts, "MarketStall_SucosRefri", col)


# 11. GARDEN PLANTER BED (HORTA / PLANTACAO)
def build_garden_planter_bed(col):
    parts = [
        make_box("WoodFrame", (2.6, 1.35, 0.36), MATERIALS["AC_WoodWarm"], col, location=(0, 0, 0.18), bevel=0.04),
        make_box("Soil", (2.36, 1.12, 0.30), MATERIALS["AC_Chocolate"], col, location=(0, 0, 0.24), bevel=0.02),
        # 4 lush herb/berry tufts (Basil, Mint, Rosemary, Strawberry)
        make_sphere("Herb1", 0.24, MATERIALS["AC_BushGreen"], col, location=(-0.82, 0.0, 0.48), scale=(1.1, 1.0, 0.9), u_segs=10, v_segs=8),
        make_sphere("Herb2", 0.24, MATERIALS["AC_MintTeal"], col, location=(-0.28, 0.05, 0.50), scale=(1.0, 1.1, 0.95), u_segs=10, v_segs=8),
        make_sphere("Herb3", 0.24, MATERIALS["AC_PalmLeaf"], col, location=(0.28, -0.04, 0.52), scale=(0.95, 1.0, 1.15), u_segs=10, v_segs=8),
        make_sphere("Herb4", 0.24, MATERIALS["AC_BushLight"], col, location=(0.82, 0.0, 0.48), scale=(1.1, 1.0, 0.85), u_segs=10, v_segs=8),
        make_sphere("Berry1", 0.075, MATERIALS["AC_Strawberry"], col, location=(0.88, -0.12, 0.58), u_segs=8, v_segs=6),
        make_sphere("Berry2", 0.075, MATERIALS["AC_Strawberry"], col, location=(0.72, 0.10, 0.56), u_segs=8, v_segs=6),
        # Cute watering can beside the planter
        make_cyl("CanBody", 0.15, 0.26, MATERIALS["AC_SkyBlue"], col, location=(1.55, -0.25, 0.15), segs=12),
        make_cyl("CanSpout", 0.035, 0.24, MATERIALS["AC_SkyBlue"], col, location=(1.36, -0.25, 0.22), rotation=(0, -0.95, 0), segs=8),
    ]
    return join_and_export(parts, "GardenPlanterBed", col)


# 12. FISHING PIER DOCK
def build_fishing_pier_dock(col):
    parts = []
    for i in range(8):
        parts.append(make_box(f"Plank_{i}", (2.4, 0.42, 0.10), MATERIALS["AC_WoodWarm"] if i % 2 == 0 else MATERIALS["AC_WoodLight"], col, location=(0, i * 0.48, 0.42), bevel=0.02))
    for r in (0, 3, 7):
        for sx in (-1.05, 1.05):
            parts.append(make_cyl(f"Pile_{r}_{sx}", 0.12, 1.4, MATERIALS["AC_WoodDark"], col, location=(sx, r * 0.48, 0.25), segs=10))
    # Fishing rod + wooden bucket with fish
    parts.append(make_cyl("Rod", 0.024, 2.25, MATERIALS["AC_WoodLight"], col, location=(0.85, 3.25, 1.35), rotation=(-0.65, 0, 0), segs=8))
    parts.append(make_cyl("Bucket", 0.22, 0.34, MATERIALS["AC_SkyBlue"], col, location=(-0.68, 2.85, 0.64), segs=12, r_top=0.26))
    parts.append(make_sphere("FishInBucket", 0.12, MATERIALS["AC_Orange"], col, location=(-0.68, 2.85, 0.80), scale=(0.7, 1.5, 0.7), u_segs=8, v_segs=6))
    return join_and_export(parts, "FishingPierDock", col)


# 13. VILLAGE NOTICE BOARD (MURAL DE AVISOS)
def build_village_notice_board(col):
    parts = [
        make_cyl("PostL", 0.08, 2.1, MATERIALS["AC_WoodDark"], col, location=(-0.95, 0, 1.05), segs=10),
        make_cyl("PostR", 0.08, 2.1, MATERIALS["AC_WoodDark"], col, location=(0.95, 0, 1.05), segs=10),
        make_box("Frame", (2.15, 0.14, 1.32), MATERIALS["AC_WoodWarm"], col, location=(0, 0, 1.56), bevel=0.04),
        make_box("Cork", (1.92, 0.16, 1.10), MATERIALS["AC_Waffle"], col, location=(0, -0.01, 1.56), bevel=0.02),
        make_box("Roof", (2.42, 0.65, 0.14), MATERIALS["AC_MintTeal"], col, location=(0, 0, 2.28), rotation=(0.22, 0, 0), bevel=0.04),
        # Pinned colorful villager quest notes!
        make_box("Note1", (0.42, 0.04, 0.46), MATERIALS["AC_Cream"], col, location=(-0.55, -0.10, 1.70), rotation=(0, 0.08, 0), bevel=0.005),
        make_box("Note2", (0.42, 0.04, 0.42), MATERIALS["AC_PinkSoft"], col, location=(0.02, -0.10, 1.66), rotation=(0, -0.06, 0), bevel=0.005),
        make_box("Note3", (0.44, 0.04, 0.44), MATERIALS["AC_SunnyYellow"], col, location=(0.56, -0.10, 1.68), rotation=(0, 0.05, 0), bevel=0.005),
        make_box("Note4", (0.46, 0.04, 0.34), MATERIALS["AC_MintSoft"], col, location=(-0.22, -0.10, 1.24), bevel=0.005),
    ]
    return join_and_export(parts, "VillageNoticeBoard", col)


# 14. WARDROBE HAT & SUNGLASSES PROP
def build_wardrobe_hat_prop(col):
    parts = [
        make_cyl("Brim", 0.42, 0.045, MATERIALS["AC_StrawHat"], col, location=(0, 0, 0.02), segs=18),
        make_sphere("Crown", 0.24, MATERIALS["AC_StrawHat"], col, location=(0, 0, 0.14), scale=(1.0, 1.0, 0.72), u_segs=14, v_segs=10),
        make_cyl("Ribbon", 0.245, 0.06, MATERIALS["AC_Coral"], col, location=(0, 0, 0.08), segs=16),
        # Stylish dark coastal sunglasses hanging neatly below the brim
        make_box("LensL", (0.14, 0.04, 0.09), MATERIALS["AC_MetalDark"], col, location=(-0.09, -0.22, -0.16), bevel=0.015),
        make_box("LensR", (0.14, 0.04, 0.09), MATERIALS["AC_MetalDark"], col, location=(0.09, -0.22, -0.16), bevel=0.015),
        make_box("Bridge", (0.08, 0.03, 0.025), MATERIALS["AC_Gold"], col, location=(0, -0.22, -0.14), bevel=0.005),
    ]
    return join_and_export(parts, "WardrobeHatProp", col)


# 15. PET BANDANA PROP (FOR PIPOCA)
def build_pet_bandana_prop(col):
    parts = [
        make_cyl("Collar", 0.18, 0.06, MATERIALS["AC_Coral"], col, location=(0, 0, 0.05), segs=14),
        make_box("Triangle", (0.20, 0.05, 0.18), MATERIALS["AC_PinkBerry"], col, location=(0, -0.16, -0.02), rotation=(0.35, 0, 0.78), bevel=0.015),
        make_sphere("Bell", 0.042, MATERIALS["AC_Gold"], col, location=(0, -0.19, 0.02), u_segs=8, v_segs=6),
    ]
    return join_and_export(parts, "PetBandanaProp", col)


# 16. TROPICAL FISH PROP
def build_fish_prop(col):
    parts = [
        make_sphere("Body", 0.18, MATERIALS["AC_Orange"], col, location=(0, 0, 0), scale=(0.62, 1.45, 0.85), u_segs=12, v_segs=8),
        make_box("Stripe", (0.23, 0.08, 0.28), MATERIALS["AC_Cream"], col, location=(0, 0.02, 0), bevel=0.02),
        make_box("Tail", (0.05, 0.22, 0.24), MATERIALS["AC_SunnyYellow"], col, location=(0, -0.28, 0), rotation=(0, 0, 0), bevel=0.015),
        make_sphere("EyeL", 0.032, MATERIALS["AC_MetalDark"], col, location=(-0.09, 0.16, 0.04), u_segs=8, v_segs=6),
        make_sphere("EyeR", 0.032, MATERIALS["AC_MetalDark"], col, location=(0.09, 0.16, 0.04), u_segs=8, v_segs=6),
    ]
    return join_and_export(parts, "FishProp", col)


def render_preview(col):
    world = bpy.data.worlds.new("MegaLifePreviewWorld")
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
    sun_d.color = hex_to_rgba("#FFF0D0")[:3]
    sun_o = bpy.data.objects.new("Sun", sun_d)
    sun_o.rotation_euler = (math.radians(44), math.radians(18), math.radians(-26))
    link_obj(sun_o, col)

    cam_d = bpy.data.cameras.new("Cam")
    cam_d.lens = 34.0
    cam_o = bpy.data.objects.new("Cam", cam_d)
    cam_o.location = (0.0, -18.5, 8.2)
    direction = Vector((0.0, 0.5, 1.0)) - cam_o.location
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
    col = ensure_collection("BaiaDoAvesso_MegaLife")

    items = [
        (build_skateboard(col), (-6.5, -3.5, 0)),
        (build_scooter(col), (-5.0, -3.5, 0)),
        (build_roller_skates(col), (-3.6, -3.5, 0)),
        (build_hawaiian_canoe(col), (-1.2, -3.5, 0)),
        (build_wardrobe_hat_prop(col), (1.4, -3.5, 0.4)),
        (build_pet_bandana_prop(col), (2.6, -3.5, 0.3)),
        (build_fish_prop(col), (3.8, -3.5, 0.4)),
        (build_restaurant_kiosk_table(col), (-5.5, 0.5, 0)),
        (build_garden_planter_bed(col), (-1.8, 0.5, 0)),
        (build_village_notice_board(col), (1.8, 0.5, 0)),
        (build_camping_tent(col), (5.5, 0.5, 0)),
        (build_stall_doces(col), (-6.0, 4.5, 0)),
        (build_restaurant_fries_kitchen(col), (-1.6, 4.8, 0)),
        (build_stall_sucos_refri(col), (2.8, 4.5, 0)),
        (build_fishing_pier_dock(col), (6.8, 3.8, 0)),
        (build_beach_tennis_court(col), (0.0, 9.5, 0)),
    ]
    for obj, pos in items:
        obj.location = pos

    bpy.ops.wm.save_as_mainfile(filepath=BLEND_OUT)
    render_preview(col)
    print("[Mega Life 16 Blender Assets Complete]")


if __name__ == "__main__":
    main()
