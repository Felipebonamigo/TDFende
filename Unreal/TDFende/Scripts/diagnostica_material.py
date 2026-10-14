# Diagnóstico dos materiais importados: imprime pai, parâmetros e texturas. Rode: ImportarArte.ps1 -Script diagnostica_material.py
import unreal

for path in ("/Game/TDFende/Materiais/M_TdfBase", "/Game/TDFende/Materiais/MI_Torre_Canhao_1", "/Game/TDFende/Materiais/MI_rato_rato_1"):
    a = unreal.load_asset(path)
    unreal.log("[TDF-DIAG] %s -> %s" % (path, a))
    if isinstance(a, unreal.MaterialInstanceConstant):
        unreal.log("[TDF-DIAG]   pai: %s" % a.get_editor_property("parent"))
        unreal.log("[TDF-DIAG]   texturas: %s" % [(p.parameter_info.name, p.parameter_value) for p in a.get_editor_property("texture_parameter_values")])
    elif isinstance(a, unreal.Material):
        exprs = unreal.MaterialEditingLibrary.get_num_material_expressions(a)
        unreal.log("[TDF-DIAG]   expressões: %d" % exprs)
        for prop in (unreal.MaterialProperty.MP_BASE_COLOR, unreal.MaterialProperty.MP_ROUGHNESS):
            node = unreal.MaterialEditingLibrary.get_material_property_input_node(a, prop)
            unreal.log("[TDF-DIAG]   %s <- %s" % (prop, node))

m = unreal.load_asset("/Game/TDFende/Torres/Torre_Canhao_1")
unreal.log("[TDF-DIAG] torre: materiais %s" % [str(x.material_interface) for x in m.static_materials])
t = unreal.load_asset("/Game/TDFende/Texturas/Torre_Canhao_1_cor")
unreal.log("[TDF-DIAG] textura: %s  %sx%s" % (t, t.blueprint_get_size_x() if t else "-", t.blueprint_get_size_y() if t else "-"))
