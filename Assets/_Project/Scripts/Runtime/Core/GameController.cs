using System.Collections.Generic;
using UnityEngine;

namespace TDFende
{
    public enum WavePhase
    {
        Building,  // intervalo entre ondas — hora de construir
        Spawning,  // onda entrando no mapa
        Fighting,  // todos spawnaram; esperando limpar
        GameOver
    }

    /// <summary>
    /// Cérebro do jogo. Constrói o mundo inteiro em código (sem prefabs, sem cena montada à mão)
    /// e dirige explicitamente os sistemas por frame — ordem de update determinística.
    /// </summary>
    public class GameController : MonoBehaviour
    {
        public GridMap Map { get; private set; }
        public FlowField Flow { get; private set; }
        public TerritoryField Territory { get; private set; }
        public PlayerState State { get; private set; }

        public int Wave { get; private set; }
        public WavePhase Phase { get; private set; }
        public float PhaseTimer { get; private set; }
        public int EnemiesAlive => Enemy.Alive.Count;

        Vector2Int _goalCell;
        Vector3 _goalWorld;
        readonly List<Vector2Int> _spawnCells = new List<Vector2Int>();
        readonly List<GameObject> _towers = new List<GameObject>();
        readonly List<Vector2Int> _towerCells = new List<Vector2Int>();

        IGameInput _input;
        CameraRigDriver _cameraRig;
        TowerPlacer _placer;
        TerritoryRenderer _territoryRenderer;
        FloatingText _floatingText;
        Transform _baseTransform;
        Vector3 _baseScale;

        SimplePool<Enemy> _enemyPool;
        SimplePool<Projectile> _projectilePool;
        System.Action<Projectile> _releaseProjectile;
        System.Action<Enemy, DespawnReason> _onEnemyDespawn;

        int _toSpawn;
        float _spawnTimer;

        void Start()
        {
            Map = new GridMap(GameConfig.GridWidth, GameConfig.GridHeight, GameConfig.CellSize);
            Flow = new FlowField(Map);
            Territory = new TerritoryField(Map);
            State = new PlayerState(GameConfig.StartLives, GameConfig.StartGold);

            _goalCell = new Vector2Int(Map.Width - 3, Map.Height / 2);
            _spawnCells.Add(new Vector2Int(2, Map.Height / 2));
            _goalWorld = Map.CellToWorld(_goalCell);
            Flow.Rebuild(_goalCell);

            _enemyPool = new SimplePool<Enemy>(CreateEnemy);
            _projectilePool = new SimplePool<Projectile>(CreateProjectile);
            _releaseProjectile = _projectilePool.Release; // delegates cacheados: nada de alocar por tiro
            _onEnemyDespawn = OnEnemyDespawn;

            var cam = FindOrCreateCamera();
            SceneAmbience.Apply(cam);
            BuildWorld();
            new Vfx();

            _input = new DesktopInput();
            _cameraRig = new CameraRigDriver(cam, Vector3.zero, Map.WorldSize);
            _placer = new TowerPlacer(this);
            _territoryRenderer = new TerritoryRenderer();
            gameObject.AddComponent<DebugHud>().Init(this);
            _floatingText = gameObject.AddComponent<FloatingText>();
            _floatingText.Init(cam);

            Phase = WavePhase.Building;
            PhaseTimer = GameConfig.FirstWaveDelay;
        }

        void Update()
        {
            _input.Tick();
            float dt = Time.deltaTime;

            // Câmera e shake em tempo NÃO-escalado: o fim de jogo congela a simulação
            // com timeScale = 0, e com dt escalado o tremor ficaria travado na tela e a
            // câmera pararia de responder justamente quando dá vontade de olhar o mapa.
            float uiDt = Time.unscaledDeltaTime;
            Juice.Tick(uiDt);
            _cameraRig.Tick(_input, uiDt);
            PulseBase();

            if (_input.RestartPressed)
            {
                ResetGame();
                return;
            }
            if (Phase == WavePhase.GameOver)
            {
                _placer.Hide();
                return;
            }

            _placer.Tick(_input, _cameraRig.Camera);

            switch (Phase)
            {
                case WavePhase.Building:
                    PhaseTimer -= dt;
                    if (PhaseTimer <= 0f || _input.CallWavePressed)
                        StartWave();
                    break;

                case WavePhase.Spawning:
                    _spawnTimer -= dt;
                    if (_spawnTimer <= 0f)
                    {
                        SpawnEnemy();
                        _spawnTimer = GameConfig.SpawnInterval;
                        if (--_toSpawn <= 0)
                            Phase = WavePhase.Fighting;
                    }
                    break;

                case WavePhase.Fighting:
                    if (Enemy.Alive.Count == 0)
                    {
                        State.AddGold(GameConfig.WaveClearBonus);
                        _floatingText.Show(_goalWorld + Vector3.up * 1.5f,
                            $"Onda {Wave} limpa!  +{GameConfig.WaveClearBonus}", Palette.TextGold);
                        Phase = WavePhase.Building;
                        PhaseTimer = GameConfig.TimeBetweenWaves;
                    }
                    break;
            }
        }

        void StartWave()
        {
            Wave++;
            _toSpawn = GameConfig.EnemiesInWave(Wave);
            _spawnTimer = 0f;
            Phase = WavePhase.Spawning;
        }

        void SpawnEnemy()
        {
            var e = _enemyPool.Get();
            var spawn = _spawnCells[Random.Range(0, _spawnCells.Count)];
            var pos = Map.CellToWorld(spawn)
                      + new Vector3(Random.Range(-0.3f, 0.3f), 0f, Random.Range(-0.3f, 0.3f));
            pos.y = 0.5f;
            e.transform.position = pos;

            float hp = GameConfig.EnemyBaseHp * Mathf.Pow(GameConfig.EnemyHpGrowth, Wave - 1);
            float speed = Mathf.Min(GameConfig.EnemyBaseSpeed + GameConfig.EnemySpeedPerWave * Wave, GameConfig.EnemyMaxSpeed)
                          * Random.Range(0.92f, 1.08f); // variação leve pra não andarem em fila indiana
            e.Init(Flow, Territory, _goalWorld, hp, speed, _onEnemyDespawn);
        }

        /// <summary>
        /// Congela a partida. Enemy e Tower são MonoBehaviours com Update próprio: sem
        /// isto, a onda continuava andando por baixo do "FIM DE JOGO" — a tela tremia a
        /// cada vazamento, chovia "-1 vida" com o contador já em zero, e o ouro subia.
        /// </summary>
        void EndGame()
        {
            Phase = WavePhase.GameOver;
            Time.timeScale = 0f;
        }

        // Time.timeScale é estado GLOBAL: se este objeto morrer congelado, leva o
        // próximo modo junto. Sempre devolver ao sair.
        void OnDisable() => Time.timeScale = 1f;

        void OnEnemyDespawn(Enemy e, DespawnReason reason)
        {
            var pos = e.transform.position;
            _enemyPool.Release(e);

            if (reason == DespawnReason.Leaked)
            {
                State.LoseLife();
                Vfx.Instance?.Leak(pos);
                Juice.Shake(0.55f); // só aqui: o evento que dói merece tremer a tela
                _floatingText.Show(pos + Vector3.up * 0.8f, "-1 vida", Palette.TextDanger);
                if (State.GameOver) EndGame();
                return;
            }

            bool byAttrition = reason == DespawnReason.KilledByAttrition;
            State.AddGold(GameConfig.KillReward);
            Vfx.Instance?.KillBurst(pos, byAttrition);
            // cor do número diz o que matou: fronteira (ciano) ou torre (dourado)
            _floatingText.Show(pos + Vector3.up * 0.6f, $"+{GameConfig.KillReward}",
                byAttrition ? Palette.TerritoryEdge : Palette.TextGold);
        }

        public bool CanPlaceTower(Vector2Int cell)
        {
            if (Phase == WavePhase.GameOver) return false;
            if (!Map.InBounds(cell.x, cell.y) || Map.IsBlocked(cell)) return false;
            if (cell == _goalCell) return false;
            for (int i = 0; i < _spawnCells.Count; i++)
                if (cell == _spawnCells[i]) return false;
            if (State.Gold < GameConfig.TowerCost) return false;

            // não construir em cima de inimigo
            var center = Map.CellToWorld(cell);
            for (int i = 0; i < Enemy.Alive.Count; i++)
            {
                var d = Enemy.Alive[i].transform.position - center;
                d.y = 0f;
                if (d.sqrMagnitude < 0.8f * 0.8f) return false;
            }

            // nunca deixar o jogador murar o caminho por completo
            if (Flow.PlacementBlocksPath(cell, _spawnCells)) return false;
            return true;
        }

        public void PlaceTower(Vector2Int cell)
        {
            if (!CanPlaceTower(cell) || !State.TrySpend(GameConfig.TowerCost)) return;
            Map.SetBlocked(cell, true);
            Flow.Rebuild(_goalCell); // todos os inimigos redirecionam na hora
            _towers.Add(CreateTowerVisual(cell));

            // a fronteira empurra: cada torre nova expande o território
            _towerCells.Add(cell);
            Territory.Rebuild(_towerCells, GameConfig.BorderRadius);
            _territoryRenderer.Rebuild(Territory, Map);

            Vfx.Instance?.Build(Map.CellToWorld(cell));
            Juice.Shake(0.12f);
        }

        public void ResetGame()
        {
            for (int i = Enemy.Alive.Count - 1; i >= 0; i--)
                _enemyPool.Release(Enemy.Alive[i]);
            foreach (var t in _towers)
                Destroy(t);
            _towers.Clear();

            Map.ClearAllBlocked();
            Flow.Rebuild(_goalCell);
            _towerCells.Clear();
            Territory.Rebuild(_towerCells, GameConfig.BorderRadius);
            _territoryRenderer.Rebuild(Territory, Map);
            State = new PlayerState(GameConfig.StartLives, GameConfig.StartGold);
            Juice.Reset();
            _floatingText.Clear();
            Wave = 0;
            Phase = WavePhase.Building;
            PhaseTimer = GameConfig.FirstWaveDelay;
            Time.timeScale = 1f; // sai do congelamento do fim de jogo
        }

        // base "respira": mostra que está viva sem custar nada
        void PulseBase()
        {
            if (_baseTransform == null) return;
            float p = 1f + 0.045f * Mathf.Sin(Time.time * 2.1f);
            _baseTransform.localScale = new Vector3(_baseScale.x, _baseScale.y * p, _baseScale.z);
        }

        // ---------- construção do mundo (tudo primitivas, tudo em código) ----------

        void BuildWorld()
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Chao";
            Destroy(ground.GetComponent<Collider>());
            var size = Map.WorldSize;
            ground.transform.localScale = new Vector3(size.x, 0.1f, size.z);
            ground.transform.position = new Vector3(0f, -0.05f, 0f); // topo do cubo em Y=0
            ground.GetComponent<Renderer>().sharedMaterial =
                MaterialFactory.GetGround(Palette.GroundDark, Palette.GroundLight, Map.Width, Map.Height);

            var baseGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            baseGo.name = "Base";
            Destroy(baseGo.GetComponent<Collider>());
            baseGo.transform.position = _goalWorld + Vector3.up * 0.6f;
            baseGo.transform.localScale = new Vector3(1.1f, 1.2f, 1.1f);
            baseGo.GetComponent<Renderer>().sharedMaterial = MaterialFactory.Get(Palette.BaseGold);
            _baseTransform = baseGo.transform;
            _baseScale = _baseTransform.localScale;

            foreach (var s in _spawnCells)
            {
                var m = GameObject.CreatePrimitive(PrimitiveType.Cube);
                m.name = "Spawn";
                Destroy(m.GetComponent<Collider>());
                m.transform.position = Map.CellToWorld(s) + Vector3.up * 0.05f;
                m.transform.localScale = new Vector3(0.9f, 0.1f, 0.9f);
                m.GetComponent<Renderer>().sharedMaterial = MaterialFactory.Get(Palette.SpawnMagenta);
            }
        }

        Enemy CreateEnemy()
        {
            // raiz vazia: a animação de escala mexe na raiz sem deformar os filhos
            var root = new GameObject("Inimigo");

            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            Destroy(body.GetComponent<Collider>());
            body.transform.SetParent(root.transform, false);
            body.transform.localScale = new Vector3(0.55f, 0.5f, 0.55f); // altura 1, centro em y=0
            body.GetComponent<Renderer>().sharedMaterial = MaterialFactory.Get(Palette.EnemyFull);

            // "olho" na frente: dá silhueta e mostra pra onde está virado
            var eye = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(eye.GetComponent<Collider>());
            eye.transform.SetParent(root.transform, false);
            eye.transform.localPosition = new Vector3(0f, 0.16f, 0.24f);
            eye.transform.localScale = new Vector3(0.24f, 0.13f, 0.14f);
            eye.GetComponent<Renderer>().sharedMaterial = MaterialFactory.Get(Palette.Background);

            return root.AddComponent<Enemy>();
        }

        Projectile CreateProjectile()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "Projetil";
            Destroy(go.GetComponent<Collider>());
            go.transform.localScale = Vector3.one * 0.22f;
            go.GetComponent<Renderer>().sharedMaterial = MaterialFactory.Get(Palette.Projectile);
            return go.AddComponent<Projectile>();
        }

        GameObject CreateTowerVisual(Vector2Int cell)
        {
            var root = new GameObject("Torre");
            root.transform.position = Map.CellToWorld(cell);

            // plataforma: assenta a torre no chão em vez de flutuar
            var plate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(plate.GetComponent<Collider>());
            plate.transform.SetParent(root.transform, false);
            plate.transform.localPosition = new Vector3(0f, 0.06f, 0f);
            plate.transform.localScale = new Vector3(0.95f, 0.12f, 0.95f);
            plate.GetComponent<Renderer>().sharedMaterial = MaterialFactory.Get(Palette.GroundLight);

            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(body.GetComponent<Collider>());
            body.transform.SetParent(root.transform, false);
            body.transform.localPosition = new Vector3(0f, 0.45f, 0f);
            body.transform.localScale = new Vector3(0.72f, 0.72f, 0.72f);
            body.GetComponent<Renderer>().sharedMaterial = MaterialFactory.Get(Palette.TowerBody);

            var head = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(head.GetComponent<Collider>());
            head.transform.SetParent(root.transform, false);
            head.transform.localPosition = new Vector3(0f, 0.95f, 0f);
            head.transform.localScale = new Vector3(0.35f, 0.25f, 0.6f);
            head.GetComponent<Renderer>().sharedMaterial = MaterialFactory.Get(Palette.TowerHead);

            root.AddComponent<Tower>().Init(head.transform, _projectilePool, _releaseProjectile);
            return root;
        }

        Camera FindOrCreateCamera()
        {
            var cam = Camera.main;
            if (cam == null)
            {
                var go = new GameObject("Main Camera") { tag = "MainCamera" };
                cam = go.AddComponent<Camera>();
                go.AddComponent<AudioListener>();
            }
            return cam;
        }
    }
}
