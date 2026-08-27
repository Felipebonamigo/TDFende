using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace TDFende.Urp
{
    /// <summary>
    /// Bloom, vinheta e color grading — o acabamento que faz primitivas
    /// parecerem estilo, não protótipo.
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
            bloom.intensity.Override(0.85f);
            bloom.threshold.Override(0.95f);
            bloom.scatter.Override(0.62f);

            var vignette = profile.Add<Vignette>(true);
            vignette.intensity.Override(0.28f);
            vignette.smoothness.Override(0.45f);

            var grade = profile.Add<ColorAdjustments>(true);
            grade.postExposure.Override(0.15f);
            grade.contrast.Override(12f);
            grade.saturation.Override(8f);

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
