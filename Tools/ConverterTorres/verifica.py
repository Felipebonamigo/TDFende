# verifica.py <nome>: monta a torre como o jogo monta (corpo do FBX + torreta, cano, base, bandeira e
# enfeites de nível vindos do código) e desenha no ângulo da câmera do jogo, nível 1 e 6, ao lado da
# torre toda em código. As peças do código saem do Tools/ArtPreview (rode "dotnet run" lá antes).
import bpy, bmesh, sys, os, json, math, mathutils
from PIL import Image, ImageDraw
SCR = os.path.dirname(os.path.abspath(__file__))
nome = sys.argv[-1]
ART = os.path.join(SCR, '..', 'ArtPreview', 'out')
art = json.load(open(os.path.join(ART, 'art.json')))
model = next(m for m in art['models'] if m['name'] == nome)
P = {p['name']: p for p in model['parts']}

bpy.ops.wm.read_factory_settings(use_empty=True)
sc = bpy.context.scene

def U(v):  # Unity (x, y, z) -> Blender: o importador do Unity espelha X; -Z do Blender é +Y do FBX
    return mathutils.Vector((-v[0], -v[2], v[1]))

mats = {}
def mat_art(nm):
    if nm in mats: return mats[nm]
    spec = art['materials'][nm]
    m = bpy.data.materials.new(nm); m.use_nodes = True
    nt = m.node_tree; b = nt.nodes['Principled BSDF']
    png = os.path.join(ART, nm + '.png')
    if nm == 'ClothTeam':
        b.inputs['Base Color'].default_value = (0.1, 0.25, 0.8, 1)
    elif os.path.exists(png):
        t = nt.nodes.new('ShaderNodeTexImage'); t.image = bpy.data.images.load(png)
        mp = nt.nodes.new('ShaderNodeMapping'); tc = nt.nodes.new('ShaderNodeTexCoord')
        k = 1 / max(spec['tile'], 1e-3); mp.inputs['Scale'].default_value = (k, k, 1)
        nt.links.new(tc.outputs['UV'], mp.inputs['Vector']); nt.links.new(mp.outputs['Vector'], t.inputs['Vector'])
        nt.links.new(t.outputs['Color'], b.inputs['Base Color'])
    b.inputs['Metallic'].default_value = spec['metal']; b.inputs['Roughness'].default_value = 1 - spec['smooth']
    if sum(spec['emit']) > 0:
        b.inputs['Emission Color'].default_value = (*spec['emit'], 1); b.inputs['Emission Strength'].default_value = 1
    mats[nm] = m; return m

def peca_codigo(p, nome_ob):
    me = bpy.data.meshes.new(nome_ob); bm = bmesh.new()
    pos = p['pos']; uv = p['uv']
    vs = [bm.verts.new(U(pos[i:i + 3])) for i in range(0, len(pos), 3)]
    lay = bm.loops.layers.uv.new()
    for gi, g in enumerate(p['groups']):
        idx = g['idx']
        for t in range(0, len(idx), 3):
            a, b_, c_ = idx[t], idx[t + 1], idx[t + 2]
            try: f = bm.faces.new((vs[a], vs[c_], vs[b_]))  # espelhado: inverte a volta
            except ValueError: continue
            f.material_index = gi
            for l, k in zip(f.loops, (a, c_, b_)): l[lay].uv = (uv[2 * k], uv[2 * k + 1])
    bm.to_mesh(me); bm.free()
    for g in p['groups']: me.materials.append(mat_art(g['mat']))
    ob = bpy.data.objects.new(nome_ob, me); sc.collection.objects.link(ob)
    return ob

def monta(ox, nivel, hibrida, mira_graus=35):
    grupo = []
    obj = {}
    corpo = {}
    if hibrida:
        antes = set(bpy.data.objects)
        bpy.ops.import_scene.fbx(filepath=os.path.join(SCR, 'out', nome + '.fbx'))
        for ob in set(bpy.data.objects) - antes:
            if ob.type == 'MESH': corpo[ob.name.split('.')[0]] = ob; print('FBX', ob.name, tuple(round(x,3) for x in ob.location), tuple(round(math.degrees(x)) for x in ob.rotation_euler), tuple(round(x,3) for x in ob.scale))
        tex = os.path.join(SCR, 'out', 'Textures', nome)
        m = bpy.data.materials.new('corpo'); m.use_nodes = True; nt = m.node_tree; b = nt.nodes['Principled BSDF']
        for suf, entrada in (('_cor', 'Base Color'), ('_normal', 'Normal')):
            src = tex + suf + '.bytes'; jpg = os.path.join('/tmp', f'_{nome}{suf}.jpg')
            open(jpg, 'wb').write(open(src, 'rb').read())
            t = nt.nodes.new('ShaderNodeTexImage'); t.image = bpy.data.images.load(jpg)
            if suf == '_normal':
                t.image.colorspace_settings.name = 'Non-Color'
                nm = nt.nodes.new('ShaderNodeNormalMap'); nt.links.new(t.outputs['Color'], nm.inputs['Color'])
                nt.links.new(nm.outputs['Normal'], b.inputs['Normal'])
            else: nt.links.new(t.outputs['Color'], b.inputs[entrada])
        b.inputs['Roughness'].default_value = 0.85; b.inputs['Metallic'].default_value = 0
        for ob in corpo.values(): ob.data.materials.clear(); ob.data.materials.append(m)
    # matriz de mundo de cada peça, como a hierarquia do ArtFactory/ModelRig: filho fica em
    # (pivô - pivô do pai) dentro do pai; nível estica o fuste e sobe o topo; a torreta gira
    for nm, ob in corpo.items(): obj[nm] = ob
    hdef = (corpo['Top'].location.z - corpo['Shaft'].location.z) if hibrida \
        else P['Top']['pivot'][1] - P['Shaft']['pivot'][1]
    grow = 0.14 * (nivel - 1)
    # o importador do Blender põe a conversão de eixo no objeto; no Unity o nó vem limpo
    base_corpo = {nm: ob.matrix_world.translation.copy() for nm, ob in corpo.items()}
    resto = {nm: ob.matrix_world.to_3x3().to_4x4() for nm, ob in corpo.items()}
    M = {}
    def mundo(nm):
        if nm in M: return M[nm]
        p = P[nm]
        if nm in corpo: m = mathutils.Matrix.Translation(base_corpo[nm])
        elif p['parent'] is None: m = mathutils.Matrix.Translation(U(p['pivot']))
        else: m = mundo(p['parent']) @ mathutils.Matrix.Translation(U(p['pivot']) - U(P[p['parent']]['pivot']))
        if p['parent'] is None or nm in corpo: m = mathutils.Matrix.Translation((ox, 0, 0)) @ m
        if nm == 'Top': m = mathutils.Matrix.Translation((0, 0, hdef * grow)) @ m
        if nm == 'Turret': m = m @ mathutils.Matrix.Rotation(math.radians(mira_graus), 4, 'Z')
        if nm == 'Shaft': m = m @ mathutils.Matrix.Diagonal((1, 1, 1 + grow, 1))
        M[nm] = m; return m
    for nm, p in P.items():
        if nm.startswith('Lv') and int(nm[2]) > nivel: continue
        if nm == 'Flag' and nivel < 2: continue
        if hibrida and nm.startswith('Lv') and P[nm]['parent'] == 'Shaft': continue  # como o ArtFactory
        ob = obj[nm] if nm in corpo else peca_codigo(p, nm)
        ob.matrix_world = mundo(nm) @ resto[nm] if nm in corpo else mundo(nm)

L = [(1.3, 1, False), (0, 1, True), (-1.3, 6, True)]  # +X do Blender fica à esquerda nesta câmera
for ox, nv, hb in L: monta(ox, nv, hb)
sun = bpy.data.objects.new('sol', bpy.data.lights.new('sol', 'SUN')); sun.data.energy = 3.5
sun.rotation_euler = (math.radians(50), 0, math.radians(-150)); sc.collection.objects.link(sun)
w = bpy.data.worlds.new('w'); sc.world = w; w.use_nodes = True
w.node_tree.nodes['Background'].inputs[0].default_value = (0.55, 0.65, 0.8, 1); w.node_tree.nodes['Background'].inputs[1].default_value = 0.6
chao = bpy.data.objects.new('chao', bpy.data.meshes.new('chao')); sc.collection.objects.link(chao)
bm = bmesh.new(); bmesh.ops.create_grid(bm, x_segments=1, y_segments=1, size=4); bm.to_mesh(chao.data); bm.free()
gm = bpy.data.materials.new('grama'); gm.use_nodes = True; gm.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value = (0.18, 0.26, 0.1, 1)
chao.data.materials.append(gm)
cam = bpy.data.objects.new('cam', bpy.data.cameras.new('cam')); sc.collection.objects.link(cam); sc.camera = cam
sc.render.engine = 'CYCLES'; sc.cycles.samples = 48
sc.view_settings.view_transform = 'Standard'
tiles = []
def foto(alvo, pitch, dist, lente, rot, rotulo, larg=900, alt=500):
    sc.render.resolution_x, sc.render.resolution_y = larg, alt
    cam.data.lens = lente
    d = mathutils.Vector((0, math.cos(math.radians(pitch)), math.sin(math.radians(pitch))))
    d.rotate(mathutils.Euler((0, 0, math.radians(rot))))
    cam.location = mathutils.Vector(alvo) + d * dist
    cam.rotation_euler = (mathutils.Vector(alvo) - cam.location).to_track_quat('-Z', 'Y').to_euler()
    p = f'/tmp/_vt_{nome}_{len(tiles)}.png'; sc.render.filepath = p; bpy.ops.render.render(write_still=True)
    tiles.append((p, rotulo))
foto((0, 0, 0.35), 50, 5.0, 50, 0, 'jogo, 50 graus: codigo nv1 | Meshy nv1 | Meshy nv6')
foto((0, 0, 0.35), 36, 2.6, 50, 20, 'zoom perto, 36 graus')
im = Image.new('RGB', (900, 1000)); d = ImageDraw.Draw(im)
for i, (p, l) in enumerate(tiles):
    im.paste(Image.open(p).convert('RGB'), (0, i * 500)); d.text((8, i * 500 + 6), l, fill='yellow')
im.save(os.path.join(SCR, f'v_{nome}.jpg'), quality=88)
print('SAIDA', os.path.join(SCR, f'v_{nome}.jpg'))
