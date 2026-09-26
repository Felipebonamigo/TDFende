using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace TDFende.Urp
{
    /// <summary>
    /// Tonemap, bloom, vinheta e color grading — o acabamento fotográfico.
    ///
    /// Realista (26/09/2026): volta o ACES, o tonemap fílmico — ele comprime o
    /// realce como filme e câmera de verdade, que é exatamente o que o cartoon não
    /// queria e o realista quer. Bloom só no que é bem claro (clarão de pólvora,
    /// cristal de gelo), vinheta leve de lente, saturação levemente abaixo do neutro
    /// e balanço de branco um tico quente (luz de tarde).
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
            bloom.intensity.Override(0.28f);
            bloom.threshold.Override(1.05f); // só o que é bem claro brilha (clarão, cristal), não tudo
            bloom.scatter.Override(0.6f);

            var vignette = profile.Add<Vignette>(true);
            vignette.intensity.Override(0.22f); // vinheta de lente, não de clima sombrio
            vignette.smoothness.Override(0.45f);

            var grade = profile.Add<ColorAdjustments>(true);
            grade.postExposure.Override(0.35f); // ACES escurece; a exposição devolve o meio-tom
            grade.contrast.Override(10f);
            grade.saturation.Override(-6f);

            var white = profile.Add<WhiteBalance>(true);
            white.temperature.Override(6f);

            var go = new GameObject("PostFX");
            var volume = go.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 0f;
            volume.profile = profile;

            var cam = Camera.main;
            if (cam != null)
            {
                var data = cam.GetUniversalAdditionalCameraData();
                if (data != null) data.renderPostProcessing = true;
            }
        }
    }
}
