# Converte um GLB do Sketchfab num FBX enxuto para o TDFende (Blender sem janela: pip install bpy).
# uso:  python3 baixa.py              -> glb/<uid>.glb de cada bicho de bichos.json
#       python3 converte.py <nome>    -> out/<nome>.fbx + out/Textures/
#       python3 verifica.py <nome>    -> v_<nome>.jpg (andar de lado + vista de frente, para conferir)
# Depois copie out/ para Assets/Resources/TDFende/Bichos/.
#
# O que faz: só as malhas com esqueleto (ou as de "keep"), só as ações de "acoes" renomeadas
# para Walk/Run/Idle/Death, ossos sem peso fora, frente do bicho para +Z do Unity, polígonos
# opacos até "tris", texturas até 1024 px (cor com "_alfa" quando tem recorte; "_normal").
import bpy, sys, os, json, math, mathutils

SCR = os.path.dirname(os.path.abspath(__file__))
CFG = json.load(open(os.path.join(SCR, 'bichos.json')))
nome = sys.argv[1]
c = CFG[nome]
OUT = os.path.join(SCR, 'out'); os.makedirs(OUT, exist_ok=True)
TEX = os.path.join(SCR, 'out', 'Textures'); os.makedirs(TEX, exist_ok=True)

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=os.path.join(SCR, 'glb', c['uid'] + '.glb'), guess_original_bind_pose=False)

def tris(o): return sum(len(p.vertices) - 2 for p in o.data.polygons)
def skinned(o): return any(m.type == 'ARMATURE' and m.object for m in o.modifiers)

# --- malhas que ficam
meshes = [o for o in bpy.data.objects if o.type == 'MESH']
keep = []
for o in meshes:
    if 'keep' in c: ok = o.name in c['keep']
    else: ok = skinned(o) and o.name not in c.get('drop', [])
    if ok: keep.append(o)
for o in meshes:
    if o not in keep: bpy.data.objects.remove(o, do_unlink=True)
arm = None
for o in keep:
    for m in o.modifiers:
        if m.type == 'ARMATURE' and m.object: arm = m.object
assert arm, 'sem esqueleto'
for pb in arm.pose.bones: pb.custom_shape = None
print('MALHAS', [(o.name, tris(o)) for o in keep])

# --- ações: só os estados do jogo, com nome limpo
want = c['acoes']  # {"Walk": "substring", ...}
acts = {}
for state, sub in want.items():
    cand = [a for a in bpy.data.actions if a.name.lower() == sub.lower()] or \
           [a for a in bpy.data.actions if sub.lower() in a.name.lower()]
    assert cand, f'acao {sub} nao achada: {[a.name for a in bpy.data.actions]}'
    acts[state] = cand[0]
for a in list(bpy.data.actions):
    if a not in acts.values(): bpy.data.actions.remove(a)
for state, a in acts.items():
    # chave no próprio objeto do esqueleto (o nó animado no glTF) desfaria a transformação de mundo
    # que fica gravada nele logo abaixo; só os ossos animam
    for l in a.layers:
        for st in l.strips:
            for cb in st.channelbags:
                for fc in list(cb.fcurves):
                    if not fc.data_path.startswith('pose.bones'): cb.fcurves.remove(fc)
    a.name = state
    a.use_fake_user = True
ad = arm.animation_data or arm.animation_data_create()
for t in list(ad.nla_tracks): ad.nla_tracks.remove(t)
for o in bpy.data.objects:
    if o is not arm and o.animation_data: o.animation_data_clear()
ad.action = acts['Walk']; ad.action_slot = acts['Walk'].slots[0]
print('ACOES', {s: (a.frame_range[0], a.frame_range[1]) for s, a in acts.items()})

# --- tira a hierarquia de vazios: esqueleto vira raiz (mantendo a transformação de mundo)
bpy.context.view_layer.update()
mw = arm.matrix_world.copy()
arm.parent = None
arm.matrix_world = mw
for o in keep:
    w = o.matrix_world.copy(); o.parent = arm; o.parent_type = 'OBJECT'; o.matrix_world = w
for o in list(bpy.data.objects):
    if o.type not in ('ARMATURE', 'MESH'): bpy.data.objects.remove(o, do_unlink=True)
arm.name = 'Bicho'; arm.data.name = 'Bicho'; 

# --- frente para -Y no Blender (= +Z no Unity): pela cabeça em relação ao quadril
bpy.context.scene.frame_set(int(acts['Walk'].frame_range[0]))
bpy.context.view_layer.update()
def bone_pos(sub):
    for pb in arm.pose.bones:
        if sub in pb.name.lower(): return arm.matrix_world @ pb.head
def first(names):
    for s in names:
        p = bone_pos(s)
        if p is not None: return p
head = first(c.get('cabeca', ['head', 'kopf', 'cabeza', 'neck', 'hals', 'jaw', 'skull']))
tail = first(c.get('rabo', ['tail', 'schwanz', 'cola', 'cauda']))
dg = bpy.context.evaluated_depsgraph_get()
pts = []
for o in keep:
    e = o.evaluated_get(dg); pts += [e.matrix_world @ mathutils.Vector(v) for v in e.bound_box]
mn = mathutils.Vector([min(p[i] for p in pts) for i in range(3)]); mx = mathutils.Vector([max(p[i] for p in pts) for i in range(3)])
ctr = (mn + mx) / 2
if 'frente' in c:
    d = mathutils.Vector(c['frente'])
else:
    d = head - (tail if tail is not None else ctr)
d.z = 0
ang = math.atan2(d.x, -d.y)  # quanto girar em Z para levar d até -Y
snap = round(ang / (math.pi / 2)) * (math.pi / 2)
if abs(ang - snap) > math.radians(12): snap = ang  # modelo torto de fábrica: giro exato
if 'giro' in c: snap = math.radians(c['giro'])  # acertado a olho no verifica.py (torto de poucos graus)
print('FRENTE', tuple(round(x, 3) for x in d), 'rabo' if tail is not None else 'centro', 'giro', round(math.degrees(snap), 1))
R = mathutils.Matrix.Rotation(-snap, 4, 'Z')
arm.matrix_world = R @ arm.matrix_world

# --- polígonos: só nas malhas opacas, até o orçamento
def has_alpha(mat):
    if not mat or not mat.node_tree or mat.name in c.get('opacos', []): return False
    for n in mat.node_tree.nodes:
        if n.type == 'BSDF_PRINCIPLED' and n.inputs['Alpha'].is_linked: return True
    return False
budget = c.get('tris', 16000)
opaque = [o for o in keep if not any(has_alpha(s.material) for s in o.material_slots)]
tot = sum(tris(o) for o in opaque)
if tot > budget:
    r = budget / tot
    for o in opaque:
        if o.data.shape_keys:
            o.shape_key_clear()
        bpy.context.view_layer.objects.active = o
        mod = o.modifiers.new('Dec', 'DECIMATE'); mod.ratio = r; mod.use_collapse_triangulate = True
        while o.modifiers[0] != mod: bpy.ops.object.modifier_move_up(modifier='Dec')
        bpy.ops.object.modifier_apply(modifier='Dec')
print('TRIS', [(o.name, tris(o)) for o in keep])

# --- texturas: cor (com ou sem alfa) e normal, no máximo N px, com nome do bicho
maxpx = c.get('px', 1024)
seen = {}
for f in os.listdir(TEX):
    if f.startswith(nome + '_'): os.remove(os.path.join(TEX, f))
for o in keep:
    for s in o.material_slots:
        m = s.material
        if not m or not m.node_tree or m in seen: continue
        seen[m] = True
        nt = m.node_tree
        bsdf = next((n for n in nt.nodes if n.type == 'BSDF_PRINCIPLED'), None)
        if not bsdf: continue
        alpha = has_alpha(m)
        if not alpha:
            for l in list(bsdf.inputs['Alpha'].links): nt.links.remove(l)
        def img_from(sock):
            if not sock.is_linked: return None
            n = sock.links[0].from_node
            seen_n = set()
            while n and n.type != 'TEX_IMAGE' and n not in seen_n:
                seen_n.add(n)
                ins = [i for i in n.inputs if i.is_linked]
                n = ins[0].links[0].from_node if ins else None
            return n.image if n and n.type == 'TEX_IMAGE' else None
        col = img_from(bsdf.inputs['Base Color'])
        nrm = img_from(bsdf.inputs['Normal'])
        # some o resto (metal/rugosidade/emissão): FBX não carrega direito; bicho é fosco
        for inp in ['Metallic', 'Roughness', 'Emission Color', 'Specular IOR Level']:
            if inp in bsdf.inputs and bsdf.inputs[inp].is_linked:
                for l in list(bsdf.inputs[inp].links): nt.links.remove(l)
        bsdf.inputs['Metallic'].default_value = 0.0
        bsdf.inputs['Roughness'].default_value = 0.85
        if not alpha:
            bsdf.inputs['Alpha'].default_value = 1.0
        base = f"{nome}_{len(seen)}"
        for img, suf in ((col, '_alfa' if alpha else ''), (nrm, '_normal')):
            if img is None or img.get('salva'): continue
            img['salva'] = True
            w, h = img.size
            k = min(1.0, maxpx / max(w, h))
            if k < 1: img.scale(max(1, int(w * k)), max(1, int(h * k)))
            ext = 'png' if suf == '_alfa' else 'jpg'
            p = os.path.join(TEX, f'{base}{suf}.{ext}')
            img.filepath_raw = p
            img.file_format = 'PNG' if ext == 'png' else 'JPEG'
            if ext == 'jpg':
                bpy.context.scene.render.image_settings.quality = 88
            img.save(filepath=p, quality=88) if ext == 'jpg' else img.save(filepath=p)
            img.name = f'{base}{suf}'
            img.filepath = p; img.reload()
        m.name = base
        print('MAT', m.name, 'alfa' if alpha else 'opaco', col and col.size[:], nrm and nrm.size[:])

# --- ossos que não mexem em nada (rosto, dedos sem peso, ajudantes): fora — encolhem a animação
used = set()
for o in keep:
    idx = {g.index: g.name for g in o.vertex_groups}
    for v in o.data.vertices:
        for g in v.groups:
            if g.weight > 1e-4 and g.group in idx: used.add(idx[g.group])
need = set()
for b in arm.data.bones:
    if b.name in used:
        p = b
        while p: need.add(p.name); p = p.parent
bpy.context.view_layer.objects.active = arm
for o in bpy.data.objects: o.select_set(o is arm)
bpy.ops.object.mode_set(mode='EDIT')
eb = arm.data.edit_bones
gone = [b for b in eb if b.name not in need]
for b in gone: eb.remove(b)
bpy.ops.object.mode_set(mode='OBJECT')
print('OSSOS', len(arm.data.bones), 'tirados', len(gone))
# curvas de osso que sumiu: o exportador só aceita a ação se TODOS os caminhos existirem
for a in bpy.data.actions:
    for l in a.layers:
        for st in l.strips:
            for cb in st.channelbags:
                for fc in list(cb.fcurves):
                    try: arm.path_resolve(fc.data_path)
                    except ValueError: cb.fcurves.remove(fc)

# --- exporta
for o in bpy.data.objects: o.select_set(o is arm or o in keep)
bpy.context.view_layer.objects.active = arm
out = os.path.join(OUT, nome + '.fbx')
bpy.ops.export_scene.fbx(filepath=out, use_selection=True, object_types={'ARMATURE', 'MESH'},
    add_leaf_bones=False, bake_anim=True, bake_anim_use_all_actions=True, bake_anim_use_nla_strips=False,
    bake_anim_force_startend_keying=True, bake_anim_simplify_factor=1.0, path_mode='RELATIVE', embed_textures=False,
    axis_forward='-Z', axis_up='Y', mesh_smooth_type='FACE', use_armature_deform_only=False)
print('SAIDA', out, round(os.path.getsize(out) / 1e6, 2), 'MB')
