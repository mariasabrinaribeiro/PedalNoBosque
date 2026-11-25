"""
Orla das Fadas — Authentic Coastal Establishments, Tables & Pet Water Spout (Blender 5.3)
Addresses Art & Readability Priorities:
1. Low, open, recognizable counters (1.0m height) replacing bulky shiny brown volumes.
2. Signature products visible from afar:
   - RestaurantFriesKitchen (Batata & Mar): fries baskets, grilled fish plates & lemon slices
   - MarketStall_Comida (Pastel & Caldo): glass vitrine with metal trays of golden pasteis + juice pitchers
   - MarketStall_Doces (Doces): colorful glass candy jars, cupcakes & brigadeiro trays
   - MarketStall_Frutas, Sushi, Artesanato, Plantas, Roupas, SucosRefri: distinct themed wares
3. Lightweight canopies exported as a separate sibling mesh (*_CanopyFade) so Unity's
   smooth occlusion fader fades ONLY the roof/awning when crossing the camera ray — never the counter!
4. Built-in uniform dark matte fascia signboards at the exact same height (y = 2.42m) on every stall
   for consistent typography & high contrast against bright ocean/sky.
5. PetWaterBowl upgraded to a full coastal Water Spout Station (wood post + metallic faucet + paw badge + turquoise bowl).
6. RestaurantKioskTable upgraded with distinct legs, stools & high lightweight umbrella (Table_CanopyFade).
"""

import bpy
import bmesh
import math
import os
from mathutils import Vector

SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
MODELS_DIR = os.path.normpath(os.path.join(SCRIPT_DIR, "..", "Models"))
BLEND_OUT = os.path.join(SCRIPT_DIR, "OrlaDasFadas_Establishments.blend")
os.makedirs(MODELS_DIR, exist_ok=True)


def clear_scene():
    bpy.ops.wm.read_factory_settings(use_empty=True)


def hex_rgba(h):
    h = h.lstrip("#")
    return (int(h[0:2], 16) / 255.0, int(h[2:4], 16) / 255.0, int(h[4:6], 16) / 255.0, 1.0)


MATS = {}


def make_mat(name, hx, rough=0.82):
    m = bpy.data.materials.new(name=name)
    bsdf = m.node_tree.nodes.get("Principled BSDF") if m.node_tree else None
    if bsdf:
        bsdf.inputs["Base Color"].default_value = hex_rgba(hx)
        bsdf.inputs["Roughness"].default_value = rough
    MATS[name] = m
    return m


def init_mats():
    MATS.clear()
    for k, hx in [
        ("AC_WoodLight", "#C9996B"),
        ("AC_WoodWarm", "#A6764E"),
        ("AC_WoodDark", "#6E4A32"),
        ("AC_Cream", "#FAF2E4"),
        ("AC_StoneWhite", "#F2E9D6"),
        ("AC_StoneWarm", "#DFD0B6"),
        ("AC_MintTeal", "#2AC6CC"),
        ("AC_MintSoft", "#8EDFE2"),
        ("AC_PinkBerry", "#EC6FA4"),
        ("AC_PinkSoft", "#F4AECB"),
        ("AC_Lavender", "#C68CE0"),
        ("AC_Coral", "#D89886"),
        ("AC_Terracotta", "#C98868"),
        ("AC_SkyBlue", "#68C4D6"),
        ("AC_SunnyYellow", "#F0D284"),
        ("AC_Gold", "#E6BE68"),
        ("AC_Orange", "#EA9B52"),
        ("AC_Strawberry", "#EC6FA4"),
        ("AC_Chocolate", "#6E4632"),
        ("AC_Vanilla", "#FBF0D4"),
        ("AC_Waffle", "#DFAA68"),
        ("AC_BushGreen", "#5D997A"),
        ("AC_BushLight", "#7EB496"),
        ("AC_FlowerPink", "#EC6FA4"),
        ("AC_FlowerYellow", "#EED286"),
        ("AC_OceanShallow", "#2AC6CC"),
        ("AC_Glass", "#D4F2F8"),
        ("AC_MetalSilver", "#CFD7E0"),
        ("AC_MetalDark", "#333A46"),
        ("AC_StrawHat", "#EED2A4"),
        ("AC_Denim", "#4694A8"),
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


def add_sphere(name, r, mat, col, loc=(0, 0, 0), scale=(1, 1, 1), u=12, v=8):
    mesh = bpy.data.meshes.new(name)
    bm = bmesh.new()
    bmesh.ops.create_uvsphere(bm, u_segments=u, v_segments=v, radius=r)
    for f in bm.faces:
        f.smooth = True
    bm.to_mesh(mesh)
    bm.free()
    obj = bpy.data.objects.new(name, mesh)
    obj.location = loc
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


def export_establishment_fbx(objs, filename):
    """Exports upright Unity-ready FBX with exact sibling sub-meshes (Base solid + CanopyFade)."""
    import time
    bpy.ops.object.select_all(action="DESELECT")
    for o in objs:
        o.select_set(True)
    bpy.context.view_layer.objects.active = objs[0]
    out_path = os.path.join(MODELS_DIR, f"{filename}.fbx")
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
                axis_forward="-Z",
                axis_up="Y",
                add_leaf_bones=False,
                bake_anim=False,
            )
            break
        except RuntimeError:
            if attempt == 3:
                raise
            time.sleep(0.35)
    print(f"[FBX Establishment] {out_path}")


# =========================================================================
# SHARED LIGHTWEIGHT STALL SHELL (Low open counter + integrated fascia board + separate CanopyFade!)
# Front faces -Y in Blender (= -Z in Unity, facing the promenade/camera!)
# =========================================================================
def build_stall_shell(col, width, depth, body_mat, stripe_a, stripe_b, extra_counter_fn):
    hw = width * 0.5
    hd = depth * 0.5

    # 1. SOLID BASE: Low inviting counter (height 0.94m) + slim corner posts + integrated high-contrast fascia signboard (z=2.42m)!
    base_parts = [
        # Ground plinth & low front counter with recessed kickplate
        add_box("BasePlinth", (width - 0.08, depth - 0.08, 0.10), MATS["AC_StoneWarm"], col, loc=(0, 0, 0.05), bevel=0.015),
        add_box("CounterBody", (width - 0.14, depth - 0.18, 0.78), body_mat, col, loc=(0, 0.04, 0.49), bevel=0.025),
        # Decorative slatted front apron so the service front is unmistakable!
        add_box("FrontPanel", (width - 0.34, 0.06, 0.56), MATS["AC_Cream"], col, loc=(0, -hd + 0.09, 0.48), bevel=0.015),
        add_box("FrontAccentBar", (width - 0.26, 0.07, 0.09), stripe_a, col, loc=(0, -hd + 0.08, 0.77), bevel=0.012),
        # Wide clean matte counter top at z=0.92m
        add_box("CounterTop", (width + 0.08, depth + 0.06, 0.09), MATS["AC_StoneWhite"], col, loc=(0, 0.0, 0.925), bevel=0.02),
        # Open back shelf for depth & establishment feel (NOT a closed opaque wall!)
        add_box("BackLowWall", (width - 0.20, 0.12, 0.62), MATS["AC_WoodLight"], col, loc=(0, hd - 0.10, 1.24), bevel=0.015),
        add_box("BackShelf", (width - 0.30, 0.26, 0.05), MATS["AC_Cream"], col, loc=(0, hd - 0.18, 1.56), bevel=0.01),
        # 4 Slim matte wooden corner posts
        add_cyl("PostFL", 0.046, 2.28, MATS["AC_WoodWarm"], col, loc=(-hw + 0.10, -hd + 0.12, 1.14), segs=10),
        add_cyl("PostFR", 0.046, 2.28, MATS["AC_WoodWarm"], col, loc=( hw - 0.10, -hd + 0.12, 1.14), segs=10),
        add_cyl("PostBL", 0.046, 2.48, MATS["AC_WoodWarm"], col, loc=(-hw + 0.10,  hd - 0.12, 1.24), segs=10),
        add_cyl("PostBR", 0.046, 2.48, MATS["AC_WoodWarm"], col, loc=( hw - 0.10,  hd - 0.12, 1.24), segs=10),
        # UNIFORM SOLID CREAM SIGNBOARD framed in warm wood at z = 2.42m (for crisp dark espresso text!)
        add_box("FasciaFrame", (width - 0.12, 0.10, 0.44), MATS["AC_WoodWarm"], col, loc=(0, -hd + 0.02, 2.42), bevel=0.02),
        add_box("FasciaBoard", (width - 0.24, 0.08, 0.34), MATS["AC_Cream"], col, loc=(0, -hd - 0.015, 2.42), bevel=0.01),
    ]

    if extra_counter_fn is not None:
        extra_counter_fn(base_parts, col, width, depth)

    base_obj = join_around_pivot(base_parts, "Estab_Base", (0, 0, 0), col)

    # 2. LIGHTWEIGHT CANOPY (Separate sibling mesh *_CanopyFade so ONLY the awning fades when blocking the protagonist!)
    canopy_parts = []
    stripes = 7
    sw = (width + 0.28) / stripes
    for i in range(stripes):
        sx = -(width + 0.28) * 0.5 + (i + 0.5) * sw
        sm = stripe_a if i % 2 == 0 else stripe_b
        canopy_parts.append(add_box(f"Awning_{i}", (sw - 0.015, depth + 0.42, 0.075), sm, col,
                                    loc=(sx, -0.04, 2.54), rot=(math.radians(11), 0, 0), bevel=0.018))
        canopy_parts.append(add_sphere(f"Valance_{i}", sw * 0.44, sm, col,
                                       loc=(sx, -hd - 0.22, 2.38), scale=(1.0, 0.42, 0.72), u=10, v=6))
    canopy_obj = join_around_pivot(canopy_parts, "Estab_CanopyFade", (0, 0, 2.54), col)
    return [base_obj, canopy_obj]


# =========================================================================
# 1. BATATA & MAR (Open Coastal Fries & Seafood Counter — replacing shiny brown box!)
# =========================================================================
def build_batata_e_mar(col):
    def props(p, c, w, d):
        # Low front display vitrine with golden fries baskets & grilled fish plates!
        p.append(add_box("VitrineGlassF", (1.85, 0.04, 0.34), MATS["AC_Glass"], c, loc=(-0.55, -0.58, 1.15), bevel=0.008))
        p.append(add_box("VitrineGlassTop", (1.85, 0.58, 0.035), MATS["AC_Glass"], c, loc=(-0.55, -0.31, 1.32), bevel=0.008))
        # 3 Crispy golden fries cones/baskets
        for fx in (-1.15, -0.65, -0.15):
            p.append(add_cyl(f"FriesCup_{fx}", 0.09, 0.16, MATS["AC_Coral"], c, loc=(fx, -0.34, 1.05), r_top=0.115))
            for k in range(5):
                ang = k * 1.25
                p.append(add_box(f"Fry_{fx}_{k}", (0.03, 0.03, 0.14), MATS["AC_SunnyYellow"], c,
                                 loc=(fx + math.cos(ang) * 0.04, -0.34 + math.sin(ang) * 0.04, 1.16),
                                 rot=(0.15, 0.15 * (k - 2), 0), bevel=0.005))
        # Grilled fish & lemon platter on right counter side
        p.append(add_cyl("FishPlate", 0.24, 0.035, MATS["AC_StoneWhite"], c, loc=(0.82, -0.25, 0.99)))
        p.append(add_sphere("FishBody", 0.15, MATS["AC_Gold"], c, loc=(0.82, -0.25, 1.05), scale=(1.35, 0.72, 0.45)))
        p.append(add_sphere("LemonSlice", 0.055, MATS["AC_SunnyYellow"], c, loc=(0.98, -0.18, 1.04), scale=(1.0, 1.0, 0.35)))
        # Condiment bottles on open back shelf
        for bi, (bx, bm) in enumerate([(-0.9, "AC_Coral"), (-0.65, "AC_SunnyYellow"), (-0.4, "AC_BushGreen")]):
            p.append(add_cyl(f"Bottle_{bi}", 0.045, 0.20, MATS[bm], c, loc=(bx, 0.44, 1.69), r_top=0.025))
        # Open side pickup shelf / welcoming entrance frame
        p.append(add_box("MenuChalk", (0.48, 0.05, 0.62), MATS["AC_MetalDark"], c, loc=(1.55, -0.45, 0.56), rot=(0, 0, -0.15), bevel=0.015))
    objs = build_stall_shell(col, 3.6, 1.45, MATS["AC_WoodLight"], MATS["AC_MintTeal"], MATS["AC_Cream"], props)
    export_establishment_fbx(objs, "RestaurantFriesKitchen")


# =========================================================================
# 2. PASTEL & CALDO (Glass vitrine with metal trays of golden pasteis + juice pitchers!)
# =========================================================================
def build_stall_pastel(col):
    def props(p, c, w, d):
        # Clear front pastry vitrine
        p.append(add_box("PastelGlassF", (1.35, 0.035, 0.36), MATS["AC_Glass"], c, loc=(-0.38, -0.48, 1.15), bevel=0.008))
        p.append(add_box("PastelGlassTop", (1.35, 0.52, 0.03), MATS["AC_Glass"], c, loc=(-0.38, -0.24, 1.33), bevel=0.008))
        # 2 Metallic trays filled with golden puffy rectangular pasteis!
        for tx in (-0.72, -0.04):
            p.append(add_box(f"Tray_{tx}", (0.56, 0.38, 0.03), MATS["AC_MetalSilver"], c, loc=(tx, -0.25, 0.99), bevel=0.008))
            for px in (-0.16, 0.0, 0.16):
                p.append(add_box(f"Pastel_{tx}_{px}", (0.14, 0.24, 0.055), MATS["AC_Waffle"], c,
                                 loc=(tx + px, -0.25, 1.045), rot=(0.18, 0, 0.08), bevel=0.018))
        # Sugarcane juice pitchers & cups on right side
        for jx, jm in [(0.58, "AC_BushLight"), (0.86, "AC_SunnyYellow")]:
            p.append(add_cyl(f"Pitcher_{jx}", 0.095, 0.28, MATS[jm], c, loc=(jx, -0.18, 1.11), r_top=0.08))
            p.append(add_cyl(f"Cup_{jx}", 0.048, 0.13, MATS["AC_Cream"], c, loc=(jx + 0.08, -0.38, 1.035), r_top=0.056))
    objs = build_stall_shell(col, 2.55, 1.18, MATS["AC_WoodLight"], MATS["AC_SunnyYellow"], MATS["AC_Cream"], props)
    export_establishment_fbx(objs, "MarketStall_Comida")


# =========================================================================
# 3. DOCES (Colorful glass candy jars, brigadeiros & cupcakes on 2 front tiers!)
# =========================================================================
def build_stall_doces(col):
    def props(p, c, w, d):
        # 2-tier pastel riser shelf
        p.append(add_box("CandyRiser", (1.95, 0.34, 0.16), MATS["AC_PinkSoft"], c, loc=(0, 0.04, 1.05), bevel=0.02))
        # 5 Colorful glass candy jars visible from afar!
        jar_colors = ["AC_PinkBerry", "AC_MintTeal", "AC_SunnyYellow", "AC_Lavender", "AC_Orange"]
        for i, jc in enumerate(jar_colors):
            jx = -0.80 + i * 0.40
            p.append(add_cyl(f"JarBody_{i}", 0.115, 0.28, MATS[jc], c, loc=(jx, 0.04, 1.27), segs=12))
            p.append(add_cyl(f"JarLid_{i}", 0.10, 0.045, MATS["AC_Cream"], c, loc=(jx, 0.04, 1.43), segs=12))
        # Front cupcake & brigadeiro cake stands
        for cx, cm in [(-0.62, "AC_Chocolate"), (0.0, "AC_Strawberry"), (0.62, "AC_Vanilla")]:
            p.append(add_cyl(f"Stand_{cx}", 0.18, 0.05, MATS["AC_StoneWhite"], c, loc=(cx, -0.32, 1.00)))
            for k in range(3):
                ang = k * 2.1
                p.append(add_sphere(f"Treat_{cx}_{k}", 0.068, MATS[cm], c,
                                    loc=(cx + math.cos(ang) * 0.075, -0.32 + math.sin(ang) * 0.075, 1.08)))
    objs = build_stall_shell(col, 2.55, 1.18, MATS["AC_Cream"], MATS["AC_PinkBerry"], MATS["AC_Cream"], props)
    export_establishment_fbx(objs, "MarketStall_Doces")


# =========================================================================
# 4. FRUTAS & SUCOS / SUSHI / ARTESANATO / PLANTAS / ROUPAS / SUCOS
# =========================================================================
def build_stall_frutas(col):
    def props(p, c, w, d):
        crates = [(-0.76, "AC_Orange"), (-0.18, "AC_Strawberry"), (0.40, "AC_SunnyYellow")]
        for cx, fm in crates:
            p.append(add_box(f"FCrate_{cx}", (0.48, 0.42, 0.14), MATS["AC_WoodWarm"], c, loc=(cx, -0.22, 1.04), rot=(0.20, 0, 0), bevel=0.015))
            for k in range(4):
                p.append(add_sphere(f"Fruit_{cx}_{k}", 0.085, MATS[fm], c, loc=(cx + (-0.10 + (k % 2) * 0.20), -0.24 + (k // 2) * 0.16, 1.14)))
        p.append(add_cyl("JuiceJug", 0.11, 0.30, MATS["AC_MintTeal"], c, loc=(0.92, -0.18, 1.12), r_top=0.09))
    objs = build_stall_shell(col, 2.55, 1.18, MATS["AC_WoodLight"], MATS["AC_Orange"], MATS["AC_Cream"], props)
    export_establishment_fbx(objs, "MarketStall_Frutas")


def build_stall_sushi(col):
    def props(p, c, w, d):
        p.append(add_box("SushiGlass", (1.65, 0.035, 0.32), MATS["AC_Glass"], c, loc=(0, -0.46, 1.13), bevel=0.008))
        for bx in (-0.52, 0.42):
            p.append(add_box(f"Boat_{bx}", (0.68, 0.32, 0.06), MATS["AC_WoodDark"], c, loc=(bx, -0.22, 1.00), bevel=0.02))
            for sx in (-0.20, 0.0, 0.20):
                p.append(add_box(f"Rice_{bx}_{sx}", (0.12, 0.10, 0.06), MATS["AC_Cream"], c, loc=(bx + sx, -0.22, 1.06), bevel=0.01))
                p.append(add_box(f"Salmon_{bx}_{sx}", (0.13, 0.11, 0.03), MATS["AC_Coral"], c, loc=(bx + sx, -0.22, 1.10), bevel=0.008))
    objs = build_stall_shell(col, 2.55, 1.18, MATS["AC_WoodWarm"], MATS["AC_Coral"], MATS["AC_Cream"], props)
    export_establishment_fbx(objs, "MarketStall_Sushi")


def build_stall_artesanato(col):
    def props(p, c, w, d):
        for cx, cm in [(-0.78, "AC_Terracotta"), (-0.28, "AC_MintSoft"), (0.24, "AC_StrawHat"), (0.76, "AC_Lavender")]:
            p.append(add_cyl(f"Pot_{cx}", 0.13, 0.24, MATS[cm], c, loc=(cx, -0.22, 1.09), r_top=0.10))
        p.append(add_cyl("HatDisp", 0.22, 0.05, MATS["AC_StrawHat"], c, loc=(0.0, 0.14, 1.62)))
    objs = build_stall_shell(col, 2.55, 1.18, MATS["AC_WoodLight"], MATS["AC_Lavender"], MATS["AC_Cream"], props)
    export_establishment_fbx(objs, "MarketStall_Artesanato")


def build_stall_plantas(col):
    def props(p, c, w, d):
        for i, px in enumerate([-0.82, -0.28, 0.28, 0.82]):
            p.append(add_cyl(f"Pots_{i}", 0.12, 0.18, MATS["AC_Terracotta"], c, loc=(px, -0.22, 1.06), r_top=0.14))
            p.append(add_sphere(f"Leaf_{i}", 0.15, MATS["AC_BushGreen" if i % 2 == 0 else "AC_BushLight"], c, loc=(px, -0.22, 1.24)))
            p.append(add_sphere(f"Bloom_{i}", 0.06, MATS["AC_FlowerPink" if i % 2 == 0 else "AC_FlowerYellow"], c, loc=(px, -0.26, 1.36)))
    objs = build_stall_shell(col, 2.55, 1.18, MATS["AC_WoodLight"], MATS["AC_BushGreen"], MATS["AC_Cream"], props)
    export_establishment_fbx(objs, "MarketStall_Plantas")


def build_stall_roupas(col):
    def props(p, c, w, d):
        for i, (rx, rm) in enumerate([(-0.72, "AC_SkyBlue"), (-0.20, "AC_PinkSoft"), (0.32, "AC_MintTeal"), (0.82, "AC_SunnyYellow")]):
            p.append(add_box(f"Fold_{i}", (0.36, 0.28, 0.14), MATS[rm], c, loc=(rx, -0.22, 1.04), bevel=0.025))
    objs = build_stall_shell(col, 2.55, 1.18, MATS["AC_Cream"], MATS["AC_SkyBlue"], MATS["AC_Cream"], props)
    export_establishment_fbx(objs, "MarketStall_Roupas")


def build_stall_sucos(col):
    def props(p, c, w, d):
        for i, (jx, jm) in enumerate([(-0.68, "AC_SunnyYellow"), (0.0, "AC_PinkBerry"), (0.68, "AC_MintTeal")]):
            p.append(add_cyl(f"Disp_{i}", 0.14, 0.34, MATS[jm], c, loc=(jx, -0.18, 1.14), segs=14))
            p.append(add_cyl(f"DispCap_{i}", 0.145, 0.045, MATS["AC_WoodWarm"], c, loc=(jx, -0.18, 1.33), segs=14))
    objs = build_stall_shell(col, 2.55, 1.18, MATS["AC_WoodLight"], MATS["AC_MintTeal"], MATS["AC_Cream"], props)
    export_establishment_fbx(objs, "MarketStall_SucosRefri")


# =========================================================================
# 5. PET WATER SPOUT STATION (Bica + Torneira + Tigela Turquesa — clearly visible from afar!)
# =========================================================================
def build_pet_water_station(col):
    base_parts = [
        # Wide ground stone splash pad anchoring the station visually along the promenade border
        add_cyl("SplashPad", 0.52, 0.055, MATS["AC_StoneWarm"], col, loc=(0, 0.04, 0.028), segs=18),
        # Taller coastal wooden water post (bica) behind the bowl — unmistakably readable from afar!
        add_box("SpoutPost", (0.24, 0.20, 1.04), MATS["AC_WoodWarm"], col, loc=(0, 0.28, 0.54), bevel=0.028),
        add_box("SpoutCap", (0.30, 0.26, 0.07), MATS["AC_Cream"], col, loc=(0, 0.28, 1.08), bevel=0.018),
        # High-contrast turquoise & cream paw badge facing the camera (-Y in Blender = -Z in Unity)
        add_sphere("PawBadgeBg", 0.115, MATS["AC_MintTeal"], col, loc=(0, 0.17, 0.84), scale=(1.0, 0.32, 1.0)),
        add_sphere("PawPadMain", 0.052, MATS["AC_Cream"], col, loc=(0, 0.145, 0.82), scale=(1.1, 0.35, 0.9)),
        add_sphere("PawToe1", 0.022, MATS["AC_Cream"], col, loc=(-0.046, 0.145, 0.885), scale=(1.0, 0.35, 1.0)),
        add_sphere("PawToe2", 0.022, MATS["AC_Cream"], col, loc=( 0.000, 0.145, 0.902), scale=(1.0, 0.35, 1.0)),
        add_sphere("PawToe3", 0.022, MATS["AC_Cream"], col, loc=( 0.046, 0.145, 0.885), scale=(1.0, 0.35, 1.0)),
        # Prominent silver metallic faucet arching over the bowl with berry-pink valve handle!
        add_cyl("FaucetPipe", 0.032, 0.30, MATS["AC_MetalSilver"], col, loc=(0, 0.10, 0.64), rot=(math.pi * 0.5, 0, 0), segs=12),
        add_cyl("FaucetNozzle", 0.028, 0.13, MATS["AC_MetalSilver"], col, loc=(0, -0.04, 0.57), segs=12),
        add_cyl("FaucetHandle", 0.068, 0.032, MATS["AC_PinkBerry"], col, loc=(0, 0.15, 0.71), segs=12),
        # Wide, unmistakable ceramic pet bowl right beneath the faucet!
        add_cyl("BowlOuter", 0.36, 0.21, MATS["AC_Cream"], col, loc=(0, -0.05, 0.145), r_top=0.33, segs=18),
        add_cyl("BowlStripe", 0.368, 0.07, MATS["AC_MintTeal"], col, loc=(0, -0.05, 0.145), r_top=0.35, segs=18),
    ]
    base = join_around_pivot(base_parts, "WaterBowl_Base", (0, 0, 0), col)
    liq = join_around_pivot([add_cyl("LiquidDisc", 0.295, 0.038, MATS["AC_OceanShallow"], col, loc=(0, -0.05, 0.225), segs=18)], "WaterBowl_Liquid", (0, -0.05, 0.225), col)
    export_establishment_fbx([base, liq], "PetWaterBowl")


# =========================================================================
# 6. RESTAURANT KIOSK TABLE (Crisp 4-legged matte table + stools + elevated Table_CanopyFade umbrella!)
# =========================================================================
def build_restaurant_table(col):
    base_parts = [
        # Matte round wooden table top + 4 clear legs (grounds solidly with contact shadow!)
        add_cyl("TableTop", 0.64, 0.075, MATS["AC_Cream"], col, loc=(0, 0, 0.78), segs=18),
        add_cyl("TableRim", 0.66, 0.045, MATS["AC_WoodWarm"], col, loc=(0, 0, 0.76), segs=18),
        add_cyl("TableLeg1", 0.042, 0.76, MATS["AC_WoodWarm"], col, loc=(-0.34, -0.34, 0.38), segs=10),
        add_cyl("TableLeg2", 0.042, 0.76, MATS["AC_WoodWarm"], col, loc=( 0.34, -0.34, 0.38), segs=10),
        add_cyl("TableLeg3", 0.042, 0.76, MATS["AC_WoodWarm"], col, loc=(-0.34,  0.34, 0.38), segs=10),
        add_cyl("TableLeg4", 0.042, 0.76, MATS["AC_WoodWarm"], col, loc=( 0.34,  0.34, 0.38), segs=10),
        # 2 Compact wooden stools
        add_cyl("StoolL_Seat", 0.22, 0.06, MATS["AC_WoodLight"], col, loc=(-0.92, 0, 0.48), segs=14),
        add_cyl("StoolL_Leg", 0.048, 0.46, MATS["AC_WoodWarm"], col, loc=(-0.92, 0, 0.23), segs=10),
        add_cyl("StoolR_Seat", 0.22, 0.06, MATS["AC_WoodLight"], col, loc=( 0.92, 0, 0.48), segs=14),
        add_cyl("StoolR_Leg", 0.048, 0.46, MATS["AC_WoodWarm"], col, loc=( 0.92, 0, 0.23), segs=10),
        # Slim center umbrella pole (tall so umbrella never hides protagonist's body!)
        add_cyl("UmbPole", 0.032, 2.60, MATS["AC_WoodDark"], col, loc=(0, 0, 1.30), segs=10),
    ]
    base = join_around_pivot(base_parts, "Table_Base", (0, 0, 0), col)

    # Lightweight elevated umbrella exported as separate sibling mesh Table_CanopyFade!
    canopy_parts = [add_sphere("UmbCap", 0.16, MATS["AC_Gold"], col, loc=(0, 0, 2.74), scale=(1, 1, 0.5))]
    segs = 8
    for i in range(segs):
        ang = (i / segs) * math.pi * 2.0
        um = MATS["AC_MintTeal"] if i % 2 == 0 else MATS["AC_Cream"]
        px = math.cos(ang) * 0.48
        py = math.sin(ang) * 0.48
        canopy_parts.append(add_box(f"TUmb_{i}", (0.84, 0.44, 0.055), um, col, loc=(px, py, 2.62), rot=(0, 0.20, ang), bevel=0.015))
    canopy = join_around_pivot(canopy_parts, "Table_CanopyFade", (0, 0, 2.62), col)
    export_establishment_fbx([base, canopy], "RestaurantKioskTable")


def main():
    builders = [
        build_batata_e_mar,
        build_stall_pastel,
        build_stall_doces,
        build_stall_frutas,
        build_stall_sushi,
        build_stall_artesanato,
        build_stall_plantas,
        build_stall_roupas,
        build_stall_sucos,
        build_pet_water_station,
        build_restaurant_table,
    ]
    for fn in builders:
        clear_scene()
        init_mats()
        col = bpy.data.collections.new("SingleEstab")
        bpy.context.scene.collection.children.link(col)
        fn(col)

    bpy.ops.wm.save_as_mainfile(filepath=BLEND_OUT)
    print(f"[Orla das Fadas Establishments & Water Spout Exported] {BLEND_OUT}")


if __name__ == "__main__":
    main()
