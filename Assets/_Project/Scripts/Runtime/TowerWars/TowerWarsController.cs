using UnityEngine;
using Random = System.Random;

namespace TDFende
{
    /// <summary>
    /// Modo Tower Wars: você defende a sua lane e compra inimigos para mandar na
    /// lane da IA. Cada envio custa ouro agora e sobe a sua renda para sempre.
    ///
    /// Este script é só ORQUESTRAÇÃO e ENTRADA — nenhuma regra de jogo mora aqui.
    /// As regras estão no LaneSim, que é lógica pura testada headless; trocar a IA
    /// por um adversário de rede depois é substituir quem chama TrySend.
    /// </summary>
    public class TowerWarsController : MonoBehaviour
    {
        const float LaneGap = 6f;      // espaço entre as duas lanes, em unidades de mundo
        const int Seed = 20260828;

        public LaneSim Player { get; private set; }
        public LaneSim Foe { get; private set; }

        LaneView _playerView;
        LaneView _foeView;
        TowerWarsAi _foeAi;
        Random _rng;

        IGameInput _input;
        CameraRigDriver _cameraRig;
        Transform _ghost;
        Renderer _ghostRenderer;
        readonly MaterialPropertyBlock _mpb = new MaterialPropertyBlock();

        /// <summary>Definida pelo seletor de modo ANTES do Start.</summary>
        public TowerWarsAi.Personality Difficulty = TowerWarsAi.Personality.Normal;

        int _selectedSend;
        float _accumulator;            // passo fixo: a simulação não depende do frame rate
        GUIStyle _label, _big, _box, _button;

        static readonly Plane GroundPlane = new Plane(Vector3.up, Vector3.zero);

        void Start()
        {
            var cam = Camera.main;
            if (cam == null)
            {
                var go = new GameObject("Main Camera") { tag = "MainCamera" };
                cam = go.AddComponent<Camera>();
                go.AddComponent<AudioListener>();
            }

            NewMatch();

            _input = new DesktopInput();
            float span = Player.Map.WorldSize.z + LaneGap;
            _cameraRig = new CameraRigDriver(cam, Vector3.zero, new Vector3(Player.Map.WorldSize.x, 0f, span * 2f));
            SceneAmbience.Apply(cam);
            BuildGhost();
        }

        void NewMatch()
        {
            foreach (Transform child in transform) Destroy(child.gameObject);
            if (_playerView != null) Destroy(_playerView.Root.gameObject);
            if (_foeView != null) Destroy(_foeView.Root.gameObject);

            _rng = new Random(Seed);
            Player = new LaneSim(GameConfig.GridWidth, GameConfig.GridHeight);
            Foe = new LaneSim(GameConfig.GridWidth, GameConfig.GridHeight);

            float off = (Player.Map.WorldSize.z + LaneGap) * 0.5f;
            _playerView = new LaneView(Player, new Vector3(0f, 0f, -off), "LaneJogador", Color.white);
            _foeView = new LaneView(Foe, new Vector3(0f, 0f, off), "LaneAdversario", new Color(0.82f, 0.82f, 0.9f));

            _foeAi = new TowerWarsAi(Foe, Player, Difficulty, _rng);
            _accumulator = 0f;
            _selectedSend = 0;
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

            bool over = Player.Dead || Foe.Dead;
            if (!over)
            {
                HandleBuildInput();
                HandleSendInput();

                // passo fixo, igual ao do FlowSim: o que você joga é a mesma
                // simulação que foi balanceada com 300 partidas headless
                _accumulator += dt;
                int guard = 0;
                while (_accumulator >= TowerWarsConfig.FixedStep && guard++ < 8)
                {
                    _accumulator -= TowerWarsConfig.FixedStep;
                    _foeAi.Tick(TowerWarsConfig.FixedStep);
                    Player.Tick(TowerWarsConfig.FixedStep);
                    Foe.Tick(TowerWarsConfig.FixedStep);
                }
            }
            else
            {
                _ghost.gameObject.SetActive(false);
            }

            _playerView.Sync();
            _foeView.Sync();
        }

        void HandleBuildInput()
        {
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
                return;
            }

            bool valid = Player.CanBuild(cell);
            _ghost.gameObject.SetActive(true);
            _ghost.position = _playerView.CellToWorld(cell) + Vector3.up * 0.05f;
            var c = valid ? Palette.GhostValid : Palette.GhostInvalid;
            _mpb.SetColor(MaterialFactory.ColorProperty, c * (0.75f + 0.25f * Mathf.Sin(Time.unscaledTime * 5f)));
            _ghostRenderer.SetPropertyBlock(_mpb);

            if (valid && _input.PlacePressed && Player.TryBuildTower(cell))
                Vfx.Instance?.Build(_ghost.position);
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
            if (!Player.TrySend(_selectedSend, Foe, _rng)) return;
            Vfx.Instance?.Muzzle(_foeView.CellToWorld(Foe.SpawnCells[0]) + Vector3.up * 0.5f);
            FloatingText.Instance?.Show(
                _playerView.CellToWorld(Player.GoalCell) + Vector3.up * 1.5f,
                $"+{SendCatalog.Get(_selectedSend).IncomeBonus} renda", Palette.TextGold);
        }

        void OnGUI()
        {
            EnsureStyles();

            GUILayout.BeginArea(new Rect(12, 10, 420, 120));
            GUILayout.Label($"VOCÊ   vidas {Player.Lives}   ouro {Player.Gold}   renda {Player.Income}", _label);
            GUILayout.Label($"IA ({Difficulty.Name})   vidas {Foe.Lives}   renda {Foe.Income}", _label);
            GUILayout.Label($"tempo {Player.MatchTime:0}s   escala dos envios x{Player.SendScale:0.0}", _label);
            GUILayout.EndArea();

            // painel de envios
            float w = 150f, h = 62f;
            float x0 = 12f, y0 = Screen.height - h - 12f;
            for (int i = 0; i < SendCatalog.Count; i++)
            {
                var u = SendCatalog.Get(i);
                bool afford = Player.CanAfford(i);
                var prev = GUI.color;
                GUI.color = afford ? Color.white : new Color(1f, 1f, 1f, 0.45f);
                var r = new Rect(x0 + i * (w + 6f), y0, w, h);
                if (GUI.Button(r, $"[{i + 1}] {u.Name}\n{u.Cost} ouro  +{u.IncomeBonus} renda", _button) && afford)
                {
                    _selectedSend = i;
                    TrySelectedSend();
                }
                GUI.color = prev;
            }

            GUI.Box(new Rect(12, y0 - 58f, 640, 50f),
                $"Clique esquerdo na SUA lane (perto): construir torre ({TowerWarsConfig.TowerCost} ouro)\n" +
                "1-6 ou os botões: enviar inimigo para a lane da IA (a de cima)  |  R: reiniciar", _box);

            if (Player.Dead || Foe.Dead)
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
