using UnityEngine;

namespace TDFende
{
    /// <summary>
    /// Modo Tower Wars: você defende a sua lane e compra inimigos para mandar na
    /// lane da IA. Cada envio custa ouro agora e sobe a sua renda para sempre.
    ///
    /// Este script é só ORQUESTRAÇÃO e ENTRADA — nenhuma regra de jogo mora aqui.
    /// As regras estão no LaneSim, que é lógica pura testada headless; trocar a IA
    /// por um adversário de rede depois é substituir quem enfileira os comandos.
    /// </summary>
    public class TowerWarsController : MonoBehaviour
    {
        const float LaneGap = 6f;      // espaço entre as duas lanes, em unidades de mundo

        public LaneSim Player => _runner?.Player;
        public LaneSim Foe => _runner?.Foe;

        MatchRunner _runner;
        Replay _replay;
        LaneView _playerView;
        LaneView _foeView;
        FloatingText _floatingText;
        string _lastSaveMessage;
        float _saveMessageTimer;

        IGameInput _input;
        CameraRigDriver _cameraRig;
        Transform _ghost;
        Renderer _ghostRenderer;
        readonly MaterialPropertyBlock _mpb = new MaterialPropertyBlock();

        /// <summary>Definida pelo seletor de modo ANTES do Start.</summary>
        public TowerWarsAi.Personality Difficulty = TowerWarsAi.Personality.Normal;

        int _selectedSend;
        int _selectedTower;            // Q/E ou os botões do topo trocam o tipo a construir
        Vector2Int _hoverCell;
        int _hoverUpgradeCost = -1;    // -1 sem torre, 0 já no máximo, >0 custo
        float _accumulator;            // passo fixo: a simulação não depende do frame rate
        GUIStyle _label, _big, _box, _button;

        static readonly Plane GroundPlane = new Plane(Vector3.up, Vector3.zero);

        // Medidas do HUD num lugar só: OnGUI desenha com elas e o clique-no-mundo as
        // consulta para não construir por baixo da interface.
        const float HudMargin = 12f;
        const float SendButtonWidth = 150f;
        const float SendButtonHeight = 62f;
        const float SendButtonGap = 6f;

        /// <summary>
        /// Retângulos do HUD em coordenadas de GUI (origem no canto superior esquerdo).
        /// </summary>
        Rect SendPanelRect =>
            new Rect(HudMargin, Screen.height - SendButtonHeight - HudMargin,
                SendCatalog.Count * (SendButtonWidth + SendButtonGap), SendButtonHeight);

        Rect HelpBoxRect =>
            new Rect(HudMargin, Screen.height - SendButtonHeight - HudMargin - 58f, 640f, 50f);

        Rect InfoRect => new Rect(HudMargin, 10f, 420f, 76f);

        const float TowerButtonWidth = 132f;
        const float TowerButtonHeight = 54f;

        /// <summary>Barra de tipos de torre, acima do painel de envios.</summary>
        Rect TowerPanelRect =>
            new Rect(HudMargin, Screen.height - SendButtonHeight - HudMargin - 58f - TowerButtonHeight - 6f,
                TowerCatalog.Count * (TowerButtonWidth + SendButtonGap), TowerButtonHeight);

        /// <summary>
        /// O clique do mouse é lido pelo Input legado, que a IMGUI não consome — sem esta
        /// checagem, clicar num botão de envio TAMBÉM constrói uma torre na célula embaixo
        /// dele, cobrando as duas coisas de um clique só.
        /// </summary>
        bool PointerOverHud()
        {
            // Input.mousePosition tem origem embaixo; Rect de GUI tem origem em cima
            var p = new Vector2(_input.PointerPos.x, Screen.height - _input.PointerPos.y);
            return SendPanelRect.Contains(p) || TowerPanelRect.Contains(p)
                   || HelpBoxRect.Contains(p) || InfoRect.Contains(p);
        }

        void Start()
        {
            var cam = Camera.main;
            if (cam == null)
            {
                var go = new GameObject("Main Camera") { tag = "MainCamera" };
                cam = go.AddComponent<Camera>();
                go.AddComponent<AudioListener>();
            }

            // efeitos: sem estes dois, todo Vfx.Instance?. e FloatingText.Instance?.
            // deste modo vira no-op silencioso e o jogo fica sem nenhum feedback
            new Vfx();
            _floatingText = gameObject.AddComponent<FloatingText>();
            _floatingText.Init(cam);

            NewMatch();

            _input = new DesktopInput();
            SceneAmbience.Apply(cam);
            BuildGhost();

            // as duas lanes empilhadas ocupam bem mais em Z do que uma só; sem esta
            // folga a câmera nasceria enquadrando apenas a sua metade do tabuleiro
            float totalZ = Player.Map.WorldSize.z * 2f + LaneGap;
            _cameraRig = new CameraRigDriver(cam, Vector3.zero,
                new Vector3(Player.Map.WorldSize.x, 0f, totalZ), totalZ * 0.95f);
        }

        void NewMatch()
        {
            if (_playerView != null)
            {
                _playerView.Dispose(); // solta os eventos antes de destruir os objetos
                Destroy(_playerView.Root.gameObject);
            }
            if (_foeView != null)
            {
                _foeView.Dispose();
                Destroy(_foeView.Root.gameObject);
            }
            Juice.Reset();
            _floatingText?.Clear();

            // semente diferente a cada partida: repetir a mesma partida-relógio a cada
            // R tornaria o teste enganoso. Fica gravada no replay, então continua reprodutível.
            int seed = System.Environment.TickCount;
            _runner = new MatchRunner(seed, Difficulty, GameConfig.GridWidth, GameConfig.GridHeight);

            _replay = new Replay { Seed = seed, Difficulty = Difficulty.Name };
            _runner.CommandApplied += _replay.Record;

            float off = (Player.Map.WorldSize.z + LaneGap) * 0.5f;
            _playerView = new LaneView(Player, new Vector3(0f, 0f, -off), "LaneJogador", Color.white, true);
            _foeView = new LaneView(Foe, new Vector3(0f, 0f, off), "LaneAdversario",
                new Color(0.82f, 0.82f, 0.9f), false);

            _accumulator = 0f;
            _selectedSend = 0;
            // sem isto, o custo de upgrade da partida ANTERIOR sobrevive e o HUD
            // consulta uma torre que não existe mais no LaneSim novo
            _hoverUpgradeCost = -1;
        }

        void BuildGhost()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "GhostTorre";
            Destroy(go.GetComponent<Collider>());
            go.transform.localScale = new Vector3(0.95f, 0.1f, 0.95f);
            _ghost = go.transform;
            _ghostRenderer = go.GetComponent<Renderer>();
            _ghostRenderer.sharedMaterial = MaterialFactory.Get(Color.white);
            go.SetActive(false);
        }

        void Update()
        {
            _input.Tick();
            float dt = Time.deltaTime;
            _cameraRig.Tick(_input, dt);
            Juice.Tick(dt);

            if (_input.RestartPressed)
            {
                NewMatch();
                return;
            }
            if (Input.GetKeyDown(KeyCode.F9)) SaveReplay();
            if (_saveMessageTimer > 0f) _saveMessageTimer -= Time.unscaledDeltaTime;

            bool over = Player.Dead || Foe.Dead;
            if (!over)
            {
                HandleTowerSelect();
                HandleBuildInput();
                HandleSendInput();

                // passo fixo, igual ao do FlowSim: o que você joga é a mesma
                // simulação que foi balanceada com 300 partidas headless
                _accumulator += dt;
                int guard = 0;
                while (_accumulator >= TowerWarsConfig.FixedStep && guard++ < 8)
                {
                    _accumulator -= TowerWarsConfig.FixedStep;
                    _runner.Step(); // comandos enfileirados valem AQUI, na fronteira do tique
                }
            }
            else
            {
                _ghost.gameObject.SetActive(false);
                _hoverUpgradeCost = -1; // partida acabou: nada de dica de upgrade parada na tela
            }

            _playerView.Sync();
            _foeView.Sync();
        }

        void HandleBuildInput()
        {
            if (PointerOverHud())
            {
                _ghost.gameObject.SetActive(false);
                _hoverUpgradeCost = -1;
                return;
            }

            var ray = _cameraRig.Camera.ScreenPointToRay(_input.PointerPos);
            if (!GroundPlane.Raycast(ray, out float dist))
            {
                _ghost.gameObject.SetActive(false);
                return;
            }

            var hit = ray.GetPoint(dist);
            var cell = _playerView.WorldToCell(hit);
            if (!Player.Map.InBounds(cell.x, cell.y))
            {
                _ghost.gameObject.SetActive(false);
                _hoverUpgradeCost = -1;
                return;
            }

            _hoverCell = cell;
            _hoverUpgradeCost = Player.UpgradeCostAt(cell);
            bool onOwnTower = _hoverUpgradeCost >= 0;
            bool canBuild = Player.CanBuild(cell, _selectedTower);
            bool canUpgrade = _hoverUpgradeCost > 0 && Player.Gold >= _hoverUpgradeCost;

            _ghost.gameObject.SetActive(true);
            _ghost.position = _playerView.CellToWorld(cell) + Vector3.up * 0.05f;
            var c = onOwnTower
                ? (canUpgrade ? Palette.TextGold : Palette.GhostInvalid)
                : (canBuild ? Palette.GhostValid : Palette.GhostInvalid);
            _mpb.SetColor(MaterialFactory.ColorProperty, c * (0.75f + 0.25f * Mathf.Sin(Time.unscaledTime * 5f)));
            _ghostRenderer.SetPropertyBlock(_mpb);

            // clique esquerdo em torre própria também sobe: quem já está com o cursor
            // ali não devia precisar lembrar de trocar de botão
            if ((_input.PlacePressed || _input.UpgradePressed) && onOwnTower)
            {
                // Sem texto flutuante aqui: TryUpgradeTowerAt levanta TowerChanged e a
                // LaneView já mostra "nv N" nesta mesma célula. Dois rótulos na mesma
                // posição e na mesma cor viravam um borrão ilegível.
                _runner.Enqueue(MatchCommand.Upgrade(cell.x, cell.y));
                return;
            }

            if (canBuild && _input.PlacePressed)
                _runner.Enqueue(MatchCommand.Build(cell.x, cell.y, _selectedTower));
        }

        void HandleTowerSelect()
        {
            // Q/E ciclam o tipo. Teclado perto da mão que já está no mouse — obrigar a
            // viajar até um botão a cada troca mataria o ritmo do jogo.
            if (Input.GetKeyDown(KeyCode.E))
                _selectedTower = (_selectedTower + 1) % TowerCatalog.Count;
            if (Input.GetKeyDown(KeyCode.Q))
                _selectedTower = (_selectedTower + TowerCatalog.Count - 1) % TowerCatalog.Count;
        }

        void HandleSendInput()
        {
            // teclas 1..6 escolhem e disparam o envio: a mão fica no teclado,
            // sem obrigar a viajar até um botão a cada compra
            for (int i = 0; i < SendCatalog.Count && i < 6; i++)
            {
                if (!Input.GetKeyDown(KeyCode.Alpha1 + i)) continue;
                _selectedSend = i;
                TrySelectedSend();
            }
        }

        void TrySelectedSend()
        {
            // Guarda na origem: com a partida decidida, nenhum caminho deve conseguir
            // comprar envio — nem o teclado, nem um botão que continue clicável.
            if (_runner.Over) return;
            // Só enfileira; quem paga e cobra é o tique. O feedback visual sai do evento
            // TowerFired/EnemyDespawned da lane, não daqui — assim o replay reproduz a
            // partida sem precisar reproduzir a interface.
            _runner.Enqueue(MatchCommand.Send(_selectedSend));
        }

        /// <summary>
        /// Grava a partida em disco. É o que transforma "achei estranho" num arquivo que
        /// eu reproduzo headless e leio o estado exato, em vez de depender da descrição.
        /// </summary>
        void SaveReplay()
        {
            try
            {
                string dir = System.IO.Path.Combine(Application.persistentDataPath, "replays");
                System.IO.Directory.CreateDirectory(dir);
                string file = System.IO.Path.Combine(dir,
                    $"tdfende-{System.DateTime.Now:yyyyMMdd-HHmmss}.txt");
                _replay.Ticks = _runner.TickCount;
                System.IO.File.WriteAllText(file, _replay.Serialize());
                _lastSaveMessage = $"replay salvo: {file}";
                Debug.Log($"[TDFende] {_lastSaveMessage}");
            }
            catch (System.Exception e)
            {
                // salvar replay nunca pode derrubar a partida
                _lastSaveMessage = $"falha ao salvar replay: {e.Message}";
                Debug.LogWarning($"[TDFende] {_lastSaveMessage}");
            }
            _saveMessageTimer = 6f;
        }

        void OnGUI()
        {
            // O ModeSelect cria este componente DE DENTRO do próprio OnGUI dele, então
            // ainda restam passes de GUI neste mesmo frame — e Start só roda na fase de
            // Update, depois. Sem esta guarda, o primeiro clique no menu estoura
            // NullReference em Player.
            if (Player == null) return;
            EnsureStyles();
            bool over = Player.Dead || Foe.Dead;

            GUILayout.BeginArea(InfoRect);
            GUILayout.Label($"VOCÊ   vidas {Player.Lives}   ouro {Player.Gold}   renda {Player.Income}", _label);
            GUILayout.Label($"IA ({Difficulty.Name})   vidas {Foe.Lives}   renda {Foe.Income}", _label);
            GUILayout.Label($"tempo {Player.MatchTime:0}s   escala dos envios x{Player.SendScale:0.0}" +
                            $"   torres {Player.TowerCount} (nv {Player.TotalTowerLevels})", _label);
            GUILayout.EndArea();

            // Barra de tipos de torre. A selecionada aparece marcada, porque o fantasma
            // no chão não diz sozinho QUAL torre vai nascer ali.
            var tp = TowerPanelRect;
            for (int i = 0; i < TowerCatalog.Count && !over; i++)
            {
                var tt = TowerCatalog.Get(i);
                var r = new Rect(tp.x + i * (TowerButtonWidth + SendButtonGap), tp.y,
                    TowerButtonWidth, TowerButtonHeight);
                bool selected = i == _selectedTower;
                var prev = GUI.color;
                if (!selected) GUI.color = new Color(1f, 1f, 1f, Player.Gold >= tt.Cost ? 0.65f : 0.35f);
                if (GUI.Button(r, $"{(selected ? "▶ " : "")}{tt.Name}\n{tt.Cost} ouro", _button))
                    _selectedTower = i;
                GUI.color = prev;
            }

            // Painel de envios. Some com a partida decidida: GUI.Box não consome clique,
            // então botão desenhado ANTES do véu de fim de jogo continuaria recebendo o
            // clique por baixo dele.
            float w = SendButtonWidth, h = SendButtonHeight;
            float x0 = HudMargin, y0 = Screen.height - h - HudMargin;
            for (int i = 0; i < SendCatalog.Count && !over; i++)
            {
                var u = SendCatalog.Get(i);
                bool afford = Player.CanAfford(i);
                var prev = GUI.color;
                GUI.color = afford ? Color.white : new Color(1f, 1f, 1f, 0.45f);
                var r = new Rect(x0 + i * (w + SendButtonGap), y0, w, h);
                if (GUI.Button(r, $"[{i + 1}] {u.Name}\n{u.Cost} ouro  +{u.IncomeBonus} renda", _button) && afford)
                {
                    _selectedSend = i;
                    TrySelectedSend();
                }
                GUI.color = prev;
            }

            // Re-consulta o índice em vez de confiar no valor guardado: OnGUI roda várias
            // vezes por frame e em pontos do ciclo onde o Update ainda não atualizou o hover.
            int hoverIdx = _hoverUpgradeCost > 0 ? Player.TowerIndexAt(_hoverCell) : -1;
            string hover =
                hoverIdx >= 0
                    ? $"  ►  subir esta torre para nv {Player.TowerLevel(hoverIdx) + 1}: {_hoverUpgradeCost} ouro"
                    : _hoverUpgradeCost == 0 ? "  ►  torre já no nível máximo" : "";

            GUI.Box(HelpBoxRect,
                $"Clique na SUA lane (a de baixo): {TowerCatalog.Get(_selectedTower).Name} " +
                $"({TowerCatalog.Get(_selectedTower).Cost} ouro)  |  Q/E troca a torre" +
                $"  |  clique numa torre sua: subir de nível{hover}\n" +
                "1-6 ou os botões: enviar inimigo para a lane da IA  |  R: reiniciar  |  F9: salvar replay", _box);

            if (_saveMessageTimer > 0f && _lastSaveMessage != null)
                GUI.Label(new Rect(HudMargin, 92f, Screen.width - HudMargin * 2f, 22f),
                    _lastSaveMessage, _label);

            if (over)
            {
                GUI.Box(new Rect(0, 0, Screen.width, Screen.height), GUIContent.none);
                GUI.Label(new Rect(0, Screen.height * 0.4f, Screen.width, 50),
                    Foe.Dead && !Player.Dead ? "VOCÊ VENCEU" : "VOCÊ PERDEU", _big);
                GUI.Label(new Rect(0, Screen.height * 0.4f + 52, Screen.width, 40), "R para jogar de novo", _big);
            }
        }

        void EnsureStyles()
        {
            if (_label != null) return;
            _label = new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold };
            _big = new GUIStyle(GUI.skin.label)
            { fontSize = 34, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            _box = new GUIStyle(GUI.skin.box) { fontSize = 13, alignment = TextAnchor.UpperLeft };
            _button = new GUIStyle(GUI.skin.button) { fontSize = 12 };
        }
    }
}
