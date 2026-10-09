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
        bool _shot, _close, _closeShot;

        // retrato de perto de cada bicho: câmera própria, porque a da partida não chega a
        // menos de 8 unidades e o rato (0,15 de altura) vira poucos pixels
        readonly System.Collections.Generic.HashSet<string> _portrayed = new System.Collections.Generic.HashSet<string>();
        Camera _probeCam;

        // retrato em dois tempos: com o material do bicho, e com um material simples sem recorte
        // (aparece só no segundo = o recorte por alfa apaga o bicho; some nos dois = malha/esqueleto)
        EnemyView _subject;
        int _stage;
        float _stageAt;
        readonly System.Collections.Generic.List<(SkinnedMeshRenderer r, Material[] m)> _saved =
            new System.Collections.Generic.List<(SkinnedMeshRenderer, Material[])>();
        Material _plain;

        void TickPortraits()
        {
            if (_subject != null || _stage > 0)
            {
                if (_t < _stageAt) return;
                bool alive = _subject != null && _subject.enabled && _subject.Rig != null;
                string name = alive ? _subject.Rig.Def.Name.Replace("Inimigo_", "") : "";
                switch (_stage)
                {
                    case 1: // troca para o material simples
                        if (alive)
                        {
                            if (_plain == null) _plain = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = new Color(1f, 0.2f, 0.6f) };
                            _saved.Clear();
                            foreach (var r in _subject.Rig.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                            {
                                _saved.Add((r, r.sharedMaterials));
                                var arr = new Material[r.sharedMaterials.Length];
                                for (int i = 0; i < arr.Length; i++) arr[i] = _plain;
                                r.sharedMaterials = arr;
                            }
                            Aim(_subject);
                        }
                        break;
                    case 2:
                        if (alive)
                        {
                            Aim(_subject);
                            ScreenCapture.CaptureScreenshot(System.IO.Path.ChangeExtension(_path, null) + "_bicho_" + name + "_simples.png");
                        }
                        break;
                    case 3: // devolve o material e desliga a câmera de retrato
                        foreach (var (r, m) in _saved) if (r != null) r.sharedMaterials = m;
                        _saved.Clear();
                        _probeCam.enabled = false;
                        _subject = null;
                        _stage = 0;
                        return;
                }
                _stage++;
                _stageAt = _t + 0.2f;
                return;
            }
            foreach (var v in Object.FindObjectsByType<EnemyView>(FindObjectsSortMode.None))
            {
                if (!v.enabled || v.Rig == null || _portrayed.Contains(v.Rig.Def.Name)) continue;
                _portrayed.Add(v.Rig.Def.Name);
                Portrait(v);
                _subject = v;
                _stage = 1;
                _stageAt = _t + 0.2f;
                return;
            }
        }

        void Aim(EnemyView v)
        {
            float h = Mathf.Max(0.15f, v.Rig.Def.Height);
            var target = v.transform.position + Vector3.up * (h * 0.5f);
            _probeCam.transform.position = target + new Vector3(h * 1.6f, h * 1.4f, -h * 2.4f);
            _probeCam.transform.LookAt(target);
        }

        void Portrait(EnemyView v)
        {
            if (_probeCam == null)
            {
                _probeCam = new GameObject("CameraRetrato").AddComponent<Camera>();
                _probeCam.depth = 100; // por cima da câmera da partida
                _probeCam.nearClipPlane = 0.01f;
                _probeCam.fieldOfView = 35f;
            }
            _probeCam.enabled = true;
            Aim(v);
            LogEnemy(v);
            ScreenCapture.CaptureScreenshot(System.IO.Path.ChangeExtension(_path, null) +
                                            "_bicho_" + v.Rig.Def.Name.Replace("Inimigo_", "") + ".png");
        }
        float _closeAt;

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
                if (!_track.TryGetValue(v, out var tr))
                {
                    // animação de verdade tocando (bicho com esqueleto) ou só o balanço de passada
                    var anim = v.Rig.GetComponentInChildren<Animation>();
                    string how = anim != null && anim.isPlaying ? "animado" : anim != null ? "animação parada" : "sem animação";
                    _track[v] = (p, 0f, 0, 0, 0f, v.Rig.Def.Name + " (" + how + ")", off, off);
                    continue;
                }
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
            else if (_shot && !_close && _t >= Wait + Record + 1f)
            {
                // segundo print: câmera colada na grama, logo à esquerda da sua lane
                var tw = Object.FindFirstObjectByType<TowerWarsController>();
                if (tw != null && tw.CameraRig != null)
                {
                    float w = tw.Player.Map.WorldSize.z;
                    float laneX = -(w + TowerWarsController.LaneGapForTests) * 0.5f;
                    tw.CameraRig.Focus(new Vector3(laneX - w * 0.5f - 2.5f, 0f, 0f), 4f);
                }
                _close = true;
                _closeAt = _t;
            }
            else if (_close && !_closeShot && _t >= _closeAt + 0.5f)
            {
                ScreenCapture.CaptureScreenshot(System.IO.Path.ChangeExtension(_path, null) + "_grama.png");
                _closeShot = true;
            }
            else if (_closeShot && _t >= _closeAt + 2f) Application.Quit(); // o print é gravado no fim do quadro

            // retratos: cada tipo de bicho é fotografado de perto assim que aparece (antes de
            // morrer ou passar da base); a câmera de retrato só fica ligada no quadro do print
            if (!_shot && _t >= 2f) TickPortraits();
        }

        /// <summary>O bicho fotografado: renderers, materiais, tamanho e se está ligado.</summary>
        static void LogEnemy(EnemyView v)
        {
            var sb = new System.Text.StringBuilder($"[TDFende] captura, bicho {v.Rig.Def.Name} em {v.transform.position}, escala {v.transform.lossyScale}:\n");
            foreach (var r in v.Rig.GetComponentsInChildren<Renderer>(true))
            {
                sb.Append($"  {r.GetType().Name} '{r.name}' ativo={r.gameObject.activeInHierarchy} ligado={r.enabled} " +
                          $"visível={r.isVisible} limites={r.bounds.size} escala={r.transform.lossyScale}\n");
                if (r is SkinnedMeshRenderer smr)
                {
                    // a malha como está NESTA pose: o limite do renderer pode estar velho; isto não
                    var baked = new Mesh();
                    smr.BakeMesh(baked, true);
                    var b = baked.bounds;
                    var worldCenter = smr.transform.TransformPoint(b.center);
                    var worldSize = Vector3.Scale(b.size, smr.transform.lossyScale);
                    var root = smr.rootBone;
                    sb.Append($"    pose real: tamanho {worldSize} centro {worldCenter} (bicho em {v.transform.position}) " +
                              $"vértices {baked.vertexCount} ossos {smr.bones.Length} " +
                              $"raiz '{(root ? root.name : "-")}' escala-raiz {(root ? root.lossyScale.ToString() : "-")} " +
                              $"malha-original {(smr.sharedMesh ? smr.sharedMesh.bounds.size.ToString() : "NULA")}\n");
                    Object.Destroy(baked);
                }
                foreach (var m in r.sharedMaterials)
                    sb.Append(m == null ? "    material NULO\n"
                        : $"    '{m.name}' shader '{m.shader.name}' suportado={m.shader.isSupported} fila={m.renderQueue} " +
                          $"keywords=[{string.Join(" ", m.shaderKeywords)}] " +
                          $"cor={(m.HasProperty("_BaseColor") ? m.GetColor("_BaseColor").ToString() : "-")}\n");
            }
            Debug.Log(sb.ToString());
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
