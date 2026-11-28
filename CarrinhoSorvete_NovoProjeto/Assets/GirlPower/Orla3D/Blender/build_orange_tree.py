import math
import os
import bpy
import bmesh
from mathutils import Vector, Matrix

SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
MODELS_DIR = os.path.normpath(os.path.join(SCRIPT_DIR, "..", "Models"))

MAT_SPECS = {
    "AC_WoodWarm":    (0.72, 0.54, 0.38, 1.0),
    "AC_BushGreen":   (0.30, 0.62, 0.40, 1.0),
    "AC_BushLight":   (0.48, 0.76, 0.52, 1.0),
    "AC_Orange":      (0.95, 0.55, 0.16, 1.0),
    "AC_FlowerWhite": (0.99, 0.97, 0.94, 1.0),
    "AC_FlowerPink":  (0.95, 0.42, 0.64, 1.0),
}
MATS = {}


def clear_scene():
    bpy.ops.wm.read_factory_settings(use_empty=True)


def init_mats():
    MATS.clear()
    for name, rgba in MAT_SPECS.items():
        m = bpy.data.materials.new(name=name)
        m.use_nodes = True
        bsdf = m.node_tree.nodes.get("Principled BSDF")
        if bsdf:
            bsdf.inputs["Base Color"].default_value = rgba
            bsdf.inputs["Roughness"].default_value = 0.75
        MATS[name] = m


def new_mesh_obj(name, bm, mat, col, loc=(0, 0, 0), rot=(0, 0, 0), smooth=True):
    mesh = bpy.data.meshes.new(name)
    bm.normal_update()
    bm.to_mesh(mesh)
    bm.free()
    if smooth:
        for p in mesh.polygons:
            p.use_smooth = True
    obj = bpy.data.objects.new(name, mesh)
    obj.location = loc
    obj.rotation_euler = rot
    obj.data.materials.append(mat)
    col.objects.link(obj)
    return obj


def add_sphere(name, radius, mat, col, loc=(0, 0, 0), scale=(1, 1, 1), u=16, v=12):
    bm = bmesh.new()
    bmesh.ops.create_uvsphere(bm, u_segments=u, v_segments=v, radius=radius)
    bmesh.ops.scale(bm, vec=Vector(scale), verts=bm.verts)
    return new_mesh_obj(name, bm, mat, col, loc=loc, smooth=True)


def add_lathe(name, profile_rz, mat, col, loc=(0, 0, 0), segs=16):
    bm = bmesh.new()
    rings = []
    for r, z in profile_rz:
        if r <= 0.001:
            rings.append([bm.verts.new((0.0, 0.0, z))])
        else:
            ring = []
            for i in range(segs):
                a = (i / segs) * math.tau
                ring.append(bm.verts.new((math.cos(a) * r, math.sin(a) * r, z)))
            rings.append(ring)
    for k in range(len(rings) - 1):
        r0, r1 = rings[k], rings[k + 1]
        if len(r0) == 1 and len(r1) > 1:
            for i in range(segs):
                bm.faces.new((r0[0], r1[(i + 1) % segs], r1[i]))
        elif len(r0) > 1 and len(r1) == 1:
            for i in range(segs):
                bm.faces.new((r0[i], r0[(i + 1) % segs], r1[0]))
        elif len(r0) > 1 and len(r1) > 1:
            for i in range(segs):
                bm.faces.new((r0[i], r0[(i + 1) % segs], r1[(i + 1) % segs], r1[i]))
    if len(rings[0]) > 1:
        bm.faces.new(list(reversed(rings[0])))
    if len(rings[-1]) > 1:
        bm.faces.new(rings[-1])
    return new_mesh_obj(name, bm, mat, col, loc=loc, smooth=True)


def join_parts(parts, name, col):
    bpy.ops.object.select_all(action="DESELECT")
    for p in parts:
        p.select_set(True)
    bpy.context.view_layer.objects.active = parts[0]
    if len(parts) > 1:
        bpy.ops.object.join()
    obj = bpy.context.active_object
    obj.name = name
    obj.data.name = name
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    if obj.name not in col.objects:
        col.objects.link(obj)
    return obj


def build_orange_tree():
    clear_scene()
    init_mats()
    col = bpy.context.scene.collection

    # 1. Cozy Animal Crossing Orange Tree Trunk & Lush Rounded Canopy
    tree_parts = [
        add_lathe(
            "Trunk",
            [(0.0, 0.0), (0.30, 0.0), (0.24, 0.28), (0.19, 0.85), (0.16, 1.48), (0.22, 1.72), (0.0, 1.78)],
            MATS["AC_WoodWarm"], col, segs=16
        ),
        # Layered cozy rounded leaf clusters
        add_sphere("CanopyCenter", 0.96, MATS["AC_BushGreen"], col, loc=(0.0, 0.0, 2.28), scale=(1.08, 1.06, 0.92), u=18, v=14),
        add_sphere("CanopyTop",    0.72, MATS["AC_BushLight"], col, loc=(0.06, -0.04, 2.86), scale=(1.06, 1.02, 0.84), u=16, v=12),
        add_sphere("CanopyLeft",   0.68, MATS["AC_BushGreen"], col, loc=(-0.66, 0.08, 2.12), scale=(1.04, 1.00, 0.88), u=14, v=10),
        add_sphere("CanopyRight",  0.70, MATS["AC_BushLight"], col, loc=(0.66, -0.06, 2.18), scale=(1.02, 1.00, 0.90), u=14, v=10),
        add_sphere("CanopyFront",  0.66, MATS["AC_BushLight"], col, loc=(0.02, -0.60, 2.08), scale=(1.06, 0.95, 0.86), u=14, v=10),
        add_sphere("CanopyBack",   0.68, MATS["AC_BushGreen"], col, loc=(-0.05, 0.58, 2.16), scale=(1.04, 0.96, 0.88), u=14, v=10),
        # Tiny cozy blossoms hinting at the pink/cream opening palette
        add_sphere("Blossom1", 0.09, MATS["AC_FlowerWhite"], col, loc=(-0.54, -0.68, 2.48)),
        add_sphere("Blossom2", 0.09, MATS["AC_FlowerPink"],  col, loc=(0.56, -0.62, 2.54)),
        add_sphere("Blossom3", 0.08, MATS["AC_FlowerWhite"], col, loc=(0.08, -0.82, 2.66)),
    ]
    tree_obj = join_parts(tree_parts, "OrangeTree_Body", col)

    # 2. Separate Ripe Oranges Group (clearly visible bright oranges that hide when harvested & regrow!)
    orange_coords = [
        (-0.56, -0.78, 2.04),
        ( 0.04, -0.94, 2.14),
        ( 0.62, -0.74, 2.08),
        (-0.88, -0.26, 2.26),
        ( 0.88, -0.20, 2.34),
        (-0.34, -0.74, 2.66),
        ( 0.42, -0.66, 2.72),
        ( 0.00,  0.86, 2.22),
    ]
    orange_parts = []
    for idx, pos in enumerate(orange_coords):
        orange_parts.append(add_sphere(f"RipeOrange_{idx}", 0.145, MATS["AC_Orange"], col, loc=pos, scale=(1.04, 1.04, 0.96), u=12, v=10))
    oranges_obj = join_parts(orange_parts, "RipeOranges", col)

    # Exact floor grounding Z = 0.000
    bpy.context.view_layer.update()
    min_z = min((o.matrix_world @ v.co).z for o in (tree_obj, oranges_obj) for v in o.data.vertices)
    if abs(min_z) > 0.0005:
        tree_obj.location.z -= min_z
        oranges_obj.location.z -= min_z
        bpy.context.view_layer.update()

    bpy.ops.object.select_all(action="DESELECT")
    tree_obj.select_set(True)
    oranges_obj.select_set(True)
    bpy.context.view_layer.objects.active = tree_obj

    out_path = os.path.join(MODELS_DIR, "OrangeTreeRipe.fbx")
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
    print(f"[FBX] Exported {out_path} (min_z={min_z:.4f})")


if __name__ == "__main__":
    build_orange_tree()
