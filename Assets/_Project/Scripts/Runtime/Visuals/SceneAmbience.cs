using UnityEngine;
using UnityEngine.Rendering;

namespace TDFende
{
    /// <summary>
    /// Luz, céu e atmosfera. Usa só RenderSettings, Light e ReflectionProbe, que
    /// funcionam igual em URP e Built-in — nenhuma dependência de pacote.
    ///
    /// Realista: um sol só (luz de preenchimento colorida é truque de cartoon), céu
    /// físico procedural, ambiente em três faixas (céu, horizonte, chão) e névoa de
    /// horizonte que esconde a borda do mundo. Metal só parece metal se tiver o que
    /// refletir — por isso a sonda de reflexo, capturada uma vez depois do mundo pronto.
    /// </summary>
    public static class SceneAmbience
    {
        static readonly Color Horizon = new Color(0.72f, 0.78f, 0.82f);

        public static Light Sun { get; private set; }

        public static void Apply(Camera cam)
        {
            // desliga luzes que já existam na cena (a SampleScene padrão traz uma):
            // a iluminação do jogo tem que ser determinística, não depender da cena aberta
            foreach (var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
                if (l.type == LightType.Directional) l.enabled = false;

            var go = new GameObject("Sol");
            go.transform.rotation = Quaternion.Euler(42f, -38f, 0f);
            Sun = go.AddComponent<Light>();
            Sun.type = LightType.Directional;
            Sun.color = new Color(1f, 0.94f, 0.85f);
            Sun.intensity = 1.55f;
            Sun.shadows = LightShadows.Soft;
            Sun.shadowStrength = 0.88f;
            RenderSettings.sun = Sun;

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.56f, 0.65f, 0.76f);
            RenderSettings.ambientEquatorColor = new Color(0.58f, 0.57f, 0.52f);
            RenderSettings.ambientGroundColor = new Color(0.27f, 0.25f, 0.21f);

            var skyShader = Shader.Find("Skybox/Procedural");
            if (skyShader != null)
            {
                var sky = new Material(skyShader);
                sky.EnableKeyword("_SUNDISK_HIGH_QUALITY");
                sky.SetFloat("_SunSize", 0.035f);
                sky.SetFloat("_SunSizeConvergence", 6f);
                sky.SetFloat("_AtmosphereThickness", 0.85f);
                sky.SetColor("_SkyTint", new Color(0.5f, 0.53f, 0.58f));
                sky.SetColor("_GroundColor", new Color(0.42f, 0.41f, 0.38f));
                sky.SetFloat("_Exposure", 1.1f);
                RenderSettings.skybox = sky;
            }
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox;

            // névoa de horizonte: começa depois do tabuleiro, some o fim do mundo
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = Horizon;
            RenderSettings.fogStartDistance = 45f;
            RenderSettings.fogEndDistance = 150f;

            if (cam != null)
            {
                cam.clearFlags = skyShader != null ? CameraClearFlags.Skybox : CameraClearFlags.SolidColor;
                cam.backgroundColor = Horizon;
                cam.farClipPlane = 400f;
            }
        }

        /// <summary>
        /// Fotografa o mundo para os reflexos (bronze, ferro, água). Chamar DEPOIS de
        /// montar terreno e cenário, uma vez — é caro, e o cenário não muda.
        /// </summary>
        public static void CaptureReflections(Vector3 center)
        {
            var go = new GameObject("SondaReflexo");
            go.transform.position = center + Vector3.up * 3f;
            var probe = go.AddComponent<ReflectionProbe>();
            probe.mode = ReflectionProbeMode.Realtime;
            probe.refreshMode = ReflectionProbeRefreshMode.ViaScripting;
            probe.timeSlicingMode = ReflectionProbeTimeSlicingMode.NoTimeSlicing;
            probe.size = new Vector3(220f, 80f, 220f);
            probe.resolution = 128;
            probe.hdr = true;
            probe.clearFlags = ReflectionProbeClearFlags.Skybox;
            probe.RenderProbe();
        }
    }
}
