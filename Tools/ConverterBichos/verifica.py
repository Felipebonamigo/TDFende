# verifica.py <nome>: importa o FBX final e desenha andar (4 quadros, de lado) + vista de frente (câmera em -Y)
import bpy, sys, os, mathutils
from PIL import Image, ImageDraw
nome = sys.argv[1]; SCR = os.path.dirname(os.path.abspath(__file__))
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=f'{SCR}/out/{nome}.fbx')
arm = [o for o in bpy.data.objects if o.type == 'ARMATURE'][0]
meshes = [o for o in bpy.data.objects if o.type == 'MESH']
for m in bpy.data.materials:
    if m.node_tree:
        b = next((n for n in m.node_tree.nodes if n.type == 'BSDF_PRINCIPLED'), None)
        if b and b.inputs['Alpha'].is_linked: m.surface_render_method = 'DITHERED'
acts = {a.name.split('|')[-1]: a for a in bpy.data.actions}
print('TAKES', sorted(acts))
sc = bpy.context.scene
cam = bpy.data.objects.new('cam', bpy.data.cameras.new('cam')); sc.collection.objects.link(cam); sc.camera = cam
sun = bpy.data.objects.new('sun', bpy.data.lights.new('sun', 'SUN')); sun.data.energy = 3; sun.rotation_euler = (0.7, 0.3, 0.5); sc.collection.objects.link(sun)
w = bpy.data.worlds.new('w'); sc.world = w; w.use_nodes = True; w.node_tree.nodes['Background'].inputs[1].default_value = 0.7
sc.render.engine = 'CYCLES'; sc.cycles.samples = 16; sc.render.resolution_x, sc.render.resolution_y = 400, 300
def bbox():
    dg = bpy.context.evaluated_depsgraph_get(); pts = []
    for o in meshes:
        e = o.evaluated_get(dg); pts += [e.matrix_world @ mathutils.Vector(c) for c in e.bound_box]
    mn = mathutils.Vector([min(p[i] for p in pts) for i in range(3)]); mx = mathutils.Vector([max(p[i] for p in pts) for i in range(3)])
    return mn, mx
tiles = []
def shot(act, fr, view, label):
    ad = arm.animation_data or arm.animation_data_create()
    ad.action = act; ad.action_slot = act.slots[0]
    sc.frame_set(fr); mn, mx = bbox(); c = (mn + mx) / 2; size = (mx - mn).length
    cam.data.clip_start = size * 0.01; cam.data.clip_end = size * 100
    cam.location = c + mathutils.Vector(view).normalized() * size * 1.25
    cam.rotation_euler = (c - cam.location).to_track_quat('-Z', 'Y').to_euler()
    p = f'/tmp/_v_{nome}_{len(tiles)}.png'; sc.render.filepath = p; bpy.ops.render.render(write_still=True)
    tiles.append((p, label))
wk = acts['Walk']; f0, f1 = int(wk.frame_range[0]), int(wk.frame_range[1])
for k in range(4): shot(wk, f0 + (f1 - f0) * k // 4, (1, 0, 0.2), f'Walk {k}')
shot(wk, f0, (0, -1, 0.25), 'frente (-Y)')
for s in ('Run', 'Idle', 'Death'):
    if s in acts:
        a = acts[s]; shot(a, int((a.frame_range[0] + a.frame_range[1]) * (0.9 if s == 'Death' else 0.5)), (0.8, -0.6, 0.3), s)
im = Image.new('RGB', (400 * 4, 300 * 2)); d = ImageDraw.Draw(im)
for i, (p, l) in enumerate(tiles):
    im.paste(Image.open(p).convert('RGB'), ((i % 4) * 400, (i // 4) * 300)); d.text(((i % 4) * 400 + 5, (i // 4) * 300 + 5), f'{nome}: {l}', fill='yellow')
im.save(f'{SCR}/v_{nome}.jpg', quality=85)
