# Materiais dos EFEITOS de combate do TDFende no Unreal, em /Game/TDFende/Efeitos (U2). Nada de arte de fora: só materiais.
#   - M_TdfBrilho: aditivo, sem luz; cor e intensidade POR INSTÂNCIA (dados customizados 0-2 cor, 3 intensidade), borda que se
#     dissolve (núcleo de tiro, rastro, clarão de disparo e de impacto: o bloom faz o resto);
#   - M_TdfFumaca: translúcido, sem luz; cor e opacidade por instância, borda macia (poeira, fumaça do canhão);
#   - M_TdfStatus: sobreposição no bicho (overlay material): brilho de borda na cor do efeito (gelo, lentidão, fogo), pulsando.
# Rode: ImportarArte.ps1 -Script importa_efeitos.py   (refaz os três do zero; o C++ cai no material básico se faltarem)
import os
import unreal

REPO = os.environ.get("TDF_REPO") or os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", ".."))
GAME = "/Game/TDFende/Efeitos"
tools = unreal.AssetToolsHelpers.get_asset_tools()
mel = unreal.MaterialEditingLibrary
eal = unreal.EditorAssetLibrary
log = []


def say(msg):
    unreal.log("[TDF-EFEITOS] " + msg)
    log.append(msg)


def new_material(name, blend, flags):
    full = GAME + "/" + name
    if eal.does_asset_exist(full):
        eal.delete_asset(full)
    mat = tools.create_asset(name, GAME, unreal.Material, unreal.MaterialFactoryNew())
    mat.set_editor_property("blend_mode", blend)
    mat.set_editor_property("shading_model", unreal.MaterialShadingModel.MSM_UNLIT)
    for f in flags:
        mat.set_editor_property(f, True)
    return mat


def node(mat, cls, x, y, **props):
    n = mel.create_material_expression(mat, cls, x, y)
    for k, v in props.items():
        n.set_editor_property(k, v)
    return n


def soft_edge(mat, exponent, x, y):
    """1 no centro da esfera, 0 na borda (vista de frente): o tiro e a fumaça não têm contorno duro."""
    fres = node(mat, unreal.MaterialExpressionFresnel, x, y, exponent=exponent)
    one = node(mat, unreal.MaterialExpressionOneMinus, x + 200, y)
    mel.connect_material_expressions(fres, "", one, "")
    return one


def finish(mat):
    mel.recompile_material(mat)
    eal.save_loaded_asset(mat)
    say("material " + mat.get_name())
    return mat


def brilho():
    mat = new_material("M_TdfBrilho", unreal.BlendMode.BLEND_ADDITIVE, ["used_with_instanced_static_meshes"])
    cor = node(mat, unreal.MaterialExpressionPerInstanceCustomData3Vector, -900, 0, data_index=0)
    inten = node(mat, unreal.MaterialExpressionPerInstanceCustomData, -900, 200, data_index=3)
    edge = soft_edge(mat, 1.6, -1100, 400)
    # brilho concentrado no centro (sem isto o clarão parece uma bolha de contorno visível)
    soft = node(mat, unreal.MaterialExpressionPower, -700, 400, const_exponent=2.5)
    mel.connect_material_expressions(edge, "", soft, "Base")
    a = node(mat, unreal.MaterialExpressionMultiply, -500, 100)
    mel.connect_material_expressions(cor, "", a, "A")
    mel.connect_material_expressions(inten, "", a, "B")
    b = node(mat, unreal.MaterialExpressionMultiply, -300, 200)
    mel.connect_material_expressions(a, "", b, "A")
    mel.connect_material_expressions(soft, "", b, "B")
    mel.connect_material_property(b, "", unreal.MaterialProperty.MP_EMISSIVE_COLOR)
    return finish(mat)


def fumaca():
    mat = new_material("M_TdfFumaca", unreal.BlendMode.BLEND_TRANSLUCENT, ["used_with_instanced_static_meshes"])
    cor = node(mat, unreal.MaterialExpressionPerInstanceCustomData3Vector, -900, 0, data_index=0)
    alpha = node(mat, unreal.MaterialExpressionPerInstanceCustomData, -900, 200, data_index=3)
    soft = soft_edge(mat, 1.2, -900, 400)
    op = node(mat, unreal.MaterialExpressionMultiply, -400, 300)
    mel.connect_material_expressions(alpha, "", op, "A")
    mel.connect_material_expressions(soft, "", op, "B")
    mel.connect_material_property(cor, "", unreal.MaterialProperty.MP_EMISSIVE_COLOR)
    mel.connect_material_property(op, "", unreal.MaterialProperty.MP_OPACITY)
    return finish(mat)


def status():
    mat = new_material("M_TdfStatus", unreal.BlendMode.BLEND_ADDITIVE, ["used_with_skeletal_mesh"])
    cor = node(mat, unreal.MaterialExpressionVectorParameter, -1100, 0, parameter_name="Cor", default_value=unreal.LinearColor(0.3, 0.7, 1.0, 1.0))
    inten = node(mat, unreal.MaterialExpressionScalarParameter, -1100, 200, parameter_name="Intensidade", default_value=2.0)
    pulso = node(mat, unreal.MaterialExpressionScalarParameter, -1100, 300, parameter_name="Pulso", default_value=0.0)
    # borda acesa (fresnel) + um véu fraco no corpo inteiro
    fres = node(mat, unreal.MaterialExpressionFresnel, -1100, 450, exponent=2.5)
    veil = node(mat, unreal.MaterialExpressionAdd, -900, 450, const_b=0.12)
    mel.connect_material_expressions(fres, "", veil, "A")
    # pulso: 1 + Pulso * seno(9 t)
    time = node(mat, unreal.MaterialExpressionTime, -1100, 650)
    speed = node(mat, unreal.MaterialExpressionMultiply, -950, 650, const_b=9.0)
    mel.connect_material_expressions(time, "", speed, "A")
    sine = node(mat, unreal.MaterialExpressionSine, -800, 650, period=6.2831853)
    mel.connect_material_expressions(speed, "", sine, "")
    wobble = node(mat, unreal.MaterialExpressionMultiply, -650, 600)
    mel.connect_material_expressions(pulso, "", wobble, "A")
    mel.connect_material_expressions(sine, "", wobble, "B")
    beat = node(mat, unreal.MaterialExpressionAdd, -500, 600, const_b=1.0)
    mel.connect_material_expressions(wobble, "", beat, "A")
    m1 = node(mat, unreal.MaterialExpressionMultiply, -700, 100)
    mel.connect_material_expressions(cor, "", m1, "A")
    mel.connect_material_expressions(inten, "", m1, "B")
    m2 = node(mat, unreal.MaterialExpressionMultiply, -500, 250)
    mel.connect_material_expressions(m1, "", m2, "A")
    mel.connect_material_expressions(veil, "", m2, "B")
    m3 = node(mat, unreal.MaterialExpressionMultiply, -300, 350)
    mel.connect_material_expressions(m2, "", m3, "A")
    mel.connect_material_expressions(beat, "", m3, "B")
    mel.connect_material_property(m3, "", unreal.MaterialProperty.MP_EMISSIVE_COLOR)
    return finish(mat)


def main():
    if not eal.does_directory_exist(GAME):
        eal.make_directory(GAME)
    brilho()
    fumaca()
    status()
    # relatório que o ImportarArte.ps1 mostra (sem ele o script conta como falha)
    with open(os.path.join(REPO, "Unreal", "TDFende", "Saved", "importa_efeitos.log"), "w", encoding="utf-8") as f:
        f.write("\n".join(log))


main()
