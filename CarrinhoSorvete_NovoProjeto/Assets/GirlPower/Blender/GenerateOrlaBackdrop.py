import bpy, math, os
from mathutils import Vector

# Girl Power coastal panorama: low-poly, hand-shaped color fields, rendered as a 2D game backdrop.
out_dir = os.path.dirname(os.path.abspath(__file__))
os.makedirs(out_dir, exist_ok=True)
bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)
for datablocks in (bpy.data.materials, bpy.data.meshes, bpy.data.curves, bpy.data.cameras, bpy.data.lights):
    pass

# Palette is deliberately limited and warm, with clean silhouettes that survive sprite scaling.
def emission(name, color, strength=1.0):
    mat=bpy.data.materials.new(name); mat.diffuse_color=(*color,1); mat.use_nodes=True
    nt=mat.node_tree; nt.nodes.clear(); out=nt.nodes.new('ShaderNodeOutputMaterial'); em=nt.nodes.new('ShaderNodeEmission'); em.inputs['Color'].default_value=(*color,1); em.inputs['Strength'].default_value=strength; nt.links.new(em.outputs[0],out.inputs['Surface']); return mat

def poly(name, pts, z, mat):
    verts=[(x,y,z) for x,y in pts]; mesh=bpy.data.meshes.new(name+'Mesh'); mesh.from_pydata(verts,[],[list(range(len(verts)))]); mesh.materials.append(mat); ob=bpy.data.objects.new(name,mesh); bpy.context.collection.objects.link(ob); return ob

def ellipse(name,x,y,rx,ry,z,mat,n=48):
    return poly(name,[(x+math.cos(i*2*math.pi/n)*rx,y+math.sin(i*2*math.pi/n)*ry) for i in range(n)],z,mat)

def cloud(name,x,y,s,mat):
    ellipse(name+' 1',x-s*.45,y,s*.55,s*.19,4.0,mat)
    ellipse(name+' 2',x,y+s*.11,s*.48,s*.24,4.01,mat)
    ellipse(name+' 3',x+s*.43,y-s*.015,s*.52,s*.18,4.02,mat)
    poly(name+' base',[(x-s*.78,y-s*.05),(x+s*.82,y-s*.05),(x+s*.72,y-s*.21),(x-s*.62,y-s*.21)],3.99,mat)

# Strong sky gradient, sampled vertically in the camera's broad panorama.
sky=bpy.data.materials.new('Céu crepuscular - coral, pêssego e lavanda'); sky.use_nodes=True
nt=sky.node_tree; nt.nodes.clear(); out=nt.nodes.new('ShaderNodeOutputMaterial'); em=nt.nodes.new('ShaderNodeEmission'); tex=nt.nodes.new('ShaderNodeTexCoord'); sep=nt.nodes.new('ShaderNodeSeparateXYZ'); ramp=nt.nodes.new('ShaderNodeValToRGB')
nt.links.new(tex.outputs['Generated'],sep.inputs['Vector']); nt.links.new(sep.outputs['Y'],ramp.inputs[0]); nt.links.new(ramp.outputs['Color'],em.inputs['Color']); nt.links.new(em.outputs[0],out.inputs['Surface'])
cr=ramp.color_ramp; cr.elements.remove(cr.elements[1]); e=cr.elements[0]; e.position=0.0;e.color=(0.42,0.36,0.60,1); e=cr.elements.new(.38);e.color=(0.78,.45,.61,1);e=cr.elements.new(.72);e.color=(1.0,.66,.49,1);e=cr.elements.new(1.0);e.color=(.53,.69,.75,1)
poly('Céu panorâmico',[(-20,-5),(20,-5),(20,5),(-20,5)],-8,sky)

mats={
 'island_far':emission('Ilhas distantes - azul névoa',(0.30,.49,.60)),
 'island_mid':emission('Ilhas distantes - verde sálvia',(0.34,.57,.56)),
 'island_light':emission('Dunas costeiras - verde claro',(0.55,.69,.52)),
 'sea_deep':emission('Oceano - azul petróleo',(0.10,.43,.52)),
 'sea_mid':emission('Oceano - turquesa',(0.15,.57,.62)),
 'sea_light':emission('Espuma - azul claro',(.56,.82,.77)),
 'sand':emission('Areia - baunilha',(0.91,.73,.52)),
 'sand_glow':emission('Areia iluminada',(1.0,.82,.60)),
 'coral':emission('Nuvens - coral claro',(1.0,.69,.62)),
 'cream':emission('Nuvens - creme rosado',(1.0,.84,.75)),
 'sun':emission('Sol dourado',(1.0,.82,.53)),
 'sun_glow':emission('Halo do sol',(1.0,.66,.49)),
 'lights':emission('Luzes pequenas da vila',(1.0,.84,.52)),
}

# Sun disk and hazy halo.
ellipse('Halo suave do sol',-6.0,2.35,1.1,1.1,-5.0,mats['sun_glow'])
ellipse('Sol baixo sobre a baía',-6.0,2.35,.68,.68,-4.9,mats['sun'])
# Distant island chain; soft asymmetry makes the horizon feel like a real bay.
poly('Ilha distante esquerda',[(-20,.52),(-18,.70),(-16,.82),(-14,.78),(-12,.95),(-10,.80),(-8,.86),(-6,.73),(-4,.80),(-2,.65),(0,.75),(2,.62),(4,.70),(6,.58),(8,.65),(10,.48),(12,.64),(14,.50),(16,.58),(18,.42),(20,.5),(20,-.1),(-20,-.1)],-4.1,mats['island_far'])
poly('Restinga do horizonte',[(-20,.20),(-18,.32),(-16,.27),(-14,.42),(-12,.36),(-10,.48),(-8,.30),(-6,.38),(-4,.25),(-2,.36),(0,.25),(2,.35),(4,.18),(6,.30),(8,.16),(10,.25),(12,.12),(14,.20),(16,.10),(18,.18),(20,.08),(20,-.48),(-20,-.48)],-3.9,mats['island_mid'])
# Ocean as a broad calm band with a few low-contrast color planes.
poly('Mar aberto',[(-20,-2.0),(20,-2.0),(20,.42),(17,.45),(14,.38),(11,.46),(8,.39),(5,.44),(2,.39),(-1,.47),(-4,.40),(-7,.48),(-10,.42),(-13,.50),(-16,.43),(-20,.49)],-3.7,mats['sea_deep'])
poly('Faixa de mar ao entardecer',[(-20,-1.65),(20,-1.65),(20,.03),(16,.08),(13,.00),(10,.08),(7,.02),(4,.09),(1,.01),(-2,.08),(-5,.02),(-8,.10),(-11,.03),(-14,.1),(-17,.04),(-20,.10)],-3.6,mats['sea_mid'])
# Distant coast and dune at the very bottom edge; open sightlines remain dominant.
poly('Faixa de areia clara',[(-20,-4),(20,-4),(20,-1.62),(18,-1.48),(15,-1.55),(12,-1.39),(9,-1.53),(6,-1.42),(3,-1.54),(0,-1.40),(-3,-1.53),(-6,-1.43),(-9,-1.55),(-12,-1.44),(-15,-1.56),(-18,-1.45),(-20,-1.52)],-3.3,mats['sand'])
poly('Brilho da maré',[(-20,-1.55),(20,-1.55),(20,-1.43),(17,-1.39),(14,-1.48),(11,-1.37),(8,-1.46),(5,-1.35),(2,-1.44),(-1,-1.34),(-4,-1.46),(-7,-1.35),(-10,-1.48),(-13,-1.39),(-16,-1.50),(-20,-1.40)],-3.2,mats['sand_glow'])

# Ocean glints: short rounded strokes, irregularly spaced and kept away from foreground gameplay.
for i,(x,y,w) in enumerate([(-17,-.52,.54),(-14,-.89,.38),(-11,-.44,.76),(-8,-1.14,.50),(-5,-.70,.44),(-2,-.38,.68),(1,-.96,.48),(4,-.55,.82),(7,-1.18,.45),(10,-.72,.58),(13,-.43,.7),(16,-1.03,.44),(19,-.62,.68),(-15,-1.35,.28),(-6,-1.28,.34),(3,-1.26,.27),(12,-1.30,.33)]):
    poly('Reflexo do mar %02d'%i,[(x-w/2,y),(x-w/2+.12,y+.045),(x+w/2,y+.045),(x+w/2-.06,y-.02)],-3.0,mats['sea_light'])
# Gentle clouds placed at varied scales for depth.
cloud('Nuvem pêssego 1',-15.2,3.20,1.65,mats['cream'])
cloud('Nuvem coral 2',-10.5,2.70,1.05,mats['coral'])
cloud('Nuvem creme 3',-2.1,3.45,1.45,mats['cream'])
cloud('Nuvem coral 4',6.3,3.0,1.8,mats['coral'])
cloud('Nuvem creme 5',14.5,3.38,1.35,mats['cream'])
cloud('Nuvem coral 6',18.3,2.45,1.0,mats['coral'])
# A few tiny warm windows suggest a distant island community without adding a hard city skyline.
for i,(x,y) in enumerate([(-13,.64),(-12.2,.69),(-9,.57),(-3,.62),(1,.60),(8,.56),(15,.48),(17,.52)]):
    poly('Janela distante %02d'%i,[(x,y),(x+.10,y),(x+.10,y+.11),(x,y+.11)],-3.45,mats['lights'])

# Camera: exact 1200:256 panorama proportions, matching the existing Unity sprite's aspect ratio.
cam_data=bpy.data.cameras.new('Camera - panorama de fundo'); cam=bpy.data.objects.new('Camera - panorama de fundo',cam_data); bpy.context.collection.objects.link(cam); cam.location=(0,0,30); cam.rotation_euler=(0,0,0); cam_data.type='ORTHO'; cam_data.ortho_scale=37.5; bpy.context.scene.camera=cam
scene=bpy.context.scene; scene.render.engine='BLENDER_EEVEE'; scene.render.resolution_x=1200; scene.render.resolution_y=256; scene.render.resolution_percentage=100; scene.render.image_settings.file_format='PNG'; scene.render.image_settings.color_mode='RGBA'; scene.render.film_transparent=False
scene.render.filepath=os.path.join(out_dir,'BlenderOrlaBackdrop.png'); scene.render.image_settings.color_mode='RGB'; scene.view_settings.view_transform='Standard'; scene.view_settings.look='None'; scene.view_settings.exposure=0; scene.view_settings.gamma=1
scene.render.image_settings.color_depth='8'; scene.render.resolution_percentage=100
scene.camera.data.lens=50
# Save a reusable, named Blender project alongside the generator.
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(out_dir,'GirlPower_Orla_AnimalCrossingInspired.blend'))
bpy.ops.render.render(write_still=True)
print('RENDERED',scene.render.filepath)



