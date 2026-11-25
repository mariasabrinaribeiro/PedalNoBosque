"""
Orla das Fadas — v36 Continuous Coastal Terrain, Hero Ice Cream Cart, Botanical Florist & Calm Kiosk (Blender 5.3)
Fixes:
1. OrlaEnvironmentBase.fbx: 3 seamless continuous surfaces (Cream Sand with gentle coastal curve,
   Continuous Boardwalk, Continuous South Plaza extending to z=-32m and x=-120..+145m) ->
   ZERO patchwork floor rectangles, ZERO diagonal sand cutoff, ZERO turquoise gap behind the fair!
2. IceCreamCart.fbx: Unmistakable hero strawberry-pink cart with large spoked wheels, curved cream canopy
   offset cleanly so lemon/basil toppings & 3D waffle-cone sign read from camera height + basil accent!
3. FloristStand.fbx: Sage-green & cream awning, hanging flower baskets, and tiered basil/flower display rack
   at its open entrance facing the protagonist's path!
4. KioskGirlPower.fbx: Repainted in calm cream, soft lilac & sage green so ONLY the Ice Cream Cart wears vivid strawberry pink!
"""

import bpy
import bmesh
import math
import os
import time
from mathutils import Vector

SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
MODELS_DIR = os.path.normpath(os.path.join(SCRIPT_DIR, "..", "Models"))
BLEND_OUT = os.path.join(SCRIPT_DIR, "OrlaDasFadas_v36_HeroTerrain.blend")
os.makedirs(MODELS_DIR, exist_ok=True)


def clear_scene():
    bpy.ops.wm.read_factory_settings(use_empty=True)


def hex_rgba(h):
    h = h.lstrip("#")
    return (int(h[0:2], 16) / 255.0, int(h[2:4], 16) / 255.0, int(h[4:6], 16) / 255.0, 1.0)


MATS = {}


def init_mats():
    MATS.clear()
    for k, hx in [
        ("AC_Sand", "#F4E6C8"),
        ("AC_SandWet", "#DEC9A4"),
        ("AC_StoneWhite", "#F4ECDC"),
        ("AC_StoneWarm", "#E6D9C2"),
        ("AC_Grass", "#6FA684"),
        ("AC_WoodLight", "#D4AE82"),
        ("AC_WoodWarm", "#B88E62"),
        ("AC_WoodDark", "#755238"),
        ("AC_Cream", "#FAF4E8"),
        ("AC_PinkBerry", "#EC5A8E"),   # Reserved vivid strawberry pink for the Hero Ice Cream Cart!
        ("AC_PinkSoft", "#F4B4CC"),
        ("AC_Lavender", "#C9A8E2"),
        ("AC_MintSoft", "#9ED6C8"),
        ("AC_MintTeal", "#35A8B4"),
        ("AC_BushGreen", "#4D9E65"),   # Basil green linking Cart & Florist!
        ("AC_BushLight", "#7AC286"),
        ("AC_Terracotta", "#C98566"),
        ("AC_SunnyYellow", "#F4D678"),
        ("AC_Waffle", "#DFA664"),
        ("AC_Vanilla", "#FFF6DC"),
        ("AC_FlowerPink", "#F07AA8"),
        ("AC_FlowerYellow", "#F5DC78"),
        ("AC_FlowerBlue", "#76BFE2"),
        ("AC_FlowerWhite", "#FFFDF6"),
        ("AC_MetalSilver", "#CFD7E0"),
        ("AC_MetalDark", "#3A424E"),
        ("AC_Gold", "#E6C274"),
        ("AC_Glass", "#D4F0F6"),
        ("AC_BulbWarm", "#FFF0B3"),
        ("AC_Starfish", "#E28E7C"),
        ("AC_Shell", "#F8ECE0"),
    ]:
        m = bpy.data.materials.new(name=k)
        bsdf = m.node_tree.nodes.get("Principled BSDF") if m.node_tree else None
        if bsdf:
            bsdf.inputs["Base Color"].default_value = hex_rgba(hx)
            bsdf.inputs["Roughness"].default_value = 0.86
        MATS[k] = m


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


def add_cyl(name, r, h, mat, col, loc=(0, 0, 0), rot=(0, 0, 0), segs=16, r_top=None):
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


def add_sphere(name, r, mat, col, loc=(0, 0, 0), scale=(1, 1, 1), rot=(0, 0, 0), u=14, v=10):
    mesh = bpy.data.meshes.new(name)
    bm = bmesh.new()
    bmesh.ops.create_uvsphere(bm, u_segments=u, v_segments=v, radius=r)
    for f in bm.faces:
        f.smooth = True
    bm.to_mesh(mesh)
    bm.free()
    obj = bpy.data.objects.new(name, mesh)
    obj.location = loc
    obj.rotation_euler = rot
    obj.scale = scale
    obj.data.materials.append(mat)
    col.objects.link(obj)
    return obj


def join_parts(parts, name, col):
    bpy.ops.object.select_all(action="DESELECT")
    for p in parts:
        p.select_set(True)
    bpy.context.view_layer.objects.active = parts[0]
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    if len(parts) > 1:
        bpy.ops.object.join()
    obj = bpy.context.view_layer.objects.active
    obj.name = name
    bpy.context.scene.cursor.location = (0, 0, 0)
    bpy.ops.object.origin_set(type="ORIGIN_CURSOR")
    return obj


def export_fbx(objs, filename):
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
    print(f"[FBX v36] {out_path}")


# =========================================================================
# 1. CONTINUOUS 3-SURFACE COASTAL BASE (Subdivided along X so Horizon Curve
#    NEVER bends or sinks flat boxes -> ZERO vertical bands, ZERO gaps!)
# =========================================================================
def build_subdivided_strip(name, x0, x1, y0, y1, z_top, thickness, segs_x, segs_y, mat, col):
    """Creates a clean closed slab subdivided along X & Y so vertex horizon curvature stays 100% flush with sand & props."""
    mesh = bpy.data.meshes.new(name)
    bm = bmesh.new()
    top_grid = []
    for ix in range(segs_x + 1):
        x = x0 + (x1 - x0) * (ix / segs_x)
        row = []
        for iy in range(segs_y + 1):
            y = y0 + (y1 - y0) * (iy / segs_y)
            row.append(bm.verts.new((x, y, z_top)))
        top_grid.append(row)
    for ix in range(segs_x):
        for iy in range(segs_y):
            bm.faces.new((top_grid[ix][iy], top_grid[ix + 1][iy], top_grid[ix + 1][iy + 1], top_grid[ix][iy + 1]))

    # Subtle vertical skirt along front/back borders so slight elevation steps read solid and grounded
    z_bot = z_top - thickness
    for iy_edge, flip in [(0, True), (segs_y, False)]:
        bot_row = [bm.verts.new((x0 + (x1 - x0) * (ix / segs_x), y0 if iy_edge == 0 else y1, z_bot)) for ix in range(segs_x + 1)]
        for ix in range(segs_x):
            v0 = top_grid[ix][iy_edge]
            v1 = top_grid[ix + 1][iy_edge]
            b0 = bot_row[ix]
            b1 = bot_row[ix + 1]
            if flip:
                bm.faces.new((v1, v0, b0, b1))
            else:
                bm.faces.new((v0, v1, b1, b0))

    for f in bm.faces:
        f.smooth = False
    bm.normal_update()
    bm.to_mesh(mesh)
    bm.free()
    obj = bpy.data.objects.new(name, mesh)
    obj.data.materials.append(mat)
    col.objects.link(obj)
    return obj


def build_continuous_environment(col):
    parts = []
    x0, x1 = -120.0, 144.0
    segs_x = 96

    # SURFACE A: Continuous Subdivided Calçadão Principal (Y = -3.75 .. +3.50, Z = 0.00)
    parts.append(build_subdivided_strip("Surf_Calcadao", x0, x1, -3.75, 3.50, 0.0, 0.22, segs_x, 4, MATS["AC_StoneWhite"], col))
    # Gentle continuous curb lip separating Calçadão & Praça (subdivided so zero height sag!)
    parts.append(build_subdivided_strip("Surf_BordaPraca", x0, x1, -3.92, -3.72, 0.015, 0.09, segs_x, 1, MATS["AC_StoneWarm"], col))

    # SURFACE B: Continuous Subdivided Praça Lateral extending far south (Y = -36.0 .. -3.75, Z = -0.04)
    parts.append(build_subdivided_strip("Surf_PracaContinua", x0, x1, -36.0, -3.75, -0.04, 0.22, segs_x, 10, MATS["AC_StoneWarm"], col))

    # Low, rounded coastal mureta segments flanking the 3 wide beach accesses (subdivided along long spans!)
    for mname, mx0, mx1 in [
        ("Mureta_FarWest", -116.0, -7.2),
        ("Mureta_MidWest",   -3.3,  5.3),
        ("Mureta_MidEast",    9.7, 17.8),
        ("Mureta_FarEast",   22.2, 142.0),
    ]:
        m_segs = max(4, int((mx1 - mx0) / 3.0))
        parts.append(build_subdivided_strip(mname, mx0, mx1, 3.38, 3.74, 0.24, 0.30, m_segs, 1, MATS["AC_StoneWhite"], col))
        parts.append(build_subdivided_strip(mname + "_Cap", mx0 - 0.03, mx1 + 0.03, 3.34, 3.78, 0.30, 0.06, m_segs, 1, MATS["AC_StoneWarm"], col))

    # SURFACE C: Continuous Curved Cream Sand Beach sloping seamlessly under the sea (X = -120 .. +144!)
    mesh_dry = bpy.data.meshes.new("Surf_AreiaCreme")
    bm = bmesh.new()
    grid = []
    for ix in range(segs_x + 1):
        u = ix / segs_x
        x = x0 + (x1 - x0) * u
        coast_curve = math.sin(x * 0.045) * 0.95 + math.cos(x * 0.022) * 0.45
        ys = [3.35, 6.40 + coast_curve * 0.35, 9.35 + coast_curve * 0.75, 11.40 + coast_curve, 18.00]
        zs = [-0.03, -0.14, -0.28, -0.44, -0.82]
        row = [bm.verts.new((x, ys[r], zs[r])) for r in range(len(ys))]
        grid.append(row)
    for ix in range(segs_x):
        for r in range(4):
            bm.faces.new((grid[ix][r], grid[ix + 1][r], grid[ix + 1][r + 1], grid[ix][r + 1]))
    for f in bm.faces:
        f.smooth = True
    bm.normal_update()
    bm.to_mesh(mesh_dry)
    bm.free()
    sand_obj = bpy.data.objects.new("Surf_AreiaCreme", mesh_dry)
    sand_obj.data.materials.append(MATS["AC_Sand"])
    col.objects.link(sand_obj)
    parts.append(sand_obj)

    # Subtle shells & starfish along dry cream sand
    for idx, (sx, sy) in enumerate([(-3.6, 5.8), (4.2, 6.8), (13.4, 6.4), (20.8, 7.1)]):
        parts.append(add_sphere(f"Starfish_{idx}", 0.16, MATS["AC_Starfish"], col, loc=(sx, sy, -0.11), scale=(1.1, 1.1, 0.25), u=10, v=6))
        parts.append(add_sphere(f"Shell_{idx}", 0.11, MATS["AC_Shell"], col, loc=(sx + 0.55, sy + 0.4, -0.12), scale=(1.2, 0.9, 0.42), u=8, v=6))

    env = join_parts(parts, "OrlaEnvironmentBase", col)
    export_fbx([env], "OrlaEnvironmentBase")
    return env


# =========================================================================
# 2. HERO ICE CREAM CART (Distinct strawberry pink, big cream spoked wheels,
#    arched cream canopy offset back, visible lemon+basil tubs, 3D cone sign!)
# =========================================================================
def build_hero_ice_cream_cart(col):
    base_parts = [
        # Vivid strawberry-pink main cart chassis (ONLY object in the scene with this vivid pink!)
        add_box("CartChassis", (1.76, 0.96, 0.74), MATS["AC_PinkBerry"], col, loc=(0, 0, 0.72), bevel=0.055),
        # Crisp cream scalloped apron trim + basil-green accent ribbon linking to Florist!
        add_box("CartCreamTrim", (1.82, 1.00, 0.12), MATS["AC_Cream"], col, loc=(0, 0, 0.44), bevel=0.025),
        add_box("CartBasilBand", (1.80, 0.99, 0.08), MATS["AC_BushGreen"], col, loc=(0, 0, 0.98), bevel=0.02),
        # Wide open cream countertop at z=1.08m (hands reach comfortably!)
        add_box("CartCounterTop", (1.92, 1.10, 0.11), MATS["AC_Cream"], col, loc=(0, 0, 1.085), bevel=0.035),
        # Visible open ingredient tubs on top (Lemon yellow + Strawberry pink + Vanilla + Fresh Basil bowl!)
        add_box("TubTray", (1.16, 0.62, 0.06), MATS["AC_MetalSilver"], col, loc=(-0.18, -0.06, 1.15), bevel=0.015),
        add_box("TubLemon", (0.32, 0.44, 0.07), MATS["AC_SunnyYellow"], col, loc=(-0.54, -0.06, 1.19), bevel=0.02),
        add_box("TubBerry", (0.32, 0.44, 0.07), MATS["AC_PinkSoft"], col, loc=(-0.18, -0.06, 1.19), bevel=0.02),
        add_box("TubVanilla", (0.32, 0.44, 0.07), MATS["AC_Vanilla"], col, loc=(0.18, -0.06, 1.19), bevel=0.02),
        # Fresh green Basil jar right on the counter corner!
        add_cyl("BasilJar", 0.11, 0.14, MATS["AC_Terracotta"], col, loc=(0.66, -0.22, 1.21), r_top=0.13),
        add_sphere("BasilLeaf1", 0.11, MATS["AC_BushGreen"], col, loc=(0.64, -0.22, 1.32), scale=(1.1, 1.0, 0.8)),
        add_sphere("BasilLeaf2", 0.09, MATS["AC_BushLight"], col, loc=(0.72, -0.18, 1.35), scale=(1.0, 1.0, 0.8)),
        # Push handle on left side
        add_cyl("HandleArmF", 0.035, 0.42, MATS["AC_WoodDark"], col, loc=(-1.04, -0.32, 1.00), rot=(0, 1.22, 0), segs=10),
        add_cyl("HandleArmB", 0.035, 0.42, MATS["AC_WoodDark"], col, loc=(-1.04,  0.32, 1.00), rot=(0, 1.22, 0), segs=10),
        add_cyl("HandleBar", 0.048, 0.74, MATS["AC_Cream"], col, loc=(-1.22, 0, 1.06), rot=(math.pi * 0.5, 0, 0), segs=12),
        # UNMISTAKABLE 3D WAFFLE CONE SIGN PLAQUE mounted on the front & right corner!
        add_box("SignPlaque", (1.28, 0.07, 0.36), MATS["AC_Cream"], col, loc=(0.04, -0.51, 0.74), bevel=0.02),
        add_cyl("ConeSignWaffle", 0.02, 0.15, MATS["AC_Waffle"], col, loc=(-0.44, -0.56, 0.68), segs=12, r_top=0.095),
        add_sphere("ConeSignLemon", 0.105, MATS["AC_SunnyYellow"], col, loc=(-0.44, -0.56, 0.80)),
        add_sphere("ConeSignBasil", 0.055, MATS["AC_BushGreen"], col, loc=(-0.38, -0.57, 0.88)),
        # Large sculptural 3D Ice Cream Cone topper on right counter post (readable from afar!)
        add_cyl("HeroConeBase", 0.035, 0.38, MATS["AC_Waffle"], col, loc=(0.74, 0.18, 1.34), segs=14, r_top=0.17),
        add_sphere("HeroConeScoop1", 0.20, MATS["AC_SunnyYellow"], col, loc=(0.74, 0.18, 1.58)),
        add_sphere("HeroConeScoop2", 0.15, MATS["AC_PinkSoft"], col, loc=(0.66, 0.14, 1.74)),
        add_sphere("HeroConeBasil", 0.085, MATS["AC_BushGreen"], col, loc=(0.82, 0.16, 1.76), scale=(1.2, 0.8, 0.5)),
    ]

    # LARGE HIGH-CONTRAST SPOKED WHEELS (r = 0.42m, clearly visible on both sides of the cart!)
    for wx in (-0.58, 0.58):
        for wy in (-0.54, 0.54):
            base_parts.append(add_cyl(f"WheelOuter_{wx}_{wy}", 0.42, 0.10, MATS["AC_WoodDark"], col, loc=(wx, wy, 0.42), rot=(math.pi * 0.5, 0, 0), segs=20))
            base_parts.append(add_cyl(f"WheelRim_{wx}_{wy}", 0.34, 0.115, MATS["AC_Cream"], col, loc=(wx, wy, 0.42), rot=(math.pi * 0.5, 0, 0), segs=18))
            base_parts.append(add_cyl(f"WheelHub_{wx}_{wy}", 0.11, 0.14, MATS["AC_Gold"], col, loc=(wx, wy, 0.42), rot=(math.pi * 0.5, 0, 0), segs=12))
            for sp in range(4):
                ang = sp * (math.pi * 0.25)
                base_parts.append(add_box(f"Spoke_{wx}_{wy}_{sp}", (0.64, 0.05, 0.045), MATS["AC_PinkBerry"], col, loc=(wx, wy, 0.42), rot=(0, ang, 0), bevel=0.008))

    # Slim rear canopy support posts (offset toward +Y back so front counter & toppings are 100% open!)
    base_parts.append(add_cyl("CanopyPostL", 0.045, 1.48, MATS["AC_WoodLight"], col, loc=(-0.80, 0.38, 1.82), segs=10))
    base_parts.append(add_cyl("CanopyPostR", 0.045, 1.48, MATS["AC_WoodLight"], col, loc=( 0.80, 0.38, 1.82), segs=10))
    base_obj = join_parts(base_parts, "Cart_Base", col)

    # CURVED ARCHED CREAM CANOPY WITH STRAWBERRY TRIM (compact, elevated at z=2.55, separate CanopyFade mesh!)
    canopy_parts = []
    segs = 7
    width = 2.08
    sw = width / segs
    for i in range(segs):
        sx = -width * 0.5 + (i + 0.5) * sw
        # Arched curved profile across depth (Y = -0.35 .. +0.62)
        sm = MATS["AC_Cream"] if i % 2 == 0 else MATS["AC_PinkBerry"]
        canopy_parts.append(add_box(f"CartArchBack_{i}", (sw - 0.01, 0.54, 0.07), sm, col, loc=(sx, 0.34, 2.58), rot=(math.radians(-16), 0, 0), bevel=0.018))
        canopy_parts.append(add_box(f"CartArchTop_{i}",  (sw - 0.01, 0.52, 0.07), sm, col, loc=(sx, -0.02, 2.66), rot=(math.radians(8), 0, 0), bevel=0.018))
        canopy_parts.append(add_box(f"CartArchFront_{i}",(sw - 0.01, 0.42, 0.07), sm, col, loc=(sx, -0.34, 2.55), rot=(math.radians(28), 0, 0), bevel=0.018))
        canopy_parts.append(add_sphere(f"CartScallop_{i}", sw * 0.44, sm, col, loc=(sx, -0.52, 2.44), scale=(1.0, 0.45, 0.65), u=10, v=6))
    canopy_obj = join_parts(canopy_parts, "Cart_CanopyFade", col)
    export_fbx([base_obj, canopy_obj], "IceCreamCart")
    return [base_obj, canopy_obj]


# =========================================================================
# 3. BOTANICAL FLORIST STAND (Sage-green & cream awning, hanging flower pots,
#    tiered basil & flower display rack right beside open entrance!)
# =========================================================================
def build_botanical_florist(col):
    base_parts = [
        # Warm light-wood platform & open botanical counter (height 0.94m!)
        add_box("FDeck", (4.10, 2.15, 0.14), MATS["AC_WoodLight"], col, loc=(0, 0, 0.07), bevel=0.03),
        add_box("FCounter", (3.30, 0.92, 0.78), MATS["AC_Cream"], col, loc=(0, -0.28, 0.52), bevel=0.035),
        add_box("FCounterBand", (3.36, 0.96, 0.12), MATS["AC_BushGreen"], col, loc=(0, -0.28, 0.80), bevel=0.02),
        add_box("FCounterTop", (3.55, 1.08, 0.10), MATS["AC_WoodLight"], col, loc=(0, -0.28, 0.94), bevel=0.025),
        # Open trellis back & 4 light wood corner posts
        add_cyl("FPostFL", 0.075, 2.45, MATS["AC_WoodWarm"], col, loc=(-1.72, -0.72, 1.32), segs=12),
        add_cyl("FPostFR", 0.075, 2.45, MATS["AC_WoodWarm"], col, loc=( 1.72, -0.72, 1.32), segs=12),
        add_cyl("FPostBL", 0.075, 2.55, MATS["AC_WoodWarm"], col, loc=(-1.72,  0.76, 1.38), segs=12),
        add_cyl("FPostBR", 0.075, 2.55, MATS["AC_WoodWarm"], col, loc=( 1.72,  0.76, 1.38), segs=12),
        # Solid cream sign plaque with wooden frame for crisp dark espresso text!
        add_box("FPlaqueFrame", (2.75, 0.14, 0.52), MATS["AC_WoodWarm"], col, loc=(0, -0.78, 2.86), bevel=0.025),
        add_box("FPlaqueFace", (2.56, 0.16, 0.38), MATS["AC_Cream"], col, loc=(0, -0.80, 2.86), bevel=0.015),
    ]
    for hx in (-1.15, -0.58, 0.0, 0.58, 1.15):
        base_parts.append(add_box(f"FTrellisV_{hx}", (0.06, 0.06, 2.10), MATS["AC_WoodLight"], col, loc=(hx, 0.76, 1.30), bevel=0.01))
    for hz in (0.75, 1.35, 1.95):
        base_parts.append(add_box(f"FTrellisH_{hz}", (3.44, 0.06, 0.06), MATS["AC_WoodLight"], col, loc=(0, 0.76, hz), bevel=0.01))

    # HANGING FLOWER POTS along front beam (unmistakable florist silhouette from camera height!)
    flowers = [MATS["AC_FlowerPink"], MATS["AC_FlowerYellow"], MATS["AC_FlowerWhite"], MATS["AC_FlowerBlue"]]
    for i, hx in enumerate((-1.22, -0.42, 0.42, 1.22)):
        base_parts.append(add_cyl(f"HangCord_{i}", 0.014, 0.34, MATS["AC_WoodDark"], col, loc=(hx, -0.88, 2.34), segs=8))
        base_parts.append(add_cyl(f"HangPot_{i}", 0.13, 0.18, MATS["AC_Terracotta"], col, loc=(hx, -0.88, 2.12), r_top=0.16, segs=12))
        base_parts.append(add_sphere(f"HangFoliage_{i}", 0.18, MATS["AC_BushGreen"], col, loc=(hx, -0.88, 2.24), scale=(1.1, 1.0, 0.78)))
        base_parts.append(add_sphere(f"HangBloomA_{i}", 0.075, flowers[i % 4], col, loc=(hx - 0.09, -0.94, 2.30)))
        base_parts.append(add_sphere(f"HangBloomB_{i}", 0.070, flowers[(i + 1) % 4], col, loc=(hx + 0.09, -0.93, 2.29)))

    # TIERED BASIL & FLOWER DISPLAY RACK on counter & beside entrance (visible from game camera!)
    for i in range(6):
        fx = -1.25 + i * 0.50
        base_parts.append(add_cyl(f"FPot_{i}", 0.12, 0.20, MATS["AC_Terracotta"], col, loc=(fx, -0.28, 1.09), r_top=0.15, segs=12))
        base_parts.append(add_sphere(f"FLeaf_{i}", 0.18, MATS["AC_BushGreen"] if i % 2 == 0 else MATS["AC_BushLight"], col, loc=(fx, -0.28, 1.25), scale=(1.0, 1.0, 0.85)))
        base_parts.append(add_sphere(f"FBloom_{i}", 0.085, flowers[i % 4], col, loc=(fx, -0.32, 1.37)))

    # Low wooden front flower-pot display bench framing the open entrance toward the promenade!
    for s_idx, sx in enumerate((-1.95, 1.95)):
        base_parts.append(add_box(f"SideRackLow_{s_idx}", (0.72, 0.96, 0.34), MATS["AC_WoodLight"], col, loc=(sx, -0.48, 0.17), bevel=0.025))
        base_parts.append(add_box(f"SideRackHigh_{s_idx}", (0.60, 0.46, 0.56), MATS["AC_WoodWarm"], col, loc=(sx, -0.18, 0.36), bevel=0.025))
        for k, (dy, dz) in enumerate([(-0.74, 0.45), (-0.22, 0.76)]):
            base_parts.append(add_cyl(f"SidePot_{s_idx}_{k}", 0.19, 0.24, MATS["AC_Terracotta"], col, loc=(sx, dy, dz), r_top=0.23, segs=14))
            base_parts.append(add_sphere(f"SideBush_{s_idx}_{k}", 0.27, MATS["AC_BushGreen"] if k == 0 else MATS["AC_BushLight"], col, loc=(sx, dy, dz + 0.20)))
            base_parts.append(add_sphere(f"SideBlm_{s_idx}_{k}", 0.10, flowers[(s_idx + k) % 4], col, loc=(sx, dy - 0.13, dz + 0.32)))

    # Low front entrance pot step right in front of the counter ends so the Florist silhouette reads without any text!
    for f_idx, fx in enumerate((-1.32, 1.32)):
        base_parts.append(add_box(f"FrontPotBench_{f_idx}", (0.64, 0.42, 0.22), MATS["AC_WoodLight"], col, loc=(fx, -1.04, 0.11), bevel=0.022))
        base_parts.append(add_cyl(f"FrontPot_{f_idx}", 0.15, 0.20, MATS["AC_Terracotta"], col, loc=(fx, -1.04, 0.31), r_top=0.19, segs=14))
        base_parts.append(add_sphere(f"FrontBush_{f_idx}", 0.22, MATS["AC_BushGreen"], col, loc=(fx, -1.04, 0.49)))
        base_parts.append(add_sphere(f"FrontBloom_{f_idx}", 0.095, flowers[(f_idx + 2) % 4], col, loc=(fx, -1.16, 0.58)))

    base_obj = join_parts(base_parts, "Florist_Base", col)

    # BOTANICAL SAGE-GREEN & CREAM AWNING (restrained height so it never towers over characters!)
    canopy_parts = []
    stripes = 9
    sw = 4.15 / stripes
    for i in range(stripes):
        sx = -2.075 + (i + 0.5) * sw
        sm = MATS["AC_BushGreen"] if i % 2 == 0 else MATS["AC_Cream"]
        canopy_parts.append(add_box(f"FAwn_{i}", (sw - 0.012, 2.02, 0.11), sm, col, loc=(sx, -0.02, 2.52), rot=(0.18, 0, 0), bevel=0.028))
        canopy_parts.append(add_sphere(f"FVal_{i}", sw * 0.45, sm, col, loc=(sx, -1.00, 2.34), scale=(1.0, 0.45, 0.70), u=10, v=6))
    canopy_obj = join_parts(canopy_parts, "Florist_CanopyFade", col)
    export_fbx([base_obj, canopy_obj], "FloristStand")
    return [base_obj, canopy_obj]


# =========================================================================
# 4. CALM COASTAL KIOSK (Cream, Soft Lilac & Sage Green — proportioned to protagonist!)
# =========================================================================
def build_calm_kiosk(col):
    base_parts = [
        add_box("KDeck", (4.2, 2.15, 0.14), MATS["AC_WoodLight"], col, loc=(0, 0, 0.07), bevel=0.035),
        add_box("KCounter", (3.45, 0.96, 0.78), MATS["AC_Cream"], col, loc=(0, -0.28, 0.51), bevel=0.035),
        add_box("KStripe", (3.50, 1.00, 0.12), MATS["AC_Lavender"], col, loc=(0, -0.28, 0.46), bevel=0.022),
        add_box("KTop", (3.70, 1.12, 0.10), MATS["AC_WoodLight"], col, loc=(0, -0.28, 0.94), bevel=0.028),
        add_box("KBackWall", (3.45, 0.16, 2.04), MATS["AC_Cream"], col, loc=(0, 0.68, 1.15), bevel=0.028),
        add_box("KPlaqueFrame", (2.78, 0.14, 0.50), MATS["AC_WoodWarm"], col, loc=(0, -0.68, 2.74), bevel=0.028),
        add_box("KPlaqueFace", (2.60, 0.16, 0.36), MATS["AC_Cream"], col, loc=(0, -0.70, 2.74), bevel=0.015),
    ]
    for px in (-1.70, 1.70):
        for py in (-0.68, 0.68):
            base_parts.append(add_cyl(f"KPost_{px}_{py}", 0.068, 2.38, MATS["AC_WoodWarm"], col, loc=(px, py, 1.28), segs=12))
    base_obj = join_parts(base_parts, "Kiosk_Base", col)

    canopy_parts = []
    stripes = 9
    sw = 4.25 / stripes
    for i in range(stripes):
        sx = -2.125 + (i + 0.5) * sw
        sm = MATS["AC_Lavender"] if i % 2 == 0 else MATS["AC_Cream"]
        canopy_parts.append(add_box(f"KAwn_{i}", (sw - 0.012, 2.08, 0.11), sm, col, loc=(sx, 0.0, 2.50), rot=(0.17, 0, 0), bevel=0.028))
        canopy_parts.append(add_sphere(f"KVal_{i}", sw * 0.45, sm, col, loc=(sx, -1.02, 2.33), scale=(1.0, 0.45, 0.70), u=10, v=6))
    canopy_obj = join_parts(canopy_parts, "Kiosk_CanopyFade", col)
    export_fbx([base_obj, canopy_obj], "KioskGirlPower")
    return [base_obj, canopy_obj]


def main():
    builders = [
        build_continuous_environment,
        build_hero_ice_cream_cart,
        build_botanical_florist,
        build_calm_kiosk,
    ]
    for fn in builders:
        clear_scene()
        init_mats()
        col = bpy.data.collections.new("Export")
        bpy.context.scene.collection.children.link(col)
        fn(col)

    clear_scene()
    init_mats()
    col_all = bpy.data.collections.new("HeroAndTerrain_Preview")
    bpy.context.scene.collection.children.link(col_all)
    for idx, fname in enumerate(["IceCreamCart", "FloristStand", "KioskGirlPower"]):
        before = set(bpy.data.objects)
        bpy.ops.import_scene.fbx(filepath=os.path.join(MODELS_DIR, f"{fname}.fbx"), axis_forward="-Z", axis_up="Y")
        for o in set(bpy.data.objects) - before:
            o.location.x += idx * 5.2
    bpy.ops.wm.save_as_mainfile(filepath=BLEND_OUT)
    print(f"[Orla v36 Continuous Terrain & Hero Landmarks Exported] {BLEND_OUT}")


if __name__ == "__main__":
    main()
