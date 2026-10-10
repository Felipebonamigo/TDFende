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
    // roda ANTES das vistas (EnemyView, ProjectileView): elas leem neste mesmo quadro o
    // alpha e os tiques que o laço de passo fixo acabou de produzir
    [DefaultExecutionOrder(-100)]
    public class TowerWarsController : MonoBehaviour
    {
        const float LaneGap = 6f;      // espaço entre as duas lanes, em unidades de mundo

        /// <summary>
        /// As lanes ficam EM PÉ, lado a lado: o inimigo desce do acampamento (em cima, longe
        /// da câmera) até a fortaleza (embaixo, perto). A simulação continua deitada em +X —
        /// só a vista gira 90°, então regra, IA e balanceamento medido não mudam.
        /// Euler(0, 90, 0) leva +X local para -Z de mundo.
        /// </summary>
        static readonly Quaternion LaneRotation = Quaternion.Euler(0f, 90f, 0f);
        static readonly Vector2 MarchDir = new Vector2(0f, -1f);

        public LaneSim Player => _runner?.Player;
        public LaneSim Foe => _runner?.Foe;
        internal MatchRunner Runner => _runner;
        internal CameraRigDriver CameraRig => _cameraRig;
        internal const float LaneGapForTests = LaneGap;

        MatchRunner _runner;
        Replay _replay;
        LaneView _playerView;
        LaneView _foeView;
        FloatingText _floatingText;
        string _lastSaveMessage;
        float _saveMessageTimer;

        IGameInput _input;
        CameraRigDriver _cameraRig;
        PlacementGhost _ghost;

        /// <summary>Definida pelo seletor de modo ANTES do Start.</summary>
        public TowerWarsAi.Personality Difficulty = TowerWarsAi.Personality.Normal;

        int _selectedSend;
        int _selectedTower;            // Q/E ou os botões do topo trocam o tipo a construir
        Vector2Int _hoverCell;
        int _hoverUpgradeCost = -1;    // -1 sem torre, 0 já no máximo, >0 custo
        float _accumulator;            // passo fixo: a simulação não depende do frame rate

        static readonly Plane GroundPlane = new Plane(Vector3.up, Vector3.zero);

        // Medidas do HUD num lugar só: OnGUI desenha com elas e o clique-no-mundo as
        // consulta para não construir por baixo da interface.
        const float HudMargin = 12f;
        const float SendButtonWidth = 118f;
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

        Rect InfoRect => new Rect(HudMargin, 10f, 470f, 82f);

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

            // texturas procedurais geradas de uma vez, em paralelo, antes de qualquer modelo
            ArtFactory.Preload();

            // efeitos: sem estes dois, todo Vfx.Instance?. e FloatingText.Instance?.
            // deste modo vira no-op silencioso e o jogo fica sem nenhum feedback
            new Vfx();
            _floatingText = gameObject.AddComponent<FloatingText>();
            _floatingText.Init(cam);

            SceneAmbience.Apply(cam);
            BuildWorld();
            NewMatch();

            _input = new DesktopInput();
            _ghost = new PlacementGhost();

            // lanes em pé lado a lado: a largura total é a de duas lanes (a altura do grid)
            // mais o vão; a profundidade, o comprimento de uma, mais muralha e acampamento
            float totalX = Player.Map.WorldSize.z * 2f + LaneGap;
            float depth = Player.Map.WorldSize.x + 6f;
            _cameraRig = new CameraRigDriver(cam, Vector3.zero,
                new Vector3(totalX, 0f, depth), totalX * 0.95f);
            // começa perto, na SUA lane (a da esquerda), puxada para o lado da fortaleza; as
            // duas lanes inteiras continuam a um scroll de distância
            float laneOff = (Player.Map.WorldSize.z + LaneGap) * 0.5f;
            _cameraRig.Focus(new Vector3(-laneOff, 0f, -depth * 0.12f), depth * 0.62f);
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
            // cada lane tem dono (fortaleza, torres, fronteira) e atacante (acampamento,
            // inimigos): azul é você, vermelho é a IA — como estandarte de batalha.
            // Você à esquerda, a IA à direita; nas duas o inimigo desce de cima para baixo.
            _playerView = new LaneView(Player, new Vector3(-off, 0f, 0f), "LaneJogador",
                Palette.TeamPlayer, Palette.TeamFoe, true, _terrain != null, LaneRotation);
            _foeView = new LaneView(Foe, new Vector3(off, 0f, 0f), "LaneAdversario",
                Palette.TeamFoe, Palette.TeamPlayer, false, _terrain != null, LaneRotation);

            _accumulator = 0f;
            _selectedSend = 0;
            // sem isto, o custo de upgrade da partida ANTERIOR sobrevive e o HUD
            // consulta uma torre que não existe mais no LaneSim novo
            _hoverUpgradeCost = -1;
        }

        Terrain _terrain;

        /// <summary>
        /// Chão de verdade (terreno com grama fotográfica e tufos 3D, GroundBuilder) e, por
        /// cima, mureta e mata. Sem a arte do chão em Resources, _terrain fica null e o
        /// mundo inteiro é procedural, com rio entre as lanes. Uma vez só: o R reinicia a
        /// partida, não o cenário.
        /// </summary>
        void BuildWorld()
        {
            float w = GameConfig.GridWidth * GameConfig.CellSize;
            float h = GameConfig.GridHeight * GameConfig.CellSize;
            float off = (h + LaneGap) * 0.5f;
            // Rect.y guarda o Z do mundo. Lane em pé: largura h em X, comprimento w em Z
            var areas = new[]
            {
                new Rect(-off - h * 0.5f, -w * 0.5f, h, w), // sua lane (esquerda)
                new Rect(off - h * 0.5f, -w * 0.5f, h, w)   // lane da IA (direita)
            };
            _terrain = GroundBuilder.Build(areas);
            if (_terrain != null)
                gameObject.AddComponent<GrassField>().Init(_terrain, areas);

            // rio correndo em Z, no vão entre as duas lanes
            var layout = new WorldLayout { River = true, RiverZ = 0f, RiverAlongZ = true };
            layout.AddPlayArea(new Vector3(-off, 0f, 0f), new Vector3(h, 0f, w), Palette.TeamPlayer, Palette.TeamFoe, MarchDir);
            layout.AddPlayArea(new Vector3(off, 0f, 0f), new Vector3(h, 0f, w), Palette.TeamFoe, Palette.TeamPlayer, MarchDir);
            WorldView.Build(layout, _terrain);
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
                    // cada tique vira um "atual" nas vistas: anterior e atual são sempre tiques vizinhos
                    _playerView.OnTick();
                    _foeView.OnTick();
                }
            }
            else
            {
                _ghost.Hide();
                _hoverUpgradeCost = -1; // partida acabou: nada de dica de upgrade parada na tela
            }

            // fração do próximo tique já decorrida: a vista desenha entre um tique e outro
            float alpha = over ? 1f : _accumulator / TowerWarsConfig.FixedStep;
            _playerView.Sync(alpha);
            _foeView.Sync(alpha);
        }

        void HandleBuildInput()
        {
            if (PointerOverHud())
            {
                _ghost.Hide();
                _hoverUpgradeCost = -1;
                return;
            }

            var ray = _cameraRig.Camera.ScreenPointToRay(_input.PointerPos);
            if (!GroundPlane.Raycast(ray, out float dist))
            {
                _ghost.Hide();
                return;
            }

            var hit = ray.GetPoint(dist);
            var cell = _playerView.WorldToCell(hit);
            if (!Player.Map.InBounds(cell.x, cell.y))
            {
                _ghost.Hide();
                _hoverUpgradeCost = -1;
                return;
            }

            _hoverCell = cell;
            _hoverUpgradeCost = Player.UpgradeCostAt(cell);
            bool onOwnTower = _hoverUpgradeCost >= 0;
            bool canBuild = Player.CanBuild(cell, _selectedTower);
            bool canUpgrade = _hoverUpgradeCost > 0 && Player.Gold >= _hoverUpgradeCost;

            // sobre torre própria: dourado se dá para subir; em célula livre: verde/vermelho.
            // O anel mostra o alcance de quem está (ou vai ficar) ali.
            var color = onOwnTower
                ? (canUpgrade ? Palette.GhostUpgrade : Palette.GhostInvalid)
                : (canBuild ? Palette.GhostValid : Palette.GhostInvalid);
            int rangeType = onOwnTower ? Player.TowerTypeAt(cell) : _selectedTower;
            _ghost.Show(_playerView.CellToWorld(cell), color, TowerCatalog.Get(rangeType).Range * Player.Map.CellSize);

            // botão direito (ou X/Delete) em torre própria: vende
            if (_input.SellPressed && onOwnTower)
            {
                _runner.Enqueue(MatchCommand.Sell(cell.x, cell.y));
                return;
            }

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
            // teclas 1..9 escolhem e disparam o envio: a mão fica no teclado,
            // sem obrigar a viajar até um botão a cada compra
            for (int i = 0; i < SendCatalog.Count && i < 9; i++)
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
                _replay.Final = _runner.StateFingerprint(); // diagnóstico (BUG-05): o headless confere ao reproduzir
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
            UiSkin.Ensure();
            bool over = Player.Dead || Foe.Dead;

            GUI.Box(InfoRect, GUIContent.none, UiSkin.Panel);
            GUILayout.BeginArea(new Rect(InfoRect.x + 10f, InfoRect.y + 6f, InfoRect.width - 20f, InfoRect.height - 12f));
            GUILayout.Label($"<color=#7FA8E8>VOCÊ</color>   vidas {Player.Lives}   ouro <color=#E8C15A>{Player.Gold}</color>" +
                            $"   renda {Player.Income}", UiSkin.Label);
            GUILayout.Label($"<color=#E07A72>IA ({Difficulty.Name})</color>   vidas {Foe.Lives}   renda {Foe.Income}", UiSkin.Label);
            GUILayout.Label($"tempo {Player.MatchTime:0}s   escala dos envios x{Player.SendScale:0.0}" +
                            $"   torres {Player.TowerCount} (nv {Player.TotalTowerLevels})", UiSkin.LabelSmall);
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
                if (GUI.Button(r, $"{tt.Name}\n<color=#E8C15A>{tt.Cost} ouro</color>",
                        selected ? UiSkin.ButtonSelected : UiSkin.Button))
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
                if (GUI.Button(r, $"[{i + 1}] {u.Name}\n<color=#E8C15A>{u.Cost} ouro</color>  +{u.IncomeBonus}/renda",
                        UiSkin.Button) && afford)
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
            int sellValue = _hoverUpgradeCost >= 0 ? Player.SellValueAt(_hoverCell) : -1;
            if (sellValue >= 0) hover += $"  |  botão direito: vender por <color=#E8C15A>{sellValue} ouro</color>";

            GUI.Box(HelpBoxRect,
                $"Clique na SUA lane (a da esquerda): {TowerCatalog.Get(_selectedTower).Name} " +
                $"({TowerCatalog.Get(_selectedTower).Cost} ouro)  |  Q/E troca a torre" +
                $"  |  clique numa torre sua: subir, botão direito: vender{hover}\n" +
                "1-9 ou os botões: enviar bicho para a lane da IA  |  R: reiniciar  |  F9: salvar replay", UiSkin.Panel);

            if (_saveMessageTimer > 0f && _lastSaveMessage != null)
                UiSkin.Shadowed(new Rect(HudMargin, 92f, Screen.width - HudMargin * 2f, 22f),
                    _lastSaveMessage, UiSkin.Plain, Palette.UiInk);

            if (over)
            {
                GUI.Box(new Rect(0, 0, Screen.width, Screen.height), GUIContent.none, UiSkin.Panel);
                bool won = Foe.Dead && !Player.Dead;
                UiSkin.Shadowed(new Rect(0, Screen.height * 0.4f, Screen.width, 50),
                    won ? "VITÓRIA" : "DERROTA", UiSkin.Title, won ? Palette.UiAccent : Palette.TextDanger);
                UiSkin.Shadowed(new Rect(0, Screen.height * 0.4f + 56, Screen.width, 30), "R para jogar de novo",
                    UiSkin.Subtitle, Palette.UiInk);
            }
        }
    }
}
