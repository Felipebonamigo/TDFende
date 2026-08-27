// Só compila quando o URP já está instalado (define vem do asmdef).
// Cria e ativa o pipeline asset automaticamente — zero cliques no editor.
#if TDFENDE_URP
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace TDFende.EditorTools
{
    [InitializeOnLoad]
    static class UrpAutoSetup
    {
        static UrpAutoSetup() => EditorApplication.delayCall += TrySetup;

        static void TrySetup()
        {
            if (GraphicsSettings.defaultRenderPipeline != null) return; // já configurado
            try
            {
                if (!AssetDatabase.IsValidFolder("Assets/Settings"))
                    AssetDatabase.CreateFolder("Assets", "Settings");

                var renderer = ScriptableObject.CreateInstance<UniversalRendererData>();

                // SEM ISTO, o URP desliga TODO o pós-processamento — em silêncio.
                // O campo nasce nulo, não tem [Reload] (então o ResourceReloader do URP não
                // o conserta), e o UniversalRenderer trata nulo como "pós-processamento
                // desativado": nem bloom, nem tonemap, nem vinheta, sem um aviso sequer.
                // O PostFx montaria o VolumeProfile inteiro e nada apareceria na tela.
                // GetDefaultPostProcessData() é internal do pacote, então carregamos o
                // mesmo asset que ele usa, pelo caminho.
                renderer.postProcessData = AssetDatabase.LoadAssetAtPath<PostProcessData>(
                    "Packages/com.unity.render-pipelines.universal/Runtime/Data/PostProcessData.asset");
                if (renderer.postProcessData == null)
                    Debug.LogWarning("[TDFende] PostProcessData não encontrado: bloom, vinheta e " +
                                     "tonemap ficarão desligados. O jogo roda, só fica mais cru.");

                AssetDatabase.CreateAsset(renderer, "Assets/Settings/URP_Renderer.asset");

                var pipeline = UniversalRenderPipelineAsset.Create(renderer);
                AssetDatabase.CreateAsset(pipeline, "Assets/Settings/URP_Asset.asset");

                GraphicsSettings.defaultRenderPipeline = pipeline;
                QualitySettings.renderPipeline = pipeline;
                AssetDatabase.SaveAssets();
                Debug.Log("[TDFende] URP ativado automaticamente.");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[TDFende] Não consegui ativar o URP sozinho ({e.Message}). " +
                                 "O jogo roda no Built-in mesmo assim.");
            }
        }
    }
}
#endif
