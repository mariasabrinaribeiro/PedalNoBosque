"""
Baia do Avesso — Modular Harmonized Archetypes + Calopsita + Water Bowl + Orchard Tree (Blender 5.3)
All characters share 1 unified slender-cozy coastal silhouette & harmonized saturation palette
with GPU wind-swaying long hair materials (AC_HairWind_*), exported 100% UPRIGHT for Unity!
"""

import bpy
import bmesh
import math
import os
from mathutils import Vector

SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
MODELS_DIR = os.path.normpath(os.path.join(SCRIPT_DIR, "..", "Models"))
BLEND_OUT = os.path.join(SCRIPT_DIR, "BaiaDoAvesso_ModularArchetypes.blend")
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
        ("AC_SkinLight", "#F4C29F"),
        ("AC_SkinWarm", "#D29269"),
        ("AC_SkinTan", "#DF9F73"),
        ("AC_CheekBlush", "#F694AA"),
        ("AC_EyeDark", "#251D29"),
        ("AC_EyeShine", "#FFFFFF"),
        ("AC_PinkBerry", "#F26392"),
        ("AC_PinkSoft", "#F6A8C0"),
        ("AC_MintTeal", "#38C8BE"),
        ("AC_MintSoft", "#98E8DF"),
        ("AC_Lavender", "#B39CD8"),
        ("AC_Cream", "#FAF2E4"),
        ("AC_Coral", "#DE8676"),
        ("AC_SkyBlue", "#6CC2E8"),
        ("AC_SunnyYellow", "#F4D678"),
        ("AC_Denim", "#567EA8"),
        ("AC_WoodDark", "#6E442A"),
        ("AC_WoodWarm", "#9E6945"),
        ("AC_WoodLight", "#CFA074"),
        ("AC_StrawHat", "#ECCE86"),
        ("AC_Gold", "#EFC45A"),
        ("AC_MetalDark", "#3F4959"),
        ("AC_ShoeDark", "#4E3A4C"),
        ("AC_BushGreen", "#5A9B68"),
        ("AC_BushLight", "#7DB87E"),
        ("AC_Orange", "#EF9B48"),
        ("AC_Strawberry", "#F26880"),
        ("AC_FlowerPink", "#FF7EA8"),
        ("AC_FlowerYellow", "#FFE066"),
        ("AC_OceanShallow", "#38D4D2"),
        # GPU Vertex-Animated Breeze Hair Materials (unified with OrlaACNHHairWind.shader!)
        ("AC_HairWind_Brown", "#6B442A"),
        ("AC_HairWind_Blonde", "#EBC772"),
        ("AC_HairWind_Dark", "#2E2736"),
        ("AC_HairWind_Silver", "#D6D8E0"),
        ("AC_HairWind_Auburn", "#B4563C"),
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
    print(f"[FBX Modular Upright] {out_path}")


# =========================================================================
# SHARED MODULAR CHARACTER BASE (1 unified coastal proportion, swappable outfits + GPU wind hair!)
# =========================================================================
def build_modular_character(
    col, filename, skin_key, hair_key, top_key, bottom_key, shoe_key,
    skirt_style="none", hair_style="long_wave", extra_fn=None
):
    m_skin = MATS[skin_key]
    m_hair = MATS[hair_key]
    m_top = MATS[top_key]
    m_bot = MATS[bottom_key]
    m_shoe = MATS[shoe_key]

    parts = [
        # Slender graceful coastal legs + shoes
        add_cyl("LegL", 0.056, 0.36, m_bot if skirt_style == "pants" else m_skin, col, loc=(-0.095, 0, 0.22)),
        add_cyl("LegR", 0.056, 0.36, m_bot if skirt_style == "pants" else m_skin, col, loc=(0.095, 0, 0.22)),
        add_sphere("ShoeL", 0.072, m_shoe, col, loc=(-0.095, -0.03, 0.05), scale=(0.95, 1.35, 0.65)),
        add_sphere("ShoeR", 0.072, m_shoe, col, loc=(0.095, -0.03, 0.05), scale=(0.95, 1.35, 0.65)),
        # Torso & graceful arms
        add_cyl("Torso", 0.16, 0.40, m_top, col, loc=(0, 0, 0.72), r_top=0.14),
        add_cyl("ArmL", 0.048, 0.34, m_skin, col, loc=(-0.21, 0, 0.68), rot=(0, 0, 0.12)),
        add_cyl("ArmR", 0.048, 0.34, m_skin, col, loc=(0.21, 0, 0.68), rot=(0, 0, -0.12)),
        add_sphere("SleeveL", 0.074, m_top, col, loc=(-0.20, 0, 0.84)),
        add_sphere("SleeveR", 0.074, m_top, col, loc=(0.20, 0, 0.84)),
        # Harmonized original head (proportional, NOT giant box head!)
        add_sphere("Head", 0.215, m_skin, col, loc=(0, 0, 1.14), scale=(0.98, 0.95, 1.04), u=16, v=12),
        add_sphere("EyeL", 0.033, MATS["AC_EyeDark"], col, loc=(-0.078, -0.188, 1.15), scale=(0.9, 0.45, 1.15)),
        add_sphere("EyeR", 0.033, MATS["AC_EyeDark"], col, loc=(0.078, -0.188, 1.15), scale=(0.9, 0.45, 1.15)),
        add_sphere("ShineL", 0.012, MATS["AC_EyeShine"], col, loc=(-0.088, -0.204, 1.165)),
        add_sphere("ShineR", 0.012, MATS["AC_EyeShine"], col, loc=(0.068, -0.204, 1.165)),
        add_sphere("CheekL", 0.040, MATS["AC_CheekBlush"], col, loc=(-0.115, -0.172, 1.10), scale=(1.1, 0.4, 0.75)),
        add_sphere("CheekR", 0.040, MATS["AC_CheekBlush"], col, loc=(0.115, -0.172, 1.10), scale=(1.1, 0.4, 0.75)),
    ]

    # Modular skirt / pants silhouette
    if skirt_style == "long_full":
        parts.append(add_cyl("SkirtFull", 0.34, 0.56, m_bot, col, loc=(0, 0, 0.34), r_top=0.15))
    elif skirt_style == "gown":
        parts.append(add_cyl("Gown", 0.40, 0.60, m_bot, col, loc=(0, 0, 0.34), r_top=0.14))
    elif skirt_style == "bombacha":
        parts.append(add_cyl("BombL", 0.115, 0.38, m_bot, col, loc=(-0.10, 0, 0.32), r_top=0.095))
        parts.append(add_cyl("BombR", 0.115, 0.38, m_bot, col, loc=(0.10, 0, 0.32), r_top=0.095))
        parts.append(add_cyl("Pelvis", 0.175, 0.16, m_bot, col, loc=(0, 0, 0.48), r_top=0.16))
    elif skirt_style == "yukata":
        parts.append(add_cyl("YukataBot", 0.22, 0.54, m_bot, col, loc=(0, 0, 0.35), r_top=0.16))
        parts.append(add_cyl("Obi", 0.185, 0.13, MATS["AC_PinkBerry"], col, loc=(0, 0, 0.62)))
    elif skirt_style == "shorts":
        parts.append(add_cyl("Shorts", 0.18, 0.20, m_bot, col, loc=(0, 0, 0.44), r_top=0.16))
    else:
        parts.append(add_cyl("PantsHip", 0.175, 0.18, m_bot, col, loc=(0, 0, 0.46), r_top=0.16))

    # GPU Wind-Swaying Long Hair Styles!
    parts.append(add_sphere("HairCap", 0.228, m_hair, col, loc=(0, 0.03, 1.20), scale=(1.03, 1.02, 0.95)))
    if hair_style == "long_wave":
        parts.append(add_cyl("HairFlowBack", 0.22, 0.54, m_hair, col, loc=(0, 0.16, 0.94), r_top=0.15))
        parts.append(add_sphere("HairSideL", 0.11, m_hair, col, loc=(-0.17, 0.02, 0.98), scale=(0.65, 0.85, 1.75)))
        parts.append(add_sphere("HairSideR", 0.11, m_hair, col, loc=(0.17, 0.02, 0.98), scale=(0.65, 0.85, 1.75)))
    elif hair_style == "high_pony":
        parts.append(add_cyl("HighPony", 0.10, 0.52, m_hair, col, loc=(0, 0.26, 1.04), rot=(0.24, 0, 0), r_top=0.06))
        parts.append(add_sphere("PonyTie", 0.055, MATS["AC_PinkBerry"], col, loc=(0, 0.21, 1.26)))
    elif hair_style == "braid":
        for bi in range(4):
            parts.append(add_sphere(f"Braid_{bi}", 0.082 - bi * 0.01, m_hair, col, loc=(0.13, 0.12 - bi * 0.02, 1.06 - bi * 0.12)))
    elif hair_style == "voluminous":
        parts.append(add_sphere("VolMain", 0.25, m_hair, col, loc=(0, 0.18, 0.96), scale=(1.22, 0.90, 1.45)))
        parts.append(add_sphere("VolBang", 0.14, m_hair, col, loc=(-0.12, -0.14, 1.28), scale=(1.2, 0.8, 0.7)))

    if extra_fn is not None:
        extra_fn(parts, col)

    mesh_obj = join_around_pivot(parts, filename, (0, 0, 0), col)
    export_flat_upright([mesh_obj], filename)
    return mesh_obj


def build_all_archetypes(col):
    # 1. Camponesa (Colhedora na Horta!)
    def extra_camponesa(p, c):
        p.append(add_box("Apron", (0.26, 0.06, 0.34), MATS["AC_Cream"], c, loc=(0, -0.22, 0.48)))
        p.append(add_cyl("HatBrim", 0.36, 0.035, MATS["AC_StrawHat"], c, loc=(0, 0, 1.32)))
        p.append(add_sphere("HatTop", 0.18, MATS["AC_StrawHat"], c, loc=(0, 0, 1.38), scale=(1.0, 1.0, 0.65)))
        p.append(add_cyl("Basket", 0.14, 0.16, MATS["AC_WoodLight"], c, loc=(0.28, -0.14, 0.62), r_top=0.17))
        p.append(add_sphere("BaskOrange", 0.065, MATS["AC_Orange"], c, loc=(0.25, -0.14, 0.72)))
        p.append(add_sphere("BaskLemon", 0.060, MATS["AC_SunnyYellow"], c, loc=(0.32, -0.12, 0.72)))
    build_modular_character(col, "Char_Camponesa", "AC_SkinLight", "AC_HairWind_Auburn", "AC_Cream", "AC_MintTeal", "AC_WoodWarm", "long_full", "braid", extra_camponesa)

    # 2. Gaúcho (Traje do Sul!)
    def extra_gaucho(p, c):
        p.append(add_sphere("Lenco", 0.12, MATS["AC_Coral"], c, loc=(0, -0.11, 0.90), scale=(1.35, 0.7, 0.95)))
        p.append(add_cyl("HatGauchoBrim", 0.35, 0.03, MATS["AC_WoodDark"], c, loc=(0, 0, 1.32)))
        p.append(add_cyl("HatGauchoTop", 0.16, 0.11, MATS["AC_WoodDark"], c, loc=(0, 0, 1.38)))
        p.append(add_cyl("BeltGaucho", 0.17, 0.07, MATS["AC_WoodWarm"], c, loc=(0, 0, 0.56)))
    build_modular_character(col, "Char_Gaucho", "AC_SkinWarm", "AC_HairWind_Brown", "AC_Cream", "AC_WoodLight", "AC_WoodDark", "bombacha", "long_wave", extra_gaucho)

    # 3. Espiã (Macacão escuro, óculos escuros, rabo de cavalo alto!)
    def extra_espia(p, c):
        p.append(add_box("Shades", (0.24, 0.05, 0.07), MATS["AC_MetalDark"], c, loc=(0, -0.21, 1.15), bevel=0.015))
        p.append(add_cyl("SpyBelt", 0.165, 0.055, MATS["AC_Gold"], c, loc=(0, 0, 0.56)))
    build_modular_character(col, "Char_Espia", "AC_SkinTan", "AC_HairWind_Dark", "AC_MetalDark", "AC_MetalDark", "AC_ShoeDark", "pants", "high_pony", extra_espia)

    # 4. Estilo Barbie (Conjunto rosa vibrante + loiro volumoso!)
    def extra_barbie(p, c):
        p.append(add_sphere("PinkShades", 0.045, MATS["AC_PinkSoft"], c, loc=(-0.08, -0.20, 1.24), scale=(1.2, 0.4, 0.8)))
        p.append(add_sphere("PinkShadesR", 0.045, MATS["AC_PinkSoft"], c, loc=(0.08, -0.20, 1.24), scale=(1.2, 0.4, 0.8)))
        p.append(add_box("Purse", (0.14, 0.06, 0.12), MATS["AC_Cream"], c, loc=(0.24, -0.08, 0.52)))
    build_modular_character(col, "Char_Barbie", "AC_SkinLight", "AC_HairWind_Blonde", "AC_PinkBerry", "AC_PinkBerry", "AC_PinkSoft", "shorts", "voluminous", extra_barbie)

    # 5. Princesa (Vestido armado + tiara dourada + cabelo ondulado!)
    def extra_princesa(p, c):
        p.append(add_cyl("Tiara", 0.13, 0.06, MATS["AC_Gold"], c, loc=(0, -0.04, 1.34), r_top=0.11))
        p.append(add_sphere("TiaraGem", 0.036, MATS["AC_PinkBerry"], c, loc=(0, -0.15, 1.36)))
        p.append(add_sphere("BowBack", 0.11, MATS["AC_PinkSoft"], c, loc=(0, 0.18, 0.58), scale=(1.4, 0.5, 0.9)))
    build_modular_character(col, "Char_Princesa", "AC_SkinLight", "AC_HairWind_Blonde", "AC_Lavender", "AC_Lavender", "AC_Gold", "gown", "long_wave", extra_princesa)

    # 6. Vovó (Vestido floral + xale nos ombros + cabelo grisalho!)
    def extra_vovo(p, c):
        p.append(add_cyl("Shawl", 0.22, 0.16, MATS["AC_PinkSoft"], c, loc=(0, 0, 0.86), r_top=0.16))
        p.append(add_sphere("GlassesL", 0.042, MATS["AC_Gold"], c, loc=(-0.075, -0.21, 1.15), scale=(1.0, 0.3, 1.0)))
        p.append(add_sphere("GlassesR", 0.042, MATS["AC_Gold"], c, loc=(0.075, -0.21, 1.15), scale=(1.0, 0.3, 1.0)))
    build_modular_character(col, "Char_Vovo", "AC_SkinLight", "AC_HairWind_Silver", "AC_MintSoft", "AC_MintSoft", "AC_WoodWarm", "long_full", "long_wave", extra_vovo)

    # 7. Lutador Kung Fu (Changshan tradicional + faixa + cabelo amarrado!)
    def extra_kungfu(p, c):
        p.append(add_cyl("Sash", 0.17, 0.08, MATS["AC_SunnyYellow"], c, loc=(0, 0, 0.56)))
        p.append(add_box("CollarTrim", (0.04, 0.04, 0.32), MATS["AC_Gold"], c, loc=(0, -0.15, 0.74)))
    build_modular_character(col, "Char_KungFu", "AC_SkinTan", "AC_HairWind_Dark", "AC_Coral", "AC_MetalDark", "AC_ShoeDark", "pants", "high_pony", extra_kungfu)

    # 8. Estilo Japonês (Yukata pastel + grampos de flor!)
    def extra_japones(p, c):
        p.append(add_sphere("Kanzashi1", 0.055, MATS["AC_FlowerPink"], c, loc=(-0.18, -0.05, 1.26)))
        p.append(add_sphere("Kanzashi2", 0.048, MATS["AC_FlowerYellow"], c, loc=(-0.21, -0.02, 1.20)))
        p.append(add_box("ObiBow", (0.20, 0.08, 0.16), MATS["AC_PinkBerry"], c, loc=(0, 0.20, 0.62)))
    build_modular_character(col, "Char_Japones", "AC_SkinLight", "AC_HairWind_Dark", "AC_SkyBlue", "AC_SkyBlue", "AC_WoodWarm", "yukata", "long_wave", extra_japones)

    # 9. Praia / Futebol (Uniforme de futebol praiano + cabelo longo!)
    def extra_futebol(p, c):
        p.append(add_box("Stripe", (0.26, 0.06, 0.10), MATS["AC_SunnyYellow"], c, loc=(0, -0.15, 0.74)))
    build_modular_character(col, "Char_FutebolPraia", "AC_SkinWarm", "AC_HairWind_Brown", "AC_MintTeal", "AC_Denim", "AC_Cream", "shorts", "high_pony", extra_futebol)

    # 10. Futebol Americano (Ombreiras estilizadas + camisa numerada + capacete!)
    def extra_fa(p, c):
        p.append(add_box("ShoulderPads", (0.48, 0.24, 0.16), MATS["AC_SkyBlue"], c, loc=(0, 0, 0.88), bevel=0.04))
        p.append(add_sphere("Helmet", 0.24, MATS["AC_Cream"], c, loc=(0, 0.02, 1.18), scale=(1.02, 1.05, 0.98)))
        p.append(add_box("Facemask", (0.18, 0.06, 0.12), MATS["AC_MetalDark"], c, loc=(0, -0.23, 1.10), bevel=0.015))
    build_modular_character(col, "Char_FutebolAmericano", "AC_SkinTan", "AC_HairWind_Brown", "AC_SkyBlue", "AC_Cream", "AC_ShoeDark", "shorts", "long_wave", extra_fa)


# =========================================================================
# PET CALOPSITA (Cockatiel with articulated wings!), WATER BOWL & ORCHARD TREE
# =========================================================================
def build_calopsita(col):
    body_p = [
        add_sphere("CalopBody", 0.11, MATS["AC_Cream"], col, loc=(0, 0, 0.16), scale=(0.82, 1.35, 0.90)),
        add_sphere("CalopHead", 0.085, MATS["AC_SunnyYellow"], col, loc=(0, -0.09, 0.27)),
        add_sphere("CalopCheekL", 0.028, MATS["AC_Orange"], col, loc=(-0.068, -0.11, 0.26)),
        add_sphere("CalopCheekR", 0.028, MATS["AC_Orange"], col, loc=(0.068, -0.11, 0.26)),
        add_cyl("CalopBeak", 0.024, 0.05, MATS["AC_WoodLight"], col, loc=(0, -0.17, 0.255), rot=(1.35, 0, 0), r_top=0.005),
        # Cute upright yellow crest feathers!
        add_cyl("Crest1", 0.015, 0.11, MATS["AC_SunnyYellow"], col, loc=(0, -0.07, 0.38), rot=(-0.35, 0, 0), r_top=0.004),
        add_cyl("Crest2", 0.012, 0.09, MATS["AC_SunnyYellow"], col, loc=(0, -0.04, 0.37), rot=(-0.55, 0, 0), r_top=0.004),
        add_cyl("TailFeather", 0.032, 0.22, MATS["AC_Cream"], col, loc=(0, 0.18, 0.12), rot=(-1.25, 0, 0), r_top=0.012),
    ]
    body = join_around_pivot(body_p, "Calop_Body", (0, 0, 0.16), col)
    wl = join_around_pivot([add_sphere("WL", 0.09, MATS["AC_SunnyYellow"], col, loc=(-0.12, 0.02, 0.18), scale=(1.4, 0.8, 0.22))], "Calop_WingL", (-0.06, 0.0, 0.18), col)
    wr = join_around_pivot([add_sphere("WR", 0.09, MATS["AC_SunnyYellow"], col, loc=(0.12, 0.02, 0.18), scale=(1.4, 0.8, 0.22))], "Calop_WingR", (0.06, 0.0, 0.18), col)
    export_flat_upright([body, wl, wr], "Pet_Calopsita")


def build_water_bowl(col):
    base_p = [
        add_cyl("BowlOuter", 0.28, 0.16, MATS["AC_Cream"], col, loc=(0, 0, 0.08), r_top=0.24),
        add_cyl("BowlStripe", 0.285, 0.045, MATS["AC_MintTeal"], col, loc=(0, 0, 0.08), r_top=0.275),
        add_sphere("PawPrint", 0.036, MATS["AC_PinkBerry"], col, loc=(0, -0.27, 0.08)),
    ]
    base = join_around_pivot(base_p, "WaterBowl_Base", (0, 0, 0), col)
    liq = join_around_pivot([add_cyl("LiquidDisc", 0.21, 0.03, MATS["AC_OceanShallow"], col, loc=(0, 0, 0.15))], "WaterBowl_Liquid", (0, 0, 0.15), col)
    export_flat_upright([base, liq], "PetWaterBowl")


def build_orchard_tree(col):
    parts = [
        add_cyl("Trunk", 0.22, 1.55, MATS["AC_WoodWarm"], col, loc=(0, 0, 0.78), r_top=0.14),
        add_sphere("Canopy1", 0.95, MATS["AC_BushGreen"], col, loc=(0, 0, 2.05), scale=(1.15, 1.10, 0.92)),
        add_sphere("Canopy2", 0.68, MATS["AC_BushLight"], col, loc=(-0.55, -0.25, 1.85)),
        add_sphere("Canopy3", 0.64, MATS["AC_BushLight"], col, loc=(0.52, 0.20, 1.92)),
    ]
    # Bright oranges, lemons & apples hanging cleanly on the orchard tree!
    fruits = [
        (-0.65, -0.58, 1.95, MATS["AC_Orange"]), (0.55, -0.62, 2.05, MATS["AC_SunnyYellow"]),
        (0.05, -0.82, 1.78, MATS["AC_Strawberry"]), (-0.28, -0.72, 2.38, MATS["AC_Orange"]),
        (0.68, -0.20, 1.65, MATS["AC_Strawberry"]), (-0.52, 0.35, 2.22, MATS["AC_SunnyYellow"]),
    ]
    for i, (fx, fy, fz, fm) in enumerate(fruits):
        parts.append(add_sphere(f"Fruit_{i}", 0.11, fm, col, loc=(fx, fy, fz)))
    tree = join_around_pivot(parts, "OrchardFruitTree", (0, 0, 0), col)
    export_flat_upright([tree], "OrchardFruitTree")


def main():
    clear_scene()
    init_mats()
    col = bpy.data.collections.new("BaiaModularArchetypes")
    bpy.context.scene.collection.children.link(col)

    build_all_archetypes(col)
    build_calopsita(col)
    build_water_bowl(col)
    build_orchard_tree(col)

    bpy.ops.wm.save_as_mainfile(filepath=BLEND_OUT)
    print("[Modular Archetypes, Calopsita, Water Bowl & Orchard Tree Complete]")


if __name__ == "__main__":
    main()
