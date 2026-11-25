"""
Baia do Avesso — Original Tropical-Magical Coastal Identity Pack (Blender 5.3)
Generates 100% original, upright Unity-ready FBX models (flat sibling meshes + bake_space_transform=True):
  1. Char_FairyWaitress.fbx      — Slender coastal Fairy Waitress with articulated Fairy_WingL / Fairy_WingR + tray!
  2. CoastalCafe_Base.fbx        — Enterable Beach Cafe & Pousada interior (open doorway, counter, pastries, tables, balcony!)
  3. CoastalCafe_RoofFade.fbx    — Matching roof & front gable that smoothly fades out when entering (zero loading screen!)
  4. CoastalFacadeRow.fbx        — Colorful low coastal town buildings, pousadas & boutiques framing the back horizon!
  5. GlowingMushroomCluster.fbx  — Bioluminescent pastel coastal mushrooms for the beach & restinga!
  6. FloatingLantern.fbx         — Warm magical floating beach lantern!
  7. FlowerLampPost.fbx          — Coastal lamp post wrapped with tropical flowers!
"""

import bpy
import bmesh
import math
import os
from mathutils import Vector

SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
MODELS_DIR = os.path.normpath(os.path.join(SCRIPT_DIR, "..", "Models"))
BLEND_OUT = os.path.join(SCRIPT_DIR, "BaiaDoAvesso_MagicalCoast.blend")
PREVIEW_OUT = os.path.join(SCRIPT_DIR, "baia_magical_preview.png")
os.makedirs(MODELS_DIR, exist_ok=True)


def clear_scene():
    bpy.ops.wm.read_factory_settings(use_empty=True)


def hex_rgba(h):
    h = h.lstrip("#")
    return (int(h[0:2], 16) / 255.0, int(h[2:4], 16) / 255.0, int(h[4:6], 16) / 255.0, 1.0)


MATS = {}


def make_mat(name, hx, rough=0.52):
    m = bpy.data.materials.new(name=name)
    bsdf = m.node_tree.nodes.get("Principled BSDF") if m.node_tree else None
    if bsdf:
        bsdf.inputs["Base Color"].default_value = hex_rgba(hx)
        bsdf.inputs["Roughness"].default_value = rough
    MATS[name] = m
    return m


def init_mats():
    for k, hx in [
        ("AC_ProtagShirt", "#F26392"),
        ("AC_PinkBerry", "#F26392"),
        ("AC_PinkSoft", "#F6A8C0"),
        ("AC_MintTeal", "#38C8BE"),
        ("AC_MintSoft", "#98E8DF"),
        ("AC_Lavender", "#B59CE4"),
        ("AC_Cream", "#FAF2E4"),
        ("AC_HouseWall", "#FCEFD9"),
        ("AC_HouseRoof", "#D97F68"),
        ("AC_Coral", "#DE8676"),
        ("AC_SkyBlue", "#6CC2E8"),
        ("AC_SunnyYellow", "#F4D678"),
        ("AC_WoodDark", "#6E442A"),
        ("AC_WoodWarm", "#9E6945"),
        ("AC_WoodLight", "#CFA074"),
        ("AC_StoneWhite", "#EFE5D2"),
        ("AC_Glass", "#CBEFF7"),
        ("AC_Gold", "#EFC45A"),
        ("AC_LanternGlow", "#FFE69C"),
        ("AC_BulbWarm", "#FFF2BC"),
        ("AC_SkinLight", "#F4C29F"),
        ("AC_SkinWarm", "#D29269"),
        ("AC_CheekBlush", "#F694AA"),
        ("AC_EyeDark", "#251D29"),
        ("AC_EyeShine", "#FFFFFF"),
        ("AC_HairAuburn", "#B8543B"),
        ("AC_HairPlum", "#7D4588"),
        ("AC_BushGreen", "#5A9B68"),
        ("AC_FlowerPink", "#FF7EA8"),
        ("AC_FlowerYellow", "#FFE066"),
        ("AC_MetalDark", "#3F4959"),
        ("AC_MetalSilver", "#D6DDE6"),
        ("AC_Chocolate", "#6E422C"),
    ]:
        make_mat(k, hx)


def add_box(name, size, mat, col, loc=(0, 0, 0), rot=(0, 0, 0), bevel=0.025):
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


def add_sphere(name, r, mat, col, loc=(0, 0, 0), scale=(1, 1, 1), u=14, v=10):
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


def export_flat_upright(objs, filename):
    bpy.ops.object.select_all(action="DESELECT")
    for o in objs:
        o.select_set(True)
    bpy.context.view_layer.objects.active = objs[0]
    out_path = os.path.join(MODELS_DIR, f"{filename}.fbx")
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
    print(f"[FBX Exported Upright] {out_path}")


# =========================================================================
# 1. FAIRY WAITRESS (Original slender tropical fairy + fluttering wings + tray!)
# =========================================================================
def build_fairy_waitress(col):
    # Slender, graceful original proportions (distinct from big-head Animal Crossing style!)
    body_parts = [
        # Tropical layered petal dress + cream waitress apron
        add_cyl("DressSkirt", 0.27, 0.42, MATS["AC_MintTeal"], col, loc=(0, 0, 0.44), r_top=0.13),
        add_cyl("Apron", 0.22, 0.26, MATS["AC_Cream"], col, loc=(0, -0.06, 0.48), r_top=0.14),
        add_cyl("Bodice", 0.14, 0.34, MATS["AC_PinkSoft"], col, loc=(0, 0, 0.78), r_top=0.12),
        # Slender legs floating gracefully
        add_cyl("LegL", 0.048, 0.30, MATS["AC_SkinWarm"], col, loc=(-0.08, 0.02, 0.16), rot=(0.18, 0, 0)),
        add_cyl("LegR", 0.048, 0.30, MATS["AC_SkinWarm"], col, loc=(0.08, 0.04, 0.14), rot=(0.24, 0, 0)),
        add_sphere("SlipperL", 0.06, MATS["AC_Gold"], col, loc=(-0.08, 0.05, 0.04), scale=(0.9, 1.3, 0.6)),
        add_sphere("SlipperR", 0.06, MATS["AC_Gold"], col, loc=(0.08, 0.08, 0.02), scale=(0.9, 1.3, 0.6)),
        # Head (proportional, graceful oval — NOT huge round chibi!)
        add_sphere("Head", 0.20, MATS["AC_SkinWarm"], col, loc=(0, 0, 1.12), scale=(0.96, 0.94, 1.06), u=16, v=12),
        add_sphere("EyeL", 0.032, MATS["AC_EyeDark"], col, loc=(-0.075, -0.175, 1.13), scale=(0.9, 0.45, 1.2)),
        add_sphere("EyeR", 0.032, MATS["AC_EyeDark"], col, loc=(0.075, -0.175, 1.13), scale=(0.9, 0.45, 1.2)),
        add_sphere("ShineL", 0.012, MATS["AC_EyeShine"], col, loc=(-0.085, -0.19, 1.145)),
        add_sphere("ShineR", 0.012, MATS["AC_EyeShine"], col, loc=(0.065, -0.19, 1.145)),
        add_sphere("CheekL", 0.038, MATS["AC_CheekBlush"], col, loc=(-0.11, -0.16, 1.08), scale=(1.1, 0.4, 0.7)),
        add_sphere("CheekR", 0.038, MATS["AC_CheekBlush"], col, loc=(0.11, -0.16, 1.08), scale=(1.1, 0.4, 0.7)),
        # Stylized wavy hair + tropical flower crown!
        add_sphere("HairTop", 0.215, MATS["AC_HairPlum"], col, loc=(0, 0.04, 1.18), scale=(1.02, 1.02, 0.95)),
        add_sphere("HairFlow", 0.16, MATS["AC_HairPlum"], col, loc=(0, 0.19, 1.00), scale=(1.15, 0.75, 1.45)),
        add_sphere("CrownF1", 0.065, MATS["AC_FlowerPink"], col, loc=(-0.14, -0.06, 1.28)),
        add_sphere("CrownF2", 0.070, MATS["AC_FlowerYellow"], col, loc=(0.0, -0.14, 1.31)),
        add_sphere("CrownF3", 0.065, MATS["AC_MintSoft"], col, loc=(0.14, -0.06, 1.28)),
        # Arms holding a floating golden serving tray with tropical juice + ice cream!
        add_cyl("ArmL", 0.040, 0.28, MATS["AC_SkinWarm"], col, loc=(-0.19, -0.08, 0.80), rot=(0.75, 0, 0.25)),
        add_cyl("ArmR", 0.040, 0.28, MATS["AC_SkinWarm"], col, loc=(0.19, -0.08, 0.80), rot=(0.75, 0, -0.25)),
        add_cyl("Tray", 0.24, 0.03, MATS["AC_Gold"], col, loc=(0, -0.34, 0.86), segs=16),
        add_cyl("TrayCup", 0.055, 0.14, MATS["AC_PinkBerry"], col, loc=(-0.08, -0.34, 0.95), segs=10),
        add_sphere("TrayIce", 0.065, MATS["AC_SunnyYellow"], col, loc=(0.08, -0.34, 0.96)),
    ]
    body = join_around_pivot(body_parts, "Fairy_Body", (0, 0, 0.0), col)

    # Articulated translucent-glow butterfly/fairy wings pivoted cleanly on the back!
    wing_l_parts = [
        add_sphere("WingL_Upper", 0.26, MATS["AC_MintSoft"], col, loc=(-0.34, 0.22, 0.96), scale=(1.45, 0.14, 0.92)),
        add_sphere("WingL_Lower", 0.18, MATS["AC_PinkSoft"], col, loc=(-0.26, 0.21, 0.72), scale=(1.25, 0.12, 0.82)),
        add_sphere("WingL_Gem", 0.055, MATS["AC_LanternGlow"], col, loc=(-0.38, 0.19, 0.96), scale=(1.0, 0.4, 1.0)),
    ]
    wing_l = join_around_pivot(wing_l_parts, "Fairy_WingL", (-0.07, 0.16, 0.86), col)

    wing_r_parts = [
        add_sphere("WingR_Upper", 0.26, MATS["AC_MintSoft"], col, loc=(0.34, 0.22, 0.96), scale=(1.45, 0.14, 0.92)),
        add_sphere("WingR_Lower", 0.18, MATS["AC_PinkSoft"], col, loc=(0.26, 0.21, 0.72), scale=(1.25, 0.12, 0.82)),
        add_sphere("WingR_Gem", 0.055, MATS["AC_LanternGlow"], col, loc=(0.38, 0.19, 0.96), scale=(1.0, 0.4, 1.0)),
    ]
    wing_r = join_around_pivot(wing_r_parts, "Fairy_WingR", (0.07, 0.16, 0.86), col)

    export_flat_upright([body, wing_l, wing_r], "Char_FairyWaitress")
    return [body, wing_l, wing_r]


# =========================================================================
# 2. ENTERABLE BEACH CAFE ("Cafe das Fadas") — Base + Fade Roof (Zero Loading!)
# =========================================================================
def build_enterable_cafe(col):
    # Open-doorway tropical beach cafe (6.4m wide x 4.4m deep, doorway facing +Y toward promenade!)
    m_wall = MATS["AC_HouseWall"]
    m_wood_d = MATS["AC_WoodDark"]
    m_wood_w = MATS["AC_WoodWarm"]
    m_wood_l = MATS["AC_WoodLight"]
    m_mint = MATS["AC_MintTeal"]
    m_pink = MATS["AC_PinkBerry"]
    m_cream = MATS["AC_Cream"]
    m_glass = MATS["AC_Glass"]
    m_glow = MATS["AC_LanternGlow"]

    base_parts = [
        # Warm dark-teak wooden deck & interior floor
        add_box("FloorDeck", (6.8, 4.8, 0.16), m_wood_d, col, loc=(0, 0, 0.08), bevel=0.03),
        add_box("FloorInner", (6.2, 4.2, 0.06), m_wood_l, col, loc=(0, 0, 0.17), bevel=0.02),
        # Back wall (south side, y = -2.05) + Side walls (x = -3.05, +3.05)
        add_box("BackWall", (6.2, 0.22, 2.55), m_wall, col, loc=(0, -2.05, 1.44), bevel=0.03),
        add_box("LeftWall", (0.22, 4.2, 2.55), m_wall, col, loc=(-3.05, 0, 1.44), bevel=0.03),
        add_box("RightWall", (0.22, 4.2, 2.55), m_wall, col, loc=(3.05, 0, 1.44), bevel=0.03),
        # Front wall flanking wide 2.4m OPEN DOORWAY (north side facing promenade, y = +2.05!)
        add_box("FrontWallL", (1.85, 0.22, 2.55), m_wall, col, loc=(-2.12, 2.05, 1.44), bevel=0.03),
        add_box("FrontWallR", (1.85, 0.22, 2.55), m_wall, col, loc=(2.12, 2.05, 1.44), bevel=0.03),
        add_box("DoorArch", (2.55, 0.26, 0.34), m_wood_w, col, loc=(0, 2.05, 2.54), bevel=0.03),
        # Cute front windows with flower boxes
        add_box("WinL", (1.10, 0.10, 0.95), m_glass, col, loc=(-2.12, 2.14, 1.55), bevel=0.02),
        add_box("WinR", (1.10, 0.10, 0.95), m_glass, col, loc=(2.12, 2.14, 1.55), bevel=0.02),
        add_box("WinBoxL", (1.20, 0.28, 0.22), m_wood_w, col, loc=(-2.12, 2.26, 0.96), bevel=0.02),
        add_box("WinBoxR", (1.20, 0.28, 0.22), m_wood_w, col, loc=(2.12, 2.26, 0.96), bevel=0.02),
        add_sphere("WinFlwL", 0.16, MATS["AC_FlowerPink"], col, loc=(-2.30, 2.28, 1.15)),
        add_sphere("WinFlwL2", 0.15, MATS["AC_FlowerYellow"], col, loc=(-1.92, 2.28, 1.14)),
        add_sphere("WinFlwR", 0.16, MATS["AC_FlowerPink"], col, loc=(1.94, 2.28, 1.15)),
        add_sphere("WinFlwR2", 0.15, MATS["AC_MintSoft"], col, loc=(2.30, 2.28, 1.14)),
        # DECORATED INTERIOR (visible seamlessly when roof fades!)
        # 1. Back turquoise & cream pastry counter + glass cake vitrine
        add_box("CounterBase", (3.6, 0.85, 0.95), m_mint, col, loc=(0, -1.25, 0.66), bevel=0.03),
        add_box("CounterTop", (3.8, 0.98, 0.10), m_cream, col, loc=(0, -1.25, 1.18), bevel=0.02),
        add_box("CakeCase", (1.35, 0.52, 0.42), m_glass, col, loc=(-0.75, -1.22, 1.44), bevel=0.02),
        add_cyl("CakePink", 0.18, 0.18, m_pink, col, loc=(-0.95, -1.22, 1.35)),
        add_cyl("CakeChoc", 0.16, 0.16, MATS["AC_Chocolate"], col, loc=(-0.48, -1.22, 1.34)),
        add_box("EspressoMachine", (0.58, 0.44, 0.50), MATS["AC_MetalSilver"], col, loc=(0.95, -1.32, 1.48), bevel=0.03),
        # 2. Cozy indoor rug
        add_box("InnerRug", (2.6, 1.8, 0.03), MATS["AC_PinkSoft"], col, loc=(0, 0.55, 0.21), bevel=0.015),
        # 3. Two indoor cafe tables with stools & warm lanterns!
        add_cyl("InTableL", 0.48, 0.08, m_wood_w, col, loc=(-1.95, 0.45, 0.86)),
        add_cyl("InLegL", 0.08, 0.68, m_wood_d, col, loc=(-1.95, 0.45, 0.52)),
        add_sphere("InLampL", 0.11, m_glow, col, loc=(-1.95, 0.45, 1.00)),
        add_cyl("InTableR", 0.48, 0.08, m_wood_w, col, loc=(1.95, 0.45, 0.86)),
        add_cyl("InLegR", 0.08, 0.68, m_wood_d, col, loc=(1.95, 0.45, 0.52)),
        add_sphere("InLampR", 0.11, m_glow, col, loc=(1.95, 0.45, 1.00)),
        # Glowing wall fairy lights inside
        add_sphere("WallLight1", 0.10, m_glow, col, loc=(-1.5, -1.86, 2.25)),
        add_sphere("WallLight2", 0.10, m_glow, col, loc=(0.0, -1.86, 2.30)),
        add_sphere("WallLight3", 0.10, m_glow, col, loc=(1.5, -1.86, 2.25)),
    ]
    cafe_base = join_around_pivot(base_parts, "CoastalCafe_Base", (0, 0, 0), col)
    export_flat_upright([cafe_base], "CoastalCafe_Base")

    # Separate Roof + Striped Entry Awning (uses OrlaACNHCozyFade when player enters!)
    roof_parts = [
        add_box("RoofSlab", (7.1, 5.0, 0.24), MATS["AC_HouseRoof"], col, loc=(0, 0, 2.80), bevel=0.05),
        add_box("RoofPitchA", (6.8, 2.75, 0.24), MATS["AC_HouseRoof"], col, loc=(0, -1.10, 3.28), rot=(0.34, 0, 0), bevel=0.04),
        add_box("RoofPitchB", (6.8, 2.75, 0.24), MATS["AC_HouseRoof"], col, loc=(0, 1.10, 3.28), rot=(-0.34, 0, 0), bevel=0.04),
        add_box("RoofRidge", (6.9, 0.28, 0.22), m_cream, col, loc=(0, 0, 3.74), bevel=0.04),
        # Cute front striped awning over doorway
        add_box("AwningBase", (3.2, 0.95, 0.12), m_pink, col, loc=(0, 2.52, 2.60), rot=(-0.28, 0, 0), bevel=0.03),
        add_box("AwningStripe1", (0.55, 0.98, 0.14), m_cream, col, loc=(-0.95, 2.52, 2.61), rot=(-0.28, 0, 0), bevel=0.02),
        add_box("AwningStripe2", (0.55, 0.98, 0.14), m_cream, col, loc=(0.0, 2.52, 2.61), rot=(-0.28, 0, 0), bevel=0.02),
        add_box("AwningStripe3", (0.55, 0.98, 0.14), m_cream, col, loc=(0.95, 2.52, 2.61), rot=(-0.28, 0, 0), bevel=0.02),
    ]
    cafe_roof = join_around_pivot(roof_parts, "CoastalCafe_RoofFade", (0, 0, 0), col)
    export_flat_upright([cafe_roof], "CoastalCafe_RoofFade")
    return [cafe_base, cafe_roof]


# =========================================================================
# 3. COLORFUL LOW COASTAL TOWN FACADES (Background Horizon Depth!)
# =========================================================================
def build_coastal_facade_row(col):
    specs = [
        (-6.2, 4.4, 3.2, 4.6, MATS["AC_HouseWall"], MATS["AC_Coral"], MATS["AC_MintTeal"]),
        (-1.6, 4.2, 3.0, 5.4, MATS["AC_SunnyYellow"], MATS["AC_HouseRoof"], MATS["AC_PinkBerry"]),
        (2.8,  4.4, 3.2, 4.2, MATS["AC_MintSoft"], MATS["AC_SkyBlue"], MATS["AC_Coral"]),
        (7.4,  4.6, 3.2, 5.0, MATS["AC_PinkSoft"], MATS["AC_HouseRoof"], MATS["AC_SunnyYellow"]),
    ]
    parts = []
    for i, (bx, bw, bd, bh, m_w, m_r, m_a) in enumerate(specs):
        parts.append(add_box(f"Bldg_{i}", (bw, bd, bh), m_w, col, loc=(bx, 0, bh * 0.5), bevel=0.06))
        parts.append(add_box(f"Roof_{i}", (bw + 0.35, bd + 0.35, 0.35), m_r, col, loc=(bx, 0, bh + 0.15), bevel=0.05))
        # Upper balcony + railing + arched windows facing promenade (+Y)
        parts.append(add_box(f"Balc_{i}", (bw * 0.72, 0.65, 0.14), MATS["AC_WoodWarm"], col, loc=(bx, bd * 0.5 + 0.25, bh * 0.55), bevel=0.02))
        parts.append(add_box(f"Rail_{i}", (bw * 0.72, 0.08, 0.42), MATS["AC_Cream"], col, loc=(bx, bd * 0.5 + 0.54, bh * 0.55 + 0.24), bevel=0.015))
        parts.append(add_box(f"Awning_{i}", (bw * 0.78, 0.72, 0.14), m_a, col, loc=(bx, bd * 0.5 + 0.28, 2.25), rot=(-0.26, 0, 0), bevel=0.02))
        for wx in (-bw * 0.24, bw * 0.24):
            parts.append(add_box(f"Win_{i}_{wx}", (0.72, 0.10, 0.92), MATS["AC_Glass"], col, loc=(bx + wx, bd * 0.5 + 0.03, bh * 0.76), bevel=0.02))
            parts.append(add_box(f"Door_{i}_{wx}", (0.76, 0.10, 1.20), MATS["AC_WoodDark"], col, loc=(bx + wx, bd * 0.5 + 0.03, 0.95), bevel=0.02))

    row = join_around_pivot(parts, "CoastalFacadeRow", (0, 0, 0), col)
    export_flat_upright([row], "CoastalFacadeRow")
    return row


# =========================================================================
# 4. MAGICAL COASTAL PROPS (Glowing Mushrooms, Floating Lantern, Flower Lamp)
# =========================================================================
def build_magical_props(col):
    # A. Glowing Coastal Mushroom Cluster
    mush_parts = [
        add_cyl("Stem1", 0.06, 0.28, MATS["AC_Cream"], col, loc=(0, 0, 0.14), r_top=0.045),
        add_sphere("Cap1", 0.20, MATS["AC_PinkBerry"], col, loc=(0, 0, 0.30), scale=(1.1, 1.1, 0.62)),
        add_sphere("GlowSpot1", 0.05, MATS["AC_LanternGlow"], col, loc=(0.08, -0.10, 0.36)),
        add_sphere("GlowSpot2", 0.045, MATS["AC_LanternGlow"], col, loc=(-0.10, 0.04, 0.35)),
        add_cyl("Stem2", 0.042, 0.18, MATS["AC_Cream"], col, loc=(0.24, -0.10, 0.09), rot=(0, 0.2, 0), r_top=0.03),
        add_sphere("Cap2", 0.13, MATS["AC_MintSoft"], col, loc=(0.26, -0.10, 0.20), scale=(1.05, 1.05, 0.65)),
        add_cyl("Stem3", 0.035, 0.14, MATS["AC_Cream"], col, loc=(-0.20, -0.14, 0.07), rot=(0.15, -0.18, 0), r_top=0.025),
        add_sphere("Cap3", 0.10, MATS["AC_LanternGlow"], col, loc=(-0.22, -0.15, 0.16), scale=(1.05, 1.05, 0.68)),
    ]
    mush = join_around_pivot(mush_parts, "GlowingMushroomCluster", (0, 0, 0), col)
    export_flat_upright([mush], "GlowingMushroomCluster")

    # B. Floating Magical Beach Lantern
    lant_parts = [
        add_cyl("LantCapTop", 0.14, 0.05, MATS["AC_Gold"], col, loc=(0, 0, 0.42)),
        add_cyl("LantBody", 0.13, 0.32, MATS["AC_LanternGlow"], col, loc=(0, 0, 0.24), r_top=0.16),
        add_cyl("LantCapBot", 0.11, 0.05, MATS["AC_Gold"], col, loc=(0, 0, 0.07)),
        add_sphere("LantTassel", 0.045, MATS["AC_PinkBerry"], col, loc=(0, 0, -0.02), scale=(0.8, 0.8, 1.6)),
    ]
    lant = join_around_pivot(lant_parts, "FloatingLantern", (0, 0, 0.22), col)
    export_flat_upright([lant], "FloatingLantern")

    # C. Flower-Wrapped Coastal Lamp Post
    fl_parts = [
        add_cyl("FPostBase", 0.18, 0.36, MATS["AC_WoodDark"], col, loc=(0, 0, 0.18), r_top=0.12),
        add_cyl("FPostPole", 0.07, 3.15, MATS["AC_MetalDark"], col, loc=(0, 0, 1.75), r_top=0.055),
        add_box("FPostArm", (0.72, 0.08, 0.08), MATS["AC_MetalDark"], col, loc=(0.30, 0, 3.22), bevel=0.015),
        add_sphere("FLampBulb", 0.20, MATS["AC_BulbWarm"], col, loc=(0.60, 0, 3.00)),
        add_cyl("FLampCap", 0.25, 0.10, MATS["AC_MintTeal"], col, loc=(0.60, 0, 3.20), r_top=0.08),
    ]
    # Spiral tropical flower vines hugging the lamp post!
    for i in range(7):
        ang = i * 1.15
        z = 0.55 + i * 0.38
        vx = math.cos(ang) * 0.11
        vy = math.sin(ang) * 0.11
        fl_parts.append(add_sphere(f"VineLeaf_{i}", 0.095, MATS["AC_BushGreen"], col, loc=(vx, vy, z)))
        fl_col = MATS["AC_FlowerPink"] if i % 2 == 0 else MATS["AC_FlowerYellow"]
        fl_parts.append(add_sphere(f"VineFlw_{i}", 0.075, fl_col, col, loc=(vx * 1.35, vy * 1.35, z + 0.05)))
    flamp = join_around_pivot(fl_parts, "FlowerLampPost", (0, 0, 0), col)
    export_flat_upright([flamp], "FlowerLampPost")

    return [mush, lant, flamp]


def main():
    clear_scene()
    init_mats()
    col = bpy.data.collections.new("BaiaMagicalCoast")
    bpy.context.scene.collection.children.link(col)

    build_fairy_waitress(col)
    build_enterable_cafe(col)
    build_coastal_facade_row(col)
    build_magical_props(col)

    bpy.ops.wm.save_as_mainfile(filepath=BLEND_OUT)
    print("[Magical Coastal Pack Complete]")


if __name__ == "__main__":
    main()
