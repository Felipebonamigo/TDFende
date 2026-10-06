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

        // movimento dos bichos: posição desenhada de cada um, quadro a quadro, entre Wait e o fim
        const float Record = 12f;
        readonly System.Collections.Generic.Dictionary<EnemyView, (Vector3 pos, float step, int frames, int jumps, float worst, string name, float offMin, float offMax)> _track =
            new System.Collections.Generic.Dictionary<EnemyView, (Vector3, float, int, int, float, string, float, float)>();

        int _sent;

        /// <summary>Manda um de cada bicho na lane da IA, um por segundo, para medir todos.</summary>
        void SendOneOfEach()
        {
            if (_sent >= SendCatalog.Count || _t < 1f + _sent) return;
            var tw = Object.FindFirstObjectByType<TowerWarsController>();
            if (tw == null || tw.Runner == null) return;
            tw.Player.GrantGoldForSmokeTest(SendCatalog.Get(_sent).Cost);
            tw.Runner.Enqueue(MatchCommand.Send(_sent));
            _sent++;
        }

        void TrackEnemies()
        {
            foreach (var v in Object.FindObjectsByType<EnemyView>(FindObjectsSortMode.None))
            {
                if (!v.enabled || v.Rig == null) continue;
                var p = v.transform.position;
                // corpo desenhado x posição do bicho, ao longo da direção em que ele olha: se a
                // animação carrega o corpo para frente e volta no fim do ciclo, isto oscila
                float off = 0f;
                var rs = v.Rig.GetComponentsInChildren<SkinnedMeshRenderer>();
                if (rs.Length > 0 && rs[0].rootBone != null) off = Vector3.Dot(rs[0].rootBone.position - p, v.transform.forward);
                if (!_track.TryGetValue(v, out var tr)) { _track[v] = (p, 0f, 0, 0, 0f, v.Rig.Def.Name, off, off); continue; }
                tr.offMin = Mathf.Min(tr.offMin, off); tr.offMax = Mathf.Max(tr.offMax, off);
                float d = Vector3.Distance(p, tr.pos);
                // passo "normal" = média móvel; salto = mais de 4x o normal e mais de 0,05 unidade
                if (tr.frames > 5 && d > 0.05f && d > 4f * tr.step) { tr.jumps++; tr.worst = Mathf.Max(tr.worst, d); }
                tr.step = tr.frames == 0 ? d : Mathf.Lerp(tr.step, d, 0.2f);
                tr.frames++; tr.pos = p;
                _track[v] = tr;
            }
        }

        static float v_height(EnemyView v) => v.Rig != null ? v.Rig.Def.Height : 0f;

        void LogMovement()
        {
            var sb = new System.Text.StringBuilder($"[TDFende] captura, movimento ({Record:0} s, fps médio {Time.frameCount / Mathf.Max(0.01f, Time.realtimeSinceStartup):0}):\n");
            foreach (var kv in _track)
            {
                var tr = kv.Value;
                sb.Append($"  {tr.name}: quadros={tr.frames} passo={tr.step:0.0000} saltos={tr.jumps} maior={tr.worst:0.000} corpo_vai_e_volta={tr.offMax - tr.offMin:0.000} altura={v_height(kv.Key):0.00}\n");
            }
            Debug.Log(sb.ToString());
        }

        void Update()
        {
            _t += Time.unscaledDeltaTime;
            SendOneOfEach();
            if (_t >= Wait && _t < Wait + Record) TrackEnemies();
            if (!_shot && _t >= Wait + Record)
            {
                ScreenCapture.CaptureScreenshot(_path);
                _shot = true;
                LogScene();
                LogMovement();
                Debug.Log($"[TDFende] captura: {_path}");
            }
            else if (_shot && _t >= Wait + Record + 2f) Application.Quit(); // o print é gravado no fim do quadro
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
