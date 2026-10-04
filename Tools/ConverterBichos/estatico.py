# estatico.py <nome> <arquivo.glb> [giro]: bicho SEM esqueleto (ex.: imagem para 3D do Meshy) num FBX
# enxuto para Assets/Resources/TDFende/Bichos/. Junta as malhas, reduz até ~15 mil triângulos, gira
# <giro> graus em Z para a frente ficar em -Y do Blender (= +Z do Unity) e salva a textura com o nome
# do bicho em out/Textures. Sem animação: o jogo dá o balanço de passada (GaitBob), e o AnimalLoader
# acerta tamanho e chão sozinho. Também desenha v_<nome>.png de cima e de lado para conferir a frente
# (a seta vermelha aponta para onde o bicho anda no jogo).
import bpy, bmesh, sys, os, math, mathutils

SCR = os.path.dirname(os.path.abspath(__file__))
nome, glb = sys.argv[-3], sys.argv[-2]
try: giro = float(sys.argv[-1])
except ValueError: nome, glb, giro = sys.argv[-2], sys.argv[-1], 0.0
OUT = os.path.join(SCR, 'out'); TEX = os.path.join(OUT, 'Textures')
os.makedirs(TEX, exist_ok=True)
TRIS = 15000

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=glb)
src = [o for o in bpy.data.objects if o.type == 'MESH']
for o in bpy.data.objects: o.select_set(o in src)
bpy.context.view_layer.objects.active = src[0]
if len(src) > 1: bpy.ops.object.join()
o = bpy.context.view_layer.objects.active
o.parent = None
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
for ob in list(bpy.data.objects):
    if ob is not o: bpy.data.objects.remove(ob)

tris = sum(len(p.vertices) - 2 for p in o.data.polygons)
if tris > TRIS:
    mod = o.modifiers.new('Dec', 'DECIMATE'); mod.ratio = TRIS / tris; mod.use_collapse_triangulate = True
    bpy.ops.object.modifier_apply(modifier='Dec')
print('TRIS', tris, '->', sum(len(p.vertices) - 2 for p in o.data.polygons))

o.rotation_mode = "XYZ"  # o glTF importa em quatérnio: sem isto o giro não pega
o.rotation_euler = (0, 0, math.radians(giro))
bpy.ops.object.transform_apply(rotation=True)
zs = [v.co.z for v in o.data.vertices]
o.location.z -= min(zs)
bpy.ops.object.transform_apply(location=True)

# --- textura com o nome do bicho (o FBX aponta para Textures/<nome>_1.jpg)
for f in os.listdir(TEX):
    if f.startswith(nome + '_'): os.remove(os.path.join(TEX, f))
m = o.data.materials[0]
nt = m.node_tree
bsdf = next(n for n in nt.nodes if n.type == 'BSDF_PRINCIPLED')
def img_de(sock):
    n = sock.links[0].from_node if sock.is_linked else None
    while n is not None and n.type != 'TEX_IMAGE':
        ins = [i for i in n.inputs if i.is_linked]
        n = ins[0].links[0].from_node if ins else None
    return n.image if n else None
for inp in ['Metallic', 'Roughness', 'Emission Color', 'Specular IOR Level', 'Alpha']:
    if inp in bsdf.inputs:
        for l in list(bsdf.inputs[inp].links): nt.links.remove(l)
bsdf.inputs['Metallic'].default_value = 0.0
bsdf.inputs['Roughness'].default_value = 0.85
bsdf.inputs['Alpha'].default_value = 1.0
base = f'{nome}_1'
for img, suf in ((img_de(bsdf.inputs['Base Color']), ''), (img_de(bsdf.inputs['Normal']), '_normal')):
    if img is None: continue
    w, h = img.size
    if max(w, h) > 1024: img.scale(1024 * w // max(w, h), 1024 * h // max(w, h))
    p = os.path.join(TEX, f'{base}{suf}.jpg')
    img.filepath_raw = p; img.file_format = 'JPEG'
    img.save(filepath=p, quality=88)
    img.name = base + suf; img.filepath = p; img.reload()
    print('TEX', os.path.basename(p), img.size[:])
m.name = base

out = os.path.join(OUT, nome + '.fbx')
for ob in bpy.data.objects: ob.select_set(ob is o)
bpy.ops.export_scene.fbx(filepath=out, use_selection=True, object_types={'MESH'}, path_mode='RELATIVE',
    embed_textures=False, axis_forward='-Z', axis_up='Y', mesh_smooth_type='FACE', bake_anim=False)
print('SAIDA', out, round(os.path.getsize(out) / 1e6, 2), 'MB')

# --- conferência: de cima e de lado, seta vermelha = frente no jogo (-Y do Blender)
sc = bpy.context.scene
d = max(o.dimensions)
seta = bpy.data.objects.new('seta', bpy.data.meshes.new('seta')); sc.collection.objects.link(seta)
bm = bmesh.new()
bmesh.ops.create_cone(bm, cap_ends=True, segments=12, radius1=0.06 * d, radius2=0, depth=0.25 * d)
bm.transform(mathutils.Matrix.Translation((0, -0.75 * d, 0.1 * d)) @ mathutils.Matrix.Rotation(math.radians(90), 4, 'X'))
bm.to_mesh(seta.data); bm.free()
vm = bpy.data.materials.new('vermelho'); vm.diffuse_color = (1, 0, 0, 1); seta.data.materials.append(vm)
sc.render.engine = 'BLENDER_WORKBENCH'; sc.display.shading.color_type = 'TEXTURE'
sc.render.resolution_x, sc.render.resolution_y = 500, 500
cam = bpy.data.objects.new('cam', bpy.data.cameras.new('cam')); sc.collection.objects.link(cam); sc.camera = cam
cam.data.type = 'ORTHO'; cam.data.ortho_scale = 1.8 * d
from PIL import Image
fotos = []
for i, (loc, rot) in enumerate((((0, 0, 3 * d), (0, 0, 0)), ((3 * d, 0, 0.4 * d), (math.radians(90), 0, math.radians(90))))):
    cam.location = loc; cam.rotation_euler = rot
    p = os.path.join(OUT, f'_v{i}.png'); sc.render.filepath = p; bpy.ops.render.render(write_still=True)
    fotos.append(Image.open(p).convert('RGB'))
im = Image.new('RGB', (1000, 500)); im.paste(fotos[0], (0, 0)); im.paste(fotos[1], (500, 0))
im.save(os.path.join(SCR, f'v_{nome}.png')); print('VERIFICA', os.path.join(SCR, f'v_{nome}.png'))
