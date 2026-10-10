using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;

namespace TDFende
{
    /// <summary>
    /// Teste de fumaça do executável: "TDFende.exe -captura C:\caminho\print.png" entra direto no
    /// Tower Wars (Normal), espera o mundo montar, tira um print da tela e fecha. Serve para
    /// conferir um build sem ninguém clicar — o que some no build (shader descartado, chão
    /// invisível) aparece no print.
    ///
    /// Sai com código 0 (passou) ou 1 (reprovou) e grava print_metricas.json. Reprova: bicho que
    /// não aparece (cobertura de pixels do retrato abaixo de <see cref="MinCoverage"/>), tipo de
    /// bicho que nunca chegou, shader não suportado, exceção no log, aviso proibido no log
    /// (<see cref="LogLint"/>), material criado em runtime com combinação de shader + keywords
    /// que o ShaderKeep não garante no build (<see cref="CheckVariants"/>), tempo esgotado. Tempo de
    /// quadro e triângulos só são medidos: o orçamento é o TEC-06.
    ///
    /// Com "-estresse" também monta um fim de partida: as 6 torres no nível máximo em cada lane
    /// e <see cref="StressRounds"/> rodadas de todos os bichos mandadas nas duas lanes.
    /// </summary>
    public class SmokeCapture : MonoBehaviour
    {
        const float Wait = 8f;
        const float Timeout = 60f;

        // cobertura: o corpo do bicho (sem o anel do time) é desenhado sozinho numa camada só dele,
        // sobre fundo preto, e conta-se o que não é fundo. Bicho sumido dá ~0
        const float MinCoverage = 0.005f;
        const int ProbeLayer = 31; // o projeto não usa camadas; a 31 fica para a medida
        const int ProbeSize = 256;
        RenderTexture _probeRT;
        Texture2D _probeTex;

        readonly List<string> _failures = new List<string>();
        readonly List<float> _frameMs = new List<float>();
        int _exceptions, _errors;
        long _triMax, _drawMax, _setPassMax;
        ProfilerRecorder _tris, _draws, _setPass;
        float _bootAt;
        bool _done;
        int _skipSamples; // quadros pesados da própria captura (retrato, censo) fora da medida

        // variantes: combinação shader + keywords de cada material criado em runtime → quem usa
        readonly Dictionary<string, string> _inUse = new Dictionary<string, string>();
        float _censusAt;

        // estresse: fim de partida de verdade, com torres no máximo atirando e lanes cheias
        const int StressRounds = 4;
        bool _stress;
        int _stressTowers, _stressSends, _aliveMax;
        readonly System.Random _stressRng = new System.Random(7);
        readonly List<Vector2Int> _pathTmp = new List<Vector2Int>();

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
            MeasureNew();
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
                            if (_plain == null) _plain = new Material(ShaderRefs.Lit) { color = new Color(1f, 0.2f, 0.6f) };
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
                            _plainCoverage[_subject.Rig.Def.Name] = Coverage(_subject);
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

        void EnsureProbe()
        {
            if (_probeCam != null) return;
            _probeCam = new GameObject("CameraRetrato").AddComponent<Camera>();
            _probeCam.depth = 100; // por cima da câmera da partida
            _probeCam.nearClipPlane = 0.01f;
            _probeCam.fieldOfView = 35f;
            _probeCam.enabled = false;
            // a medida só vale se a camada vazia der fundo puro
            float empty = Coverage(null);
            if (empty > 0.001f) Fail($"medida de cobertura quebrada: camada vazia cobre {empty:P1}");
        }

        void Portrait(EnemyView v)
        {
            EnsureProbe();
            _probeCam.enabled = true;
            Aim(v);
            LogEnemy(v);
            ScreenCapture.CaptureScreenshot(System.IO.Path.ChangeExtension(_path, null) +
                                            "_bicho_" + v.Rig.Def.Name.Replace("Inimigo_", "") + ".png");
        }

        // cobertura de cada tipo de bicho, medida no quadro em que ele aparece: o render na
        // textura é síncrono, então não espera a fila de retratos (o cachorro, frágil, morria
        // antes da vez dele). Material simples: medido no retrato, se o bicho ainda viver
        readonly Dictionary<string, float> _coverage = new Dictionary<string, float>();
        readonly Dictionary<string, float> _plainCoverage = new Dictionary<string, float>();

        void MeasureNew()
        {
            foreach (var v in Object.FindObjectsByType<EnemyView>(FindObjectsSortMode.None))
            {
                if (!v.enabled || v.Rig == null || _coverage.ContainsKey(v.Rig.Def.Name)) continue;
                string name = v.Rig.Def.Name;
                EnsureProbe();
                Aim(v);
                float c = Coverage(v);
                _coverage[name] = c;
                bool ok = c >= MinCoverage;
                if (!ok) Fail($"{name} invisível: cobertura {c:P2} (mínimo {MinCoverage:P1})");
                Debug.Log($"[TDFende] captura, cobertura {name}: {c:P2} {(ok ? "ok" : "INVISÍVEL")}");
                foreach (var r in v.Rig.GetComponentsInChildren<Renderer>(true))
                    foreach (var m in r.sharedMaterials)
                    {
                        if (m == null) Fail($"{name}: '{r.name}' com material nulo");
                        else if (!m.shader.isSupported) Fail($"{name}: shader '{m.shader.name}' não suportado");
                    }
            }
            // o retrato em andamento volta a mirar no bicho dele antes do print deste quadro
            if (_subject != null && _subject.Rig != null) Aim(_subject);
        }

        /// <summary>
        /// Fração dos pixels de um quadro 256×256 da câmera de retrato coberta pelo corpo do bicho,
        /// desenhado sozinho sobre fundo preto. v = null mede a camada vazia (tem que dar 0).
        /// </summary>
        float Coverage(EnemyView v)
        {
            var moved = new List<(GameObject go, int layer)>();
            if (v != null)
                foreach (var r in v.Rig.GetComponentsInChildren<Renderer>())
                    if (r.name != "AnelTime") { moved.Add((r.gameObject, r.gameObject.layer)); r.gameObject.layer = ProbeLayer; }
            if (_probeRT == null)
            {
                _probeRT = new RenderTexture(ProbeSize, ProbeSize, 24);
                _probeTex = new Texture2D(ProbeSize, ProbeSize, TextureFormat.RGBA32, false);
            }
            var (mask, flags, bg, target) = (_probeCam.cullingMask, _probeCam.clearFlags, _probeCam.backgroundColor, _probeCam.targetTexture);
            _probeCam.cullingMask = 1 << ProbeLayer;
            _probeCam.clearFlags = CameraClearFlags.SolidColor;
            _probeCam.backgroundColor = Color.black;
            _probeCam.targetTexture = _probeRT;
            _probeCam.Render();
            var prev = RenderTexture.active;
            RenderTexture.active = _probeRT;
            _probeTex.ReadPixels(new Rect(0, 0, ProbeSize, ProbeSize), 0, 0);
            _probeTex.Apply(false);
            RenderTexture.active = prev;
            (_probeCam.cullingMask, _probeCam.clearFlags, _probeCam.backgroundColor, _probeCam.targetTexture) = (mask, flags, bg, target);
            foreach (var (go, layer) in moved) if (go != null) go.layer = layer;

            int n = 0;
            foreach (var c in _probeTex.GetPixels32()) if (c.r > 3 || c.g > 3 || c.b > 3) n++;
            return n / (float)(ProbeSize * ProbeSize);
        }

        /// <summary>Uma linha de json por bicho medido; cobertura_simples -1 = morreu antes do retrato.</summary>
        List<string> AnimalsJson()
        {
            var list = new List<string>();
            foreach (var kv in _coverage)
                list.Add(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                    "{{\"nome\": \"{0}\", \"cobertura\": {1:0.0000}, \"cobertura_simples\": {2:0.0000}, \"visivel\": {3}}}",
                    kv.Key, kv.Value, _plainCoverage.TryGetValue(kv.Key, out var p) ? p : -1f,
                    kv.Value >= MinCoverage ? "true" : "false"));
            return list;
        }

        void Fail(string why)
        {
            _failures.Add(why);
            Debug.Log("[TDFende] captura, REPROVA: " + why);
        }

        // avisos que já foram defeito de verdade: um deles no log reprova (BUG-02). ": variant "
        // é variante de shader faltando, que só vira erro no build de diagnóstico (TEC-11)
        static readonly string[] LogLint = { "Default clip could not be found", ": variant ", "[TDFende] sem modelo para " };
        readonly HashSet<string> _linted = new HashSet<string>();

        void Lint(string message)
        {
            foreach (var bad in LogLint)
                if (message.Contains(bad) && _linted.Add(bad))
                    _failures.Add("aviso proibido no log: " + (message.Length > 240 ? message.Substring(0, 240) + "..." : message));
        }

        /// <summary>
        /// Erro do próprio motor (variante de shader faltando, por exemplo) vai direto para o
        /// Player.log sem passar pelo <see cref="OnLog"/>: lê o arquivo no fim.
        /// </summary>
        void LintLogFile()
        {
            var file = Application.consoleLogPath;
            if (string.IsNullOrEmpty(file) || !System.IO.File.Exists(file)) return;
            try
            {
                using (var fs = new System.IO.FileStream(file, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.ReadWrite))
                using (var reader = new System.IO.StreamReader(fs))
                    for (string line; (line = reader.ReadLine()) != null;) Lint(line);
            }
            catch (System.Exception e) { Debug.Log("[TDFende] captura: não leu o Player.log: " + e.Message); }
        }

        void OnLog(string message, string stack, LogType type)
        {
            Lint(message);
            if (type == LogType.Exception || type == LogType.Assert)
            {
                _exceptions++;
                if (_exceptions <= 3) _failures.Add("exceção: " + message);
            }
            else if (type == LogType.Error) _errors++;
        }

        void OnEnable()
        {
            Application.logMessageReceived += OnLog;
            _tris = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Triangles Count");
            _draws = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count");
            _setPass = ProfilerRecorder.StartNew(ProfilerCategory.Render, "SetPass Calls Count");
        }

        void OnDisable()
        {
            Application.logMessageReceived -= OnLog;
            _tris.Dispose(); _draws.Dispose(); _setPass.Dispose();
        }

        /// <summary>Quadro da partida (sem a câmera de retrato): tempo e o que foi desenhado.</summary>
        void SampleFrame()
        {
            if (_probeCam != null && _probeCam.enabled) { _skipSamples = 1; return; }
            // o tempo de um quadro chega no seguinte: o quadro depois do retrato/censo também sai
            if (_skipSamples > 0) { _skipSamples--; return; }
            _frameMs.Add(Time.unscaledDeltaTime * 1000f);
            if (_tris.Valid) _triMax = System.Math.Max(_triMax, _tris.LastValue);
            if (_draws.Valid) _drawMax = System.Math.Max(_drawMax, _draws.LastValue);
            if (_setPass.Valid) _setPassMax = System.Math.Max(_setPassMax, _setPass.LastValue);
        }

        static float Percentile(List<float> sorted, float p) =>
            sorted.Count == 0 ? 0f : sorted[Mathf.Clamp(Mathf.CeilToInt(p * sorted.Count) - 1, 0, sorted.Count - 1)];

        static string Recorded(ProfilerRecorder r, long max) => r.Valid ? max.ToString() : "null";

        static string VariantKey(Material m)
        {
            var kws = new List<string>();
            foreach (var k in m.enabledKeywords) kws.Add(k.name);
            kws.Sort(System.StringComparer.Ordinal);
            return m.shader.name + " [" + string.Join(" ", kws) + "]";
        }

        /// <summary>
        /// Anota a combinação shader + keywords de cada material criado em runtime que está em
        /// uso. Material de asset (id positivo) fica de fora: o build já leva a variante dele.
        /// O de runtime só existe no build se um material do ShaderKeep tiver a mesma combinação.
        /// </summary>
        void Census()
        {
            void Add(Material m, string who)
            {
                if (m == null || m.shader == null || m.GetInstanceID() > 0) return;
                var key = VariantKey(m);
                if (!_inUse.ContainsKey(key)) _inUse[key] = who + " / " + m.name;
            }
            foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
                foreach (var m in r.sharedMaterials) Add(m, r.name);
            foreach (var g in Object.FindObjectsByType<GrassField>(FindObjectsSortMode.None))
                foreach (var m in g.Materials) Add(m, "grama");
            foreach (var t in Object.FindObjectsByType<Terrain>(FindObjectsSortMode.None)) Add(t.materialTemplate, "terreno");
            Add(RenderSettings.skybox, "céu");
            CensusAssets();
        }

        /// <summary>Reprova combinação em uso que nenhum material do ShaderKeep garante (TEC-11).</summary>
        void CheckVariants()
        {
            var kept = new HashSet<string>();
            foreach (var m in Resources.LoadAll<Material>("TDFende/ShaderKeep")) kept.Add(VariantKey(m));
            var lines = new List<string>();
            foreach (var kv in _inUse)
            {
                bool ok = kept.Contains(kv.Key);
                lines.Add((ok ? "  ok    " : "  FALTA ") + kv.Key + "  (" + kv.Value + ")");
                if (!ok) Fail("variante fora do ShaderKeep: " + kv.Key + " (" + kv.Value + "); acrescente em BuildJogo.Shaders");
            }
            lines.Sort(System.StringComparer.Ordinal);
            Debug.Log("[TDFende] captura, variantes criadas em runtime (" + _inUse.Count + "):\n" + string.Join("\n", lines));
        }

        void TickStress()
        {
            var tw = Object.FindFirstObjectByType<TowerWarsController>();
            if (tw == null || tw.Runner == null) return;
            var run = tw.Runner;
            if (_stressTowers < TowerCatalog.Count && _t >= 1f)
            {
                BuildMaxed(run.Player, _stressTowers);
                BuildMaxed(run.Foe, _stressTowers);
                _stressTowers++;
            }
            // depois do "um de cada" (um por segundo até ~10 s), uma leva a cada 0,1 s nas duas lanes
            if (_stressSends < StressRounds * SendCatalog.Count && _t >= 10f + _stressSends * 0.1f)
            {
                int id = _stressSends % SendCatalog.Count;
                run.Player.GrantGoldForSmokeTest(SendCatalog.Get(id).Cost);
                run.Enqueue(MatchCommand.Send(id));
                run.Foe.GrantGoldForSmokeTest(SendCatalog.Get(id).Cost);
                run.Foe.TrySend(id, run.Player, _stressRng);
                _stressSends++;
            }
        }

        /// <summary>Uma torre do tipo dado colada no caminho atual da marcha, subida ao nível máximo.</summary>
        void BuildMaxed(LaneSim lane, int type)
        {
            if (lane.SpawnCells.Count == 0) return;
            lane.Flow.Path(lane.SpawnCells[0], _pathTmp);
            lane.GrantGoldForSmokeTest(TowerCatalog.Get(type).Cost);
            // cada tipo num trecho diferente do caminho, para as 6 cobrirem a lane toda
            int start = _pathTmp.Count * (type + 1) / (TowerCatalog.Count + 1);
            for (int k = 0; k < _pathTmp.Count; k++)
            {
                var p = _pathTmp[(start + k) % _pathTmp.Count];
                for (int dx = -1; dx <= 1; dx++)
                for (int dy = -1; dy <= 1; dy++)
                {
                    var cell = p + new Vector2Int(dx, dy);
                    if (!lane.TryBuildTower(cell, type)) continue;
                    for (int cost; (cost = lane.UpgradeCostAt(cell)) > 0;)
                    {
                        lane.GrantGoldForSmokeTest(cost);
                        if (!lane.TryUpgradeTowerAt(cell)) break;
                    }
                    return;
                }
            }
            Debug.Log($"[TDFende] captura, estresse: sem lugar para a torre {type} na lane {lane.Id}");
        }

        // orçamento de desempenho (TEC-06): só AVISO, nunca reprova — com a arte mudando, um teto
        // rígido deixaria a captura sempre com 1. Tabela e números medidos no MANUAL, seção 4.
        // Referência: RTX 4070 Ti, janela 1600×900, qualidade padrão, captura -estresse
        const float BudgetP95Ms = 8f;
        const long BudgetSceneTris = 2_500_000, BudgetDraws = 2000, BudgetSetPass = 500;
        const float BudgetBootS = 5f;
        const int BudgetTower = 25_000, BudgetKeep = 40_000, BudgetAnimal = 15_000, BudgetGrass = 400_000;

        // triângulos de cada modelo visto: "Torre_Canhao_3" (estágio), "Fortaleza", "Inimigo_Rato"
        readonly SortedDictionary<string, long> _assetTris = new SortedDictionary<string, long>();

        static long Triangles(GameObject root)
        {
            long n = 0;
            void Count(Mesh m) { if (m != null) for (int i = 0; i < m.subMeshCount; i++) n += m.GetIndexCount(i) / 3; }
            foreach (var mf in root.GetComponentsInChildren<MeshFilter>()) if (mf.name != "AnelTime") Count(mf.sharedMesh);
            foreach (var s in root.GetComponentsInChildren<SkinnedMeshRenderer>()) Count(s.sharedMesh);
            return n;
        }

        void CensusAssets()
        {
            foreach (var rig in Object.FindObjectsByType<ModelRig>(FindObjectsSortMode.None))
            {
                string name = rig.gameObject.name;
                string key = name.StartsWith("Torre_") ? name + "_" + rig.Stage
                           : name == "Base" ? "Fortaleza"
                           : name.StartsWith("Inimigo") && rig.Def != null ? rig.Def.Name : null;
                if (key != null && !_assetTris.ContainsKey(key)) _assetTris[key] = Triangles(rig.gameObject);
            }
        }

        /// <summary>Veredito de cada item do orçamento (json); o que estoura vai para o log como AVISO.</summary>
        List<string> Budget(float p95)
        {
            var inv = System.Globalization.CultureInfo.InvariantCulture;
            var items = new List<(string item, double measured, double cap)>
            {
                ("quadro p95 (ms)", p95, BudgetP95Ms),
                ("boot (s)", _bootAt, BudgetBootS),
            };
            if (_tris.Valid) items.Add(("triângulos na cena", _triMax, BudgetSceneTris));
            if (_draws.Valid) items.Add(("draws", _drawMax, BudgetDraws));
            if (_setPass.Valid) items.Add(("SetPass", _setPassMax, BudgetSetPass));
            var grass = Object.FindFirstObjectByType<GrassField>();
            if (grass != null) items.Add(("grama (triângulos)", grass.Triangles, BudgetGrass));
            foreach (var kv in _assetTris)
                items.Add((kv.Key + " (triângulos)", kv.Value,
                    kv.Key.StartsWith("Torre_") ? BudgetTower : kv.Key == "Fortaleza" ? BudgetKeep : BudgetAnimal));

            var json = new List<string>();
            var over = new List<string>();
            foreach (var (item, measured, cap) in items)
            {
                bool ok = measured <= cap;
                if (!ok) over.Add(string.Format(inv, "{0} {1:0.##} > {2:0.##}", item, measured, cap));
                json.Add(string.Format(inv, "{{\"item\": \"{0}\", \"medido\": {1:0.##}, \"teto\": {2:0.##}, \"ok\": {3}}}",
                    item, measured, cap, ok ? "true" : "false"));
            }
            Debug.Log(over.Count == 0 ? "[TDFende] captura, orçamento: tudo dentro"
                : "[TDFende] captura, orçamento: AVISO (" + over.Count + " acima do teto, não reprova):\n  " + string.Join("\n  ", over));
            return json;
        }

        /// <summary>Confere o que falta, grava print_metricas.json e fecha com 0 ou 1.</summary>
        void Finish()
        {
            _done = true;
            Census();
            CheckVariants();
            LintLogFile();
            // TEC-12: as tabelas de modelo da vista têm que ter exatamente as chaves de ViewKeys
            string viewMismatch = ModelLib.SelfCheck();
            if (viewMismatch.Length > 0) Fail("tabelas da vista fora de sincronia com ViewKeys: " + viewMismatch);
            // e todo bicho/torre do catálogo em uso precisa ter modelo (catálogo editado por arquivo com chave nova)
            for (int i = 0; i < SendCatalog.Count; i++)
                if (System.Array.IndexOf(ViewKeys.Enemy, SendCatalog.Get(i).Key) < 0)
                    Fail($"bicho '{SendCatalog.Get(i).Key}' (envio {i}) sem modelo na vista");
            for (int i = 0; i < TowerCatalog.Count; i++)
                if (System.Array.IndexOf(ViewKeys.Tower, TowerCatalog.Get(i).Key) < 0)
                    Fail($"torre '{TowerCatalog.Get(i).Key}' (tipo {i}) sem modelo na vista");
            // executável sem selo = gerado fora do BuildJogo (ou o selo quebrou): não dá para
            // saber de que commit veio (TEC-03)
            if (!Application.isEditor && Application.version == BuildStamp.Unstamped)
                Fail("executável sem selo de build (Application.version = " + Application.version + ")");
            if (_coverage.Count < SendCatalog.Count)
                Fail($"só {_coverage.Count} de {SendCatalog.Count} tipos de bicho apareceram ({string.Join(", ", _coverage.Keys)})");

            var ms = new List<float>(_frameMs);
            ms.Sort();
            float p50 = Percentile(ms, 0.5f), p95 = Percentile(ms, 0.95f), p99 = Percentile(ms, 0.99f);
            bool passed = _failures.Count == 0;
            var inv = System.Globalization.CultureInfo.InvariantCulture;
            var json = new System.Text.StringBuilder("{\n");
            json.Append("  \"resultado\": \"" + (passed ? "passou" : "reprovou") + "\",\n");
            json.Append("  \"build\": \"" + Application.version + "\",\n");
            json.Append("  \"falhas\": [" + string.Join(", ", _failures.ConvertAll(f => "\"" + f.Replace("\\", "/").Replace("\"", "'") + "\"")) + "],\n");
            json.Append(string.Format(inv, "  \"boot_s\": {0:0.00},\n", _bootAt));
            json.Append(string.Format(inv, "  \"quadro_ms\": {{\"p50\": {0:0.00}, \"p95\": {1:0.00}, \"p99\": {2:0.00}, \"quadros\": {3}}},\n", p50, p95, p99, ms.Count));
            json.Append("  \"triangulos_max\": " + Recorded(_tris, _triMax) + ",\n");
            json.Append("  \"draws_max\": " + Recorded(_draws, _drawMax) + ",\n");
            json.Append("  \"setpass_max\": " + Recorded(_setPass, _setPassMax) + ",\n");
            json.Append("  \"excecoes\": " + _exceptions + ",\n  \"erros\": " + _errors + ",\n");
            json.Append("  \"estresse\": " + (_stress ? "true" : "false") + ",\n");
            json.Append("  \"camada_privada\": {\"usada\": " + (ArtLayers.PrivateServed > 0 ? "true" : "false") + ", \"arquivos\": " + ArtLayers.PrivateServed + "},\n");
            json.Append("  \"bichos_vivos_max\": " + _aliveMax + ",\n");
            json.Append("  \"orcamento\": [\n    " + string.Join(",\n    ", Budget(p95)) + "\n  ],\n");
            json.Append("  \"bichos\": [\n    " + string.Join(",\n    ", AnimalsJson()) + "\n  ]\n}\n");
            var file = System.IO.Path.ChangeExtension(_path, null) + "_metricas.json";
            try { System.IO.File.WriteAllText(file, json.ToString()); }
            catch (System.Exception e) { Debug.Log("[TDFende] captura: não gravou " + file + ": " + e.Message); }

            Debug.Log(string.Format(inv, "[TDFende] captura, desempenho: quadro p50 {0:0.0} ms, p95 {1:0.0} ms, p99 {2:0.0} ms; " +
                                    "triângulos {3}, draws {4}, SetPass {5}; boot {6:0.0} s; bichos vivos no máximo {7}{8}",
                                    p50, p95, p99, Recorded(_tris, _triMax), Recorded(_draws, _drawMax),
                                    Recorded(_setPass, _setPassMax), _bootAt, _aliveMax, _stress ? " (estresse)" : ""));
            // build com pacote pago (fora do git) não é o mesmo que o de um clone novo: o log diz qual é (TEC-23)
            Debug.Log("[TDFende] captura, camada privada: " + (ArtLayers.PrivateServed > 0
                ? $"com privado ({ArtLayers.PrivateServed} arquivos vieram de {ArtLayerPaths.PrivateFolder})"
                : "sem privado (só a arte pública e a procedural)"));
            Debug.Log(passed ? "[TDFende] captura: PASSOU"
                             : "[TDFende] captura: REPROVOU (" + _failures.Count + "): " + string.Join(" | ", _failures));
            Application.Quit(passed ? 0 : 1);
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
            var c = new GameObject("== captura ==").AddComponent<SmokeCapture>();
            c._path = path;
            c._stress = System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-estresse") >= 0;
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
            int alive = 0;
            foreach (var v in Object.FindObjectsByType<EnemyView>(FindObjectsSortMode.None))
            {
                if (!v.enabled || v.Rig == null) continue;
                alive++;
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
            _aliveMax = Mathf.Max(_aliveMax, alive);
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
            if (_done) return;
            _t += Time.unscaledDeltaTime;
            if (_bootAt == 0f) _bootAt = Time.realtimeSinceStartup;
            if (_t > Timeout)
            {
                Fail($"tempo esgotado ({Timeout:0} s) sem terminar a captura");
                Finish();
                return;
            }
            SendOneOfEach();
            if (_stress) TickStress();
            if (_t >= 2f && _t >= _censusAt)
            {
                Census();
                _censusAt = _t + 0.5f;
                _skipSamples = 1;
            }
            if (_t >= Wait && _t < Wait + Record) { TrackEnemies(); SampleFrame(); }
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
            else if (_closeShot && _t >= _closeAt + 2f) Finish(); // o print é gravado no fim do quadro

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
