// Só compila com o URP presente (define vem do asmdef).
#if TDFENDE_URP
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace TDFende.EditorTools
{
    /// <summary>
    /// Liga o SSAO (oclusão de ambiente em tela) no renderer do URP — a sombra de contato
    /// onde a torre encosta no chão e entre os tufos de grama. É o que tira o aspecto de
    /// "objeto flutuando sobre o chão", e a grama não projeta sombra (seria caro), então o
    /// contato dela depende disto.
    ///
    /// Adicionar renderer feature por código é o mesmo passo a passo que o editor do próprio
    /// URP faz (ScriptableRendererDataEditor.AddComponent, URP 17): sub-asset + lista de
    /// features + mapa de ids. Idempotente: se já existe, não faz nada.
    /// </summary>
    [InitializeOnLoad]
    static class SsaoSetup
    {
        static SsaoSetup() => EditorApplication.delayCall += Ensure;

        static void Ensure()
        {
            try
            {
                if (!(GraphicsSettings.defaultRenderPipeline is UniversalRenderPipelineAsset urp)) return;

                var urpSo = new SerializedObject(urp);
                var list = urpSo.FindProperty("m_RendererDataList");
                if (list == null || list.arraySize == 0) return;
                var data = list.GetArrayElementAtIndex(0).objectReferenceValue as ScriptableRendererData;
                if (data == null) return;

                foreach (var f in data.rendererFeatures)
                    if (f is ScreenSpaceAmbientOcclusion) return; // já ligado

                var ssao = ScriptableObject.CreateInstance<ScreenSpaceAmbientOcclusion>();
                ssao.name = nameof(ScreenSpaceAmbientOcclusion);
                AssetDatabase.AddObjectToAsset(ssao, data);
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(ssao, out _, out long localId);

                var so = new SerializedObject(data);
                var features = so.FindProperty("m_RendererFeatures");
                var map = so.FindProperty("m_RendererFeatureMap");
                features.arraySize++;
                features.GetArrayElementAtIndex(features.arraySize - 1).objectReferenceValue = ssao;
                map.arraySize++;
                map.GetArrayElementAtIndex(map.arraySize - 1).longValue = localId;
                so.ApplyModifiedProperties();

                // O raio padrão (0,035) é para detalhe fino; nesta escala (1 unidade = 1 m,
                // câmera a ~30 m) ele não apareceria. Nomes conferidos em
                // ScreenSpaceAmbientOcclusionSettings do URP 17.3.
                var s = new SerializedObject(ssao);
                SetFloat(s, "m_Settings.Radius", 0.3f);
                SetFloat(s, "m_Settings.Intensity", 1.4f);
                SetFloat(s, "m_Settings.DirectLightingStrength", 0.3f);
                s.ApplyModifiedProperties();

                EditorUtility.SetDirty(data);
                AssetDatabase.SaveAssets();
                Debug.Log("[TDFende] SSAO ligado no renderer do URP.");
            }
            catch (System.Exception e)
            {
                // ferramenta auxiliar: se falhar, o jogo roda sem SSAO, não trava o editor
                Debug.LogWarning($"[TDFende] não consegui ligar o SSAO ({e.Message}). O jogo roda sem ele.");
            }
        }

        static void SetFloat(SerializedObject so, string path, float value)
        {
            var p = so.FindProperty(path);
            if (p != null) p.floatValue = value;
            else Debug.LogWarning($"[TDFende] SSAO: campo {path} não encontrado (URP mudou?).");
        }
    }
}
#endif
