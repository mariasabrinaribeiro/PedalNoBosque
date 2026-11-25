"""Regenerate only the Orla floor and smiling scene characters.

Run with Blender 5.3 in background mode. This deliberately avoids rebuilding
the full legacy assembled scene, so unrelated FBX assets are left untouched.
"""

import bpy
import os
import runpy

SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
SOURCE = os.path.join(SCRIPT_DIR, "build_orla_acnh.py")
BLEND_OUT = os.path.join(SCRIPT_DIR, "Orla_SpacingCharacters_Polished.blend")

builder = runpy.run_path(SOURCE, run_name="orla_asset_builder")

builder["clear_scene"]()
builder["init_palette"]()
col = builder["ensure_collection"]("Orla_SpacingAndCharacters")
materials = builder["MATERIALS"]

assets = [
    builder["build_environment_base"](col),
    builder["build_chibi_character"](
        col, "Char_Florist",
        materials["AC_SkinLight"], materials["AC_HairBrown"],
        materials["AC_Cream"], materials["AC_WoodLight"],
        hat="straw", apron=True,
    ),
    builder["build_chibi_character"](
        col, "Char_PedestrianBlue",
        materials["AC_SkinTan"], materials["AC_HairDark"],
        materials["AC_SkyBlue"], materials["AC_Denim"],
        hat="cap",
    ),
    builder["build_chibi_character"](
        col, "Char_PedestrianCoral",
        materials["AC_SkinLight"], materials["AC_HairAuburn"],
        materials["AC_Coral"], materials["AC_Cream"],
        hat="visor",
    ),
]

for asset, filename in zip(
    assets,
    ("OrlaEnvironmentBase.fbx", "Char_Florist.fbx", "Char_PedestrianBlue.fbx", "Char_PedestrianCoral.fbx"),
):
    builder["export_fbx"](asset, filename)

bpy.ops.wm.save_as_mainfile(filepath=BLEND_OUT)
print(f"[Orla spacing and character polish saved] {BLEND_OUT}")
