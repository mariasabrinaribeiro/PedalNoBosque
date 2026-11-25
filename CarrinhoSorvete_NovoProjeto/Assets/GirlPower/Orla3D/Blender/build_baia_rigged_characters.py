"""
Baia do Avesso — Articulated Chibi Protagonist & Detective Dog Pipoca (Blender 5.3)
Exports multi-part jointed FBX hierarchies (with exact shoulder/hip/neck/tail/eye pivots)
so Unity drives real arm/leg strides, scooter kicks, swimming strokes, blinking eyes,
wagging tail, and floppy ears!
"""

import bpy
import bmesh
import math
import os
from mathutils import Vector

SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
MODELS_DIR = os.path.normpath(os.path.join(SCRIPT_DIR, "..", "Models"))
BLEND_OUT = os.path.join(SCRIPT_DIR, "BaiaDoAvesso_ArticulatedChars.blend")
PREVIEW_OUT = os.path.join(SCRIPT_DIR, "baia_chars_preview.png")
os.makedirs(MODELS_DIR, exist_ok=True)


def clear_scene():
    bpy.ops.wm.read_factory_settings(use_empty=True)


def hex_rgba(h):
    h = h.lstrip("#")
    return (int(h[0:2], 16) / 255.0, int(h[2:4], 16) / 255.0, int(h[4:6], 16) / 255.0, 1.0)


MATS = {}


def make_mat(name, hx, rough=0.52):
    m = bpy.data.materials.new(name=name)
    m.use_nodes = True
    bsdf = m.node_tree.nodes.get("Principled BSDF")
    if bsdf:
        bsdf.inputs["Base Color"].default_value = hex_rgba(hx)
        bsdf.inputs["Roughness"].default_value = rough
    MATS[name] = m
    return m


def init_mats():
    for k, hx in [
        ("AC_ProtagShirt", "#F26392"),
        ("AC_PinkSoft", "#F6A8C0"),
        ("AC_Denim", "#567EA8"),
        ("AC_MintTeal", "#4CC6B8"),
        ("AC_Cream", "#FAF2E4"),
        ("AC_SkinLight", "#F4C29F"),
        ("AC_CheekBlush", "#F694AA"),
        ("AC_EyeDark", "#251D29"),
        ("AC_EyeShine", "#FFFFFF"),
        ("AC_HairBrown", "#6B442A"),
        ("AC_ShoeDark", "#4E3A4C"),
        ("AC_WoodLight", "#D8B085"),
        ("AC_WoodWarm", "#BA885E"),
        ("AC_WoodDark", "#7A4C2F"),
        ("AC_Gold", "#E8BE62"),
        ("AC_Coral", "#D68E7E"),
    ]:
        make_mat(k, hx)


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
    """Joins mesh parts into a single mesh whose origin sits cleanly at pivot_world."""
    bpy.ops.object.select_all(action="DESELECT")
    for p in parts:
        p.select_set(True)
    bpy.context.view_layer.objects.active = parts[0]
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    if len(parts) > 1:
        bpy.ops.object.join()
    obj = bpy.context.view_layer.objects.active
    obj.name = name
    # Shift vertices so origin sits at pivot_world
    px, py, pz = pivot_world
    for v in obj.data.vertices:
        v.co.x -= px
        v.co.y -= py
        v.co.z -= pz
    obj.location = pivot_world
    return obj


def export_flat_parts(objs, filename):
    """Exports flat sibling meshes with bake_space_transform=True so Unity imports every joint
    100% UPRIGHT (Y-up, zero -90 X tilt!) with exact pivot origins and localRotation = (0,0,0)!"""
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
    print(f"[FBX Flat Articulated Upright] {out_path}")


# =========================================================================
# 1. ARTICULATED PROTAGONIST (Swinging Arms/Legs, Blinking Eyes, Ponytail!)
# =========================================================================
def build_protagonist(col):
    exported = []

    # Torso (Berry Pink shirt + Denim shorts + Teal crossbody ice-cream satchel!)
    torso_parts = [
        add_cyl("T_Shirt", 0.21, 0.42, MATS["AC_ProtagShirt"], col, loc=(0, 0, 0.60), r_top=0.17),
        add_sphere("T_Collar", 0.14, MATS["AC_Cream"], col, loc=(0, 0, 0.80), scale=(1.05, 1.0, 0.35)),
        add_cyl("T_Shorts", 0.215, 0.17, MATS["AC_Denim"], col, loc=(0, 0, 0.38), r_top=0.20),
        add_sphere("T_Backpack", 0.16, MATS["AC_MintTeal"], col, loc=(0, 0.20, 0.60), scale=(1.05, 0.68, 1.15)),
        add_sphere("T_PackButton", 0.045, MATS["AC_Gold"], col, loc=(0, 0.30, 0.62)),
    ]
    torso = join_around_pivot(torso_parts, "Protag_Torso", (0, 0, 0.42), col)
    exported.append(torso)

    # Head (pivoted at neck z=0.82; facing -Y in Blender = -Z in Unity!)
    head_parts = [
        add_sphere("H_Skull", 0.29, MATS["AC_SkinLight"], col, loc=(0, 0, 1.08), scale=(1.02, 0.98, 0.98), u=18, v=14),
        add_sphere("H_CheekL", 0.055, MATS["AC_CheekBlush"], col, loc=(-0.15, -0.24, 1.02), scale=(1.2, 0.45, 0.85)),
        add_sphere("H_CheekR", 0.055, MATS["AC_CheekBlush"], col, loc=(0.15, -0.24, 1.02), scale=(1.2, 0.45, 0.85)),
        add_sphere("H_Nose", 0.032, MATS["AC_SkinLight"], col, loc=(0, -0.29, 1.04)),
        add_sphere("H_HairMain", 0.315, MATS["AC_HairBrown"], col, loc=(0, 0.04, 1.13), scale=(1.04, 1.02, 0.98), u=16, v=12),
        add_sphere("H_BangL", 0.13, MATS["AC_HairBrown"], col, loc=(-0.14, -0.21, 1.24), scale=(1.2, 0.75, 0.75)),
        add_sphere("H_BangR", 0.13, MATS["AC_HairBrown"], col, loc=(0.14, -0.21, 1.24), scale=(1.2, 0.75, 0.75)),
        add_sphere("H_Ponytail", 0.17, MATS["AC_HairBrown"], col, loc=(0, 0.34, 1.05), scale=(0.95, 1.05, 1.25)),
        add_sphere("H_HairRibbon", 0.09, MATS["AC_ProtagShirt"], col, loc=(0, 0.26, 1.20), scale=(1.35, 0.65, 0.75)),
    ]
    head = join_around_pivot(head_parts, "Protag_Head", (0, 0, 0.82), col)
    exported.append(head)

    # Separate Eyes so Unity can blink smoothly!
    for side, sx in [("L", -0.105), ("R", 0.105)]:
        ep = [
            add_sphere(f"EyeBall_{side}", 0.048, MATS["AC_EyeDark"], col, loc=(sx, -0.255, 1.08), scale=(0.88, 0.45, 1.12)),
            add_sphere(f"EyeShine_{side}", 0.018, MATS["AC_EyeShine"], col, loc=(sx - 0.015, -0.278, 1.10)),
        ]
        eye = join_around_pivot(ep, f"Protag_Eye{side}", (sx, -0.255, 1.08), col)
        exported.append(eye)

    # Articulated Arms (pivoted at shoulders z=0.74!)
    for side, sx in [("L", -0.25), ("R", 0.25)]:
        ap = [
            add_sphere(f"Sleeve_{side}", 0.088, MATS["AC_ProtagShirt"], col, loc=(sx, 0, 0.71)),
            add_cyl(f"ArmTube_{side}", 0.056, 0.24, MATS["AC_SkinLight"], col, loc=(sx, 0, 0.56)),
            add_sphere(f"Hand_{side}", 0.068, MATS["AC_SkinLight"], col, loc=(sx, 0, 0.43)),
        ]
        arm = join_around_pivot(ap, f"Protag_Arm{side}", (sx, 0, 0.74), col)
        exported.append(arm)

    # Articulated Legs (pivoted at hips z=0.34!)
    for side, sx in [("L", -0.115), ("R", 0.115)]:
        lp = [
            add_cyl(f"LegTube_{side}", 0.068, 0.24, MATS["AC_SkinLight"], col, loc=(sx, 0, 0.22)),
            add_sphere(f"Sock_{side}", 0.072, MATS["AC_Cream"], col, loc=(sx, 0, 0.10), scale=(1.0, 1.0, 0.7)),
            add_sphere(f"Shoe_{side}", 0.088, MATS["AC_ProtagShirt"], col, loc=(sx, -0.04, 0.055), scale=(0.95, 1.35, 0.68)),
        ]
        leg = join_around_pivot(lp, f"Protag_Leg{side}", (sx, 0, 0.34), col)
        exported.append(leg)

    export_flat_parts(exported, "Char_Protagonist")
    return exported


# =========================================================================
# 2. ARTICULATED PIPOCA (Wagging Tail, Floppy Ears, Galloping Legs!)
# =========================================================================
def build_pipoca(col):
    exported = []

    body_parts = [
        add_sphere("P_Torso", 0.22, MATS["AC_Cream"], col, loc=(0, 0.02, 0.28), scale=(0.92, 1.38, 0.90), u=16, v=12),
        add_sphere("P_SpotBack", 0.13, MATS["AC_WoodLight"], col, loc=(0.06, 0.08, 0.36), scale=(1.0, 1.2, 0.65)),
        add_cyl("P_Collar", 0.15, 0.055, MATS["AC_ProtagShirt"], col, loc=(0, -0.18, 0.34), rot=(math.radians(32), 0, 0)),
        add_sphere("P_Bell", 0.036, MATS["AC_Gold"], col, loc=(0, -0.28, 0.26)),
    ]
    body = join_around_pivot(body_parts, "Pipoca_Body", (0, 0, 0.26), col)
    exported.append(body)

    head_parts = [
        add_sphere("P_Skull", 0.20, MATS["AC_Cream"], col, loc=(0, -0.28, 0.48), scale=(1.04, 1.0, 0.95), u=16, v=12),
        add_sphere("P_Snout", 0.105, MATS["AC_Cream"], col, loc=(0, -0.44, 0.44), scale=(1.05, 0.95, 0.80)),
        add_sphere("P_Nose", 0.044, MATS["AC_EyeDark"], col, loc=(0, -0.53, 0.465), scale=(1.1, 0.8, 0.8)),
        add_sphere("P_EyeL", 0.036, MATS["AC_EyeDark"], col, loc=(-0.09, -0.43, 0.52)),
        add_sphere("P_EyeR", 0.036, MATS["AC_EyeDark"], col, loc=(0.09, -0.43, 0.52)),
        add_sphere("P_ShineL", 0.014, MATS["AC_EyeShine"], col, loc=(-0.10, -0.458, 0.535)),
        add_sphere("P_ShineR", 0.014, MATS["AC_EyeShine"], col, loc=(0.08, -0.458, 0.535)),
        add_sphere("P_Patch", 0.095, MATS["AC_WoodLight"], col, loc=(-0.09, -0.37, 0.53), scale=(0.9, 0.6, 0.9)),
    ]
    head = join_around_pivot(head_parts, "Pipoca_Head", (0, -0.22, 0.38), col)
    exported.append(head)

    for side, sx in [("L", -0.17), ("R", 0.17)]:
        ep = [add_sphere(f"EarMesh_{side}", 0.085, MATS["AC_WoodWarm"], col, loc=(sx * 1.15, -0.26, 0.46), scale=(0.55, 0.85, 1.45))]
        ear = join_around_pivot(ep, f"Pipoca_Ear{side}", (sx, -0.26, 0.55), col)
        exported.append(ear)

    tp = [add_sphere("TailMesh", 0.072, MATS["AC_WoodLight"], col, loc=(0, 0.34, 0.40), scale=(0.75, 1.45, 0.85))]
    tail = join_around_pivot(tp, "Pipoca_Tail", (0, 0.26, 0.34), col)
    exported.append(tail)

    for lname, lx, ly in [("FL", -0.11, -0.15), ("FR", 0.11, -0.15), ("BL", -0.11, 0.16), ("BR", 0.11, 0.16)]:
        lp = [
            add_cyl(f"PLeg_{lname}", 0.048, 0.18, MATS["AC_Cream"], col, loc=(lx, ly, 0.10)),
            add_sphere(f"PPaw_{lname}", 0.056, MATS["AC_WoodLight"], col, loc=(lx, ly - 0.02, 0.035), scale=(1.0, 1.2, 0.65)),
        ]
        leg = join_around_pivot(lp, f"Pipoca_Leg{lname}", (lx, ly, 0.19), col)
        exported.append(leg)

    export_flat_parts(exported, "PetDog")
    return exported


def main():
    clear_scene()
    init_mats()
    col = bpy.data.collections.new("ArticulatedChars")
    bpy.context.scene.collection.children.link(col)

    build_protagonist(col)
    pipoca_objs = build_pipoca(col)
    for o in pipoca_objs:
        o.location.x += 1.1

    bpy.ops.wm.save_as_mainfile(filepath=BLEND_OUT)
    print("[Articulated Blender Characters Complete]")


if __name__ == "__main__":
    main()
