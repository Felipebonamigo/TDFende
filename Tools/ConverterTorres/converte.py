# Converte o GLB de uma torre do Meshy no FBX que o jogo usa (Blender sem janela: pip install bpy).
# uso:  python3 baixa.py [nome]       -> glb/<nome>.glb
#       python3 converte.py <nome>    -> out/<nome>.fbx + out/Textures/<nome>_cor.bytes e _normal.bytes
#       python3 verifica.py <nome>    -> v_<nome>.jpg (torre montada com as peças do código, nível 1 e 6)
# Depois copie out/ para Assets/Resources/TDFende/Torres/.
#
# O modelo vira o CORPO da torre; o que mexe (torreta, cano, bandeira, enfeites de nível) continua
# vindo do código (ArtFactory: modelo híbrido). Por isso o conversor:
#   - fica só com a maior peça solta da malha (o canhão que o Meshy grudou no topo sai);
#   - centra, gira a porta para a câmera e põe na escala do jogo pelo raio do pé;
#   - corta a malha em duas na altura do pivô do Top: "Shaft" (o fuste, que estica quando a torre
#     sobe de nível) e "Top" (adarve e ameias, que sobem junto). O piso do adarve fica onde o
#     código põe a torreta, então o canhão de bronze do jogo assenta no chão de pedra do modelo;
#   - texturas de cor e normal em JPG 1024 com extensão .bytes (o Unity não recomprime; o
#     ArtFactory monta o material em código, como faz com as fotos de Resources/TDFende/Textures).
#
# Torre com estágios (Torre_Gelo_1, _2, _3: um modelo por par de níveis, ver TowerStages): cada
# estágio é uma entrada própria em torres.json. Modelo de telhado pontudo, cristal ou braseiro não
# tem piso plano no topo: aí a torreta do código vai em cima do modelo (piso = ponto mais alto).
#
# modo "inteiro" (Fortaleza, Acampamento): o modelo troca o do código inteiro. Fica com todas as
# peças, centra pelo pé, põe a maior largura do pé na medida do jogo e sai numa malha só, "Corpo"
# (nome que o ModelDef não conhece: o ArtFactory troca o modelo todo).
import bpy, bmesh, sys, os, json, math, mathutils
from collections import Counter

SCR = os.path.dirname(os.path.abspath(__file__))
CFG = json.load(open(os.path.join(SCR, 'torres.json')))
nome = sys.argv[-1]
c = CFG[nome]
inteiro = c.get('modo') == 'inteiro'
OUT = os.path.join(SCR, 'out'); TEX = os.path.join(OUT, 'Textures')
os.makedirs(TEX, exist_ok=True)

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=os.path.join(SCR, 'glb', nome + '.glb'))
src = [o for o in bpy.data.objects if o.type == 'MESH']
assert src, 'o GLB não tem malha'
mats = [m for o in src for m in o.data.materials if m]
mat = mats[0]
if len(src) > 1 or len(set(mats)) > 1:
    print('AVISO', len(src), 'malhas,', len(set(mats)), 'materiais: junto tudo e fico com a textura do primeiro')

bm = bmesh.new()
for o in src:
    tmp = bmesh.new(); tmp.from_mesh(o.data); tmp.transform(o.matrix_world)
    me_tmp = bpy.data.meshes.new('_tmp'); tmp.to_mesh(me_tmp); tmp.free()
    bm.from_mesh(me_tmp); bpy.data.meshes.remove(me_tmp)
# costura de UV vem como vértice duplicado: junta, senão tudo vira ilha solta
bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-5)
o = src[0]

def exporta(obs):
    for ob in bpy.data.objects: ob.select_set(ob in obs)
    out = os.path.join(OUT, nome + '.fbx')
    bpy.ops.export_scene.fbx(filepath=out, use_selection=True, object_types={'MESH'},
        apply_scale_options='FBX_SCALE_ALL', bake_space_transform=True, axis_forward='-Z', axis_up='Y',
        mesh_smooth_type='OFF', use_mesh_modifiers=False, path_mode='STRIP', embed_textures=False,
        bake_anim=False, use_tspace=False)
    print('SAIDA', out, round(os.path.getsize(out) / 1e6, 2), 'MB')

def malha(nome_parte, b, pivo_z):
    b.transform(mathutils.Matrix.Translation((0, 0, -pivo_z)))
    me = bpy.data.meshes.new(nome_parte); b.to_mesh(me); b.free()
    me.materials.append(mat)
    ob = bpy.data.objects.new(nome_parte, me)
    ob.location = (0, 0, pivo_z)
    bpy.context.scene.collection.objects.link(ob)
    for p in me.polygons: p.use_smooth = True
    me.set_sharp_from_angle(angle=math.radians(40))
    print('PARTE', nome_parte, len(me.polygons), 'faces')
    return ob

def texturas():
    nodes = mat.node_tree.nodes
    bsdf = next(n for n in nodes if n.type == 'BSDF_PRINCIPLED')
    def img_de(sock):
        n = sock.links[0].from_node if sock.is_linked else None
        while n is not None and n.type != 'TEX_IMAGE':
            ins = [i for i in n.inputs if i.is_linked]
            n = ins[0].links[0].from_node if ins else None
        return n.image if n else None
    for img, suf, tom in ((img_de(bsdf.inputs['Base Color']), '_cor', c.get('tom')),
                          (img_de(bsdf.inputs['Normal']), '_normal', None)):
        if img is None:
            print('AVISO sem textura', suf); continue
        w, h = img.size
        if max(w, h) > 1024: img.scale(1024 * w // max(w, h), 1024 * h // max(w, h))
        if tom and any(abs(k - 1) > 1e-3 for k in tom):
            import numpy as np
            px = np.empty(len(img.pixels), dtype=np.float32); img.pixels.foreach_get(px)
            px = px.reshape(-1, 4); px[:, :3] = np.clip(px[:, :3] * np.array(tom, dtype=np.float32), 0, 1)
            img.pixels.foreach_set(px.ravel())
        p = os.path.join(TEX, f'{nome}{suf}.jpg')
        img.filepath_raw = p; img.file_format = 'JPEG'
        img.save(filepath=p, quality=90)
        os.replace(p, p[:-4] + '.bytes')
        print('TEX', os.path.basename(p)[:-4] + '.bytes', img.size[:], round(os.path.getsize(p[:-4] + '.bytes') / 1e3), 'KB')

if inteiro:
    xs = [v.co.x for v in bm.verts]; ys = [v.co.y for v in bm.verts]; zs = [v.co.z for v in bm.verts]
    cx, cy, zmin = (min(xs) + max(xs)) / 2, (min(ys) + max(ys)) / 2, min(zs)
    s = c['largura'] / max(max(xs) - min(xs), max(ys) - min(ys))
    bm.transform(mathutils.Matrix.Scale(s, 4) @ mathutils.Matrix.Rotation(math.radians(c['giro']), 4, 'Z')
                 @ mathutils.Matrix.Translation((-cx, -cy, -zmin)))
    print('ESCALA %.3f  pé %.3f x %.3f  altura %.3f' % (s, (max(xs) - min(xs)) * s, (max(ys) - min(ys)) * s,
                                                      (max(zs) - zmin) * s))
    corpo = malha('Corpo', bm, 0.0)
    for ob in src: bpy.data.objects.remove(ob)
    texturas()
    exporta([corpo])
    sys.exit(0)

# --- só a maior peça solta
bm.faces.ensure_lookup_table()
seen = set(); ilhas = []
for f in bm.faces:
    if f.index in seen: continue
    pilha = [f]; comp = []; seen.add(f.index)
    while pilha:
        x = pilha.pop(); comp.append(x)
        for e in x.edges:
            for g in e.link_faces:
                if g.index not in seen: seen.add(g.index); pilha.append(g)
    ilhas.append(comp)
ilhas.sort(key=len, reverse=True)
fora = [f for comp in ilhas[1:] for f in comp]
print('ILHAS', [len(i) for i in ilhas[:6]], 'tiradas', len(fora), 'faces')
bmesh.ops.delete(bm, geom=fora, context='FACES')
bmesh.ops.delete(bm, geom=[v for v in bm.verts if not v.link_faces], context='VERTS')

zs = [v.co.z for v in bm.verts]
zmin, zmax = min(zs), max(zs)
H = zmax - zmin

# --- piso do adarve: o piso plano mais alto perto do eixo. Sem piso perto do topo (telhado
# pontudo, cristal, braseiro), ou com piso: "topo" no torres.json, a torreta vai no ponto mais alto.
cx0 = (min(v.co.x for v in bm.verts) + max(v.co.x for v in bm.verts)) / 2
cy0 = (min(v.co.y for v in bm.verts) + max(v.co.y for v in bm.verts)) / 2
area = Counter()
for f in bm.faces:
    m = f.calc_center_median()
    if f.normal.z > 0.9 and m.z > zmin + 0.3 * H and math.hypot(m.x - cx0, m.y - cy0) < 0.3 * H:
        area[round(m.z, 2)] += f.calc_area()
grande = max(area.values()) if area else 0
pisos = sorted(z for z, a in area.items() if a >= 0.25 * grande)
piso = pisos[-1] if pisos else zmax
if c.get('piso', 'auto') == 'topo' or piso < zmax - 0.15 * H:
    piso = zmax
modo_piso = 'topo do modelo' if piso == zmax else 'piso plano'

# --- centro pelo anel do parapeito (redondo; a porta não puxa); sem parapeito, pelo pé. Raio pelo pé.
anel = [v.co for v in bm.verts if piso + 0.05 * H < v.co.z < zmax - 0.02 * H]
if len(anel) < 50:
    anel = [v.co for v in bm.verts if v.co.z < zmin + 0.25 * H]
cx = (min(p.x for p in anel) + max(p.x for p in anel)) / 2
cy = (min(p.y for p in anel) + max(p.y for p in anel)) / 2
pe = sorted(math.hypot(v.co.x - cx, v.co.y - cy) for v in bm.verts if v.co.z < zmin + 0.25 * H)
r_pe = pe[len(pe) // 2]
s = c['raio'] / r_pe
if 'altura' in c:
    # estágio: a altura manda (cada estágio mais alto que o anterior, seja o modelo fino ou
    # largo), e o raio só segura o pé dentro da célula
    s = min((c['altura'] - c['base']) / H, 1.1 * c['raio'] / r_pe)

T = (mathutils.Matrix.Translation((0, 0, c['base'])) @ mathutils.Matrix.Scale(s, 4)
     @ mathutils.Matrix.Rotation(math.radians(c['giro']), 4, 'Z')
     @ mathutils.Matrix.Translation((-cx, -cy, -zmin)))
bm.transform(T)
piso_y = c['base'] + (piso - zmin) * s
top_y = piso_y - c['piso_turret']
print('ESCALA %.3f  pé r=%.3f  altura %.3f  piso %.3f (%s)  corte (pivô do Top) %.3f'
      % (s, r_pe * s, H * s, piso_y, modo_piso, top_y))

# --- corta em Shaft / Top na altura do pivô do Top
bmesh.ops.bisect_plane(bm, geom=bm.verts[:] + bm.edges[:] + bm.faces[:], dist=1e-6,
                       plane_co=(0, 0, top_y), plane_no=(0, 0, 1))

def parte(nome_parte, de_cima, pivo_y):
    b = bm.copy()
    b.faces.ensure_lookup_table()
    tirar = [f for f in b.faces if (f.calc_center_median().z > top_y) != de_cima]
    bmesh.ops.delete(b, geom=tirar, context='FACES')
    bmesh.ops.delete(b, geom=[v for v in b.verts if not v.link_faces], context='VERTS')
    return malha(nome_parte, b, pivo_y)

shaft = parte('Shaft', False, c['base'])
top = parte('Top', True, top_y)
for ob in src: bpy.data.objects.remove(ob)

# --- texturas: cor (com o tom da paleta) e normal, 1024 px, JPG em .bytes
texturas()

# --- exporta: Y para cima, -Z para a frente, escala 1 = 1 unidade do Unity, transformação limpa
exporta([shaft, top])
