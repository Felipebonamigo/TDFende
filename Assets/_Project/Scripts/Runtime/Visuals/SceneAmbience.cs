using UnityEngine;
using UnityEngine.Rendering;

namespace TDFende
{
    /// <summary>
    /// Luz e atmosfera — direção REALISTA (decidida 25/09/2026).
    ///
    /// O que mais separa "realista" de "protótipo" não é polígono, é luz:
    ///   - céu HDRI (Poly Haven, CC0) como fonte da luz ambiente e dos reflexos: as faces
    ///     em sombra ganham o azul do céu e o chão ganha o verde rebatido, de graça;
    ///   - UM sol, com sombra suave e forte — ao ar livre a sombra do sol é nítida;
    ///   - neblina leve e azulada ao longe (perspectiva atmosférica), que também esconde
    ///     a borda do terreno.
    /// A luz de preenchimento azul da versão cartoon saiu: com o céu iluminando, ela só
    /// achatava o volume.
    ///
    /// Degrada sem quebrar: sem o HDRI, usa o céu procedural nativo do Unity; sem ele,
    /// cor chapada. O jogo roda nos três casos.
    /// </summary>
    public static class SceneAmbience
    {
        const string SkyPath = "Art/Sky/kloofendal_48d_partly_cloudy_puresky_2k";

        static readonly Color SunColor = new Color(1f, 0.955f, 0.885f);
        static readonly Color Haze = new Color(0.70f, 0.78f, 0.86f);

        public static void Apply(Camera cam)
        {
            // desliga luzes que já existam na cena (a SampleScene padrão traz uma):
            // a iluminação do jogo tem que ser determinística, não depender da cena aberta
            foreach (var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
                if (l.type == LightType.Directional) l.enabled = false;

            var sky = MakeHdriSky() ?? MakeProceduralSky();
            if (sky != null)
            {
                RenderSettings.skybox = sky;
                RenderSettings.ambientMode = AmbientMode.Skybox;
                // Meia intensidade porque o sol está no HDRI E na luz "Sol": com 1, o sol
                // entra duas vezes (medido no .hdr: ambiente ~1,5 contra ~1,26 de luz direta)
                // e a sombra fica só ~45% mais escura que o chão ao sol — imagem chapada.
                RenderSettings.ambientIntensity = 0.5f;
                RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox;
                RenderSettings.reflectionIntensity = 1f;
                // recalcula a luz ambiente a partir do céu novo; sem isto ela fica
                // presa na do céu que existia quando a cena abriu
                DynamicGI.UpdateEnvironment();
                if (cam != null) cam.clearFlags = CameraClearFlags.Skybox;
            }
            else
            {
                RenderSettings.ambientMode = AmbientMode.Flat;
                RenderSettings.ambientLight = Palette.Ambient;
                RenderSettings.skybox = null;
                if (cam != null)
                {
                    cam.clearFlags = CameraClearFlags.SolidColor;
                    cam.backgroundColor = Palette.Background;
                }
            }

            // perspectiva atmosférica: começa depois das lanes e engole a borda do terreno
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = Haze;
            RenderSettings.fogStartDistance = 55f;
            RenderSettings.fogEndDistance = 170f;

            var sun = new GameObject("Sol").AddComponent<Light>();
            sun.transform.rotation = Quaternion.Euler(52f, -35f, 0f);
            sun.type = LightType.Directional;
            sun.color = SunColor;
            sun.intensity = 1.6f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.85f;
        }

        static Material MakeHdriSky()
        {
            var hdr = Resources.Load<Texture2D>(SkyPath);
            if (hdr == null) return null;

            // TODO(build): Shader.Find sofre stripping em build (ver GroundBuilder).
            var shader = Shader.Find("Skybox/Panoramic");
            if (shader == null) return null;

            var m = new Material(shader) { name = "CeuHDRI" };
            if (!m.HasProperty("_MainTex"))
            {
                // _MainTex/_Mapping/_ImageType conferidos no unity_builtin_extra do editor
                // 6000.3.11f1; a guarda fica para versão futura que renomeie — avisa em vez
                // de desenhar um céu cinza em silêncio
                Debug.LogWarning("[TDFende] Skybox/Panoramic sem _MainTex; usando céu procedural.");
                return null;
            }
            m.SetTexture("_MainTex", hdr);
            m.SetFloat("_Mapping", 1f);   // latitude-longitude
            m.SetFloat("_ImageType", 0f); // 360°
            m.SetFloat("_Exposure", 1f);
            return m;
        }

        static Material MakeProceduralSky()
        {
            var shader = Shader.Find("Skybox/Procedural");
            if (shader == null) return null;
            var m = new Material(shader) { name = "CeuProcedural" };
            m.SetFloat("_SunSize", 0.04f);
            m.SetFloat("_AtmosphereThickness", 1f);
            m.SetFloat("_Exposure", 1.25f);
            return m;
        }
    }
}
