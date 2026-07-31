using UnityEngine;

namespace FrontierTD
{
    /// <summary>
    /// Luz e atmosfera. Usa só RenderSettings e Light, que funcionam igual em
    /// URP e Built-in — nenhuma dependência de pacote.
    /// É o item que mais transforma o visual por R$ 0: sol quente + luz de
    /// preenchimento fria + névoa dá volume a um mundo feito de cubos.
    /// </summary>
    public static class SceneAmbience
    {
        public static void Apply(Camera cam)
        {
            // desliga luzes que já existam na cena (a SampleScene padrão traz uma):
            // a iluminação do jogo tem que ser determinística, não depender da cena aberta
            foreach (var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
                if (l.type == LightType.Directional) l.enabled = false;

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = Palette.Ambient;
            RenderSettings.skybox = null;

            // névoa: escurece o fundo do mapa e cria profundidade
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = Palette.Background;
            RenderSettings.fogStartDistance = 22f;
            RenderSettings.fogEndDistance = 65f;

            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = Palette.Background;
            }

            var sun = CreateLight("Sol", Palette.SunColor, 1.15f, new Vector3(52f, -38f, 0f));
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.55f;

            // preenchimento frio no lado oposto: tira o preto chapado das faces em sombra
            CreateLight("LuzPreenchimento", Palette.FillLight, 0.42f, new Vector3(28f, 150f, 0f))
                .shadows = LightShadows.None;
        }

        static Light CreateLight(string name, Color color, float intensity, Vector3 euler)
        {
            var go = new GameObject(name);
            go.transform.rotation = Quaternion.Euler(euler);
            var l = go.AddComponent<Light>();
            l.type = LightType.Directional;
            l.color = color;
            l.intensity = intensity;
            return l;
        }
    }
}
