using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace TDFende.Urp
{
    /// <summary>
    /// Bloom, vinheta e color grading — o acabamento que faz primitivas
    /// parecerem estilo, não protótipo.
    ///
    /// Cartoon colorido: ACES é tonemap FÍLMICO — ele estoura o vermelho e desatura
    /// realces, exatamente o oposto do "cor de brinquedo, sempre pura" que cartoon
    /// pede. Vinheta forte é escolha de clima sombrio/terror, não de brinquedo.
    /// Bloom pesado empurra pra sci-fi neon. Trocados os três; saturação sobe MUITO
    /// (cartoon vive de cor saturada), contraste desce um pouco (o visual antigo
    /// "escuro com acento neon" tinha alto contraste; cartoon é mais chapado/direto).
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
            tone.mode.Override(TonemappingMode.Neutral); // preserva cor saturada, sem rolloff fílmico

            var bloom = profile.Add<Bloom>(true);
            bloom.intensity.Override(0.35f);
            bloom.threshold.Override(1.1f); // só o que é bem claro brilha (projétil), não tudo
            bloom.scatter.Override(0.5f);

            var vignette = profile.Add<Vignette>(true);
            vignette.intensity.Override(0.10f); // quase nada: vinheta forte é clima sombrio
            vignette.smoothness.Override(0.6f);

            var grade = profile.Add<ColorAdjustments>(true);
            grade.postExposure.Override(0.2f);
            grade.contrast.Override(4f);
            grade.saturation.Override(28f);

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
