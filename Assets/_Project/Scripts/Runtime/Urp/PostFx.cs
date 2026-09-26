using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace TDFende.Urp
{
    /// <summary>
    /// Pós-processamento — direção REALISTA (decidida 25/09/2026).
    ///
    /// Inverte a escolha feita para o cartoon um dia antes: lá o ACES saiu porque, sendo
    /// fílmico, desatura realces e o brinquedo precisava de cor pura. No realista é
    /// exatamente isso que se quer — o ACES é a curva de câmera de cinema, e é a diferença
    /// entre "renderizado" e "fotografado". Saturação volta para quase neutra, vinheta
    /// discreta de lente, bloom só no que é realmente claro.
    ///
    /// Antisserrilhado SMAA na câmera: sem ele, borda de cubo e folha de grama cintilam ao
    /// mover a câmera. MSAA não resolveria a grama (recorte por alfa não é borda de
    /// polígono), e TAA deixaria rastro nos inimigos em movimento.
    ///
    /// Esta assembly INTEIRA só compila se o URP estiver instalado
    /// (defineConstraints no asmdef). Sem URP, ela simplesmente não existe e o
    /// jogo roda igual no Built-in — por isso ela se registra sozinha em vez de
    /// ser chamada pelo GameController.
    /// </summary>
    public static class PostFx
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Setup()
        {
            if (GraphicsSettings.currentRenderPipeline == null) return; // URP instalado mas não ativo

            var profile = ScriptableObject.CreateInstance<VolumeProfile>();

            var tone = profile.Add<Tonemapping>(true);
            tone.mode.Override(TonemappingMode.ACES);

            var bloom = profile.Add<Bloom>(true);
            bloom.intensity.Override(0.25f);
            bloom.threshold.Override(1.0f);
            bloom.scatter.Override(0.65f);

            var vignette = profile.Add<Vignette>(true);
            vignette.intensity.Override(0.18f);
            vignette.smoothness.Override(0.4f);

            var grade = profile.Add<ColorAdjustments>(true);
            grade.postExposure.Override(0.1f);
            grade.contrast.Override(6f);
            grade.saturation.Override(4f);

            var go = new GameObject("PostFX");
            var volume = go.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 0f;
            volume.profile = profile;

            var cam = Camera.main;
            if (cam != null)
            {
                var data = cam.GetUniversalAdditionalCameraData();
                if (data != null)
                {
                    data.renderPostProcessing = true;
                    data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
                    data.antialiasingQuality = AntialiasingQuality.High;
                    data.renderShadows = true;
                }
            }
        }
    }
}
