# Importa o AMBIENTE do TDFende para o Content do Unreal, em /Game/TDFende/Ambiente (U1, fatia de beleza):
#   - chão: duas texturas Poly Haven 2k (aerial_grass_rock, leafy_grass) com normal, misturadas por um padrão grande (sem repetição
#     visível) num material com UV pelo mundo (M_TdfChao);
#   - cenário: rochas, tocos, troncos, barris e caixas da Poly Haven (FBX 1k + cor/normal/aspereza) -> /Ambiente/Cenario/<nome>;
#   - grama 3D: as touceiras dos FBX da Poly Haven (cada nó vira uma malha) -> /Ambiente/Grama/;
#   - materiais-mestre: M_TdfPbr (cor/normal/aspereza), M_TdfFolhagem (recorte, duas faces), M_TdfTerritorio (translúcido).
# Tudo CC0 (Poly Haven), já no repositório em Assets/Resources (THIRD_PARTY.md). Rode: ImportarArte.ps1 -Script importa_ambiente.py
import os
import shutil
import tempfile
import unreal

REPO = os.environ.get("TDF_REPO") or os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", ".."))
ART = os.path.join(REPO, "Assets", "Resources", "Art")
CEN = os.path.join(REPO, "Assets", "Resources", "TDFende", "Cenario")
GAME = "/Game/TDFende/Ambiente"
TMP = tempfile.mkdtemp(prefix="tdf_amb_")
tools = unreal.AssetToolsHelpers.get_asset_tools()
mel = unreal.MaterialEditingLibrary
eal = unreal.EditorAssetLibrary
log = []


def say(msg):
    unreal.log("[TDF-AMBIENTE] " + msg)
    log.append(msg)


def run_task(src, dest, name, options=None):
    task = unreal.AssetImportTask()
    task.filename = src
    task.destination_path = dest
    task.destination_name = name
    task.automated = True
    task.replace_existing = True
    task.replace_existing_settings = True
    task.save = True
    if options is not None:
        task.options = options
    tools.import_asset_tasks([task])
    return list(task.imported_object_paths or [])


def nanite(mesh):
    """Liga o Nanite na malha (detalhe geométrico alto com custo baixo; o padrão do UE5 para cenário e folhagem)."""
    try:
        s = mesh.get_editor_property("nanite_settings")
        if not s.enabled:
            s.enabled = True
            mesh.set_editor_property("nanite_settings", s)  # PostEditChange refaz a malha com Nanite
    except Exception as e:  # sem Nanite a malha continua valendo; só registra
        say("sem Nanite em %s: %s" % (mesh.get_name(), e))


def import_texture(src, name, kind="cor"):
    """kind: cor (sRGB), normal (mapa de normal), dado (aspereza etc., linear)."""
    ext = os.path.splitext(src)[1].lower()
    tmp = os.path.join(TMP, name + ext)
    shutil.copyfile(src, tmp)
    run_task(tmp, GAME + "/Texturas", name)
    tex = unreal.load_asset(GAME + "/Texturas/" + name)
    if not tex:
        say("FALHOU textura " + name)
        return None
    if kind == "normal":
        tex.set_editor_property("compression_settings", unreal.TextureCompressionSettings.TC_NORMALMAP)
        tex.set_editor_property("srgb", False)
    elif kind == "dado":
        tex.set_editor_property("compression_settings", unreal.TextureCompressionSettings.TC_MASKS)
        tex.set_editor_property("srgb", False)
    eal.save_loaded_asset(tex)
    return tex


def new_material(name):
    path = GAME + "/Materiais"
    full = path + "/" + name
    if eal.does_asset_exist(full):
        eal.delete_asset(full)
    mat = tools.create_asset(name, path, unreal.Material, unreal.MaterialFactoryNew())
    mat.set_editor_property("used_with_instanced_static_meshes", True)
    return mat


def tex_param(mat, name, x, y, default, normal=False):
    e = mel.create_material_expression(mat, unreal.MaterialExpressionTextureSampleParameter2D, x, y)
    e.set_editor_property("parameter_name", name)
    if default:
        e.set_editor_property("texture", default)
    if normal:
        e.set_editor_property("sampler_type", unreal.MaterialSamplerType.SAMPLERTYPE_NORMAL)
    return e


def const(mat, value, x, y):
    e = mel.create_material_expression(mat, unreal.MaterialExpressionConstant, x, y)
    e.set_editor_property("r", value)
    return e


def finish(mat):
    mel.recompile_material(mat)
    eal.save_loaded_asset(mat)
    return mat


def master_pbr():
    mat = new_material("M_TdfPbr")
    white = unreal.load_asset("/Engine/EngineResources/WhiteSquareTexture")
    flat = unreal.load_asset("/Engine/EngineMaterials/FlatNormal")
    cor = tex_param(mat, "Cor", -600, -200, white)
    nor = tex_param(mat, "Normal", -600, 100, flat, normal=True)
    asp = tex_param(mat, "Aspereza", -600, 400, white)
    mel.connect_material_property(cor, "RGB", unreal.MaterialProperty.MP_BASE_COLOR)
    mel.connect_material_property(nor, "RGB", unreal.MaterialProperty.MP_NORMAL)
    mel.connect_material_property(asp, "R", unreal.MaterialProperty.MP_ROUGHNESS)
    return finish(mat)


def master_folhagem():
    mat = new_material("M_TdfFolhagem")
    mat.set_editor_property("blend_mode", unreal.BlendMode.BLEND_MASKED)
    mat.set_editor_property("two_sided", True)
    white = unreal.load_asset("/Engine/EngineResources/WhiteSquareTexture")
    cor = tex_param(mat, "Cor", -600, 0, white)
    mel.connect_material_property(cor, "RGB", unreal.MaterialProperty.MP_BASE_COLOR)
    mel.connect_material_property(cor, "A", unreal.MaterialProperty.MP_OPACITY_MASK)
    mel.connect_material_property(const(mat, 0.85, -600, 300), "", unreal.MaterialProperty.MP_ROUGHNESS)
    return finish(mat)


def master_territorio():
    mat = new_material("M_TdfTerritorio")
    mat.set_editor_property("blend_mode", unreal.BlendMode.BLEND_TRANSLUCENT)
    mat.set_editor_property("shading_model", unreal.MaterialShadingModel.MSM_UNLIT)
    cor = mel.create_material_expression(mat, unreal.MaterialExpressionVectorParameter, -600, 0)
    cor.set_editor_property("parameter_name", "Cor")
    cor.set_editor_property("default_value", unreal.LinearColor(0.25, 0.55, 1.0, 1.0))
    opa = mel.create_material_expression(mat, unreal.MaterialExpressionScalarParameter, -600, 250)
    opa.set_editor_property("parameter_name", "Opacidade")
    opa.set_editor_property("default_value", 0.22)
    mel.connect_material_property(cor, "RGB", unreal.MaterialProperty.MP_EMISSIVE_COLOR)
    mel.connect_material_property(opa, "", unreal.MaterialProperty.MP_OPACITY)
    return finish(mat)


def chao(leafy, leafy_n, rock, rock_n):
    """Chão com UV pelo mundo: duas texturas misturadas por uma amostra grande (quebra a repetição) e variação de tom."""
    mat = new_material("M_TdfChao")
    wp = mel.create_material_expression(mat, unreal.MaterialExpressionWorldPosition, -1600, 0)
    xy = mel.create_material_expression(mat, unreal.MaterialExpressionComponentMask, -1400, 0)
    xy.set_editor_property("r", True)
    xy.set_editor_property("g", True)
    xy.set_editor_property("b", False)
    xy.set_editor_property("a", False)
    mel.connect_material_expressions(wp, "", xy, "")

    def uv(scale_cm, y):
        m = mel.create_material_expression(mat, unreal.MaterialExpressionMultiply, -1200, y)
        m.set_editor_property("const_b", 1.0 / scale_cm)
        mel.connect_material_expressions(xy, "", m, "A")
        return m

    def sample(tex, uvnode, x, y, normal=False):
        s = mel.create_material_expression(mat, unreal.MaterialExpressionTextureSample, x, y)
        s.set_editor_property("texture", tex)
        if normal:
            s.set_editor_property("sampler_type", unreal.MaterialSamplerType.SAMPLERTYPE_NORMAL)
        mel.connect_material_expressions(uvnode, "", s, "UVs")
        return s

    uv_leafy = uv(260.0, -400)     # um ladrilho a cada 2,6 m
    uv_rock = uv(620.0, -100)      # outro ritmo: os dois nunca repetem juntos
    uv_macro = uv(4100.0, 300)     # mancha grande (41 m) decide onde é grama e onde é chão batido
    uv_tone = uv(2300.0, 600)      # variação de tom (23 m)
    a = sample(leafy, uv_leafy, -900, -500)
    b = sample(rock, uv_rock, -900, -200)
    an = sample(leafy_n, uv_leafy, -900, 100, normal=True)
    bn = sample(rock_n, uv_rock, -900, 400, normal=True)
    macro = sample(rock, uv_macro, -900, 700)
    tone = sample(leafy, uv_tone, -900, 1000)

    sub = mel.create_material_expression(mat, unreal.MaterialExpressionSubtract, -650, 700)
    sub.set_editor_property("const_b", 0.52)
    mel.connect_material_expressions(macro, "G", sub, "A")
    mul = mel.create_material_expression(mat, unreal.MaterialExpressionMultiply, -500, 700)
    mul.set_editor_property("const_b", 2.5)
    mel.connect_material_expressions(sub, "", mul, "A")
    alpha = mel.create_material_expression(mat, unreal.MaterialExpressionSaturate, -350, 700)
    mel.connect_material_expressions(mul, "", alpha, "")

    base = mel.create_material_expression(mat, unreal.MaterialExpressionLinearInterpolate, -300, -350)
    mel.connect_material_expressions(a, "RGB", base, "A")
    mel.connect_material_expressions(b, "RGB", base, "B")
    mel.connect_material_expressions(alpha, "", base, "Alpha")

    tone_l = mel.create_material_expression(mat, unreal.MaterialExpressionLinearInterpolate, -300, 1000)
    tone_l.set_editor_property("const_a", 0.68)
    tone_l.set_editor_property("const_b", 0.98)
    mel.connect_material_expressions(tone, "R", tone_l, "Alpha")
    color = mel.create_material_expression(mat, unreal.MaterialExpressionMultiply, -100, -200)
    mel.connect_material_expressions(base, "", color, "A")
    mel.connect_material_expressions(tone_l, "", color, "B")

    nrm = mel.create_material_expression(mat, unreal.MaterialExpressionLinearInterpolate, -300, 250)
    mel.connect_material_expressions(an, "RGB", nrm, "A")
    mel.connect_material_expressions(bn, "RGB", nrm, "B")
    mel.connect_material_expressions(alpha, "", nrm, "Alpha")

    mel.connect_material_property(color, "", unreal.MaterialProperty.MP_BASE_COLOR)
    mel.connect_material_property(nrm, "", unreal.MaterialProperty.MP_NORMAL)
    mel.connect_material_property(const(mat, 0.93, -100, 500), "", unreal.MaterialProperty.MP_ROUGHNESS)
    return finish(mat)


def instance(master, name, params):
    path = GAME + "/Materiais"
    full = path + "/" + name
    if eal.does_asset_exist(full):
        eal.delete_asset(full)
    mi = tools.create_asset(name, path, unreal.MaterialInstanceConstant, unreal.MaterialInstanceConstantFactoryNew())
    mi.set_editor_property("parent", master)
    for pname, tex in params.items():
        if tex:
            mel.set_material_instance_texture_parameter_value(mi, pname, tex)
    eal.save_loaded_asset(mi)
    return mi


def fbx_ui(combine):
    ui = unreal.FbxImportUI()
    ui.import_mesh = True
    ui.import_textures = False
    ui.import_materials = False
    ui.import_as_skeletal = False
    ui.import_animations = False
    ui.mesh_type_to_import = unreal.FBXImportType.FBXIT_STATIC_MESH
    ui.static_mesh_import_data.set_editor_property("combine_meshes", combine)
    ui.static_mesh_import_data.set_editor_property("generate_lightmap_u_vs", False)
    return ui


def find_tex(folder, *keys):
    if not os.path.isdir(folder):
        return None
    for fn in sorted(os.listdir(folder)):
        low = fn.lower()
        if low.endswith(".meta"):
            continue
        if any(("_" + k + "_") in low for k in keys):
            return os.path.join(folder, fn)
    return None


def main():
    pbr = master_pbr()
    fol = master_folhagem()
    master_territorio()

    gdir = os.path.join(ART, "Ground")
    leafy = import_texture(os.path.join(gdir, "leafy_grass_diff_2k.jpg"), "T_Chao_Folhas_Cor")
    leafy_n = import_texture(os.path.join(gdir, "leafy_grass_nor_gl_2k.jpg"), "T_Chao_Folhas_Normal", "normal")
    rock = import_texture(os.path.join(gdir, "aerial_grass_rock_diff_2k.jpg"), "T_Chao_Pedra_Cor")
    rock_n = import_texture(os.path.join(gdir, "aerial_grass_rock_nor_gl_2k.jpg"), "T_Chao_Pedra_Normal", "normal")
    chao(leafy, leafy_n, rock, rock_n)
    say("chão: M_TdfChao (folhas + grama com pedra, UV pelo mundo)")

    # cenário (Poly Haven): uma malha por modelo, material PBR com as texturas dele
    for cat in sorted(os.listdir(CEN)):
        cdir = os.path.join(CEN, cat)
        if not os.path.isdir(cdir):
            continue
        for model in sorted(os.listdir(cdir)):
            mdir = os.path.join(cdir, model)
            fbx = os.path.join(mdir, model + ".fbx")
            if not os.path.isfile(fbx):
                continue
            name = "SM_" + model
            run_task(fbx, GAME + "/Cenario", name, fbx_ui(True))
            mesh = unreal.load_asset(GAME + "/Cenario/" + name)
            if not mesh:
                say("FALHOU cenário " + model)
                continue
            tdir = os.path.join(mdir, "textures")
            cor = find_tex(tdir, "diff", "diffuse", "albedo")
            nor = find_tex(tdir, "nor_gl", "normal")
            rgh = find_tex(tdir, "rough", "roughness")
            t_cor = import_texture(cor, "T_" + model + "_Cor") if cor else None
            t_nor = import_texture(nor, "T_" + model + "_Normal", "normal") if nor else None
            t_rgh = import_texture(rgh, "T_" + model + "_Aspereza", "dado") if rgh else None
            mi = instance(pbr, "MI_" + model, {"Cor": t_cor, "Normal": t_nor, "Aspereza": t_rgh})
            for i in range(len(mesh.static_materials)):
                mesh.set_material(i, mi)
            nanite(mesh)
            eal.save_loaded_asset(mesh)
            b = mesh.get_bounds()
            say("cenário %s/%s: %.0f x %.0f x %.0f cm" % (cat, model, b.box_extent.x * 2, b.box_extent.y * 2, b.box_extent.z * 2))

    # grama 3D: cada touceira do mostruário vira uma malha (combine_meshes = False)
    grdir = os.path.join(ART, "Grass")
    for fn in sorted(os.listdir(grdir)):
        if not fn.endswith(".fbx"):
            continue
        base = fn[:-4]
        tex_src = os.path.join(grdir, base.replace("_1k", "") + "_diffalpha_1k.png")
        tex = import_texture(tex_src, "T_" + base + "_Cor") if os.path.exists(tex_src) else None
        mi = instance(fol, "MI_" + base, {"Cor": tex})
        paths = run_task(os.path.join(grdir, fn), GAME + "/Grama", "SM_" + base, fbx_ui(False))
        n = 0
        for p in paths:
            mesh = unreal.load_asset(p)
            if isinstance(mesh, unreal.StaticMesh):
                for i in range(len(mesh.static_materials)):
                    mesh.set_material(i, mi)
                nanite(mesh)
                eal.save_loaded_asset(mesh)
                b = mesh.get_bounds()
                say("  touceira %s: %.0f x %.0f x %.0f cm" % (mesh.get_name(), b.box_extent.x * 2, b.box_extent.y * 2, b.box_extent.z * 2))
                n += 1
        say("grama %s: %d touceiras" % (base, n))

    eal.save_directory(GAME, only_if_is_dirty=False, recursive=True)
    out = os.path.join(REPO, "Unreal", "TDFende", "Saved", "importa_ambiente.log")
    os.makedirs(os.path.dirname(out), exist_ok=True)
    with open(out, "w", encoding="utf-8") as f:
        f.write("\n".join(log))
    say("pronto; relatório em " + out)


main()
