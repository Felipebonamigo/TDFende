using UnityEngine;

namespace TDFende
{
    /// <summary>
    /// Teste de fumaça do executável: "TDFende.exe -captura C:\caminho\print.png" entra direto no
    /// Tower Wars (Normal), espera o mundo montar, tira um print da tela e fecha. Serve para
    /// conferir um build sem ninguém clicar — o que some no build (shader descartado, chão
    /// invisível) aparece no print.
    /// </summary>
    public class SmokeCapture : MonoBehaviour
    {
        const float Wait = 8f;

        string _path;
        float _t;
        bool _shot;

        /// <summary>Caminho pedido em "-captura", ou null se o jogo abriu normal.</summary>
        public static string RequestedPath()
        {
            var args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == "-captura") return args[i + 1];
            return null;
        }

        public static void Begin(string path)
        {
            // aberto por script, a janela pode nascer sem foco: sem isto o jogo fica pausado
            Application.runInBackground = true;
            new GameObject("== captura ==").AddComponent<SmokeCapture>()._path = path;
        }

        void Update()
        {
            _t += Time.unscaledDeltaTime;
            if (!_shot && _t >= Wait)
            {
                ScreenCapture.CaptureScreenshot(_path);
                _shot = true;
                LogScene();
                Debug.Log($"[TDFende] captura: {_path}");
            }
            else if (_shot && _t >= Wait + 2f) Application.Quit(); // o print é gravado no fim do quadro
        }

        /// <summary>O que mais some num build: shader, keyword, textura, céu e luz ambiente.</summary>
        static void LogScene()
        {
            var sb = new System.Text.StringBuilder("[TDFende] captura, cena:\n");
            void Mat(string who, Material m)
            {
                if (m == null) { sb.Append($"  {who}: sem material\n"); return; }
                sb.Append($"  {who}: shader '{m.shader.name}' suportado={m.shader.isSupported} " +
                          $"keywords=[{string.Join(" ", m.shaderKeywords)}] instancing={m.enableInstancing}\n");
            }
            foreach (var t in Object.FindObjectsByType<Terrain>(FindObjectsSortMode.None))
            {
                Mat("terreno", t.materialTemplate);
                foreach (var l in t.terrainData.terrainLayers)
                    sb.Append($"  camada: cor={(l.diffuseTexture ? l.diffuseTexture.name + " " + l.diffuseTexture.width : "NULA")} " +
                              $"normal={(l.normalMapTexture ? l.normalMapTexture.name : "nula")}\n");
            }
            Mat("céu", RenderSettings.skybox);
            sb.Append($"  ambiente: modo={RenderSettings.ambientMode} intensidade={RenderSettings.ambientIntensity} " +
                      $"cor={RenderSettings.ambientLight} névoa={RenderSettings.fog} {RenderSettings.fogColor}\n");
            var sun = RenderSettings.sun;
            sb.Append($"  sol: {(sun ? sun.intensity + " " + sun.color : "nenhum")}\n");
            Debug.Log(sb.ToString());
        }
    }
}
