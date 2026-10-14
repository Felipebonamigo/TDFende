# Importa a arte do TDFende (FBX das torres e dos bichos + texturas) para o Content do Unreal, em /Game/TDFende/.
# Rode pelo PowerShell (editor fechado ou aberto, sem GUI):
#   powershell -ExecutionPolicy Bypass -File Unreal\TDFende\ImportarArte.ps1
# Fonte: Assets/Resources/TDFende/{Torres,Bichos} (a mesma arte do Unity). O Content/ não vai para o git (D4): este script é a receita
# para refazê-lo. Nome do asset = nome do arquivo (Torre_Gelo_2, rato...). Material = M_TdfBase (cor) ou M_TdfMascara (cor + recorte).
import os
import shutil
import tempfile
import unreal

REPO = os.environ.get("TDF_REPO") or os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", ".."))
RES = os.path.join(REPO, "Assets", "Resources", "TDFende")
GAME = "/Game/TDFende"
TMP = tempfile.mkdtemp(prefix="tdf_import_")
tools = unreal.AssetToolsHelpers.get_asset_tools()
mel = unreal.MaterialEditingLibrary
eal = unreal.EditorAssetLibrary
log = []


def say(msg):
    unreal.log("[TDF-IMPORT] " + msg)
    log.append(msg)


def import_fbx(src, dest, name, skeletal):
    task = unreal.AssetImportTask()
    task.filename = src
    task.destination_path = dest
    task.destination_name = name
    task.automated = True
    task.replace_existing = True
    task.replace_existing_settings = True
    task.save = True
    ui = unreal.FbxImportUI()
    ui.import_mesh = True
    ui.import_textures = False
    ui.import_materials = False
    ui.import_as_skeletal = skeletal
    ui.import_animations = skeletal
    if skeletal:
        ui.mesh_type_to_import = unreal.FBXImportType.FBXIT_SKELETAL_MESH
        ui.skeletal_mesh_import_data.set_editor_property("import_morph_targets", False)
        # os clipes do Meshy/Sketchfab não caem em quadro inteiro a 24 FPS: sem isto o clipe de andar era recusado
        ui.anim_sequence_import_data.set_editor_property("snap_to_closest_frame_boundary", True)
    else:
        ui.mesh_type_to_import = unreal.FBXImportType.FBXIT_STATIC_MESH
        ui.static_mesh_import_data.set_editor_property("combine_meshes", True)
        ui.static_mesh_import_data.set_editor_property("generate_lightmap_u_vs", False)
    task.options = ui
    tools.import_asset_tasks([task])
    return task.imported_object_paths


def nanite(mesh):
    """Liga o Nanite na malha (detalhe geométrico alto com custo baixo; o padrão do UE5 para cenário e folhagem)."""
    try:
        s = mesh.get_editor_property("nanite_settings")
        if not s.enabled:
            s.enabled = True
            mesh.set_editor_property("nanite_settings", s)  # PostEditChange refaz a malha com Nanite
    except Exception as e:  # sem Nanite a malha continua valendo; só registra
        say("sem Nanite em %s: %s" % (mesh.get_name(), e))


def import_texture(src, dest, name, normal=False):
    """src pode ser .bytes (é um JPEG/PNG com outro nome): copia para um nome de verdade antes."""
    ext = os.path.splitext(src)[1].lower()
    if ext == ".bytes":
        with open(src, "rb") as f:
            head = f.read(4)
        ext = ".png" if head[:4] == b"\x89PNG" else ".jpg"
    tmp = os.path.join(TMP, name + ext)
    shutil.copyfile(src, tmp)
    task = unreal.AssetImportTask()
    task.filename = tmp
    task.destination_path = dest
    task.destination_name = name
    task.automated = True
    task.replace_existing = True
    task.save = True
    tools.import_asset_tasks([task])
    tex = unreal.load_asset(dest + "/" + name)
    if tex and normal:
        tex.set_editor_property("compression_settings", unreal.TextureCompressionSettings.TC_NORMALMAP)
        tex.set_editor_property("srgb", False)
    return tex


def make_master(name, masked):
    path = GAME + "/Materiais"
    full = path + "/" + name
    if eal.does_asset_exist(full):
        eal.delete_asset(full)
    mat = tools.create_asset(name, path, unreal.Material, unreal.MaterialFactoryNew())
    # sem esta flag o material cai no cinza padrão nas malhas esqueléticas (e o log enche de avisos)
    mat.set_editor_property("used_with_skeletal_mesh", True)
    mat.set_editor_property("used_with_instanced_static_meshes", True)  # as torres são instâncias (ISM)
    if masked:
        mat.set_editor_property("blend_mode", unreal.BlendMode.BLEND_MASKED)
        mat.set_editor_property("two_sided", True)
    cor = mel.create_material_expression(mat, unreal.MaterialExpressionTextureSampleParameter2D, -500, 0)
    cor.set_editor_property("parameter_name", "Cor")
    mel.connect_material_property(cor, "RGB", unreal.MaterialProperty.MP_BASE_COLOR)
    if masked:
        mel.connect_material_property(cor, "A", unreal.MaterialProperty.MP_OPACITY_MASK)
    rough = mel.create_material_expression(mat, unreal.MaterialExpressionConstant, -500, 300)
    rough.set_editor_property("r", 0.85)
    mel.connect_material_property(rough, "", unreal.MaterialProperty.MP_ROUGHNESS)
    mel.recompile_material(mat)
    eal.save_loaded_asset(mat)
    return mat


def make_instance(master, name, texture):
    path = GAME + "/Materiais"
    full = path + "/" + name
    if eal.does_asset_exist(full):
        eal.delete_asset(full)
    mi = tools.create_asset(name, path, unreal.MaterialInstanceConstant, unreal.MaterialInstanceConstantFactoryNew())
    mi.set_editor_property("parent", master)
    if texture:
        mel.set_material_instance_texture_parameter_value(mi, "Cor", texture)
    eal.save_loaded_asset(mi)
    return mi


def slot_names(mesh):
    if isinstance(mesh, unreal.SkeletalMesh):
        return [str(m.material_slot_name) for m in mesh.materials]
    return [str(m.material_slot_name) for m in mesh.static_materials]


def set_slot(mesh, index, mi):
    if isinstance(mesh, unreal.SkeletalMesh):
        mats = list(mesh.materials)
        mats[index].set_editor_property("material_interface", mi)
        mesh.set_editor_property("materials", mats)
    else:
        mesh.set_material(index, mi)


def main():
    master = make_master("M_TdfBase", False)
    masked = make_master("M_TdfMascara", True)

    # ---- torres: Torre_<Tipo>_<estágio>.fbx, textura Textures/<nome>_cor.bytes
    tdir = os.path.join(RES, "Torres")
    for fn in sorted(os.listdir(tdir)):
        if not fn.endswith(".fbx"):
            continue
        name = fn[:-4]
        paths = import_fbx(os.path.join(tdir, fn), GAME + "/Torres", name, False)
        mesh = unreal.load_asset(GAME + "/Torres/" + name)
        if not mesh:
            say("FALHOU torre " + name)
            continue
        tex_src = os.path.join(tdir, "Textures", name + "_cor.bytes")
        tex = import_texture(tex_src, GAME + "/Texturas", name + "_cor") if os.path.exists(tex_src) else None
        mi = make_instance(master, "MI_" + name, tex)
        for i in range(len(slot_names(mesh))):
            set_slot(mesh, i, mi)
        nanite(mesh)
        eal.save_loaded_asset(mesh)
        b = mesh.get_bounds()
        say("torre %s: slots %s, caixa %.0f x %.0f x %.0f cm (origem em %.0f,%.0f,%.0f)" % (
            name, slot_names(mesh), b.box_extent.x * 2, b.box_extent.y * 2, b.box_extent.z * 2, b.origin.x, b.origin.y, b.origin.z))

    # ---- bichos: <nome>.fbx (esqueleto + animações); texturas Bichos/Textures/<slot>.jpg|_alfa.png
    bdir = os.path.join(RES, "Bichos")
    texdir = os.path.join(bdir, "Textures")
    for fn in sorted(os.listdir(bdir)):
        if not fn.endswith(".fbx"):
            continue
        name = fn[:-4]
        import_fbx(os.path.join(bdir, fn), GAME + "/Bichos", name, True)
        mesh = unreal.load_asset(GAME + "/Bichos/" + name)
        if not mesh:
            say("FALHOU bicho " + name)
            continue
        slots = slot_names(mesh)
        for i, slot in enumerate(slots):
            base = slot.split(".")[0]
            cand = [(base + ".jpg", False), (base + ".png", False), (base + "_alfa.png", True)]
            src = next(((os.path.join(texdir, c), m) for c, m in cand if os.path.exists(os.path.join(texdir, c))), None)
            tex = import_texture(src[0], GAME + "/Texturas", name + "_" + base) if src else None
            mi = make_instance(masked if (src and src[1]) else master, "MI_%s_%s" % (name, base), tex)
            set_slot(mesh, i, mi)
        eal.save_loaded_asset(mesh)
        b = mesh.get_bounds()
        say("bicho %s: slots %s, caixa %.0f x %.0f x %.0f cm" % (name, slots, b.box_extent.x * 2, b.box_extent.y * 2, b.box_extent.z * 2))

    eal.save_directory(GAME, only_if_is_dirty=False, recursive=True)
    out = os.path.join(REPO, "Unreal", "TDFende", "Saved", "importa_arte.log")
    os.makedirs(os.path.dirname(out), exist_ok=True)
    with open(out, "w", encoding="utf-8") as f:
        f.write("\n".join(log))
    say("pronto; relatório em " + out)


main()
